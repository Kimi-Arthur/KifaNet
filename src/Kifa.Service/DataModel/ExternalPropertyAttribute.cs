using System;

namespace Kifa.Service;

[AttributeUsage(AttributeTargets.Property)]
public class ExternalPropertyAttribute(string suffix) : Attribute {
    // Suffix to be added after id. This can be just an extension or with some additional text.
    public string Suffix { get; set; } = suffix;
}
