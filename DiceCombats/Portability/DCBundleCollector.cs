/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Text.Json;
using DiceCombats.Automations;

namespace DiceCombats.Portability;

/// <summary>
/// Builds the bundle for each export (spec §2.2, §3.1): what the user chose,
/// plus what it needs. Everything is copied, so the local data is never changed
/// by an export.
/// </summary>
public sealed class DCBundleCollector
{
    private readonly IReadOnlyList<DCCreature> _library;
    private readonly IReadOnlyList<DCCombat> _combats;
    private readonly IReadOnlyList<DCPlayerGroup> _playerGroups;
    private readonly IReadOnlyList<DCCreatureCustomField> _fieldTemplates;
    private readonly AutomationConfiguration _automations;
    private readonly string _appVersion;

    public DCBundleCollector(IReadOnlyList<DCCreature> library, IReadOnlyList<DCCombat> combats,
                             IReadOnlyList<DCPlayerGroup> playerGroups, IReadOnlyList<DCCreatureCustomField> fieldTemplates,
                             AutomationConfiguration automations, string appVersion)
    {
        _library = library;
        _combats = combats;
        _playerGroups = playerGroups;
        _fieldTemplates = fieldTemplates;
        _automations = automations;
        _appVersion = appVersion;
    }

    /// <summary>One creature, or all of them: one root per creature.</summary>
    public DCBundle ForCreatures(IEnumerable<DCCreature> creatures)
    {
        var bundle = NewBundle();
        foreach (var creature in creatures)
        {
            bundle.Roots.Add(new BundleRoot { Kind = DCBundleKinds.Creature, Id = creature.Id });
            AddCreature(bundle, creature);
        }
        return bundle;
    }

    /// <summary>One combat, or all of them: one root per combat, with the library version of its creatures.</summary>
    public DCBundle ForCombats(IEnumerable<DCCombat> combats)
    {
        var bundle = NewBundle();
        foreach (var combat in combats)
        {
            bundle.Roots.Add(new BundleRoot { Kind = DCBundleKinds.Combat, Id = combat.Id });
            AddCombat(bundle, combat);
        }
        return bundle;
    }

    /// <summary>
    /// The whole library: every creature, combat, player group, user custom
    /// field, external app and automation, with the access tokens left out.
    /// Combat logs and pinned combats never travel.
    /// </summary>
    public DCBundle ForLibrary()
    {
        var bundle = NewBundle();
        bundle.Roots.Add(new BundleRoot { Kind = DCBundleKinds.Library });
        foreach (var creature in _library) AddCreature(bundle, creature);
        foreach (var combat in _combats) AddCombat(bundle, combat);
        foreach (var group in _playerGroups)
        {
            bundle.PlayerGroups.Add(Copy(group));
            foreach (var member in _library.Where(c => group.PlayerIds.Contains(c.Id))) AddCreature(bundle, member);
        }
        foreach (var field in _fieldTemplates) bundle.FieldTemplates.Add(field.Clone());
        foreach (var app in _automations.ExternalApps) bundle.ExternalApps.Add(WithoutTokens(app, bundle.Warnings));
        foreach (var binding in _automations.Bindings) bundle.Automations.Add(WithoutTokens(binding, bundle.Warnings));
        return bundle;
    }

    private DCBundle NewBundle() => new() { AppVersion = _appVersion, BundleId = Guid.NewGuid(), ExportedAtUtc = DateTime.UtcNow.ToString("o") };

    private static void AddCreature(DCBundle bundle, DCCreature creature)
    {
        if (bundle.Creatures.Any(c => c.Id == creature.Id)) return;
        bundle.Creatures.Add((DCCreature)creature.Clone());
    }

    // A combat ships, in Creatures, the library version of each entry. An
    // entry matched by name gets the library GUID in the exported copy only;
    // an entry matching nothing keeps its copy in the combat and is reported.
    private void AddCombat(DCBundle bundle, DCCombat combat)
    {
        if (bundle.Combats.Any(c => c.Id == combat.Id)) return;
        var copy = Copy(combat);
        for (int i = 0; i < combat.CreaturesList.Count; i++)
        {
            var entry = combat.CreaturesList[i];
            var libraryCreature = combat.FindLibraryCreatureFor(entry, _library);
            if (libraryCreature == null)
            {
                bundle.Warnings.Add(new BundleWarning
                {
                    Code = DCBundleWarningCodes.CreatureNotInLibrary, Kind = DCBundleKinds.Combat, Id = combat.Id, Detail = entry.Name,
                });
                continue;
            }
            AddCreature(bundle, libraryCreature);
            copy.CreaturesList[i].Id = libraryCreature.Id;
        }
        bundle.Combats.Add(copy);
    }

