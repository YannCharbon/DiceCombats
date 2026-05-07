/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

namespace DiceCombats
{
    public class DCCreatureNumericField : DCCreatureCustomField
    {
        public override string FieldType => "Numeric";
        public int Value { get; set; }
        public int MaxValue { get; set; }
        public bool ShowMaxValue { get; set; }
        public string Unit {  get; set; } = string.Empty;
        public bool ShowUnit { get; set; }

        public override object GetValue()
        {
            return Value;
        }
        public override string Discriminator => nameof(DCCreatureNumericField);

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureNumericField
            {
                Title = this.Title,
                Value = this.Value,
                MaxValue = this.MaxValue,
                ShowMaxValue = this.ShowMaxValue,
                Unit = this.Unit,
                ShowUnit = this.ShowUnit,
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }
    }
}
