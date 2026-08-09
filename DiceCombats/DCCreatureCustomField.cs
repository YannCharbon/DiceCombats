/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System;
using System.Text.Json.Serialization;

namespace DiceCombats
{
    [JsonConverter(typeof(DCCreatureCustomFieldConverter))]
    public abstract class DCCreatureCustomField
    {
        public string Title { get; set; } = "Default";
        public abstract string FieldType { get; }
        public abstract object GetValue();
        public abstract string Discriminator { get; }
        public bool SharedAcrossCreatureInstances { get; set; } = false;
        public abstract DCCreatureCustomField Clone();

        /// <summary>
        /// Copy kept when the field is saved as a reusable user custom field.
        /// Fields holding values that only make sense for one creature drop them
        /// here, so the template only carries what is common to a rule system.
        /// </summary>
        public virtual DCCreatureCustomField CloneAsTemplate()
        {
            return Clone();
        }
    }
}
