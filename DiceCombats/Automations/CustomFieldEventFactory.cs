using System.Globalization;

namespace DiceCombats.Automations;

public static class CustomFieldEventFactory
{
    public static IReadOnlyList<DiceEvent> CreateCustomFieldEvents(DCCombat combat, DCCreature creature, int instanceIndex, CustomFieldChangeInfo info)
    {
        var typedEventType = GetCustomFieldEventType(info);
        var data = BuildCustomFieldEventData(combat, creature, instanceIndex, info);

        var events = new List<DiceEvent>
        {
            new()
            {
                Type = DiceEventTypes.CustomFieldChanged,
                Category = "Custom fields",
                DisplayName = "Custom field changed",
                Data = new Dictionary<string, object?>(data)
            }
        };

        if (typedEventType != DiceEventTypes.CustomFieldChanged)
        {
            events.Add(new DiceEvent
            {
                Type = typedEventType,
                Category = "Custom fields",
                DisplayName = data.TryGetValue("fieldType", out var fieldType) ? $"{fieldType} field event" : "Custom field event",
                Data = data
            });
        }

        events.AddRange(CreateDamageEvents(combat, creature, instanceIndex, info));

        return events;
    }

    /// <summary>
    /// An attack raises one event describing the whole hit, then one event per
    /// damage line. A listener that wants to play one sound per kind subscribes
    /// to the lines, one that wants to react to the hit as a whole subscribes to
    /// the attack, and both carry the intensity needed to scale a reaction.
    /// Lines of the same attack share attackId.
    /// </summary>
    private static IReadOnlyList<DiceEvent> CreateDamageEvents(DCCombat combat, DCCreature creature, int instanceIndex, CustomFieldChangeInfo info)
    {
        var events = new List<DiceEvent>();
        if (info.Field is not DCCreatureHitPointsField hitPoints) return events;

        var lines = info.DamageLines.Where(x => x.AppliedAmount > 0 || x.RawAmount > 0).ToList();
        string attackId = Guid.NewGuid().ToString("N");

        if (lines.Count > 0)
        {
            int totalRaw = lines.Sum(x => x.RawAmount);
            int totalApplied = lines.Sum(x => x.AppliedAmount);

            var attackData = BuildDamageEventData(combat, creature, instanceIndex, info, hitPoints, null, attackId);
            attackData["damageKinds"] = string.Join(", ", lines.Select(x => x.Kind).Where(x => !string.IsNullOrEmpty(x)).Distinct());
            attackData["damageKindCount"] = lines.Select(x => x.Kind).Where(x => !string.IsNullOrEmpty(x)).Distinct().Count();
            attackData["lineCount"] = lines.Count;
            attackData["rawAmount"] = totalRaw;
            attackData["appliedAmount"] = totalApplied;
            attackData["tags"] = string.Join(", ", lines.SelectMany(x => x.Tags).Distinct());
            AddIntensity(attackData, totalApplied, hitPoints);

            events.Add(new DiceEvent
            {
                Type = DiceEventTypes.AttackApplied,
                Category = "Combat log",
                DisplayName = "Attack applied",
                Data = attackData
            });
        }

        for (int index = 0; index < lines.Count; index++)
        {
            var data = BuildDamageEventData(combat, creature, instanceIndex, info, hitPoints, lines[index], attackId);
            data["lineIndex"] = index;
            data["lineCount"] = lines.Count;
            AddIntensity(data, lines[index].AppliedAmount, hitPoints);

            events.Add(new DiceEvent
            {
                Type = DiceEventTypes.DamageApplied,
                Category = "Combat log",
                DisplayName = "Damage applied",
                Data = data
            });
        }

        int heal = ToInt(info.Data.TryGetValue("appliedHeal", out var healValue) ? healValue : null);
        if (heal > 0)
        {
            var data = BuildDamageEventData(combat, creature, instanceIndex, info, hitPoints, null, attackId);
            data["appliedAmount"] = heal;
            data["rawAmount"] = heal;
            AddIntensity(data, heal, hitPoints);

            events.Add(new DiceEvent
            {
                Type = DiceEventTypes.HealApplied,
                Category = "Combat log",
                DisplayName = "Healing applied",
                Data = data
            });
        }

        return events;
    }

    /// <summary>
    /// How hard the hit landed, as a share of what the target can take. Meant to
    /// be used directly as a volume or to pick between a light and a heavy sound.
    /// </summary>
    private static void AddIntensity(Dictionary<string, object?> data, int amount, DCCreatureHitPointsField hitPoints)
    {
        int maximum = hitPoints.EffectiveMaximumHP;
        double intensity = maximum <= 0 ? 0 : Math.Clamp(amount / (double)maximum, 0d, 1d);

        data["intensity"] = Math.Round(intensity, 3);
        data["severity"] = intensity switch
        {
            < 0.1 => "Minor",
            < 0.25 => "Moderate",
            < 0.5 => "Severe",
            _ => "Critical"
        };
    }

