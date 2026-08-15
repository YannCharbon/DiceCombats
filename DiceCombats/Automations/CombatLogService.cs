/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Globalization;

namespace DiceCombats.Automations;

/// <summary>
/// Records what happens while a combat is played. It listens to the event stream
/// rather than being called from the UI, so anything already published ends up in
/// the log without the call sites knowing about it.
///
/// Only the specific damage, healing and initiative events are kept: the generic
/// "custom field changed" event describes the same change a second time and would
/// double every entry.
/// </summary>
public sealed class CombatLogService
{
    private readonly DiceCombatsService _combatService;
    private readonly DiceEventDispatcher _dispatcher;
    private readonly object _lock = new();

    private DCCombatLogSession? _session;
    private Guid _combatId = Guid.Empty;
    private int _sequence;
    private int _round = 1;

    public CombatLogService(DiceCombatsService combatService, DiceEventDispatcher dispatcher)
    {
        _combatService = combatService;
        _dispatcher = dispatcher;
        _dispatcher.EventPublished += OnEventPublished;
    }

    public bool IsRecording
    {
        get { lock (_lock) { return _session != null; } }
    }

    public DCCombatLogSession? CurrentSession
    {
        get { lock (_lock) { return _session; } }
    }

    public int CurrentRound
    {
        get { lock (_lock) { return _round; } }
    }

    /// <summary>
    /// Opens a session. Playing the same combat again always records a new one.
    /// </summary>
    public void StartSession(DCCombat combat)
    {
        lock (_lock)
        {
            if (_session != null) return;

            _combatId = combat.Id;
            _sequence = 0;
            _round = 1;
            _session = new DCCombatLogSession
            {
                CombatId = combat.Id,
                CombatName = combat.Name
            };
        }

        _ = _dispatcher.PublishAsync(new DiceEvent
        {
            Type = DiceEventTypes.SessionStarted,
            Category = "Combat log",
            DisplayName = "Combat started",
            Data = new() { ["combatId"] = combat.Id, ["combatName"] = combat.Name }
        });
    }

    /// <summary>
    /// Closes the session and stores it. Empty sessions are dropped: opening the
    /// timer and stopping it right away is not worth a log.
    /// </summary>
    public void EndSession()
    {
        DCCombatLogSession? finished;

        lock (_lock)
        {
            if (_session == null) return;
            finished = _session;
            finished.EndedAt = DateTimeOffset.UtcNow;
            _session = null;
            _combatId = Guid.Empty;
        }

        if (finished.Entries.Count > 0)
        {
            _combatService.AddCombatLogSession(finished);
        }

        _ = _dispatcher.PublishAsync(new DiceEvent
        {
            Type = DiceEventTypes.SessionEnded,
            Category = "Combat log",
            DisplayName = "Combat ended",
            Data = new()
            {
                ["combatId"] = finished.CombatId,
                ["combatName"] = finished.CombatName,
                ["rounds"] = finished.Rounds,
                ["entries"] = finished.Entries.Count
            }
        });
    }

    private void OnEventPublished(DiceEvent diceEvent)
    {
        lock (_lock)
        {
            if (_session == null) return;
            if (GetGuid(diceEvent, "combatId") != _combatId) return;

            switch (diceEvent.Type)
            {
                case DiceEventTypes.InitiativeNext:
                    // Back to the top of the order means a new round started.
                    if (GetInt(diceEvent, "initiativeIndex") == 0) _round++;
                    break;

                case DiceEventTypes.CreatureGotInitiative:
                    Append(diceEvent, DCCombatLogEntryTypes.Turn, GetText(diceEvent, "creatureName"), string.Empty);
                    break;

                case DiceEventTypes.DamageApplied:
                    Append(diceEvent, DCCombatLogEntryTypes.Damage, GetText(diceEvent, "sourceName"), GetText(diceEvent, "targetName"));
                    break;

                case DiceEventTypes.HealApplied:
                    Append(diceEvent, DCCombatLogEntryTypes.Heal, GetText(diceEvent, "sourceName"), GetText(diceEvent, "targetName"));
                    break;
            }
        }
    }

    private void Append(DiceEvent diceEvent, string type, string sourceName, string targetName)
    {
        if (_session == null) return;

        _session.Entries.Add(new DCCombatLogEntry
        {
            Sequence = ++_sequence,
            At = diceEvent.OccurredAt,
            Type = type,
            Round = _round,
            SourceName = sourceName,
            SourceInferred = GetBool(diceEvent, "sourceInferred"),
            TargetName = targetName,
            InstanceIndex = GetInt(diceEvent, "instanceIndex"),
            DamageKind = GetText(diceEvent, "damageKind"),
            RuleName = GetText(diceEvent, "ruleName"),
            RawAmount = GetInt(diceEvent, "rawAmount"),
            AppliedAmount = GetInt(diceEvent, "appliedAmount")
        });
    }

    private static string GetText(DiceEvent diceEvent, string key)
        => diceEvent.Data.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;

    private static int GetInt(DiceEvent diceEvent, string key)
        => int.TryParse(GetText(diceEvent, key), NumberStyles.Any, CultureInfo.InvariantCulture, out int result) ? result : 0;

    private static bool GetBool(DiceEvent diceEvent, string key)
        => bool.TryParse(GetText(diceEvent, key), out bool result) && result;

    private static Guid GetGuid(DiceEvent diceEvent, string key)
        => Guid.TryParse(GetText(diceEvent, key), out Guid result) ? result : Guid.Empty;
}
