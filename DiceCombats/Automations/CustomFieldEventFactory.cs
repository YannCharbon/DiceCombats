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

        return events;
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