    /// <summary>
    /// Whether a header may carry a secret: Authorization, Proxy-Authorization,
    /// Cookie, or any name containing token, key or secret.
    /// </summary>
    public static bool IsSecretHeader(string name)
    {
        return name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
               || name.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase)
               || name.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
               || name.Contains("token", StringComparison.OrdinalIgnoreCase)
               || name.Contains("key", StringComparison.OrdinalIgnoreCase)
               || name.Contains("secret", StringComparison.OrdinalIgnoreCase);
    }

    private static ExternalAppDefinition WithoutTokens(ExternalAppDefinition app, List<BundleWarning> warnings)
    {
        var copy = Copy(app);
        bool changed = !string.IsNullOrEmpty(copy.BearerToken);
        copy.BearerToken = null;
        changed |= BlankSecretHeaders(copy.DefaultHeaders);
        copy.BaseUrl = WithoutUrlSecrets(copy.BaseUrl, ref changed);
        if (changed)
        {
            warnings.Add(new BundleWarning { Code = DCBundleWarningCodes.TokenRemoved, Kind = DCBundleKinds.ExternalApp, Id = app.Id, Detail = app.Name });
        }
        return copy;
    }

    private static EventActionBinding WithoutTokens(EventActionBinding binding, List<BundleWarning> warnings)
    {
        var copy = Copy(binding);
        copy.LastTriggeredAt = null;
        foreach (var action in copy.Actions.OfType<HttpRequestActionDefinition>())
        {
            bool changed = BlankSecretHeaders(action.Headers);
            action.Url = WithoutUrlSecrets(action.Url, ref changed);
            if (changed)
            {
                warnings.Add(new BundleWarning { Code = DCBundleWarningCodes.TokenRemoved, Kind = DCBundleKinds.Automation, Id = binding.Id, Detail = action.Name });
            }

            // A body is a template the user wrote: it is exported as it is, and
            // the importer is told when it may hold a secret.
            if (action.BodyTemplate != null && SecretWords.Any(w => action.BodyTemplate.Contains(w, StringComparison.OrdinalIgnoreCase)))
            {
                warnings.Add(new BundleWarning { Code = DCBundleWarningCodes.BodyKeptAsIs, Kind = DCBundleKinds.Automation, Id = binding.Id, Detail = action.Name });
            }
        }
        return copy;
    }

    private static readonly string[] SecretWords = { "token", "key", "secret" };

    /// <summary>
    /// A URL without what may be a secret: the user-info part
    /// (<c>user:password@</c>) is removed, and the value of any query parameter
    /// whose name would be a secret header is blanked. The rest, placeholders
    /// included, is left as it is.
    /// </summary>
    public static string WithoutUrlSecrets(string url, ref bool changed)
    {
        if (string.IsNullOrEmpty(url)) return url;
        string result = url;

        int scheme = result.IndexOf("://", StringComparison.Ordinal);
        int authorityStart = scheme >= 0 ? scheme + 3 : 0;
        int authorityEnd = result.IndexOfAny(new[] { '/', '?', '#' }, authorityStart);
        if (authorityEnd < 0) authorityEnd = result.Length;
        int at = scheme >= 0 && authorityEnd > authorityStart
            ? result.LastIndexOf('@', authorityEnd - 1, authorityEnd - authorityStart)
            : -1;
        if (at >= authorityStart)
        {
            result = result[..authorityStart] + result[(at + 1)..];
        }

        int query = result.IndexOf('?');
        if (query >= 0)
        {
            int fragment = result.IndexOf('#', query);
            string queryText = fragment < 0 ? result[(query + 1)..] : result[(query + 1)..fragment];
            var parts = queryText.Split('&').Select(part =>
            {
                int eq = part.IndexOf('=');
                if (eq <= 0) return part;
                string name = Uri.UnescapeDataString(part[..eq]);
                return IsSecretHeader(name) && eq < part.Length - 1 ? part[..(eq + 1)] : part;
            });
            result = result[..(query + 1)] + string.Join("&", parts) + (fragment < 0 ? "" : result[fragment..]);
        }

        if (result != url) changed = true;
        return result;
    }

    private static bool BlankSecretHeaders(Dictionary<string, string> headers)
    {
        bool changed = false;
        foreach (string name in headers.Keys.ToList())
        {
            if (IsSecretHeader(name) && !string.IsNullOrEmpty(headers[name]))
            {
                headers[name] = "";
                changed = true;
            }
        }
        return changed;
    }

    private static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
}
