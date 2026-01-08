using System;

namespace Playnite.Library;

public sealed class LibraryIdName
{
    public LibraryIdName(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; }
    public string Name { get; }

    public override string ToString() => Name;
}

