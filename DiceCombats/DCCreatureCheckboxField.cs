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
    public class DCCreatureCheckboxField : DCCreatureCustomField
    {
        public override string FieldType => "Checkbox";
        public override string Discriminator => nameof(DCCreatureCheckboxField);
        public List<string> Labels { get; set; } = new List<string>(new string[1]);
        public List<bool> SelectedOptions { get; set; } = new List<bool>(new bool[1]);
        public List<string> TextFields { get; set; } = new List<string>(new string[1]);
        public List<string> TextFieldsLabels { get; set; } = new List<string>(new string[1]);
        public bool ShowFieldName { get; set; } = false;
        public bool ShowTextFields { get; set; } = false;

        /// <summary>
        /// When set, a ticked option lights its whole cell — the option's name and its
        /// text box included, not just the tick box — so an active option is obvious at a
        /// glance on a crowded combat strip. Field-level rather than per-option, so there
        /// is no fifth parallel list to keep in step with <see cref="Labels"/>.
        /// Absent from older creatures.json, where it lands on false and nothing changes.
        /// </summary>
        public bool HighlightWhenChecked { get; set; } = false;

        public override object GetValue()
        {
            return SelectedOptions;
        }

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureCheckboxField
            {
                Title = this.Title,
                SelectedOptions = new List<bool>(this.SelectedOptions),
                Labels = new List<string>(this.Labels),
                TextFieldsLabels = new List<string>(this.TextFieldsLabels),
                TextFields = new List<string>(this.TextFields),
                ShowFieldName = this.ShowFieldName,
                ShowTextFields = this.ShowTextFields,
                HighlightWhenChecked = this.HighlightWhenChecked,
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }

        public void AddOption()
        {
            Labels.Add("New option");
            SelectedOptions.Add(false);
            TextFields.Add("");
            TextFieldsLabels.Add("");
        }

        public void RemoveOption(string label)
        {
            int idx = Labels.IndexOf(label);
            if (idx != -1)
            {
                Labels.RemoveAt(idx);
                SelectedOptions.RemoveAt(idx);
                TextFields.RemoveAt(idx);
                TextFieldsLabels.RemoveAt(idx);
            }
        }
    }
}
