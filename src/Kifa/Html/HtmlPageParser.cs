using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;

namespace Kifa.Html;

public class HtmlPageParser {
    static readonly Regex SimpleIdRegex = new(@"^[A-Za-z][A-Za-z0-9_-]*$", RegexOptions.Compiled);
    static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    readonly HashSet<IElement> consumed = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<IElement> consumedOwnText = new(ReferenceEqualityComparer.Instance);

    public IDocument Document { get; }

    public HtmlPageParser(string html) : this(html.GetDocument()) { }

    public HtmlPageParser(IDocument document) {
        Document = document;
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

    public static string GetXPath(IElement el) {
        var segments = new List<string>();
        var current = el;
        while (current != null) {
            var id = current.Id;
            if (!string.IsNullOrEmpty(id) && SimpleIdRegex.IsMatch(id) &&
                current.Owner?.QuerySelectorAll($"[id='{id}']").Length == 1) {
                segments.Add($"//*[@id='{id}']");
                segments.Reverse();
                return string.Join("/", segments);
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

    public static string GetSnippet(IElement el, int maxLength = 80) {
        var normalized = WhitespaceRegex.Replace(el.OuterHtml, " ").Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..(maxLength - 1)] + "…";
    }
}
