/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiceCombats
{
    public class DCCreatureCustomFieldConverter : JsonConverter<DCCreatureCustomField>
    {
        public override DCCreatureCustomField? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                if (doc.RootElement.TryGetProperty("Discriminator", out JsonElement discriminatorElement))
                {
                    string? discriminator = discriminatorElement.GetString();
                    return discriminator switch
                    {
                        nameof(DCCreatureHitPointsField) => JsonSerializer.Deserialize<DCCreatureHitPointsField>(doc.RootElement.GetRawText(), options),
                        nameof(DCCreatureNumericField) => JsonSerializer.Deserialize<DCCreatureNumericField>(doc.RootElement.GetRawText(), options),
                        nameof(DCCreatureCheckboxGridField) => JsonSerializer.Deserialize<DCCreatureCheckboxGridField>(doc.RootElement.GetRawText(), options),
                        nameof(DCCreatureCheckboxField) => JsonSerializer.Deserialize<DCCreatureCheckboxField>(doc.RootElement.GetRawText(), options),
                        nameof(DCCreatureTextField) => JsonSerializer.Deserialize<DCCreatureTextField>(doc.RootElement.GetRawText(), options),
                        nameof(DCCreatureTextGridField) => JsonSerializer.Deserialize<DCCreatureTextGridField>(doc.RootElement.GetRawText(), options),
                        nameof(DCCreatureColorField) => JsonSerializer.Deserialize<DCCreatureColorField>(doc.RootElement.GetRawText(), options),
                        nameof(DCCreatureDnDConditionsField) => JsonSerializer.Deserialize<DCCreatureDnDConditionsField>(doc.RootElement.GetRawText(), options),
                        nameof(DCCreatureStatsField) => JsonSerializer.Deserialize<DCCreatureStatsField>(doc.RootElement.GetRawText(), options),
                        _ => throw new JsonException($"Unknown discriminator: {discriminator}")
                    };
                }
            }
            throw new JsonException("Discriminator property not found.");
        }

        public override void Write(Utf8JsonWriter writer, DCCreatureCustomField value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }
    }
}
