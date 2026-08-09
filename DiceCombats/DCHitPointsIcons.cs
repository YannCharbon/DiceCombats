/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Collections.Generic;
using MudBlazor;

namespace DiceCombats
{
    /// <summary>
    /// The icons a damage kind can be given. Only the short name is stored in the
    /// creature file, so the saved data does not carry the raw SVG of an icon and
    /// keeps working if MudBlazor changes it.
    /// </summary>
    public static class DCHitPointsIcons
    {
        public const string DefaultIcon = "Whatshot";

        private static readonly Dictionary<string, string> _icons = new()
        {
            { "Whatshot", Icons.Material.Filled.Whatshot },
            { "LocalFireDepartment", Icons.Material.Filled.LocalFireDepartment },
            { "AcUnit", Icons.Material.Filled.AcUnit },
            { "ElectricBolt", Icons.Material.Filled.ElectricBolt },
            { "Thunderstorm", Icons.Material.Filled.Thunderstorm },
            { "Tornado", Icons.Material.Filled.Tornado },
            { "Air", Icons.Material.Filled.Air },
            { "WaterDrop", Icons.Material.Filled.WaterDrop },
            { "Waves", Icons.Material.Filled.Waves },
            { "Science", Icons.Material.Filled.Science },
            { "Coronavirus", Icons.Material.Filled.Coronavirus },
            { "Psychology", Icons.Material.Filled.Psychology },
            { "AutoAwesome", Icons.Material.Filled.AutoAwesome },
            { "LightMode", Icons.Material.Filled.LightMode },
            { "Bloodtype", Icons.Material.Filled.Bloodtype },
            { "ContentCut", Icons.Material.Filled.ContentCut },
            { "RestaurantMenu", Icons.Material.Filled.RestaurantMenu },
            { "Colorize", Icons.Material.Filled.Colorize },
            { "ArrowRightAlt", Icons.Material.Filled.ArrowRightAlt },
            { "Hardware", Icons.Material.Filled.Hardware },
            { "Gavel", Icons.Material.Filled.Gavel },
            { "SportsMartialArts", Icons.Material.Filled.SportsMartialArts },
            { "Shield", Icons.Material.Filled.Shield },
            { "Star", Icons.Material.Filled.Star }
        };

        public static List<string> Names => new List<string>(_icons.Keys);

        public static string Resolve(string name)
        {
            if (!string.IsNullOrEmpty(name) && _icons.TryGetValue(name, out string? icon))
            {
                return icon;
            }
            return _icons[DefaultIcon];
        }
    }
}
