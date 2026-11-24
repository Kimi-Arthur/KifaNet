using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;

namespace Kifa.Html;

public class HtmlPageParser {
    static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    readonly HashSet<IElement> consumed = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<IElement> consumedOwnText = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<string> anchorClasses;

    Dictionary<string, int>? idCounts;
    Dictionary<(string Tag, string ClassAttr), List<IElement>>? anchorGroups;

    public IDocument Document { get; }

    public HtmlPageParser(string html, IEnumerable<string>? anchorClasses = null)
        : this(html.GetDocument(), anchorClasses) { }

    public HtmlPageParser(IDocument document, IEnumerable<string>? anchorClasses = null) {
        Document = document;
        this.anchorClasses = anchorClasses != null
            ? new HashSet<string>(anchorClasses, StringComparer.Ordinal)
            : [];
    }

    public T? Consume<T>(T? element) where T : class, IElement {
        if (element != null) {
            consumed.Add(element);
        }

        return element;
    }

    public List<T> ConsumeAll<T>(IEnumerable<T> elements) where T : class, IElement {
        var list = elements.ToList();
        foreach (var element in list) {
            consumed.Add(element);
        }

        return list;
    }

    public T? ConsumeOwnText<T>(T? element) where T : class, IElement {
        if (element != null) {
            consumedOwnText.Add(element);
        }

        return element;
    }

    public bool IsConsumed(IElement element) => consumed.Contains(element);

    public void Ignore(string selector, IParentNode? root = null) {
        var target = root ?? Document;
        foreach (var el in target.QuerySelectorAll(selector)) {
            consumed.Add(el);
        }
    }

    public void Ignore(IEnumerable<string> selectors, IParentNode? root = null) {
        foreach (var selector in selectors) {
            Ignore(selector, root);
        }
    }

    public void IgnoreWhere(string selector, Func<IElement, bool> predicate,
        IParentNode? root = null) {
        var target = root ?? Document;
        foreach (var el in target.QuerySelectorAll(selector)) {
            if (predicate(el)) {
                consumed.Add(el);
            }
        }
    }

    public List<IElement> GetUnconsumedElements(IElement root)
        => CollectUnconsumed(root).Pending;

    (bool AnyConsumed, List<IElement> Pending) CollectUnconsumed(IElement el) {
        if (consumed.Contains(el)) {
            return (true, []);
        }

        var anyChildConsumed = false;
        var childPending = new List<IElement>();
        foreach (var child in el.Children) {
            var (childConsumed, pending) = CollectUnconsumed(child);
            if (childConsumed) {
                anyChildConsumed = true;
            }

            childPending.AddRange(pending);
        }

        var hasResidualText = !consumedOwnText.Contains(el) && HasMeaningfulOwnText(el);

        if (anyChildConsumed) {
            if (hasResidualText) {
                childPending.Insert(0, el);
            }

            return (true, childPending);
        }

        return (false, [el]);
    }

    public static bool HasMeaningfulOwnText(IElement el)
        => el.ChildNodes.OfType<IText>().Any(t => t.Data.Any(char.IsLetterOrDigit));

    public static string DescribeElement(IElement el) {
        var classes = string.Join(".", el.ClassList);
        var tag = el.TagName.ToLowerInvariant();
        return string.IsNullOrEmpty(classes) ? tag : $"{tag}.{classes}";
    }

    public string GetXPath(IElement el) {
        EnsureXPathIndex();

        var segments = new List<string>();
        var current = el;
        while (current != null) {
            var id = current.Id;
            if (!string.IsNullOrEmpty(id) && idCounts != null &&
                idCounts.TryGetValue(id, out var idCount) && idCount == 1) {
                segments.Add($"//*[@id='{id}']");
                segments.Reverse();
                return string.Join("/", segments);
            }

            if (anchorClasses.Count > 0 && anchorGroups != null) {
                var classAttr = current.GetAttribute("class");
                if (!string.IsNullOrEmpty(classAttr) &&
                    anchorGroups.TryGetValue((current.TagName, classAttr), out var group)) {
                    var idx = group.IndexOf(current);
                    if (idx >= 0) {
                        var tagName = current.TagName.ToLowerInvariant();
                        var expr = $"//{tagName}[@class='{classAttr}']";
                        segments.Add(group.Count == 1 ? expr : $"({expr})[{idx + 1}]");
                        segments.Reverse();
                        return string.Join("/", segments);
                    }
                }
            }

            var tag = current.TagName.ToLowerInvariant();
            var parent = current.ParentElement;
            if (parent != null) {
                var sameTagSiblings = parent.Children
                    .Where(c => c.TagName == current.TagName)
                    .ToList();
                if (sameTagSiblings.Count > 1) {
                    var index = sameTagSiblings.IndexOf(current) + 1;
                    tag = $"{tag}[{index}]";
                }
            }

            segments.Add(tag);
            current = parent;
        }

        segments.Reverse();
        return "/" + string.Join("/", segments);
    }

    void EnsureXPathIndex() {
        if (idCounts != null) {
            return;
        }

        idCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        if (anchorClasses.Count > 0) {
            anchorGroups = [];
        }

        foreach (var node in Document.All) {
            var id = node.Id;
            if (IsSafeXPathId(id)) {
                idCounts[id!] = idCounts.GetValueOrDefault(id!) + 1;
            }

            if (anchorGroups != null && node.ClassList.Any(anchorClasses.Contains)) {
                var classAttr = node.GetAttribute("class");
                if (!string.IsNullOrEmpty(classAttr) && !classAttr.Contains('\'')) {
                    var key = (node.TagName, classAttr);
                    if (!anchorGroups.TryGetValue(key, out var list)) {
                        list = [];
                        anchorGroups[key] = list;
                    }

                    list.Add(node);
                }
            }
        }
    }

    static bool IsSafeXPathId(string? id)
        => !string.IsNullOrEmpty(id) && !id.Contains('\'') && !id.Any(char.IsWhiteSpace);

    public static string GetSnippet(IElement el, int maxLength = 80) {
        var normalized = WhitespaceRegex.Replace(el.OuterHtml, " ").Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..(maxLength - 1)] + "…";
    }
}
