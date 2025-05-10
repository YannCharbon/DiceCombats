/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Drawing;

namespace DiceCombats
{
    public class DCCreatureColorField : DCCreatureCustomField
    {
        public override string FieldType => "Color";
        public string Value { get; set; } = string.Empty;

        public override object GetValue()
        {
            return Value;
        }
        public override string Discriminator => nameof(DCCreatureColorField);

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureColorField
            {
                Title = this.Title,
                Value = this.Value,
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }
    }
}
