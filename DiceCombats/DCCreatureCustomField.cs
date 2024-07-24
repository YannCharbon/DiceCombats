using System;

namespace DiceCombats
{
    public abstract class DCCreatureCustomField
    {
        public string Title { get; set; } = "Default";
        public abstract string FieldType { get; }
        public abstract object GetValue();
    }
}
