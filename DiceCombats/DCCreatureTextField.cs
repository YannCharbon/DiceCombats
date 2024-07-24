namespace DiceCombats
{
    public class DCCreatureTextField : DCCreatureCustomField
    {
        public override string FieldType => "Text";
        public string Text { get; set; } = string.Empty;

        public override object GetValue()
        {
            return Text;
        }
    }
}
