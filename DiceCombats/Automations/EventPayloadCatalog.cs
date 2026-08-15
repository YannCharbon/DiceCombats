namespace DiceCombats.Automations;

/// <summary>
/// Which placeholders an event carries, so the editor can list what is usable
/// for the selected trigger instead of leaving it to be found in the code.
/// Kept next to the trigger catalog: an event that gains a key is expected to
/// gain it here too.
/// </summary>
public static class EventPayloadCatalog
{
    /// <summary>The event itself, available whatever the trigger is.</summary>
    public static IReadOnlyList<string> EventPlaceholders { get; } =
        ["Type", "Category", "DisplayName", "OccurredAt", "Id"];

    public static IReadOnlyList<string> ForTrigger(string triggerKind)
    {
        var entry = EventTriggerCatalog.Get(triggerKind);
        return entry is null ? [] : ForEventType(entry.EventType);
    }

    public static IReadOnlyList<string> ForEventType(string eventType)
    {
        var keys = new List<string>();

        if (eventType != DiceEventTypes.UserCustomFieldSaved && eventType != DiceEventTypes.UserCustomFieldRemoved)
        {
            keys.AddRange(["combatId", "combatName"]);
        }

        switch (eventType)
        {
            case DiceEventTypes.InitiativeNext:
                keys.AddRange(["previousCreatureId", "previousCreatureName", "previousInitiativeIndex", "currentCreatureId", "currentCreatureName", "initiativeIndex"]);
                break;

            case DiceEventTypes.CreatureGotInitiative:
            case DiceEventTypes.CreatureLostInitiative:
                keys.AddRange(["creatureId", "creatureName", "initiativeIndex"]);
                break;

            case DiceEventTypes.TimerStarted:
            case DiceEventTypes.TimerPaused:
            case DiceEventTypes.TimerStopped:
                keys.Add("durationSeconds");
                break;

            case DiceEventTypes.AttackApplied:
                keys.AddRange(DamageKeys());
                keys.AddRange(["damageKinds", "damageKindCount", "lineCount"]);
                break;

            case DiceEventTypes.DamageApplied:
                keys.AddRange(DamageKeys());
                keys.AddRange(["damageKind", "ruleName", "lineIndex", "lineCount"]);
                break;

            case DiceEventTypes.HealApplied:
                keys.AddRange(DamageKeys());
                break;

            case DiceEventTypes.SessionEnded:
                keys.AddRange(["rounds", "entries"]);
                break;

            case DiceEventTypes.SessionStarted:
            case DiceEventTypes.InitiativeSorted:
            case DiceEventTypes.InitiativeReset:
            case DiceEventTypes.CombatOpened:
            case DiceEventTypes.CombatSaved:
                break;

            case DiceEventTypes.UserCustomFieldSaved:
            case DiceEventTypes.UserCustomFieldRemoved:
                keys.AddRange(["fieldTitle", "fieldType", "isUserCustomField"]);
                break;

            default:
                keys.AddRange(CustomFieldKeys(eventType));
                break;
        }

        return keys.Select(x => $"Data.{x}").ToList();
    }

    private static IReadOnlyList<string> DamageKeys()
    {
        return
        [
            "creatureName", "targetName", "targetIsPlayer",
            "sourceName", "sourceIsPlayer", "sourceInferred",
            "rawAmount", "appliedAmount", "intensity", "severity",
            "tags", "attackId", "fieldTitle", "resultHitPoints", "maximumHitPoints"
        ];
    }

    private static IReadOnlyList<string> CustomFieldKeys(string eventType)
    {
        var keys = new List<string> { "creatureId", "creatureName", "instanceIndex", "fieldTitle", "fieldType", "eventAction", "oldValue", "newValue" };

        switch (eventType)
        {
            case DiceEventTypes.CustomFieldNumericChanged:
                keys.AddRange(["oldNumericValue", "numericValue", "delta", "changeDirection", "showMaxValue", "maximumValue", "showUnit", "unit"]);
                break;

            case DiceEventTypes.CustomFieldHitPointsChanged:
                keys.AddRange(["oldNumericValue", "numericValue", "delta", "changeDirection", "maximumValue", "oldPercentage", "percentage", "damageKinds", "rawDamage", "appliedDamage", "appliedHeal", "temporaryAbsorbed"]);
                break;

            case DiceEventTypes.CustomFieldStatsValueChanged:
                keys.AddRange(["statIndex", "statName", "oldNumericValue", "numericValue", "delta", "changeDirection"]);
                break;

            case DiceEventTypes.CustomFieldCheckboxOptionChanged:
            case DiceEventTypes.CustomFieldConditionsOptionChanged:
                keys.AddRange(["optionIndex", "optionName"]);
                break;

            case DiceEventTypes.CustomFieldCheckboxTextChanged:
                keys.AddRange(["optionIndex", "optionName", "textLabel"]);
                break;

            case DiceEventTypes.CustomFieldCheckboxGridCellChanged:
            case DiceEventTypes.CustomFieldTextGridCellChanged:
                keys.AddRange(["rowIndex", "rowName", "columnIndex", "columnName", "textLabel"]);
                break;
        }

        return keys;
    }
}
