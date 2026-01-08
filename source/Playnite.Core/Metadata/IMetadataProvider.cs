using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace Playnite.Metadata;

public interface IMetadataProvider
{
    string Id { get; }
    string Name { get; }
    OnDemandMetadataProvider CreateProvider(Game game);
}

