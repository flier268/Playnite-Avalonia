using System;
using System.Collections.Generic;
using System.Linq;

namespace Playnite.Metadata;

public sealed class MetadataProviderRegistry
{
    private readonly List<IMetadataProvider> providers = new List<IMetadataProvider>();

    public static MetadataProviderRegistry Default { get; } = new MetadataProviderRegistry();

    public IReadOnlyList<IMetadataProvider> Providers => providers;

    public void Register(IMetadataProvider provider)
    {
        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (providers.Any(p => string.Equals(p.Id, provider.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        providers.Add(provider);
    }

    public void Clear()
    {
        providers.Clear();
    }
}

