using System.Globalization;
using System.Text.Json.Serialization;

namespace DiceCombats.Automations;

public sealed class DiceEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "";
    public string Category { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object?> Data { get; set; } = new();
}

public static class DiceEventTypes
{
    public const string CombatOpened = "combat.opened";
    public const string CombatSaved = "combat.saved";
    public const string InitiativeSorted = "combat.initiative.sorted";
    public const string InitiativeReset = "combat.initiative.reset";
    public const string InitiativeNext = "combat.initiative.next";
    public const string CreatureGotInitiative = "combat.creature.initiative.got";
    public const string CreatureLostInitiative = "combat.creature.initiative.lost";
    public const string TimerStarted = "combat.timer.started";
    public const string TimerPaused = "combat.timer.paused";
    public const string TimerStopped = "combat.timer.stopped";
    public const string CustomFieldChanged = "combat.customField.changed";
    public const string CustomFieldNumericChanged = "combat.customField.numeric.changed";
    public const string CustomFieldHitPointsChanged = "combat.customField.hitPoints.changed";
    public const string CustomFieldTextChanged = "combat.customField.text.changed";
    public const string CustomFieldCheckboxOptionChanged = "combat.customField.checkbox.optionChanged";
    public const string CustomFieldCheckboxTextChanged = "combat.customField.checkbox.textChanged";
    public const string CustomFieldCheckboxGridCellChanged = "combat.customField.checkboxGrid.cellChanged";
    public const string CustomFieldConditionsOptionChanged = "combat.customField.conditions.optionChanged";
    public const string CustomFieldTextGridCellChanged = "combat.customField.textGrid.cellChanged";
    public const string CustomFieldColorChanged = "combat.customField.color.changed";
    public const string CustomFieldStatsValueChanged = "combat.customField.stats.valueChanged";
    public const string CustomFieldDetailedPopupOpened = "combat.customField.detailedPopup.opened";
    public const string UserCustomFieldSaved = "user.customField.saved";
    public const string UserCustomFieldRemoved = "user.customField.removed";
}

public static class EventTriggerKinds
{
    public const string InitiativeNext = "InitiativeNext";
    public const string InitiativeSorted = "InitiativeSorted";
    public const string InitiativeReset = "InitiativeReset";
    public const string CreatureGotInitiative = "CreatureGotInitiative";
    public const string CreatureLostInitiative = "CreatureLostInitiative";
    public const string TimerStarted = "TimerStarted";
    public const string TimerPaused = "TimerPaused";
    public const string TimerStopped = "TimerStopped";
    public const string AnyCustomFieldChanged = "AnyCustomFieldChanged";
    public const string NumericChanged = "NumericChanged";
    public const string NumericIncreased = "NumericIncreased";
    public const string NumericDecreased = "NumericDecreased";
    public const string NumericThreshold = "NumericThreshold";
    public const string NumericReachedMaxValue = "NumericReachedMaxValue";
    public const string HitPointsChanged = "HitPointsChanged";
    public const string HitPointsDamaged = "HitPointsDamaged";
    public const string HitPointsHealed = "HitPointsHealed";
    public const string HitPointsThreshold = "HitPointsThreshold";
    public const string CheckboxOptionChanged = "CheckboxOptionChanged";
    public const string CheckboxOptionChecked = "CheckboxOptionChecked";
    public const string CheckboxOptionUnchecked = "CheckboxOptionUnchecked";
    public const string CheckboxTextChanged = "CheckboxTextChanged";
    public const string ConditionsOptionChanged = "ConditionsOptionChanged";
    public const string ConditionAdded = "ConditionAdded";
    public const string ConditionRemoved = "ConditionRemoved";
    public const string CheckboxGridCellChanged = "CheckboxGridCellChanged";
    public const string CheckboxGridCellChecked = "CheckboxGridCellChecked";
    public const string CheckboxGridCellUnchecked = "CheckboxGridCellUnchecked";
    public const string TextChanged = "TextChanged";
    public const string TextGridCellChanged = "TextGridCellChanged";
    public const string ColourChanged = "ColourChanged";
    public const string StatsChanged = "StatsChanged";
    public const string StatsIncreased = "StatsIncreased";
    public const string StatsDecreased = "StatsDecreased";
    public const string StatsThreshold = "StatsThreshold";
    public const string DetailedPopupOpened = "DetailedPopupOpened";
    public const string UserCustomFieldSaved = "UserCustomFieldSaved";
    public const string UserCustomFieldRemoved = "UserCustomFieldRemoved";
}

