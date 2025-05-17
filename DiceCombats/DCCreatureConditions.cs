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
    public class DCCreatureConditionsField : DCCreatureCustomField
    {
        public override string FieldType => "DnDConditions";
        public override string Discriminator => nameof(DCCreatureConditionsField);
        public List<string> Labels { get; set; } = new List<string>{"Blinded", "Charmed", "Deafened", "Frightened", "Grappled", "Incapacitated", "Invisible", "Paralyzed", "Petrified", "Poisoned", "Prone", "Restrained", "Stunned", "Unconscious"};
        public List<bool> SelectedOptions { get; set; } = new List<bool>(new bool[14]);

        public override object GetValue()
        {
            return SelectedOptions;
        }

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureConditionsField
            {
                Title = this.Title,
                SelectedOptions = this.SelectedOptions,
                Labels = this.Labels,
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }

        public void AddOption()
        {
            Labels.Add("New option");
            SelectedOptions.Add(false);
        }

        public void RemoveOption(string label)
        {
            int idx = Labels.IndexOf(label);
            if (idx != -1)
            {
                Labels.RemoveAt(idx);
                SelectedOptions.RemoveAt(idx);
            }
        }
    }
}
