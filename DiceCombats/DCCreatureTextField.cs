namespace DiceCombats
{
    public class DCCreatureTextField : DCCreatureCustomField
    {
        public override string FieldType => "Text";
        public override string Discriminator => nameof(DCCreatureTextField);
        public string Text { get; set; } = string.Empty;

        public override object GetValue()
        {
            return Text;
        }

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureTextField
            {
                Title = this.Title,
                Text = this.Text,
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }
    }
}
