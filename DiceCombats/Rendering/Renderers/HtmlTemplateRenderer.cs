/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Linq;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using AngleSharp;
using DiceCombats.Rendering.Contracts;
using DiceCombats.Rendering.Util;

namespace DiceCombats.Rendering.Renderers
{
    /// <summary>
    /// Extracts multiple pieces from an HTML source. Each selected node (selector) becomes one RenderPiece.
    /// IMPORTANT: CSS is NOT injected here anymore; the caller (DetailedPopupFieldPopup) aggregates and injects once.
    /// </summary>
    public sealed class HtmlTemplateRenderer : IRenderer
    {
        public bool CanRender(IContentItem item)
            => item.Kind.Equals("html", System.StringComparison.OrdinalIgnoreCase);

        public IEnumerable<RenderPiece> RenderMany(IContentItem item)
        {
            var html = item.Text ?? (item.Data != null ? Encoding.UTF8.GetString(item.Data) : string.Empty);
            var selector = item.Meta.TryGetValue("selector", out var s) ? s : "body";
            var template = item.Meta.TryGetValue("template", out var t) ? t : "{{{html}}}";

            Dictionary<string, string>? mapSelectors = null;
            if (item.Meta.TryGetValue("map.json", out var mapJson) && !string.IsNullOrWhiteSpace(mapJson))
            {
                try { mapSelectors = JsonSerializer.Deserialize<Dictionary<string, string>>(mapJson); }
                catch { mapSelectors = null; }
            }

            return TransformHtmlToPieces(html, selector, template, mapSelectors);
        }

        private static IEnumerable<RenderPiece> TransformHtmlToPieces(
            string sourceHtml,
            string selector,
            string template,
            Dictionary<string, string>? mapSelectors)
        {
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var doc = context.OpenAsync(req => req.Content(sourceHtml)).GetAwaiter().GetResult();

            var nodes = doc.QuerySelectorAll(selector);
            var compiled = HandlebarsDotNet.Handlebars.Compile(template);

            foreach (var node in nodes)
            {
                var model = BuildModel((global::AngleSharp.Dom.IElement)node, mapSelectors);
                var renderedInner = compiled(model);

                var textForIndex =
                    model.TryGetValue("text", out var t) ? (t?.ToString() ?? string.Empty) :
                    node.TextContent ?? string.Empty;

                var idx = new SimpleIndex(new[] { ("self", textForIndex) });
                RenderFragment frag = b => b.AddMarkupContent(0, renderedInner);

                // Optional key for grouping/search display
                var key =
                    (model.TryGetValue("title", out var title) ? title?.ToString() : null) ??
                    (node.Id ?? null) ??
                    null;

                yield return new RenderPiece(frag, idx, key);
            }
        }

        private static Dictionary<string, object> BuildModel(global::AngleSharp.Dom.IElement node, Dictionary<string, string>? map)
        {
            var m = new Dictionary<string, object>();

            if (map == null || map.Count == 0)
            {
                m["html"] = node.InnerHtml ?? string.Empty;
                m["text"] = node.TextContent ?? string.Empty;
                return m;
            }

            foreach (var kv in map)
            {
                var key = kv.Key;
                var sel = kv.Value;

                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(sel))
                    continue;

                if (sel.StartsWith("~"))
                {
                    m[key] = sel.Equals("~selfHtml") ? (node.InnerHtml ?? string.Empty) : (node.TextContent ?? string.Empty);
                    continue;
                }

                var els = node.QuerySelectorAll(sel);
                if (key.Equals("rows", System.StringComparison.OrdinalIgnoreCase))
                {
                    var rows = els.Select(e => e.InnerHtml?.Trim() ?? string.Empty)
                                  .Where(s => !string.IsNullOrWhiteSpace(s))
                                  .ToList();
                    m[key] = rows;
                }
                else
                {
                    var e = els.FirstOrDefault();
                    m[key] = e != null ? (object)(e.InnerHtml?.Trim() ?? string.Empty) : string.Empty;
                }
            }

            if (!m.ContainsKey("html")) m["html"] = node.InnerHtml ?? string.Empty;
            if (!m.ContainsKey("text")) m["text"] = node.TextContent ?? string.Empty;

            return m;
        }
    }
}
