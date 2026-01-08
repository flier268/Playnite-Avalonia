using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Playnite.Configuration;

namespace Playnite.Addons.OutOfProc;

public sealed class OutOfProcAddonsHost : IDisposable
{
    private sealed class AddonInstance
    {
        private readonly object stateLock = new();
        private readonly OutOfProcAddonDescriptor descriptor;
        private OutOfProcJsonRpcClient? client;
        private CancellationTokenSource? stderrCts;
        private readonly Queue<string> stderrTail;
        private readonly int stderrTailCapacity;
        private int restartCount;
        private DateTime restartWindowStartUtc = DateTime.MinValue;
        private DateTime lastStartUtc = DateTime.MinValue;
        private string lastError = string.Empty;
        private readonly OutOfProcAddonsHostOptions options;

        public AddonInstance(OutOfProcAddonDescriptor descriptor, OutOfProcAddonsHostOptions options)
        {
            this.descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            this.options = options ?? new OutOfProcAddonsHostOptions();
            stderrTailCapacity = this.options.StderrTailLines <= 0 ? 50 : this.options.StderrTailLines;
            stderrTail = new Queue<string>(stderrTailCapacity);
        }

        public string AddonId => descriptor.Manifest.Id ?? string.Empty;

        public bool EnsureStarted(out string errorMessage)
        {
            errorMessage = string.Empty;

            lock (stateLock)
            {
                if (client != null && !client.HasExited)
                {
                    return true;
                }

                StopClient_NoLock();

                if (!ConsumeRestartBudget_NoLock(out errorMessage))
                {
                    lastError = errorMessage;
                    return false;
                }

                try
                {
                    client = new OutOfProcJsonRpcClient(descriptor.FileName, descriptor.Arguments, descriptor.WorkingDirectory);
                    lastStartUtc = DateTime.UtcNow;
                    lastError = string.Empty;

                    stderrCts = new CancellationTokenSource();
                    client.StartStderrPump(AppendStderrLine, stderrCts.Token);

                    return true;
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                    lastError = errorMessage;
                    StopClient_NoLock();
                    return false;
                }
            }
        }

        public bool TryInvoke(string method, Action<Utf8JsonWriter> writeParams, out JsonDocument? response, out string errorMessage)
        {
            response = null;
            errorMessage = string.Empty;

            if (!EnsureStarted(out errorMessage))
            {
                return false;
            }

            // One restart+retry on failures that commonly indicate a dead process.
            for (var attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    var timeout = options.RequestTimeoutMs <= 0 ? 5000 : options.RequestTimeoutMs;
                    response = client!.Invoke(method, writeParams, timeoutMs: timeout, cancellationToken: CancellationToken.None);
                    return true;
                }
                catch (Exception e) when (attempt == 0)
                {
                    errorMessage = e.Message;
                    lock (stateLock)
                    {
                        lastError = errorMessage;
                        StopClient_NoLock();
                    }
                    if (!EnsureStarted(out var restartError))
                    {
                        errorMessage = string.IsNullOrWhiteSpace(restartError) ? errorMessage : restartError;
                        return false;
                    }
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                    lock (stateLock)
                    {
                        lastError = errorMessage;
                    }
                    return false;
                }
            }

            errorMessage = "Failed to invoke out-of-proc add-on.";
            lock (stateLock)
            {
                lastError = errorMessage;
            }
            return false;
        }

        public void Stop()
        {
            lock (stateLock)
            {
                StopClient_NoLock();
            }
        }

        public OutOfProcAddonStatus GetStatusSnapshot()
        {
            lock (stateLock)
            {
                return new OutOfProcAddonStatus
                {
                    AddonId = AddonId,
                    IsRunning = client != null && !client.HasExited,
                    LastStartUtc = lastStartUtc,
                    LastError = lastError ?? string.Empty,
                    StderrTail = stderrTail.ToArray()
                };
            }
        }

        private void AppendStderrLine(string line)
        {
            lock (stateLock)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    return;
                }

                if (stderrTail.Count >= stderrTailCapacity)
                {
                    stderrTail.Dequeue();
                }

