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
    public const string TimerStarted = "combat.timer.started";
    public const string TimerPaused = "combat.timer.paused";
    public const string TimerStopped = "combat.timer.stopped";
    public const string CustomFieldChanged = "combat.customField.changed";
}

public sealed class DiceEventCatalogEntry
{
    public string Type { get; set; } = "";
    public string Category { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> CommonPlaceholders { get; set; } = new();
}

public static class DiceEventCatalog
{
    public static readonly IReadOnlyList<DiceEventCatalogEntry> Entries =
    [
        new() { Type = DiceEventTypes.InitiativeNext, Category = "Initiative", DisplayName = "Next creature in initiative", Description = "Triggered when moving to next creature.", CommonPlaceholders = ["{{Data.combatName}}", "{{Data.currentCreatureName}}", "{{Data.initiativeIndex}}"] },
        new() { Type = DiceEventTypes.InitiativeSorted, Category = "Initiative", DisplayName = "Initiative sorted", Description = "Triggered when initiative is sorted." },
        new() { Type = DiceEventTypes.InitiativeReset, Category = "Initiative", DisplayName = "Initiative reset", Description = "Triggered when initiative values are reset." },
        new() { Type = DiceEventTypes.TimerStarted, Category = "Timer", DisplayName = "Timer started", Description = "Triggered when timer starts.", CommonPlaceholders = ["{{Data.durationSeconds}}"] },
        new() { Type = DiceEventTypes.TimerPaused, Category = "Timer", DisplayName = "Timer paused", Description = "Triggered when timer pauses." },
        new() { Type = DiceEventTypes.TimerStopped, Category = "Timer", DisplayName = "Timer stopped", Description = "Triggered when timer stops." },
        new() { Type = DiceEventTypes.CustomFieldChanged, Category = "Custom fields", DisplayName = "Custom field changed", Description = "Triggered when a creature custom field changes.", CommonPlaceholders = ["{{Data.creatureName}}", "{{Data.fieldTitle}}", "{{Data.percentage}}", "{{Data.newValue}}"] }
    ];
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
    public EventCondition? Condition { get; set; }
    public List<ActionDefinition> Actions { get; set; } = new();
    public int CooldownMs { get; set; }
    public DateTimeOffset? LastTriggeredAt { get; set; }
}

public sealed class EventCondition
{
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
