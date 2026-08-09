/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiceCombats
{
    /// <summary>
    /// A named set of player characters (e.g. "Tuesday table") used as a quick
    /// add in the creatures selector, so a whole party can be dropped into a
    /// combat in one click instead of being picked one by one.
    /// Members are stored by creature id: renaming a player keeps the group
    /// intact, and a deleted player simply drops out when the group resolves.
    /// </summary>
    public class DCPlayerGroup
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Name { get; set; } = string.Empty;
        public List<Guid> PlayerIds { get; set; } = new List<Guid>();

        public DCPlayerGroup(string name)
        {
            Name = name;
            Id = Guid.NewGuid();
        }
    }
}
