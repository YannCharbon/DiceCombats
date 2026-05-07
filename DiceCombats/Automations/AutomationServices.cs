using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DiceCombats.Automations;

public interface IDiceEventPublisher
{
    Task PublishAsync(DiceEvent diceEvent, CancellationToken cancellationToken = default);
}

public sealed class EventTemplateRenderer
{
    private static readonly Regex PlaceholderRegex = new("{{\\s*(.*?)\\s*}}", RegexOptions.Compiled);
    public string Render(string? template, DiceEvent diceEvent)
        => string.IsNullOrEmpty(template) ? string.Empty : PlaceholderRegex.Replace(template, m => ResolvePath(diceEvent, m.Groups[1].Value)?.ToString() ?? string.Empty);

    public object? ResolvePath(DiceEvent diceEvent, string path)
    {
        if (path == nameof(DiceEvent.Id)) return diceEvent.Id;
        if (path == nameof(DiceEvent.Type)) return diceEvent.Type;
        if (path == nameof(DiceEvent.OccurredAt)) return diceEvent.OccurredAt.ToString("O", CultureInfo.InvariantCulture);
        if (!path.StartsWith("Data.", StringComparison.OrdinalIgnoreCase)) return null;
        var key = path[5..];
        return diceEvent.Data.TryGetValue(key, out var value) ? value : null;
    }
}

public sealed class EventConditionEvaluator
{
    private readonly EventTemplateRenderer _renderer;
    public EventConditionEvaluator(EventTemplateRenderer renderer) => _renderer = renderer;

    public bool Evaluate(EventCondition? condition, DiceEvent diceEvent)
    {
        if (condition is null) return true;
        if (condition.All.Count > 0) return condition.All.All(x => Evaluate(x, diceEvent));
        if (string.IsNullOrWhiteSpace(condition.Operator)) return true;

        var left = _renderer.ResolvePath(diceEvent, condition.Left);
        var result = condition.Operator switch
        {
            "Exists" => left is not null,
            "Equals" => string.Equals(left?.ToString(), condition.Right, StringComparison.OrdinalIgnoreCase),
            "NotEquals" => !string.Equals(left?.ToString(), condition.Right, StringComparison.OrdinalIgnoreCase),
            "Contains" => (left?.ToString() ?? "").Contains(condition.Right, StringComparison.OrdinalIgnoreCase),
            "OneOf" => condition.Right.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(left?.ToString() ?? "", StringComparer.OrdinalIgnoreCase),
            "Below" => ToDouble(left) < ToDouble(condition.Right),
            "BelowOrEqual" => ToDouble(left) <= ToDouble(condition.Right),
            "Above" => ToDouble(left) > ToDouble(condition.Right),
            "AboveOrEqual" => ToDouble(left) >= ToDouble(condition.Right),
            _ => false
        };
        return result && EvaluateCrossing(condition, diceEvent);
    }

