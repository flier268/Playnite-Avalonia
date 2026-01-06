using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Playnite
{
    public delegate void CommandExecutedEventHandler(object sender, CommandExecutedEventArgs args);

    public class CommandExecutedEventArgs : EventArgs
    {
        public CmdlineCommand Command { get; set; }
        public string Args { get; set; }

        public CommandExecutedEventArgs()
        {
        }

        public CommandExecutedEventArgs(CmdlineCommand command, string args)
        {
            Command = command;
            Args = args;
        }
    }

    // Command message for serialization
    internal class PipeMessage
    {
        public CmdlineCommand Command { get; set; }
        public string Args { get; set; }
    }

    public interface IPipeService
    {
        void InvokeCommand(CmdlineCommand command, string args);
    }

    public class PipeService : IPipeService
    {
        private readonly SynchronizationContext syncContext;
        public event CommandExecutedEventHandler CommandExecuted;

        public PipeService()
        {
            syncContext = SynchronizationContext.Current;
        }

        public void InvokeCommand(CmdlineCommand command, string args)
        {
            // We don't want to block this call because it causes issues if some sync operation that shuts down server is also called.
            // For example, mode switch or instance shutdown calls are stopping server,
            // which results in server shutdown timeout, since server would be still waiting for InvokeCommand to finish.
            Task.Run(async () =>
            {
                await Task.Delay(100);
                syncContext?.Post(_ => CommandExecuted?.Invoke(this, new CommandExecutedEventArgs(command, args)), null);
            });
        }
    }

    /// <summary>
    /// Cross-platform IPC server using named pipes.
    /// </summary>
    public class PipeServer : IDisposable
    {
        private CancellationTokenSource cancellationTokenSource;
        private Task listenTask;
        private readonly string pipeName;

        public PipeServer(string endpoint)
        {
            pipeName = GetPipeName(endpoint);
        }

        private static string GetPipeName(string endpoint)
        {
            // Extract the pipe name from WCF-style endpoint
            var pipeName = "playnite";
            if (!string.IsNullOrEmpty(endpoint))
            {
                try
                {
                    var uri = new Uri(endpoint.Replace("net.pipe://", "http://"));
                    pipeName = uri.AbsolutePath.Trim('/').ToLowerInvariant();
                    if (string.IsNullOrEmpty(pipeName))
                    {
                        pipeName = "playnite";
                    }
                }
                catch
                {
                    pipeName = "playnite";
                }
            }

            return pipeName.Replace('/', '_');
        }

        public void StartServer(IPipeService pipeService)
        {
            if (listenTask != null)
            {
                throw new InvalidOperationException("Server is already running");
            }

            cancellationTokenSource = new CancellationTokenSource();
            listenTask = Task.Run(() => AcceptClientsAsync(pipeService, cancellationTokenSource.Token));
        }

        private async Task AcceptClientsAsync(IPipeService service, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                NamedPipeServerStream serverStream = null;
                try
                {
                    serverStream = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.In,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await serverStream.WaitForConnectionAsync(cancellationToken);
                    _ = Task.Run(() => HandleClientAsync(serverStream, service), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    serverStream?.Dispose();
                    break;
                }
                catch (Exception)
                {
                    serverStream?.Dispose();
                    // Log error if needed, but continue accepting connections
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
            }
        }

        private async Task HandleClientAsync(NamedPipeServerStream serverStream, IPipeService service)
        {
            try
            {
                using (serverStream)
                using (var reader = new StreamReader(serverStream, Encoding.UTF8))
                {
                    var json = await reader.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(json))
                    {
                        var message = JsonSerializer.Deserialize<PipeMessage>(json);
                        if (message != null)
                        {
                            service.InvokeCommand(message.Command, message.Args);
                        }
                    }
                }
            }
            catch
            {
                // Client disconnected or error occurred
            }
        }

        public void StopServer()
        {
            cancellationTokenSource?.Cancel();

            try
            {
                listenTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Ignore timeout
            }

            listenTask = null;

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }

        public void Dispose()
        {
            StopServer();
        }
    }

    /// <summary>
    /// Cross-platform IPC client using named pipes.
    /// </summary>
    public class PipeClient : IDisposable
    {
        private readonly string pipeName;

        public PipeClient(string endpoint)
        {
            pipeName = GetPipeName(endpoint);
        }

        private static string GetPipeName(string endpoint)
        {
            // Extract the pipe name from WCF-style endpoint
            var pipeName = "playnite";
            if (!string.IsNullOrEmpty(endpoint))
            {
                try
                {
                    var uri = new Uri(endpoint.Replace("net.pipe://", "http://"));
                    pipeName = uri.AbsolutePath.Trim('/').ToLowerInvariant();
                    if (string.IsNullOrEmpty(pipeName))
                    {
                        pipeName = "playnite";
                    }
                }
                catch
                {
                    pipeName = "playnite";
                }
            }

            return pipeName.Replace('/', '_');
        }

        public void InvokeCommand(CmdlineCommand command, string args)
        {
            var message = new PipeMessage
            {
                Command = command,
                Args = args
            };

            var json = JsonSerializer.Serialize(message);

            using (var clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
            {
                clientStream.Connect(2000);
                using (var writer = new StreamWriter(clientStream, Encoding.UTF8))
                {
                    writer.Write(json);
                    writer.Flush();
                }
            }
        }

        public void Dispose()
        {
            // Nothing to dispose for client
        }
    }
}
