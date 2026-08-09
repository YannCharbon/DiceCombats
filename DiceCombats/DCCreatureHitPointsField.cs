/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiceCombats
{
    /// <summary>
    /// Reads the legacy "not set yet" marker. Current HP used to be a uint whose
    /// unset value was uint.MaxValue, which no longer fits now that hit points
    /// can go negative, so it is mapped onto the new marker on load.
    /// </summary>
    public class DCHitPointsValueConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TryGetInt64(out long value))
            {
                if (value > int.MaxValue) return DCCreatureHitPointsField.UnsetHP;
                if (value < int.MinValue) return int.MinValue;
                return (int)value;
            }
            return 0;
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    /// <summary>
    /// Reads a damage kind that was saved before it gained a colour and an icon,
    /// when it was just its name.
    /// </summary>
    public class DCHitPointsDamageKindConverter : JsonConverter<DCHitPointsDamageKind>
    {
        public override DCHitPointsDamageKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return new DCHitPointsDamageKind { Name = reader.GetString() ?? string.Empty };
            }

            var kind = new DCHitPointsDamageKind();
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                if (doc.RootElement.TryGetProperty("Name", out JsonElement name))
                {
                    kind.Name = name.GetString() ?? string.Empty;
                }
                if (doc.RootElement.TryGetProperty("Color", out JsonElement color))
                {
                    kind.Color = color.GetString() ?? kind.Color;
                }
                if (doc.RootElement.TryGetProperty("Icon", out JsonElement icon))
                {
                    kind.Icon = icon.GetString() ?? kind.Icon;
                }
            }
            return kind;
        }

        public override void Write(Utf8JsonWriter writer, DCHitPointsDamageKind value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", value.Name);
            writer.WriteString("Color", value.Color);
            writer.WriteString("Icon", value.Icon);
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// A kind of damage the creature can take. The colour and icon are what the
    /// play mode shows on the buttons used to pick it.
    /// </summary>
    [JsonConverter(typeof(DCHitPointsDamageKindConverter))]
    public class DCHitPointsDamageKind
    {
        public string Name { get; set; } = "New kind";
        public string Color { get; set; } = "#607D8B";
        public string Icon { get; set; } = DCHitPointsIcons.DefaultIcon;

        /// <summary>
        /// Black or white, whichever stays readable on top of Color.
        /// </summary>
        [JsonIgnore]
        public string TextColor
        {
            get
            {
                string hex = (Color ?? string.Empty).TrimStart('#');
                if (hex.Length < 6
                    || !int.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int red)
                    || !int.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int green)
                    || !int.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int blue))
                {
                    return "#FFFFFF";
                }

                double luminance = (0.299 * red) + (0.587 * green) + (0.114 * blue);
                return luminance > 150 ? "#000000" : "#FFFFFF";
            }
        }

        public DCHitPointsDamageKind Clone()
        {
            return new DCHitPointsDamageKind
            {
                Name = this.Name,
                Color = this.Color,
                Icon = this.Icon
            };
        }
    }

    /// <summary>
    /// A named way to soften (or worsen) incoming damage: the raw amount is
    /// multiplied by Factor, rounded, then FlatReduction is subtracted.
    /// </summary>
    public class DCHitPointsMitigationRule
    {
        public string Name { get; set; } = "New rule";
        public double Factor { get; set; } = 1.0;
        public int FlatReduction { get; set; } = 0;
        // "Down", "Up" or "Nearest"
        public string Rounding { get; set; } = "Down";

        public DCHitPointsMitigationRule Clone()
        {
            return new DCHitPointsMitigationRule
            {
                Name = this.Name,
                Factor = this.Factor,
                FlatReduction = this.FlatReduction,
                Rounding = this.Rounding
            };
        }

        public int Apply(int amount)
        {
            double scaled = amount * Factor;
            int rounded = Rounding switch
            {
                "Up" => (int)Math.Ceiling(scaled),
                "Nearest" => (int)Math.Round(scaled, MidpointRounding.AwayFromZero),
                _ => (int)Math.Floor(scaled)
            };
            return Math.Max(0, rounded - FlatReduction);
        }
    }

    /// <summary>
    /// Binds a damage kind to a mitigation rule, optionally only when the attack
    /// carries (or lacks) a given tag, e.g. "slashing is halved unless magical".
    /// </summary>
    public class DCHitPointsMitigation
    {
        public string Kind { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string RequiredTag { get; set; } = string.Empty;
        public bool TagMustBePresent { get; set; } = true;

        public DCHitPointsMitigation Clone()
        {
            return new DCHitPointsMitigation
            {
                Kind = this.Kind,
                RuleName = this.RuleName,
                RequiredTag = this.RequiredTag,
                TagMustBePresent = this.TagMustBePresent
            };
        }
    }

    public class DCCreatureHitPointsField : DCCreatureCustomField
    {
        public const int UnsetHP = int.MinValue;

        public override string FieldType => "HitPoints";

        [JsonConverter(typeof(DCHitPointsValueConverter))]
        public int CurrentHP { get; set; } = UnsetHP;

        private uint _maxHP = 0;
        public uint MaximumHP
        {
            get { return _maxHP; }
            set
            {
                _maxHP = value;
                if (CurrentHP == UnsetHP)
                {
                    CurrentHP = (int)_maxHP;
                }
            }
        }

        // Both are effects applied during play, not part of the creature setup.
        // Absorbs damage before current HP and is never touched by healing.
        public uint TemporaryHP { get; set; } = 0;
        // Lowers the ceiling healing may reach, without touching MaximumHP.
        public uint MaximumHPReduction { get; set; } = 0;

        public List<DCHitPointsDamageKind> DamageKinds { get; set; } = new List<DCHitPointsDamageKind>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<DCHitPointsMitigationRule> MitigationRules { get; set; } = new List<DCHitPointsMitigationRule>();
        public List<DCHitPointsMitigation> Mitigations { get; set; } = new List<DCHitPointsMitigation>();

        public bool AllowNegative { get; set; } = false;

        // One step back is enough to undo a mistyped hit, and it is not saved.
        [JsonIgnore]
        public int? UndoCurrentHP { get; set; }
        [JsonIgnore]
        public uint UndoTemporaryHP { get; set; }
        [JsonIgnore]
        public bool CanUndo => UndoCurrentHP.HasValue;

        public override object GetValue()
        {
            return CurrentHP;
        }
        public override string Discriminator => nameof(DCCreatureHitPointsField);

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureHitPointsField
            {
                Title = this.Title,
                CurrentHP = this.CurrentHP,
                MaximumHP = this.MaximumHP,
                TemporaryHP = this.TemporaryHP,
                MaximumHPReduction = this.MaximumHPReduction,
                DamageKinds = this.DamageKinds.Select(x => x.Clone()).ToList(),
                Tags = new List<string>(this.Tags),
                MitigationRules = this.MitigationRules.Select(x => x.Clone()).ToList(),
                Mitigations = this.Mitigations.Select(x => x.Clone()).ToList(),
                AllowNegative = this.AllowNegative,
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }

        /// <summary>
        /// Only what a rule system shares between creatures: the damage kinds,
        /// the attack tags and the mitigation rules. Hit points and the
        /// resistances of one particular creature are left out.
        /// </summary>
        public override DCCreatureCustomField CloneAsTemplate()
        {
            return new DCCreatureHitPointsField
            {
                Title = this.Title,
                DamageKinds = this.DamageKinds.Select(x => x.Clone()).ToList(),
                Tags = new List<string>(this.Tags),
                MitigationRules = this.MitigationRules.Select(x => x.Clone()).ToList(),
                AllowNegative = this.AllowNegative,
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }

        /// <summary>
        /// Highest value healing may reach.
        /// </summary>
        [JsonIgnore]
        public int EffectiveMaximumHP => Math.Max(0, (int)MaximumHP - (int)MaximumHPReduction);

        public DCHitPointsMitigationRule? GetRule(string ruleName)
        {
            return MitigationRules.Find(x => x.Name == ruleName);
        }

        /// <summary>
        /// Finds the rule to use for a damage kind. Entries restricted to a tag
        /// win over the unrestricted ones, so a general "halved" line can be
        /// overridden by a "not halved when magical" line.
        /// </summary>
        public DCHitPointsMitigationRule? ResolveRule(string kind, List<string> tags)
        {
            var candidates = Mitigations.FindAll(x => x.Kind == kind && Matches(x, tags));
            var best = candidates.Find(x => !string.IsNullOrEmpty(x.RequiredTag)) ?? candidates.FirstOrDefault();
            return best == null ? null : GetRule(best.RuleName);
        }

        private static bool Matches(DCHitPointsMitigation mitigation, List<string> tags)
        {
            if (string.IsNullOrEmpty(mitigation.RequiredTag)) return true;
            bool present = tags.Contains(mitigation.RequiredTag);
            return present == mitigation.TagMustBePresent;
        }

        public DCHitPointsDamageKind? GetKind(string name)
        {
            return DamageKinds.Find(x => x.Name == name);
        }

        public void AddDamageKind()
        {
            DamageKinds.Add(new DCHitPointsDamageKind());
        }

        public void RemoveDamageKind(DCHitPointsDamageKind kind)
        {
            DamageKinds.Remove(kind);
            Mitigations.RemoveAll(x => x.Kind == kind.Name);
        }

        public void AddTag()
        {
            Tags.Add("New tag");
        }

        public void RemoveTag(string tag)
        {
            Tags.Remove(tag);
            Mitigations.RemoveAll(x => x.RequiredTag == tag);
        }

        public void AddMitigationRule()
        {
            MitigationRules.Add(new DCHitPointsMitigationRule());
        }

        public void RemoveMitigationRule(DCHitPointsMitigationRule rule)
        {
            MitigationRules.Remove(rule);
            Mitigations.RemoveAll(x => x.RuleName == rule.Name);
        }

        public void AddMitigation()
        {
            Mitigations.Add(new DCHitPointsMitigation
            {
                Kind = DamageKinds.FirstOrDefault()?.Name ?? string.Empty,
                RuleName = MitigationRules.FirstOrDefault()?.Name ?? string.Empty
            });
        }

        public void RemoveMitigation(DCHitPointsMitigation mitigation)
        {
            Mitigations.Remove(mitigation);
        }

        /// <summary>
        /// Fills in the rules a DnD-like game expects, so a fresh field is usable
        /// without having to type them in.
        /// </summary>
        public void AddDefaultMitigationRules()
        {
            if (MitigationRules.Count > 0) return;
            MitigationRules.Add(new DCHitPointsMitigationRule { Name = "Resistant", Factor = 0.5, Rounding = "Down" });
            MitigationRules.Add(new DCHitPointsMitigationRule { Name = "Immune", Factor = 0.0 });
            MitigationRules.Add(new DCHitPointsMitigationRule { Name = "Vulnerable", Factor = 2.0 });
        }

        /// <summary>
        /// Sets the maximum and lets the current value follow it when the creature
        /// was at full, or had no value yet. A field taken from a user template
        /// starts with both at zero, so without this it would stay at zero.
        /// </summary>
        public void SetMaximumHP(uint value)
        {
            bool wasFull = CurrentHP == UnsetHP || CurrentHP == (int)MaximumHP;
            MaximumHP = value;
            if (wasFull)
            {
                CurrentHP = (int)value;
            }
        }

        /// <summary>
        /// Full health, with the temporary pool and the maximum reduction cleared:
        /// both are effects of a past fight and do not carry into a new one.
        /// </summary>
        public void ResetForCombat()
        {
            TemporaryHP = 0;
            MaximumHPReduction = 0;
            CurrentHP = (int)MaximumHP;
            UndoCurrentHP = null;
        }

        public void SaveUndo()
        {
            UndoCurrentHP = CurrentHP;
            UndoTemporaryHP = TemporaryHP;
        }

        public void Undo()
        {
            if (UndoCurrentHP.HasValue)
            {
                CurrentHP = UndoCurrentHP.Value;
                TemporaryHP = UndoTemporaryHP;
                UndoCurrentHP = null;
            }
        }

        /// <summary>
        /// Applies damage through the temporary pool first. Returns how much was
        /// absorbed by it.
        /// </summary>
        public uint ApplyDamage(int amount)
        {
            if (amount <= 0) return 0;

            uint absorbed = Math.Min(TemporaryHP, (uint)amount);
            TemporaryHP -= absorbed;

            int remaining = amount - (int)absorbed;
            int result = CurrentHP - remaining;
            CurrentHP = AllowNegative ? result : Math.Max(0, result);

            return absorbed;
        }

        public void ApplyHeal(int amount)
        {
            if (amount <= 0) return;
            CurrentHP = Math.Min(EffectiveMaximumHP, CurrentHP + amount);
        }
    }
}
