/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

namespace DiceCombats
{
    public class DCCreatureTextField : DCCreatureCustomField
    {
        public override string FieldType => "Text";
        public override string Discriminator => nameof(DCCreatureTextField);
        public string Text { get; set; } = string.Empty;

        public override object GetValue()
        {
            return Text;
        }

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureTextField
            {
                Title = this.Title,
                Text = this.Text,
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }
    }
}