    public bool EvaluateTrigger(EventTriggerDefinition? trigger, DiceEvent diceEvent)
    {
        if (trigger is null) return true;

        var entry = EventTriggerCatalog.Get(trigger.Kind);
        if (entry is null) return false;
        if (!string.Equals(diceEvent.Type, entry.EventType, StringComparison.OrdinalIgnoreCase)) return false;

        if (entry.SupportsCreatureFilter && trigger.CreatureNames.Count > 0 && !trigger.CreatureNames.Contains(GetDataText(diceEvent, "creatureName"), StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(trigger.FieldTitle) && !TextEquals(GetDataText(diceEvent, "fieldTitle"), trigger.FieldTitle)) return false;
        if (!string.IsNullOrWhiteSpace(entry.FieldType) && !TextEquals(GetDataText(diceEvent, "fieldType"), entry.FieldType)) return false;
        if (!string.IsNullOrWhiteSpace(trigger.OptionName) && !TextEquals(GetDataText(diceEvent, "optionName"), trigger.OptionName)) return false;
        if (!string.IsNullOrWhiteSpace(trigger.StatName) && !TextEquals(GetDataText(diceEvent, "statName"), trigger.StatName)) return false;

        return trigger.Kind switch
        {
            EventTriggerKinds.NumericIncreased or EventTriggerKinds.HitPointsHealed or EventTriggerKinds.StatsIncreased => TextEquals(GetDataText(diceEvent, "changeDirection"), "Increased"),
            EventTriggerKinds.NumericDecreased or EventTriggerKinds.HitPointsDamaged or EventTriggerKinds.StatsDecreased => TextEquals(GetDataText(diceEvent, "changeDirection"), "Decreased"),
            EventTriggerKinds.CheckboxOptionChecked or EventTriggerKinds.CheckboxGridCellChecked => TextEquals(GetDataText(diceEvent, "eventAction"), "Checked"),
            EventTriggerKinds.CheckboxOptionUnchecked or EventTriggerKinds.CheckboxGridCellUnchecked => TextEquals(GetDataText(diceEvent, "eventAction"), "Unchecked"),
            EventTriggerKinds.ConditionAdded => TextEquals(GetDataText(diceEvent, "eventAction"), "Added"),
            EventTriggerKinds.ConditionRemoved => TextEquals(GetDataText(diceEvent, "eventAction"), "Removed"),
            EventTriggerKinds.NumericReachedMaxValue => EvaluateNumericReachedMaxValue(diceEvent),
            EventTriggerKinds.NumericThreshold or EventTriggerKinds.StatsThreshold => EvaluateTriggerThreshold(trigger, diceEvent, "oldValue", "newValue"),
            EventTriggerKinds.HitPointsThreshold => EvaluateTriggerThreshold(trigger, diceEvent, "oldPercentage", "percentage"),
            _ => true
        };
    }

    private static bool EvaluateTriggerThreshold(EventTriggerDefinition trigger, DiceEvent diceEvent, string oldKey, string newKey)
    {
        if (trigger.ThresholdValue is null) return false;

        var oldValue = ToDouble(diceEvent.Data.TryGetValue(oldKey, out var oldObj) ? oldObj : null);
        var newValue = ToDouble(diceEvent.Data.TryGetValue(newKey, out var newObj) ? newObj : null);
        var threshold = trigger.ThresholdValue.Value;
        if (double.IsNaN(newValue)) return false;

        var nowMatches = trigger.ThresholdOperator switch
        {
            "Below" => newValue < threshold,
            "BelowOrEqual" => newValue <= threshold,
            "Above" => newValue > threshold,
            "AboveOrEqual" => newValue >= threshold,
            _ => false
        };

        if (!trigger.TriggerOnlyOnCrossing) return nowMatches;
        if (double.IsNaN(oldValue)) return false;

        return trigger.ThresholdOperator switch
        {
            "Below" => oldValue >= threshold && newValue < threshold,
            "BelowOrEqual" => oldValue > threshold && newValue <= threshold,
            "Above" => oldValue <= threshold && newValue > threshold,
            "AboveOrEqual" => oldValue < threshold && newValue >= threshold,
            _ => false
        };
    }

    private static string GetDataText(DiceEvent diceEvent, string key)
    {
        return diceEvent.Data.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private static bool TextEquals(string? left, string? right)
    {
        return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EvaluateNumericReachedMaxValue(DiceEvent diceEvent)
    {
        if (!GetDataBool(diceEvent, "showMaxValue")) return false;

        var oldValue = ToDouble(diceEvent.Data.TryGetValue("oldValue", out var oldObj) ? oldObj : null);
        var newValue = ToDouble(diceEvent.Data.TryGetValue("newValue", out var newObj) ? newObj : null);
        var maxValue = ToDouble(diceEvent.Data.TryGetValue("maximumValue", out var maxObj) ? maxObj : null);
        if (double.IsNaN(oldValue) || double.IsNaN(newValue) || double.IsNaN(maxValue)) return false;

        return !ValuesEqual(oldValue, maxValue) && ValuesEqual(newValue, maxValue);
    }

    private static bool GetDataBool(DiceEvent diceEvent, string key)
    {
        if (!diceEvent.Data.TryGetValue(key, out var value)) return false;

        return value switch
        {
            bool boolValue => boolValue,
            string text => bool.TryParse(text, out var parsed) && parsed,
            _ => false
        };
    }

    private static bool ValuesEqual(double left, double right) => Math.Abs(left - right) < 0.0000001d;

    private static double ToDouble(object? value) => double.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : double.NaN;

    private static bool EvaluateCrossing(EventCondition condition, DiceEvent ev)
    {
        if (!condition.TriggerOnlyOnCrossing) return true;
        var oldVal = ToDouble(ev.Data.TryGetValue("oldPercentage", out var op) ? op : ev.Data.TryGetValue("oldValue", out var ov) ? ov : null);
        var newVal = ToDouble(ev.Data.TryGetValue("percentage", out var p) ? p : ev.Data.TryGetValue("newValue", out var nv) ? nv : null);
        var threshold = ToDouble(condition.Right);
        if (double.IsNaN(oldVal) || double.IsNaN(newVal) || double.IsNaN(threshold)) return false;
        var direction = condition.CrossingDirection ?? "Below";
        return direction == "Above" ? oldVal <= threshold && newVal > threshold : oldVal >= threshold && newVal < threshold;
    }
}

public sealed class ActionDefinitionJsonConverter : JsonConverter<ActionDefinition>
{
    public override ActionDefinition? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var obj = JsonNode.Parse(ref reader)?.AsObject() ?? throw new JsonException();
        var type = obj["Type"]?.GetValue<string>();
        return type switch
        {
            "HttpRequest" => obj.Deserialize<HttpRequestActionDefinition>(options),
            "Delay" => obj.Deserialize<DelayActionDefinition>(options),
            _ => throw new JsonException($"Unknown action type '{type}'")
        };
    }
    public override void Write(Utf8JsonWriter writer, ActionDefinition value, JsonSerializerOptions options)
    {
        if (value is HttpRequestActionDefinition http) JsonSerializer.Serialize(writer, http, options);
        else if (value is DelayActionDefinition delay) JsonSerializer.Serialize(writer, delay, options);
        else throw new JsonException($"Unknown action class '{value.GetType().Name}'");
    }
}

public sealed class DiceEventDispatcher : IDiceEventPublisher
{
    private readonly AutomationConfigurationService _configService;
    private readonly EventConditionEvaluator _conditionEvaluator;
    private readonly EventTemplateRenderer _renderer;
    private readonly HttpClient _httpClient = new();
    private readonly List<EventDeliveryLogEntry> _logs = new();
    public IReadOnlyList<EventDeliveryLogEntry> Logs => _logs;
    public event Action<EventDeliveryLogEntry>? DeliveryLogged;

    public DiceEventDispatcher(AutomationConfigurationService configService, EventConditionEvaluator conditionEvaluator, EventTemplateRenderer renderer)
    { _configService = configService; _conditionEvaluator = conditionEvaluator; _renderer = renderer; }

    public Task PublishAsync(DiceEvent diceEvent, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(() => DispatchInternalAsync(diceEvent), CancellationToken.None);
        return Task.CompletedTask;
    }

    private async Task DispatchInternalAsync(DiceEvent diceEvent)
    {
        try
        {
            var cfg = await _configService.GetConfigurationAsync();
            foreach (var binding in cfg.Bindings.Where(x => x.IsEnabled && GetBindingEventType(x) == diceEvent.Type))
            {
                if (!_conditionEvaluator.EvaluateTrigger(binding.Trigger, diceEvent)) continue;
                if (!_conditionEvaluator.Evaluate(binding.Condition, diceEvent)) continue;
                if (binding.CooldownMs > 0 && binding.LastTriggeredAt.HasValue && (DateTimeOffset.UtcNow - binding.LastTriggeredAt.Value).TotalMilliseconds < binding.CooldownMs) continue;
                binding.LastTriggeredAt = DateTimeOffset.UtcNow;
                foreach (var action in binding.Actions)
                {
                    var result = await ExecuteActionAsync(action, diceEvent, cfg);
                    Log(new EventDeliveryLogEntry { AutomationName = binding.Name, EventType = diceEvent.Type, Success = result.Success, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage });
                    if (!result.Success && !action.ContinueOnFailure) break;
                }
            }
            await _configService.SaveAsync();
        }
        catch (Exception ex) { Debug.WriteLine($"Automation dispatch failed: {ex}"); }
    }

    private static string GetBindingEventType(EventActionBinding binding)
    {
        if (binding.Trigger is not null)
        {
            return EventTriggerCatalog.Get(binding.Trigger.Kind)?.EventType ?? binding.EventType;
        }

        return binding.EventType;
    }

    private async Task<ActionExecutionResult> ExecuteActionAsync(ActionDefinition action, DiceEvent diceEvent, AutomationConfiguration cfg)
    {
        return action switch
        {
            DelayActionDefinition delayAction => await ExecuteDelayAsync(delayAction),
            HttpRequestActionDefinition httpAction => await ExecuteHttpActionAsync(httpAction, diceEvent, cfg),
            _ => new ActionExecutionResult { Success = false, ErrorMessage = $"Unsupported action type {action.Type}" }
        };
    }
    private static async Task<ActionExecutionResult> ExecuteDelayAsync(DelayActionDefinition action) { await Task.Delay(Math.Max(0, action.Milliseconds)); return new() { Success = true }; }
    private async Task<ActionExecutionResult> ExecuteHttpActionAsync(HttpRequestActionDefinition action, DiceEvent ev, AutomationConfiguration cfg)
    {
        try
        {
            var app = action.ExternalAppId.HasValue ? cfg.ExternalApps.FirstOrDefault(x => x.Id == action.ExternalAppId.Value && x.IsEnabled) : null;
            var url = _renderer.Render(action.Url, ev);
            if (app is not null && !Uri.IsWellFormedUriString(url, UriKind.Absolute)) url = new Uri(new Uri(app.BaseUrl.TrimEnd('/') + "/"), url.TrimStart('/')).ToString();
            using var cts = new CancellationTokenSource(Math.Max(100, action.TimeoutMs));
            using var request = new HttpRequestMessage(new HttpMethod(action.Method), url);
            foreach (var h in app?.DefaultHeaders ?? new()) request.Headers.TryAddWithoutValidation(h.Key, _renderer.Render(h.Value, ev));
            foreach (var h in action.Headers) request.Headers.TryAddWithoutValidation(h.Key, _renderer.Render(h.Value, ev));
            if (app?.AuthType == "BearerToken" && !string.IsNullOrWhiteSpace(app.BearerToken)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", app.BearerToken);
            if (!string.IsNullOrWhiteSpace(action.BodyTemplate)) request.Content = new StringContent(_renderer.Render(action.BodyTemplate, ev), Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(request, cts.Token);
            var responseText = await response.Content.ReadAsStringAsync(cts.Token);
            return new ActionExecutionResult { Success = response.IsSuccessStatusCode, StatusCode = (int)response.StatusCode, ErrorMessage = response.IsSuccessStatusCode ? null : response.ReasonPhrase, ResponsePreview = responseText[..Math.Min(500, responseText.Length)] };
        }
        catch (Exception ex) { return new ActionExecutionResult { Success = false, ErrorMessage = ex.Message }; }
    }
    private void Log(EventDeliveryLogEntry entry) { _logs.Insert(0, entry); if (_logs.Count > 100) _logs.RemoveAt(_logs.Count - 1); DeliveryLogged?.Invoke(entry); }
}