                stderrTail.Enqueue(line);
            }
        }

        private bool ConsumeRestartBudget_NoLock(out string errorMessage)
        {
            errorMessage = string.Empty;

            var now = DateTime.UtcNow;
            if (restartWindowStartUtc == DateTime.MinValue || (now - restartWindowStartUtc).TotalSeconds > 60)
            {
                restartWindowStartUtc = now;
                restartCount = 0;
            }

            restartCount++;
            var limit = options.RestartLimitPerMinute <= 0 ? 3 : options.RestartLimitPerMinute;
            if (restartCount > limit)
            {
                errorMessage = $"Restart limit exceeded ({limit}/min).";
                return false;
            }

            return true;
        }

        private void StopClient_NoLock()
        {
            try
            {
                stderrCts?.Cancel();
            }
            catch
            {
            }

            try
            {
                stderrCts?.Dispose();
            }
            catch
            {
            }

            stderrCts = null;

            try
            {
                client?.Dispose();
            }
            catch
            {
            }

            client = null;
        }
    }

    private readonly AddonsManager addonsManager;
    private readonly Dictionary<string, AddonInstance> instancesByAddonId = new(StringComparer.OrdinalIgnoreCase);
    private readonly OutOfProcAddonsHostOptions options;
    private bool disposed;

    public OutOfProcAddonsHost(AddonsManager addonsManager, OutOfProcAddonsHostOptions options)
    {
        this.addonsManager = addonsManager ?? throw new ArgumentNullException(nameof(addonsManager));
        this.options = options ?? new OutOfProcAddonsHostOptions();
    }

    public OutOfProcAddonsHost(AddonsManager addonsManager) : this(addonsManager, new OutOfProcAddonsHostOptions())
    {
    }

    public IReadOnlyList<AddonManifest> GetEnabledOutOfProcExtensions(AppSettings settings)
    {
        settings ??= new AppSettings();
        var disabled = settings.DisabledAddons?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return addonsManager.GetInstalledExtensions()
            .Where(OutOfProcAddonResolver.IsOutOfProc)
            .Where(a => !disabled.Contains(a.Id))
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyDictionary<string, string> StartAllEnabled(AppSettings settings)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var addon in GetEnabledOutOfProcExtensions(settings))
        {
            if (!TryStart(addon, out var errorMessage))
            {
                errors[addon.Id] = errorMessage;
            }
        }

        return errors;
    }

    public bool TryStart(AddonManifest manifest, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (disposed)
        {
            errorMessage = "Host is disposed.";
            return false;
        }

        if (manifest is null || string.IsNullOrWhiteSpace(manifest.Id))
        {
            errorMessage = "Invalid manifest.";
            return false;
        }

        if (instancesByAddonId.TryGetValue(manifest.Id, out var existing))
        {
            return existing.EnsureStarted(out errorMessage);
        }

        if (!OutOfProcAddonResolver.TryResolve(manifest, out var descriptor, out errorMessage))
        {
            return false;
        }

        var instance = new AddonInstance(descriptor, options);
        instancesByAddonId[manifest.Id] = instance;

        if (!instance.EnsureStarted(out errorMessage))
        {
            instancesByAddonId.Remove(manifest.Id);
            return false;
        }

        // Best-effort validation: ping once so we can surface errors early.
        if (!TryInvoke(manifest.Id, "ping", w =>
            {
                w.WriteStartObject();
                w.WriteEndObject();
            }, out var doc, out errorMessage))
        {
            return false;
        }

        doc?.Dispose();
        return true;
    }

    public IReadOnlyList<OutOfProcAddonStatus> GetStatusSnapshots()
    {
        if (disposed)
        {
            return Array.Empty<OutOfProcAddonStatus>();
        }

        return instancesByAddonId.Values.Select(a => a.GetStatusSnapshot()).ToList();
    }

    public bool TryInvoke(string addonId, string method, Action<Utf8JsonWriter> writeParams, out JsonDocument? response, out string errorMessage)
    {
        response = null;
        errorMessage = string.Empty;

        if (disposed)
        {
            errorMessage = "Host is disposed.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(addonId))
        {
            errorMessage = "Invalid add-on id.";
            return false;
        }

        if (!instancesByAddonId.TryGetValue(addonId, out var instance))
        {
            errorMessage = "Add-on not started.";
            return false;
        }

        return instance.TryInvoke(method, writeParams, out response, out errorMessage);
    }

    public bool TryStop(string addonId)
    {
        if (disposed)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(addonId))
        {
            return false;
        }

        if (!instancesByAddonId.TryGetValue(addonId, out var instance))
        {
            return true;
        }

        instance.Stop();
        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (var instance in instancesByAddonId.Values)
        {
            try
            {
                instance.Stop();
            }
            catch
            {
            }
        }

        instancesByAddonId.Clear();
    }
}