public sealed class EventTriggerCatalogEntry
{
    public string Kind { get; set; } = "";
    public string EventType { get; set; } = "";
    public string Category { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string FieldType { get; set; } = "";
    public bool SupportsCreatureFilter { get; set; }
    public bool NeedsField { get; set; }
    public bool NeedsOption { get; set; }
    public bool NeedsStat { get; set; }
    public bool NeedsThreshold { get; set; }
    public string ThresholdLabel { get; set; } = "Value";
}

public static class EventTriggerCatalog
{
    public static readonly IReadOnlyList<EventTriggerCatalogEntry> Entries =
    [
        new() { Kind = EventTriggerKinds.InitiativeNext, EventType = DiceEventTypes.InitiativeNext, Category = "Initiative", DisplayName = "Next creature in initiative" },
        new() { Kind = EventTriggerKinds.InitiativeSorted, EventType = DiceEventTypes.InitiativeSorted, Category = "Initiative", DisplayName = "Initiative sorted" },
        new() { Kind = EventTriggerKinds.InitiativeReset, EventType = DiceEventTypes.InitiativeReset, Category = "Initiative", DisplayName = "Initiative reset" },
        new() { Kind = EventTriggerKinds.CreatureGotInitiative, EventType = DiceEventTypes.CreatureGotInitiative, Category = "Creatures", DisplayName = "Creature got initiative", SupportsCreatureFilter = true },
        new() { Kind = EventTriggerKinds.CreatureLostInitiative, EventType = DiceEventTypes.CreatureLostInitiative, Category = "Creatures", DisplayName = "Creature lost initiative", SupportsCreatureFilter = true },
        new() { Kind = EventTriggerKinds.TimerStarted, EventType = DiceEventTypes.TimerStarted, Category = "Combat timer", DisplayName = "Timer started" },
        new() { Kind = EventTriggerKinds.TimerPaused, EventType = DiceEventTypes.TimerPaused, Category = "Combat timer", DisplayName = "Timer paused" },
        new() { Kind = EventTriggerKinds.TimerStopped, EventType = DiceEventTypes.TimerStopped, Category = "Combat timer", DisplayName = "Timer stopped" },
        new() { Kind = EventTriggerKinds.AnyCustomFieldChanged, EventType = DiceEventTypes.CustomFieldChanged, Category = "Custom fields", DisplayName = "Custom field changed", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.NumericChanged, EventType = DiceEventTypes.CustomFieldNumericChanged, Category = "Custom fields", DisplayName = "Numeric field changed", FieldType = "Numeric", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.NumericIncreased, EventType = DiceEventTypes.CustomFieldNumericChanged, Category = "Custom fields", DisplayName = "Numeric field increased", FieldType = "Numeric", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.NumericDecreased, EventType = DiceEventTypes.CustomFieldNumericChanged, Category = "Custom fields", DisplayName = "Numeric field decreased", FieldType = "Numeric", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.NumericThreshold, EventType = DiceEventTypes.CustomFieldNumericChanged, Category = "Custom fields", DisplayName = "Numeric field crossed a value", FieldType = "Numeric", SupportsCreatureFilter = true, NeedsField = true, NeedsThreshold = true, ThresholdLabel = "Numeric value" },
        new() { Kind = EventTriggerKinds.NumericReachedMaxValue, EventType = DiceEventTypes.CustomFieldNumericChanged, Category = "Custom fields", DisplayName = "Numeric field reached max value", FieldType = "Numeric", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.HitPointsChanged, EventType = DiceEventTypes.CustomFieldHitPointsChanged, Category = "Custom fields", DisplayName = "Hit points changed", FieldType = "HitPoints", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.HitPointsDamaged, EventType = DiceEventTypes.CustomFieldHitPointsChanged, Category = "Custom fields", DisplayName = "Hit points decreased", FieldType = "HitPoints", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.HitPointsHealed, EventType = DiceEventTypes.CustomFieldHitPointsChanged, Category = "Custom fields", DisplayName = "Hit points increased", FieldType = "HitPoints", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.HitPointsThreshold, EventType = DiceEventTypes.CustomFieldHitPointsChanged, Category = "Custom fields", DisplayName = "Hit points crossed a percentage", FieldType = "HitPoints", SupportsCreatureFilter = true, NeedsField = true, NeedsThreshold = true, ThresholdLabel = "Hit points (%)" },
        new() { Kind = EventTriggerKinds.CheckboxOptionChanged, EventType = DiceEventTypes.CustomFieldCheckboxOptionChanged, Category = "Custom fields", DisplayName = "Checkbox option changed", FieldType = "Checkbox", SupportsCreatureFilter = true, NeedsField = true, NeedsOption = true },
        new() { Kind = EventTriggerKinds.CheckboxOptionChecked, EventType = DiceEventTypes.CustomFieldCheckboxOptionChanged, Category = "Custom fields", DisplayName = "Checkbox option checked", FieldType = "Checkbox", SupportsCreatureFilter = true, NeedsField = true, NeedsOption = true },
        new() { Kind = EventTriggerKinds.CheckboxOptionUnchecked, EventType = DiceEventTypes.CustomFieldCheckboxOptionChanged, Category = "Custom fields", DisplayName = "Checkbox option unchecked", FieldType = "Checkbox", SupportsCreatureFilter = true, NeedsField = true, NeedsOption = true },
        new() { Kind = EventTriggerKinds.CheckboxTextChanged, EventType = DiceEventTypes.CustomFieldCheckboxTextChanged, Category = "Custom fields", DisplayName = "Checkbox text changed", FieldType = "Checkbox", SupportsCreatureFilter = true, NeedsField = true, NeedsOption = true },
        new() { Kind = EventTriggerKinds.ConditionsOptionChanged, EventType = DiceEventTypes.CustomFieldConditionsOptionChanged, Category = "Custom fields", DisplayName = "Condition option changed", FieldType = "DnDConditions", SupportsCreatureFilter = true, NeedsField = true, NeedsOption = true },
        new() { Kind = EventTriggerKinds.ConditionAdded, EventType = DiceEventTypes.CustomFieldConditionsOptionChanged, Category = "Custom fields", DisplayName = "Condition option added", FieldType = "DnDConditions", SupportsCreatureFilter = true, NeedsField = true, NeedsOption = true },
        new() { Kind = EventTriggerKinds.ConditionRemoved, EventType = DiceEventTypes.CustomFieldConditionsOptionChanged, Category = "Custom fields", DisplayName = "Condition option removed", FieldType = "DnDConditions", SupportsCreatureFilter = true, NeedsField = true, NeedsOption = true },
        new() { Kind = EventTriggerKinds.CheckboxGridCellChanged, EventType = DiceEventTypes.CustomFieldCheckboxGridCellChanged, Category = "Custom fields", DisplayName = "Checkbox grid cell changed", FieldType = "CheckboxGrid", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.CheckboxGridCellChecked, EventType = DiceEventTypes.CustomFieldCheckboxGridCellChanged, Category = "Custom fields", DisplayName = "Checkbox grid cell checked", FieldType = "CheckboxGrid", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.CheckboxGridCellUnchecked, EventType = DiceEventTypes.CustomFieldCheckboxGridCellChanged, Category = "Custom fields", DisplayName = "Checkbox grid cell unchecked", FieldType = "CheckboxGrid", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.TextChanged, EventType = DiceEventTypes.CustomFieldTextChanged, Category = "Custom fields", DisplayName = "Text field changed", FieldType = "Text", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.TextGridCellChanged, EventType = DiceEventTypes.CustomFieldTextGridCellChanged, Category = "Custom fields", DisplayName = "Text grid cell changed", FieldType = "TextGrid", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.ColourChanged, EventType = DiceEventTypes.CustomFieldColorChanged, Category = "Custom fields", DisplayName = "Colour field changed", FieldType = "Color", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.StatsChanged, EventType = DiceEventTypes.CustomFieldStatsValueChanged, Category = "Custom fields", DisplayName = "Stat value changed", FieldType = "Stats", SupportsCreatureFilter = true, NeedsField = true, NeedsStat = true },
        new() { Kind = EventTriggerKinds.StatsIncreased, EventType = DiceEventTypes.CustomFieldStatsValueChanged, Category = "Custom fields", DisplayName = "Stat value increased", FieldType = "Stats", SupportsCreatureFilter = true, NeedsField = true, NeedsStat = true },
        new() { Kind = EventTriggerKinds.StatsDecreased, EventType = DiceEventTypes.CustomFieldStatsValueChanged, Category = "Custom fields", DisplayName = "Stat value decreased", FieldType = "Stats", SupportsCreatureFilter = true, NeedsField = true, NeedsStat = true },
        new() { Kind = EventTriggerKinds.StatsThreshold, EventType = DiceEventTypes.CustomFieldStatsValueChanged, Category = "Custom fields", DisplayName = "Stat crossed a value", FieldType = "Stats", SupportsCreatureFilter = true, NeedsField = true, NeedsStat = true, NeedsThreshold = true, ThresholdLabel = "Stat value" },
        new() { Kind = EventTriggerKinds.DetailedPopupOpened, EventType = DiceEventTypes.CustomFieldDetailedPopupOpened, Category = "Custom fields", DisplayName = "Detailed popup opened", FieldType = "DetailedPopup", SupportsCreatureFilter = true, NeedsField = true },
        new() { Kind = EventTriggerKinds.UserCustomFieldSaved, EventType = DiceEventTypes.UserCustomFieldSaved, Category = "Custom fields", DisplayName = "User custom field saved", NeedsField = true },
        new() { Kind = EventTriggerKinds.UserCustomFieldRemoved, EventType = DiceEventTypes.UserCustomFieldRemoved, Category = "Custom fields", DisplayName = "User custom field removed", NeedsField = true }
    ];

