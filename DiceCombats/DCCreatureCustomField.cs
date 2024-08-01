using System;
using System.Text.Json.Serialization;

namespace DiceCombats
{
    [JsonConverter(typeof(DCCreatureCustomFieldConverter))]
    public abstract class DCCreatureCustomField
    {
        public string Title { get; set; } = "Default";
        public abstract string FieldType { get; }
        public abstract object GetValue();
        public abstract string Discriminator { get; }
        public bool SharedAcrossCreatureInstances { get; set; } = true;
        public abstract DCCreatureCustomField Clone();
    }
}
