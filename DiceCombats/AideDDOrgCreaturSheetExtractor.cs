/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiceCombats
{
    public class AideDDOrgCreaturSheetExtractor
    {
        private string _url;

        public string CreatureName { get; private set; } = "N/A";
        public AideDDOrgCreaturSheetExtractor(string url)
        {
            _url = url;
        }

        public async Task<string> GetSheetAsHtmlString()
        {
            try
            {
                HttpClient client = new HttpClient();   // actually only one object should be created by Application
                string rawAideDDCreaturePage = await client.GetStringAsync(_url);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(rawAideDDCreaturePage);

                string creaturSheetDivContent = "";

                // div with class 'col1' stores the creature sheet
                var creatureSheetdiv = doc.DocumentNode.SelectSingleNode($"//div[contains(@class, 'col1')]");
                if (creatureSheetdiv != null)
                {
                    CreatureName = GetH1(creatureSheetdiv.InnerHtml);

                    foreach (string line in creatureSheetdiv.InnerHtml.Split("\n"))
                    {
                        if (line.Contains("class=\"description\""))
                        {
                            break;
                        }
                        creaturSheetDivContent += line;
                    }

                    // Strip some unwanted elements
                    creaturSheetDivContent = RemoveDivByClass(creaturSheetDivContent, "trad");
                    creaturSheetDivContent = RemoveDivByClass(creaturSheetDivContent, "description");
                    creaturSheetDivContent = RemoveDivByClass(creaturSheetDivContent, "source");
                    creaturSheetDivContent = RemoveAllOlElements(creaturSheetDivContent);
                }
                else
                {
                    creaturSheetDivContent = "Error";
                }

                // Get the styles
                var styles = doc.DocumentNode.SelectNodes("//style");
                string styleContents = "";

                if (styles != null)
                {
                    foreach (var style in styles)
                    {
                        foreach (string line in style.InnerHtml.Replace("../", "https://www.aidedd.org/").Split('\n'))
                        {
                            // Remove styles that could mess up the application styles
                            if (!(line.Contains("\t*") || line.Contains("html") || line.Contains("body") || line.Contains("\ta") || line.Contains("a:hover") || line.Contains("h1") || line.Contains("\tp") || line.Contains("\tul") || line.Contains("\tli") || line.Contains("\tol") || line.Contains("span") || line.Contains(".col\t")))
                            {
                                if (line.Contains(".red"))
                                {
                                    // Replace conflicting style
                                    styleContents += line.Replace(".red", ".red-aidedd") + "\n";
                                    creaturSheetDivContent = creaturSheetDivContent.Replace("class='red'", "class='red-aidedd'");
                                }
                                else
                                {
                                    styleContents += line + "\n";
                                }
                            }
                        }
                    }

                    styleContents += "a:link { color:#6D0000; }\na:hover { color: #6D0000; text-decoration: underline; }\n";
                }

                
                // Construct the final html
                return "<div>" + "<style>" + styleContents + "</style>" + creaturSheetDivContent + "</div>";
            }
            catch (Exception ex)
            {
                return "Error: Could not access/parse page: " + ex.Message;
            }
        }

        private string RemoveDivByClass(string html, string divClass)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode.SelectNodes($"//div[contains(@class, '{divClass}')]");
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    node.ParentNode.RemoveChild(node);
                }
            }

            return doc.DocumentNode.InnerHtml;
        }

        private string RemoveAllOlElements(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var olNodes = doc.DocumentNode.SelectNodes("//ol");
            if (olNodes != null)
            {
                foreach (var node in olNodes)
                {
                    node.ParentNode.RemoveChild(node);
                }
            }

            return doc.DocumentNode.OuterHtml;
        }

        private string GetH1(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var h1Node = doc.DocumentNode.SelectNodes("//h1").First();
            if (h1Node != null)
            {
                return h1Node.InnerHtml;
            }

            return "N/A";
        }
    }
}
