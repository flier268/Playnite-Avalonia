using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Playnite.Common
{
    public class IniSection
    {
        public string Name { get; }
        public List<IniItem> Items { get; } = new List<IniItem>();

        public string this[string itemName]
        {
            get
            {
                return Items.FirstOrDefault(a => a.Name == itemName)?.Value;
            }

            set
            {
                if (string.IsNullOrEmpty(itemName))
                {
                    throw new ArgumentNullException(nameof(itemName));
                }

                var index = Items.FindIndex(a => a.Name == itemName);
                var item = new IniItem(itemName, value);

                if (index >= 0)
                {
                    Items[index] = item;
                }
                else
                {
                    Items.Add(item);
                }
            }
        }

        public IniSection(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class IniItem
    {
        public string Name { get; }
        public string Value { get; }

        public IniItem(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Value = value ?? string.Empty;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class IniData
    {
        public List<IniSection> Sections { get; } = new List<IniSection>();

        public IniSection this[string sectionName]
        {
            get
            {
                return Sections.FirstOrDefault(a => a.Name == sectionName);
            }

            set
            {
                if (string.IsNullOrEmpty(sectionName))
                {
                    throw new ArgumentNullException(nameof(sectionName));
                }

                if (value == null)
                {
                    Sections.RemoveAll(a => string.Equals(a.Name, sectionName, StringComparison.OrdinalIgnoreCase));
                    return;
                }

                if (!string.Equals(value.Name, sectionName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Section name '{value.Name}' doesn't match indexer name '{sectionName}'.", nameof(value));
                }

                var index = Sections.FindIndex(a => string.Equals(a.Name, sectionName, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    Sections[index] = value;
                }
                else
                {
                    Sections.Add(value);
                }
            }
        }
    }

    public class IniParser
    {
        public static IniData Parse(string[] iniString)
        {            
            if (iniString?.Any() != true)
            {
                throw new ArgumentNullException(nameof(iniString));
            }

            var data = new IniData();
            IniSection curSection = null;
            foreach (var line in iniString)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Comment
                if (line.TrimStart().StartsWith(";"))
                {
                    continue;
                }

                // Section
                var sectionMatch = Regex.Match(line.Trim(), @"^\[(.+)\]$");
                if (sectionMatch.Success)
                {
                    curSection = new IniSection(sectionMatch.Groups[1].Value);
                    data.Sections.Add(curSection);
                    continue;
                }

                // Section item
                var valueMatch = Regex.Match(line.Trim(), @"^(.+)=(.*)$");
                if (valueMatch.Success)
                {
                    if (curSection != null)
                    {
                        curSection.Items.Add(new IniItem(valueMatch.Groups[1].Value, valueMatch.Groups[2].Value));
                    }
                }
            }

            return data;
        }
    }
}
