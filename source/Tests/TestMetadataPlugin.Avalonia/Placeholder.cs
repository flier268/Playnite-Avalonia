using System;
using System.Collections.Generic;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace TestMetadataPlugin.Avalonia;

public sealed class TestMetadataProviderPlugin : MetadataPlugin
{
    public override Guid Id { get; } = Guid.Parse("7BE74C9E-0245-4E72-90A6-8F2D6E24B0F0");

    public TestMetadataProviderPlugin(IPlayniteAPI playniteAPI) : base(playniteAPI)
    {
    }

    public override string Name => "Test Metadata Provider";

    public override List<MetadataField> SupportedFields => new List<MetadataField>
    {
        MetadataField.Name,
        MetadataField.Description,
        MetadataField.CoverImage
    };

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        return new TestOnDemandMetadataProvider(options?.GameData);
    }
}

internal sealed class TestOnDemandMetadataProvider : OnDemandMetadataProvider
{
    private readonly Game game;

    public TestOnDemandMetadataProvider(Game game)
    {
        this.game = game ?? new Game();
    }

    public override List<MetadataField> AvailableFields => new List<MetadataField>
    {
        MetadataField.Name,
        MetadataField.Description,
        MetadataField.CoverImage
    };

    public override string GetName(GetMetadataFieldArgs args)
    {
        return string.IsNullOrWhiteSpace(game.Name) ? "Test Game Name" : $"{game.Name} (Test)";
    }

    public override string GetDescription(GetMetadataFieldArgs args)
    {
        return "Test description from metadata provider.";
    }

    public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
    {
        // 1x1 transparent PNG.
        var bytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQImWNgYGBgAAAABQABJzQnCgAAAABJRU5ErkJggg==");
        return new MetadataFile("cover.png", bytes);
    }
}
