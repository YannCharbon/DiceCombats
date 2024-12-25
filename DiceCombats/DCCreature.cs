using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MudBlazor;

namespace DiceCombats
{
    public class DCCreature : ICloneable
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Name { get; private set; } = string.Empty;

        [JsonInclude]
        public string ImageSheetBas64 { get; private set; } = string.Empty;
        [JsonInclude]
        public string HtmlSheet { get; private set; } = string.Empty;

        public List<DCCreatureCustomField> CustomFields { get; set; } = new List<DCCreatureCustomField>();
        public uint InCombatInstanceCount { get; set; } = 1;
        public bool IsPlayer { get; set; } = false;
        public bool IsManual { get; set; } = false;
        public int InitiativeRank { get; set; } = 0;
        public int InitiativeRoll { get; set; } = 0;

        public DCCreature(string name)
        {
            Name = name;
            Id = Guid.NewGuid();
        }

        public void SetImageSheetBase64(string imageSheetBase64)
        {
            ImageSheetBas64 = imageSheetBase64;
        }

        public void SetHtmlSheet(string htmlSheet)
        {
            HtmlSheet = htmlSheet;
        }

        public string SerializeToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public object Clone()
        {
            var json = JsonSerializer.Serialize(this);
            return JsonSerializer.Deserialize<DCCreature>(json)!;
        }
    }
}
