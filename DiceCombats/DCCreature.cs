using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace DiceCombats
{
    public class DCCreature
    {
        public Guid Id { get; private set; } = Guid.Empty;
        public string Name { get; private set; } = string.Empty;
        public int MaxHitPoints { get; set; } = 0;
        public int CurrentHitPoints { get; set; } = 0;
        public string ImageSheetBas64 { get; private set; } = string.Empty;
        public string HtmlSheet { get; private set; } = string.Empty;

        public List<DCCreatureCustomField> CustomFields { get; set; } = new List<DCCreatureCustomField>();

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
    }
}
