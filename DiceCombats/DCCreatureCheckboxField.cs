using System.Collections.Generic;

namespace DiceCombats
{
    public class DCCreatureCheckboxField : DCCreatureCustomField
    {
        public override string FieldType => "Checkbox";
        public override string Discriminator => nameof(DCCreatureCheckboxField);
        public List<string> Labels { get; set; } = new List<string>(new string[1]);
        public List<bool> SelectedOptions { get; set; } = new List<bool>(new bool[1]);
        public List<string> TextFields { get; set; } = new List<string>(new string[1]);
        public List<string> TextFieldsLabels { get; set; } = new List<string>(new string[1]);
        public bool ShowTextFields { get; set; } = false;

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
                TextFieldsLabels = this.TextFieldsLabels,
                TextFields = this.TextFields,
                ShowTextFields = this.ShowTextFields,
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }

        public void AddOption()
        {
            Labels.Add("New option");
            SelectedOptions.Add(false);
            TextFields.Add("");
            TextFieldsLabels.Add("");
        }

        public void RemoveOption(string label)
        {
            int idx = Labels.IndexOf(label);
            if (idx != -1)
            {
                Labels.RemoveAt(idx);
                SelectedOptions.RemoveAt(idx);
                TextFields.RemoveAt(idx);
                TextFieldsLabels.RemoveAt(idx);
            }
        }
    }
}
