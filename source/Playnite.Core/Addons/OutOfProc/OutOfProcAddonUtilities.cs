using System;
using System.IO;
using System.Text.Json;
using Playnite.SDK.OutOfProc;

namespace Playnite.Addons.OutOfProc;

public static class OutOfProcAddonUtilities
{
    public static bool TryPing(string fileName, string arguments, string workingDirectory, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            using var client = new OutOfProcJsonRpcClient(fileName, arguments, workingDirectory);
            using var doc = client.Invoke(
                "ping",
                w =>
                {
                    w.WriteStartObject();
                    w.WriteEndObject();
                },
                timeoutMs: 5000);

            if (!doc.RootElement.TryGetProperty(OutOfProcProtocol.ResponseResultProperty, out var resultEl) ||
                resultEl.ValueKind != JsonValueKind.Object)
            {
                errorMessage = "Missing result in response.";
                return false;
            }

            if (!resultEl.TryGetProperty("pong", out var pongEl) || pongEl.ValueKind != JsonValueKind.True)
            {
                errorMessage = "Unexpected ping response.";
                return false;
            }

            return true;
        }
        catch (FileNotFoundException e)
        {
            errorMessage = e.Message;
            return false;
        }
        catch (DirectoryNotFoundException e)
        {
            errorMessage = e.Message;
            return false;
        }
        catch (Exception e)
        {
            errorMessage = e.Message;
            return false;
        }
    }

    public static bool TryGetGenericCommandCount(string fileName, string arguments, string workingDirectory, out int commandCount, out string errorMessage)
    {
        commandCount = 0;
        errorMessage = string.Empty;

        try
        {
            using var client = new OutOfProcJsonRpcClient(fileName, arguments, workingDirectory);
            using var doc = client.Invoke(
                OutOfProcProtocol.Methods.GenericGetCommands,
                w =>
                {
                    w.WriteStartObject();
                    w.WriteEndObject();
                },
                timeoutMs: 5000);

            if (!doc.RootElement.TryGetProperty(OutOfProcProtocol.ResponseResultProperty, out var resultEl) ||
                resultEl.ValueKind != JsonValueKind.Object)
            {
                errorMessage = "Missing result in response.";
                return false;
            }

            if (!resultEl.TryGetProperty("commands", out var commandsEl) || commandsEl.ValueKind != JsonValueKind.Array)
            {
                errorMessage = "Missing commands in response.";
                return false;
            }

            commandCount = commandsEl.GetArrayLength();
            return true;
        }
        catch (Exception e)
        {
            errorMessage = e.Message;
            return false;
        }
    }
}
