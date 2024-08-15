using System.Collections.Generic;

namespace DiceCombats
{
    public class DCCreatureDnDConditionsField : DCCreatureCustomField
    {
        public override string FieldType => "DnDConditions";
        public override string Discriminator => nameof(DCCreatureCheckboxField);
        public List<string> Labels { get; set; } = new List<string>{"Blinded", "Charmed", "Deafened", "Frightened", "Grappled", "Incapacitated", "Invisible", "Paralyzed", "Petrified", "Poisoned", "Prone", "Restrained", "Stunned", "Unconscious"};
        public List<bool> SelectedOptions { get; set; } = new List<bool>(new bool[14]);

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
    }
}
