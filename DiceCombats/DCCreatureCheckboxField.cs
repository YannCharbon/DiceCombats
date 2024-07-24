using System.Collections.Generic;

namespace DiceCombats
{
    public class DCCreatureCheckboxField : DCCreatureCustomField
    {
        public override string FieldType => "Checkbox";
        public List<string> Options { get; set; } = new List<string>();
        public List<string> SelectedOptions { get; set; } = new List<string>();

        public override object GetValue()
        {
            return SelectedOptions;
        }
    }
}