    private static Dictionary<string, object?> BuildDamageEventData(DCCombat combat, DCCreature creature, int instanceIndex, CustomFieldChangeInfo info, DCCreatureHitPointsField hitPoints, DCDamageLineInfo? line, string attackId)
    {
        var data = new Dictionary<string, object?>
        {
            ["combatId"] = combat.Id,
            ["combatName"] = combat.Name,
            // creatureName is what the existing creature filter matches on, and
            // for a hit that is the creature being hit.
            ["creatureName"] = creature.Name,
            ["targetName"] = creature.Name,
            ["targetIsPlayer"] = creature.IsPlayer,
            ["instanceIndex"] = instanceIndex,
            ["attackId"] = attackId,
            ["sourceName"] = info.SourceName ?? string.Empty,
            ["sourceInferred"] = info.SourceInferred,
            ["sourceIsPlayer"] = info.SourceIsPlayer,
            ["fieldTitle"] = hitPoints.Title,
            ["resultHitPoints"] = hitPoints.CurrentHP,
            ["maximumHitPoints"] = hitPoints.EffectiveMaximumHP,
            ["damageKind"] = line?.Kind ?? string.Empty,
            ["ruleName"] = line?.RuleName ?? string.Empty,
            ["rawAmount"] = line?.RawAmount ?? 0,
            ["appliedAmount"] = line?.AppliedAmount ?? 0,
            ["tags"] = line == null ? string.Empty : string.Join(", ", line.Tags)
        };

        return data;
    }

    private static int ToInt(object? value)
    {
        return int.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int result) ? result : 0;
    }

    private static Dictionary<string, object?> BuildCustomFieldEventData(DCCombat combat, DCCreature creature, int instanceIndex, CustomFieldChangeInfo info)
    {
        var data = new Dictionary<string, object?>
        {
            ["combatId"] = combat.Id,
            ["combatName"] = combat.Name,
            ["creatureId"] = creature.Id,
            ["creatureName"] = creature.Name,
            ["instanceIndex"] = instanceIndex,
            ["fieldTitle"] = info.Field.Title,
            ["fieldType"] = info.Field.FieldType,
            ["eventAction"] = info.Action,
            ["oldValue"] = info.OldValue,
            ["newValue"] = info.NewValue
        };

        AddIfNotNull(data, "changeDirection", info.ChangeDirection);
        AddIfNotNull(data, "optionIndex", info.OptionIndex);
        AddIfNotNull(data, "optionName", info.OptionName);
        AddIfNotNull(data, "rowIndex", info.RowIndex);
        AddIfNotNull(data, "rowName", info.RowName);
        AddIfNotNull(data, "columnIndex", info.ColumnIndex);
        AddIfNotNull(data, "columnName", info.ColumnName);
        AddIfNotNull(data, "statIndex", info.StatIndex);
        AddIfNotNull(data, "statName", info.StatName);
        AddIfNotNull(data, "textLabel", info.TextLabel);

        foreach (var item in info.Data)
        {
            AddIfNotNull(data, item.Key, item.Value);
        }

        var oldNumber = ToNullableDouble(info.OldValue);
        var newNumber = ToNullableDouble(info.NewValue);
        if (oldNumber.HasValue && newNumber.HasValue)
        {
            data["oldNumericValue"] = oldNumber.Value;
            data["numericValue"] = newNumber.Value;
            data["delta"] = newNumber.Value - oldNumber.Value;
            data["changeDirection"] = GetChangeDirection(oldNumber.Value, newNumber.Value);
        }

        if (info.Field is DCCreatureHitPointsField hp && hp.MaximumHP > 0)
        {
            var oldHp = oldNumber ?? hp.CurrentHP;
            var newHp = newNumber ?? hp.CurrentHP;
            data["maximumValue"] = hp.MaximumHP;
            data["oldPercentage"] = oldHp * 100d / hp.MaximumHP;
            data["percentage"] = newHp * 100d / hp.MaximumHP;
        }

        return data;
    }

    private static string GetCustomFieldEventType(CustomFieldChangeInfo info)
    {
        return info.Field switch
        {
            DCCreatureNumericField => DiceEventTypes.CustomFieldNumericChanged,
            DCCreatureHitPointsField => DiceEventTypes.CustomFieldHitPointsChanged,
            DCCreatureTextField => DiceEventTypes.CustomFieldTextChanged,
            DCCreatureCheckboxField => info.Action == "TextChanged" ? DiceEventTypes.CustomFieldCheckboxTextChanged : DiceEventTypes.CustomFieldCheckboxOptionChanged,
            DCCreatureCheckboxGridField => DiceEventTypes.CustomFieldCheckboxGridCellChanged,
            DCCreatureConditionsField => DiceEventTypes.CustomFieldConditionsOptionChanged,
            DCCreatureTextGridField => DiceEventTypes.CustomFieldTextGridCellChanged,
            DCCreatureColorField => DiceEventTypes.CustomFieldColorChanged,
            DCCreatureStatsField => DiceEventTypes.CustomFieldStatsValueChanged,
            DCCreatureDetailedPopupField => DiceEventTypes.CustomFieldDetailedPopupOpened,
            _ => DiceEventTypes.CustomFieldChanged
        };
    }

    private static void AddIfNotNull(Dictionary<string, object?> data, string key, object? value)
    {
        if (value is not null)
        {
            data[key] = value;
        }
    }

    private static double? ToNullableDouble(object? value)
    {
        if (value is null) return null;
        if (value is IConvertible convertible)
        {
            try
            {
                return convertible.ToDouble(CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static string GetChangeDirection(double oldValue, double newValue)
    {
        if (newValue > oldValue) return "Increased";
        if (newValue < oldValue) return "Decreased";
        return "Unchanged";
    }
}
