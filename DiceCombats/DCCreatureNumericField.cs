namespace DiceCombats
{
    public class DCCreatureNumericField : DCCreatureCustomField
    {
        public override string FieldType => "Numeric";
        public int Value { get; set; }

        public override object GetValue()
        {
            return Value;
        }
        public override string Discriminator => nameof(DCCreatureNumericField);

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureNumericField
            {
                Title = this.Title,
                Value = this.Value,
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }
    }
}
