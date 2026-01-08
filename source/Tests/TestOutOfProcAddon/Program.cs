using System;
using System.Text;
using System.Text.Json;
using Playnite.SDK.OutOfProc;

namespace TestOutOfProcAddon;

internal static class Program
{
    public static int Main(string[] args)
    {
        Console.InputEncoding = new UTF8Encoding(false);
        Console.OutputEncoding = new UTF8Encoding(false);

        if (args != null && args.Length > 0 && string.Equals(args[0], "--version", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(OutOfProcProtocol.ProtocolVersion);
            return 0;
        }

        string line;
        while ((line = Console.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                var id = root.TryGetProperty(OutOfProcProtocol.RequestIdProperty, out var idEl) && idEl.ValueKind == JsonValueKind.String
                    ? idEl.GetString()
                    : string.Empty;

                var method = root.TryGetProperty(OutOfProcProtocol.RequestMethodProperty, out var methodEl) && methodEl.ValueKind == JsonValueKind.String
                    ? methodEl.GetString()
                    : string.Empty;

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(method))
                {
                    WriteError(id, -32600, "Invalid request.");
                    continue;
                }

                switch (method)
                {
                    case "ping":
                        WriteResult(id, w =>
                        {
                            w.WriteStartObject();
                            w.WriteBoolean("pong", true);
                            w.WriteEndObject();
                        });
                        break;

                    case "generic.getCommands":
                        WriteResult(id, w =>
                        {
                            w.WriteStartObject();
                            w.WritePropertyName("commands");
                            w.WriteStartArray();

                            w.WriteStartObject();
                            w.WriteString("id", "hello");
                            w.WriteString("name", "Say hello");
                            w.WriteString("description", "Writes a hello message to stderr.");
                            w.WriteEndObject();

                            w.WriteStartObject();
                            w.WriteString("id", "fail");
                            w.WriteString("name", "Fail intentionally");
                            w.WriteString("description", "Returns an error response for testing.");
                            w.WriteEndObject();

                            w.WriteEndArray();
                            w.WriteEndObject();
                        });
                        break;

                    case "generic.runCommand":
                        if (!root.TryGetProperty(OutOfProcProtocol.RequestParamsProperty, out var p) || p.ValueKind != JsonValueKind.Object)
                        {
                            WriteError(id, -32602, "Missing params.");
                            break;
                        }

                        var cmdId = p.TryGetProperty("id", out var cmdIdEl) && cmdIdEl.ValueKind == JsonValueKind.String ? cmdIdEl.GetString() : string.Empty;
                        if (string.IsNullOrWhiteSpace(cmdId))
                        {
                            WriteError(id, -32602, "Missing command id.");
                            break;
                        }

                        if (string.Equals(cmdId, "hello", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.Error.WriteLine($"hello from TestOutOfProcAddon at {DateTime.UtcNow:O}");
                            WriteResult(id, w =>
                            {
                                w.WriteStartObject();
                                w.WriteBoolean("ok", true);
                                w.WriteEndObject();
                            });
                        }
                        else if (string.Equals(cmdId, "fail", StringComparison.OrdinalIgnoreCase))
                        {
                            WriteError(id, 1, "Intentional failure.");
                        }
                        else
                        {
                            WriteError(id, -32602, "Unknown command id.");
                        }

                        break;

                    default:
                        WriteError(id, -32601, "Method not found.");
                        break;
                }
            }
            catch (Exception e)
            {
                WriteError(string.Empty, -32700, e.Message);
            }
        }

        return 0;
    }

    private static void WriteResult(string id, Action<Utf8JsonWriter> writeResult)
    {
        using var ms = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            writer.WriteString(OutOfProcProtocol.RequestIdProperty, id);
            writer.WritePropertyName(OutOfProcProtocol.ResponseResultProperty);
            writeResult(writer);
            writer.WriteEndObject();
        }

        Console.WriteLine(Encoding.UTF8.GetString(ms.ToArray()));
    }

    private static void WriteError(string id, int code, string message)
    {
        using var ms = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            writer.WriteString(OutOfProcProtocol.RequestIdProperty, id ?? string.Empty);
            writer.WritePropertyName(OutOfProcProtocol.ResponseErrorProperty);
            writer.WriteStartObject();
            writer.WriteNumber(OutOfProcProtocol.ErrorCodeProperty, code);
            writer.WriteString(OutOfProcProtocol.ErrorMessageProperty, message ?? string.Empty);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        Console.WriteLine(Encoding.UTF8.GetString(ms.ToArray()));
    }
}
