using System.Drawing;

namespace DiceCombats
{
    public class DCCreatureColorField : DCCreatureCustomField
    {
        public override string FieldType => "Color";
        public string Value { get; set; } = string.Empty;

        public override object GetValue()
        {
            return Value;
        }
        public override string Discriminator => nameof(DCCreatureColorField);
    }
}
