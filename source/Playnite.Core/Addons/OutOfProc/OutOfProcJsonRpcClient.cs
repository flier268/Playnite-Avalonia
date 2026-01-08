using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using Playnite.SDK.OutOfProc;

namespace Playnite.Addons.OutOfProc;

internal sealed class OutOfProcJsonRpcClient : IDisposable
{
    private readonly Process process;
    private readonly StreamWriter stdin;
    private readonly StreamReader stdout;
    private readonly StreamReader stderr;

    public OutOfProcJsonRpcClient(string fileName, string arguments, string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Executable path is empty.", nameof(fileName));
        }

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments ?? string.Empty,
            WorkingDirectory = workingDirectory ?? string.Empty,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start out-of-proc add-on.");
        stdin = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false)) { AutoFlush = true };
        stdout = new StreamReader(process.StandardOutput.BaseStream, new UTF8Encoding(false));
        stderr = new StreamReader(process.StandardError.BaseStream, new UTF8Encoding(false));
    }

    public bool HasExited
    {
        get
        {
            try
            {
                return process.HasExited;
            }
            catch
            {
                return true;
            }
        }
    }

    public JsonDocument Invoke(string method, Action<Utf8JsonWriter> writeParams, int timeoutMs = 5000, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException("Method is empty.", nameof(method));
        }

        var requestId = OutOfProcProtocol.NewRequestId();
        var line = BuildRequestLine(requestId, method, writeParams);

        try
        {
            stdin.WriteLine(line);
        }
        catch (Exception e)
        {
            throw new IOException("Failed to write request to out-of-proc add-on.", e);
        }

        var responseLine = ReadLineWithTimeout(timeoutMs, cancellationToken);
        if (string.IsNullOrWhiteSpace(responseLine))
        {
            throw new IOException("Out-of-proc add-on returned an empty response.");
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(responseLine);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Out-of-proc add-on returned invalid JSON.", e);
        }

        if (!doc.RootElement.TryGetProperty(OutOfProcProtocol.RequestIdProperty, out var idEl) ||
            idEl.ValueKind != JsonValueKind.String ||
            !string.Equals(idEl.GetString(), requestId, StringComparison.Ordinal))
        {
            doc.Dispose();
            throw new InvalidOperationException("Out-of-proc add-on returned a response with an unexpected id.");
        }

        if (doc.RootElement.TryGetProperty(OutOfProcProtocol.ResponseErrorProperty, out var errEl) && errEl.ValueKind == JsonValueKind.Object)
        {
            var msg = "Out-of-proc add-on returned an error.";
            if (errEl.TryGetProperty(OutOfProcProtocol.ErrorMessageProperty, out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
            {
                msg = msgEl.GetString();
            }

            doc.Dispose();
            throw new InvalidOperationException(msg);
        }

        return doc;
    }

    public void StartStderrPump(Action<string> onLine, CancellationToken cancellationToken)
    {
        if (onLine is null)
        {
            throw new ArgumentNullException(nameof(onLine));
        }

        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await stderr.ReadLineAsync().ConfigureAwait(false);
                    if (line is null)
                    {
                        break;
                    }

                    if (line.Length == 0)
                    {
                        continue;
                    }

                    try
                    {
                        onLine(line);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }, cancellationToken);
    }

    private string ReadLineWithTimeout(int timeoutMs, CancellationToken cancellationToken)
    {
        if (timeoutMs <= 0)
        {
            timeoutMs = 5000;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);

        try
        {
            // StreamReader doesn't support CancellationToken; use a background read.
            var task = stdout.ReadLineAsync();
            while (!task.IsCompleted)
            {
                if (cts.IsCancellationRequested)
                {
                    throw new TimeoutException("Timed out waiting for out-of-proc add-on response.");
                }

                Thread.Sleep(5);
            }

            return task.Result;
        }
        catch (AggregateException ae) when (ae.InnerException != null)
        {
            throw ae.InnerException;
        }
    }

    private static string BuildRequestLine(string id, string method, Action<Utf8JsonWriter> writeParams)
    {
        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            writer.WriteString(OutOfProcProtocol.RequestIdProperty, id);
            writer.WriteString(OutOfProcProtocol.RequestMethodProperty, method);
            writer.WritePropertyName(OutOfProcProtocol.RequestParamsProperty);
            writeParams?.Invoke(writer);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public void Dispose()
    {
        try
        {
            stdin?.Dispose();
        }
        catch
        {
        }

        try
        {
            stdout?.Dispose();
        }
        catch
        {
        }

        try
        {
            stderr?.Dispose();
        }
        catch
        {
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
        finally
        {
            try
            {
                process.Dispose();
            }
            catch
            {
            }
        }
    }
}
