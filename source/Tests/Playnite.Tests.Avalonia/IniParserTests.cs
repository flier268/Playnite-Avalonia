using NUnit.Framework;
using Playnite.Common;
using System;

namespace Playnite.Tests.Avalonia;

public sealed class IniParserTests
{
    [Test]
    public void Parse_BasicIni_Works()
    {
        var ini = new[]
        {
            "; comment",
            "",
            "   ; another comment",
            "[Sec]",
            "Key=Val",
            "Key2=",
        };

        var data = IniParser.Parse(ini);

        Assert.That(data["Sec"], Is.Not.Null);
        Assert.That(data["Sec"]["Key"], Is.EqualTo("Val"));
        Assert.That(data["Sec"]["Key2"], Is.EqualTo(string.Empty));
    }

    [Test]
    public void IniSectionIndexerSetter_AddsAndReplaces()
    {
        var section = new IniSection("Sec");
        section["A"] = "1";
        Assert.That(section["A"], Is.EqualTo("1"));

        section["A"] = "2";
        Assert.That(section["A"], Is.EqualTo("2"));
        Assert.That(section.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public void IniDataIndexerSetter_AddsAndReplaces()
    {
        var data = new IniData();
        var section1 = new IniSection("Sec");
        var section2 = new IniSection("Sec");

        data["Sec"] = section1;
        Assert.That(data["Sec"], Is.SameAs(section1));

        data["Sec"] = section2;
        Assert.That(data.Sections, Has.Count.EqualTo(1));
        Assert.That(data["Sec"], Is.SameAs(section2));
    }

    [Test]
    public void IniDataIndexerSetter_RejectsMismatchedName()
    {
        var data = new IniData();
        Assert.Throws<ArgumentException>(() => data["X"] = new IniSection("Y"));
    }
}

