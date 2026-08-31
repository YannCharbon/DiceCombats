/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using DiceCombats.Rendering.Contracts;
using DiceCombats.Rendering.Util;

namespace DiceCombats.Rendering.Renderers
{
    /// <summary>
    /// Per-page rendering: one RenderPiece per PDF page.
    /// Visual: pdf.js (DC_pdfRenderPage); Index: PdfPig
    /// </summary>
    public sealed class PdfRenderer : IRenderer
    {
        public bool CanRender(IContentItem item)
            => item.Kind.Equals("pdf", StringComparison.OrdinalIgnoreCase);

        public IEnumerable<RenderPiece> RenderMany(IContentItem item)
        {
            var bytes = item.Data ?? (item.Path != null && File.Exists(item.Path) ? File.ReadAllBytes(item.Path) : null);
            bytes ??= Array.Empty<byte>();

            var base64 = Convert.ToBase64String(bytes);

            // The element id has to be stable across renders. RenderMany runs again on
            // every render of the hosting component (the combat timer alone re-renders
            // once a second), and a fresh id made the emitted markup differ every time,
            // so Blazor replaced the node, the embedded script re-ran, and the page was
            // cleared and rendered from scratch - visibly, and leaking a ResizeObserver
            // each round.
            var docKey = ComputeDocKey(item, bytes);

            // Try to index with PdfPig
            var pageTexts = new List<(int page, string text)>();
            if (bytes.Length > 0)
            {
                try
                {
                    using var ms = new MemoryStream(bytes);
                    using var doc = global::UglyToad.PdfPig.PdfDocument.Open(ms);
                    foreach (var p in doc.GetPages())
                    {
                        pageTexts.Add((p.Number, p.Text ?? string.Empty));
                    }
                }
                catch
                {
                    // leave pageTexts empty if parsing fails
                }
            }

            if (pageTexts.Count == 0)
            {
                // Fallback: single failure piece
                var id = "pdf_" + docKey;
                var html = $@"<div class='pdf-doc' id='{id}'>
<div class='pdf-page'><div class='pdf-page-header'>PDF</div>
<div style='color:#f55'>No pages or parsing failed.</div></div></div>";
                RenderFragment frag = b => b.AddMarkupContent(0, html);
                yield return new RenderPiece(frag, new SimpleIndex(Array.Empty<(string, string)>()), null);
                yield break;
            }

            foreach (var (pageNo, text) in pageTexts)
            {
                var id = $"pdf_{pageNo}_{docKey}";
                var html = $@"<div class='pdf-doc' id='{id}'></div>
<script>
(function(){{
  if(!window.pdfjsLib){{
    var s=document.createElement('script');
    s.src='https://cdn.jsdelivr.net/npm/pdfjs-dist@3.11.174/build/pdf.min.js';
    document.head.appendChild(s);
  }}
  if(!window.__dc_pdfhelper_injected){{
    window.__dc_pdfhelper_injected=true;
    var s2=document.createElement('script');
    s2.src='js/dc-pdf.js';
    document.head.appendChild(s2);
  }}
  function start(){{
    if(!window.pdfjsLib || !window.DC_pdfRenderPage){{ setTimeout(start,80); return; }}
    window.DC_pdfRenderPage('{id}','{base64}', {pageNo}, {{ initialScale: 'fit-width' }});
  }}
  start();
}})();
</script>";

                RenderFragment frag = b => b.AddMarkupContent(0, html);
                var idx = new SimpleIndex(new[] { ($"page-{pageNo}", text ?? string.Empty) });
                yield return new RenderPiece(frag, idx, $"page-{pageNo}");
            }
        }

        /// <summary>
        /// Deterministic short key for this document, so that repeated renders of the
        /// same content produce byte-identical markup and Blazor leaves the DOM alone.
        /// </summary>
        private static string ComputeDocKey(IContentItem item, byte[] bytes)
        {
            using var hash = System.Security.Cryptography.IncrementalHash.CreateHash(
                System.Security.Cryptography.HashAlgorithmName.SHA256);

            var header = System.Text.Encoding.UTF8.GetBytes(
                $"{item.Name}|{item.Path}|{bytes.Length}");

            hash.AppendData(header);
            hash.AppendData(bytes);

            return Convert.ToHexString(hash.GetHashAndReset()).Substring(0, 16).ToLowerInvariant();
        }
    }
}