    public static EventTriggerCatalogEntry? Get(string kind)
    {
        return Entries.FirstOrDefault(x => string.Equals(x.Kind, kind, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class EventTriggerDefinition
{
    public string Kind { get; set; } = EventTriggerKinds.InitiativeNext;
    public List<string> CreatureNames { get; set; } = new();
    public string FieldTitle { get; set; } = "";
    public string OptionName { get; set; } = "";
    public string StatName { get; set; } = "";
    public string ThresholdOperator { get; set; } = "Below";
    public double? ThresholdValue { get; set; }
    public bool TriggerOnlyOnCrossing { get; set; } = true;
}

public sealed class ExternalAppDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    public string AuthType { get; set; } = "None";
    public string? BearerToken { get; set; }
}

public sealed class EventActionBinding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public string EventType { get; set; } = "";
    public EventTriggerDefinition? Trigger { get; set; }
    public EventCondition? Condition { get; set; }
    public List<ActionDefinition> Actions { get; set; } = new();
    public int CooldownMs { get; set; }
    public DateTimeOffset? LastTriggeredAt { get; set; }
}

public sealed class EventCondition
{
    public List<EventCondition> All { get; set; } = new();
    public string Left { get; set; } = "";
    public string Operator { get; set; } = "";
    public string Right { get; set; } = "";
    public bool TriggerOnlyOnCrossing { get; set; }
    public string? CrossingDirection { get; set; }
}

[JsonConverter(typeof(ActionDefinitionJsonConverter))]
public abstract class ActionDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool ContinueOnFailure { get; set; }
}

public sealed class HttpRequestActionDefinition : ActionDefinition
{
    public Guid? ExternalAppId { get; set; }
    public string Method { get; set; } = "POST";
    public string Url { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? BodyTemplate { get; set; }
    public int TimeoutMs { get; set; } = 2000;
}

public sealed class DelayActionDefinition : ActionDefinition
{
    public int Milliseconds { get; set; } = 100;
}

public sealed class ActionExecutionResult
{
    public bool Success { get; set; }
    public int? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResponsePreview { get; set; }
}

public sealed class EventDeliveryLogEntry
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string AutomationName { get; set; } = "";
    public string EventType { get; set; } = "";
    public bool Success { get; set; }
    public int? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class AutomationConfiguration
{
    public List<ExternalAppDefinition> ExternalApps { get; set; } = new();
    public List<EventActionBinding> Bindings { get; set; } = new();
}
