using System.Collections.Generic;

namespace DiceCombats
{
    public class DCCreatureCheckboxField : DCCreatureCustomField
    {
        public override string FieldType => "Checkbox";
        public override string Discriminator => nameof(DCCreatureCheckboxField);        
        public List<string> Labels { get; set; } = new List<string>(new string[1]);
        public List<bool> SelectedOptions { get; set; } = new List<bool>(new bool[1]);

        public override object GetValue()
        {
            return SelectedOptions;
        }

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureCheckboxField
            {
                Title = this.Title,
                SelectedOptions = this.SelectedOptions,
                Labels = this.Labels,
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }

        public void AddOption()
        {
            Labels.Add("New option");
            SelectedOptions.Add(false);
        }

        public void RemoveOption(string label)
        {
            int idx = Labels.IndexOf(label);
            if (idx != -1)
            {
                Labels.RemoveAt(idx);
                SelectedOptions.RemoveAt(idx);
            }
        }
    }
}
