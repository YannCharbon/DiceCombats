/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Collections.Generic;

namespace DiceCombats
{
    /// <summary>
    /// One line of an applied attack, kept apart from the others so a hit made of
    /// several damage kinds raises one event per kind.
    /// </summary>
    public class DCDamageLineInfo
    {
        public string Kind { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public int RawAmount { get; set; }
        public int AppliedAmount { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class DCDamageSourceCandidate
    {
        public string Name { get; set; } = string.Empty;
        public bool IsPlayer { get; set; }
    }

    /// <summary>
    /// Who can be credited for damage, cascaded down from the combat manager so
    /// the hit points field does not have to be handed the whole combat.
    /// </summary>
    public class DCDamageSourceContext
    {
        public List<DCDamageSourceCandidate> Candidates { get; set; } = new List<DCDamageSourceCandidate>();
        // Whoever holds the initiative: the right answer most of the time, and
        // what the quick damage button credits without asking.
        public string SuggestedSource { get; set; } = string.Empty;

        public bool IsPlayer(string name)
        {
            return Candidates.Find(x => x.Name == name)?.IsPlayer ?? false;
        }
    }
}
