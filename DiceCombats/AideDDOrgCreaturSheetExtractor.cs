/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using HtmlAgilityPack;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiceCombats
{
    public class AideDDOrgCreaturSheetExtractor
    {
        private static readonly HttpClient Http = new HttpClient();

        private readonly string _url;

        public string CreatureName { get; private set; } = "N/A";

        public AideDDOrgCreaturSheetExtractor(string url)
        {
            _url = url;
        }

        public async Task<string> GetSheetAsHtmlString()
        {
            try
            {
                string rawAideDDCreaturePage = await Http.GetStringAsync(_url);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(rawAideDDCreaturePage);

                var creatureSheetDiv = doc.DocumentNode.SelectSingleNode(
                    "//div[contains(concat(' ', normalize-space(@class), ' '), ' col1 ')]");

                if (creatureSheetDiv == null)
                    return "Error: creature sheet not found";

                creatureSheetDiv = creatureSheetDiv.CloneNode(true);

                bool isDnd2024 = IsDnd2024Sheet(creatureSheetDiv);

                CreatureName = GetH1(creatureSheetDiv);

                RemoveDivByClass(creatureSheetDiv, "trad");
                RemoveDivByClass(creatureSheetDiv, "description");
                RemoveDivByClass(creatureSheetDiv, "source");
                RemoveAllOlElements(creatureSheetDiv);
                RemoveDirectBrElements(creatureSheetDiv);

                if (isDnd2024)
                {
                    Rename2024Classes(creatureSheetDiv);
                }
                else
                {
                    // Avoid conflict with app/global CSS using a generic .red class.
                    RenameClass(creatureSheetDiv, "red", "red-aidedd");
                }

                string creatureSheetDivContent = creatureSheetDiv.InnerHtml;

                return
                    "<div class=\"" + (isDnd2024 ? "aidedd-sheet-2024" : "aidedd-sheet") + "\">" +
                    "<style>" + GetMonsterSheetCss(isDnd2024) + "</style>" +
                    creatureSheetDivContent +
                    "</div>";
            }
            catch (Exception ex)
            {
                return "Error: Could not access/parse page: " + ex.Message;
            }
        }

        private static bool IsDnd2024Sheet(HtmlNode root)
        {
            return
                root.SelectSingleNode(".//div[contains(concat(' ', normalize-space(@class), ' '), ' init ')]") != null ||
                root.SelectSingleNode(".//div[contains(concat(' ', normalize-space(@class), ' '), ' car1 ')]") != null ||
                root.SelectSingleNode(".//h2[contains(concat(' ', normalize-space(@class), ' '), ' rub ')]") != null;
        }

        private static void Rename2024Classes(HtmlNode root)
        {
            RenameClass(root, "jaune", "jaune-2024");
            RenameClass(root, "sansSerif", "sansSerif-2024");
            RenameClass(root, "red", "red-2024");
            RenameClass(root, "type", "type-2024");
            RenameClass(root, "init", "init-2024");
            RenameClass(root, "rub", "rub-2024");

            RenameClass(root, "car", "car-2024");
            RenameClass(root, "car1", "car1-2024");
            RenameClass(root, "car2", "car2-2024");
            RenameClass(root, "car3", "car3-2024");
            RenameClass(root, "car4", "car4-2024");
            RenameClass(root, "car5", "car5-2024");
            RenameClass(root, "car6", "car6-2024");
        }

        private static string GetMonsterSheetCss(bool isDnd2024)
        {
            return isDnd2024 ? GetMonsterSheetCss2024() : GetMonsterSheetCss2014();
        }

        private static string GetMonsterSheetCss2014()
        {
            return @"
.aidedd-sheet {
    display: block;
    width: 100%;
    color: black;
    font-family: Georgia, ""Times New Roman"", serif;
    font-size: 14px;
    line-height: 1.55;
}

.aidedd-sheet * {
    box-sizing: border-box;
}

.aidedd-sheet .orange {
    border: 1px black solid;
    background-color: #e69a28;
    width: 100%;
    height: 6px;
}

.aidedd-sheet .jaune {
    background-color: #fdf1dc;
    padding: 10px 8px 5px 8px;
    margin: 0 2px 0 2px;
    box-shadow: 0px 0px 6px 0px rgba(0,0,0,0.5);
    background-image:
        url(""https://www.aidedd.org/images/fond-mm-effet.png""),
        url(""https://www.aidedd.org/images/fond-ph.jpg"");
    background-repeat: no-repeat, repeat;
}

.aidedd-sheet h1 {
    margin: 0;
    padding: 0;
    font-variant: small-caps;
    letter-spacing: 1px;
    color: #6D0000;
    font-size: 32px;
    line-height: 1;
    break-after: avoid;
}

.aidedd-sheet .sansSerif {
    font-family: Arial, sans-serif;
}

.aidedd-sheet .type {
    padding: 2px 0 0 0;
    font-size: 14px;
    font-style: italic;
}

.aidedd-sheet div.sansSerif svg {
    width: 100%;
    height: 5px;
}

.aidedd-sheet .red-aidedd {
    color: #6D0000;
    font-size: 14px;
}

.aidedd-sheet .carac {
    display: inline-block;
    width: 16.6%;
    max-width: 55px;
    text-align: center;
    vertical-align: top;
}

.aidedd-sheet p {
    margin: 0;
    padding: 0;
    font-size: 14px;
    line-height: 1.55;
}

.aidedd-sheet .rub {
    color: #6D0000;
    margin: 6px 0 4px 0;
    border-bottom: 1px solid #6D0000;
    font-size: 18px;
    font-weight: normal;
    font-variant: small-caps;
}

.aidedd-sheet a {
    color: #6D0000;
    text-decoration: none;
}

.aidedd-sheet a:hover {
    color: #6D0000;
    text-decoration: underline;
}
";
        }

        private static string GetMonsterSheetCss2024()
        {
            return @"
.aidedd-sheet-2024 {
    display: block;
    width: 100%;
    color: black;
    font-family: ""Open Sans"", Arial, sans-serif;
    font-size: 14px;
    line-height: 1.55;
    font-weight: 500;
}

.aidedd-sheet-2024 * {
    box-sizing: border-box;
}

.aidedd-sheet-2024 .jaune-2024 {
    background-color: #f1eee8;
    padding: 12px 18px 14px 18px;
    margin: 0;
    border: 4px solid #bdbdb8;
    border-radius: 12px;
    box-shadow: none;
    background-image: none;
}

.aidedd-sheet-2024 h1 {
    margin: 0 0 4px 0;
    padding: 0 0 2px 0;
    color: #6D0000;
    border-bottom: 1px solid #6D0000;
    font-family: ""Open Sans"", Arial, sans-serif;
    font-size: 28px;
    font-weight: 700;
    font-variant: small-caps;
    letter-spacing: 2px;
    line-height: 1.1;
}

.aidedd-sheet-2024 .sansSerif-2024 {
    font-family: ""Open Sans"", Arial, sans-serif;
}

.aidedd-sheet-2024 .red-2024 {
    color: #6D0000;
    font-size: 14px;
}

.aidedd-sheet-2024 .type-2024 {
    padding: 0 0 4px 0;
    color: #555;
    font-size: 14px;
    font-style: italic;
}

.aidedd-sheet-2024 .init-2024 {
    float: right;
    margin-left: 12px;
    white-space: nowrap;
    color: #6D0000;
    font-weight: 700;
}

.aidedd-sheet-2024 .car-2024,
.aidedd-sheet-2024 .car1-2024,
.aidedd-sheet-2024 .car2-2024,
.aidedd-sheet-2024 .car3-2024,
.aidedd-sheet-2024 .car4-2024,
.aidedd-sheet-2024 .car5-2024,
.aidedd-sheet-2024 .car6-2024 {
    display: inline-block;
    width: 7.75%;
    margin: 0 0.15%;
    padding: 3px 2px;
    text-align: center;
    vertical-align: top;
    line-height: 1.25;
    font-size: 14px;
}

.aidedd-sheet-2024 .car-2024 {
    color: #555;
    background-color: transparent;
    font-size: 12px;
    font-weight: 500;
}

.aidedd-sheet-2024 .car1-2024,
.aidedd-sheet-2024 .car2-2024,
.aidedd-sheet-2024 .car4-2024,
.aidedd-sheet-2024 .car5-2024 {
    background-color: #e8e5dc;
}

.aidedd-sheet-2024 .car3-2024,
.aidedd-sheet-2024 .car6-2024 {
    background-color: #d9d4d1;
}

.aidedd-sheet-2024 .car1-2024,
.aidedd-sheet-2024 .car4-2024 {
    color: #6D0000;
    font-weight: 700;
    text-transform: uppercase;
}

.aidedd-sheet-2024 .car2-2024,
.aidedd-sheet-2024 .car3-2024,
.aidedd-sheet-2024 .car5-2024,
.aidedd-sheet-2024 .car6-2024 {
    color: #6D0000;
    font-weight: 700;
}

.aidedd-sheet-2024 p {
    margin: 0;
    padding: 0;
    color: black;
    font-size: 14px;
    line-height: 1.55;
}

.aidedd-sheet-2024 h2.rub-2024,
.aidedd-sheet-2024 .rub-2024 {
    color: #6D0000;
    margin: 10px 0 6px 0;
    padding: 0 0 2px 0;
    border-bottom: 1px solid #6D0000;
    font-size: 20px;
    font-weight: 500;
    font-variant: small-caps;
    letter-spacing: 1px;
    line-height: 1.2;
}

.aidedd-sheet-2024 a {
    color: #6D0000;
    text-decoration: none;
}

.aidedd-sheet-2024 a:hover {
    color: #6D0000;
    text-decoration: underline;
}
";
        }

        private static void RemoveDivByClass(HtmlNode root, string divClass)
        {
            var nodes = root.SelectNodes(
                $".//div[contains(concat(' ', normalize-space(@class), ' '), ' {divClass} ')]");

            if (nodes == null)
                return;

            foreach (var node in nodes.ToList())
                node.Remove();
        }

        private static void RemoveAllOlElements(HtmlNode root)
        {
            var nodes = root.SelectNodes(".//ol");

            if (nodes == null)
                return;

            foreach (var node in nodes.ToList())
                node.Remove();
        }

        private static void RemoveDirectBrElements(HtmlNode root)
        {
            var nodes = root.SelectNodes("./br");

            if (nodes == null)
                return;

            foreach (var node in nodes.ToList())
                node.Remove();
        }

        private static void RenameClass(HtmlNode root, string oldClass, string newClass)
        {
            var nodes = root
                .DescendantsAndSelf()
                .Where(n => n.Attributes["class"] != null);

            foreach (var node in nodes)
            {
                var classes = node.GetAttributeValue("class", "")
                    .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c == oldClass ? newClass : c);

                node.SetAttributeValue("class", string.Join(" ", classes));
            }
        }

        private static string GetH1(HtmlNode root)
        {
            var h1Node = root.SelectSingleNode(".//h1");

            if (h1Node != null)
                return WebUtility.HtmlDecode(h1Node.InnerText.Trim());

            return "N/A";
        }
    }
}