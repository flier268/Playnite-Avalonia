using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Playnite.SDK.Models;

namespace Playnite.Emulation;

public static class BuiltInEmulatorDefinitionStore
{
    private static readonly object SyncLock = new object();
    private sealed class DefinitionEntry
    {
        public EmulatorDefinition Definition { get; init; }
        public string DirectoryPath { get; init; } = string.Empty;
    }

    private static Dictionary<string, DefinitionEntry>? entriesById;

    public static bool TryGetProfile(string emulatorDefinitionId, string profileName, out EmulatorDefinitionProfile profile)
    {
        profile = null;
        if (string.IsNullOrWhiteSpace(emulatorDefinitionId) || string.IsNullOrWhiteSpace(profileName))
        {
            return false;
        }

        EnsureLoaded();

        if (entriesById == null || !entriesById.TryGetValue(emulatorDefinitionId, out var entry))
        {
            return false;
        }

        profile = entry.Definition.Profiles?.FirstOrDefault(a => string.Equals(a.Name, profileName, StringComparison.Ordinal));
        return profile != null;
    }

    public static bool TryGetStartupScriptPath(string emulatorDefinitionId, out string scriptPath)
    {
        scriptPath = string.Empty;
        if (string.IsNullOrWhiteSpace(emulatorDefinitionId))
        {
            return false;
        }

        EnsureLoaded();

        if (entriesById == null || !entriesById.TryGetValue(emulatorDefinitionId, out var entry))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry.DirectoryPath))
        {
            return false;
        }

        scriptPath = Path.Combine(entry.DirectoryPath, "startGame.ps1");
        return true;
    }

    private static void EnsureLoaded()
    {
        if (entriesById != null)
        {
            return;
        }

        lock (SyncLock)
        {
            if (entriesById != null)
            {
                return;
            }

            entriesById = LoadAllDefinitions()
                .Where(a => !string.IsNullOrWhiteSpace(a.Definition?.Id))
                .ToDictionary(a => a.Definition.Id, StringComparer.Ordinal);
        }
    }

    private static IEnumerable<DefinitionEntry> LoadAllDefinitions()
    {
        var root = ResolveDefinitionsRoot();
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return Array.Empty<DefinitionEntry>();
        }

        var result = new List<DefinitionEntry>();
        foreach (var dir in Directory.GetDirectories(root))
        {
            var manifest = Path.Combine(dir, "emulator.yaml");
            if (!File.Exists(manifest))
            {
                continue;
            }

            try
            {
                var yaml = File.ReadAllText(manifest);
                var definition = SimpleEmulatorYamlParser.TryParseDefinition(yaml);
                if (definition != null && !string.IsNullOrWhiteSpace(definition.Id))
                {
                    result.Add(new DefinitionEntry
                    {
                        Definition = definition,
                        DirectoryPath = dir
                    });
                }
            }
            catch
            {
            }
        }

        return result;
    }

    private static string ResolveDefinitionsRoot()
    {
        var env = Environment.GetEnvironmentVariable("PLAYNITE_EMU_DEFINITIONS_PATH") ??
                  Environment.GetEnvironmentVariable("PLAYNITE_EMULATION_DEFINITIONS_PATH");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
        {
            return env;
        }

        var baseDir = AppContext.BaseDirectory;
        if (string.IsNullOrWhiteSpace(baseDir))
        {
            return string.Empty;
        }

        var current = new DirectoryInfo(baseDir);
        for (var i = 0; i < 10 && current != null; i++)
        {
            var candidate1 = Path.Combine(current.FullName, "Emulation", "Emulators");
            if (Directory.Exists(candidate1))
            {
                return candidate1;
            }

            var candidate2 = Path.Combine(current.FullName, "Playnite", "Emulation", "Emulators");
            if (Directory.Exists(candidate2))
            {
                return candidate2;
            }

            current = current.Parent;
        }

        return string.Empty;
    }
}
