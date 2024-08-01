namespace DiceCombats
{
    public class DCCreatureHitPointsField : DCCreatureCustomField
    {
        public override string FieldType => "HitPoints";
        public uint CurrentHP { get; set; } = 0;
        private uint _maxHP = 0;
        public uint MaximumHP
        {
            get { return _maxHP; }
            set
            {
                _maxHP = value;
                CurrentHP = _maxHP;
            }
        }

        public override object GetValue()
        {
            return CurrentHP;
        }
        public override string Discriminator => nameof(DCCreatureHitPointsField);

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureHitPointsField
            {
                Title = this.Title,
                CurrentHP = this.CurrentHP,
                MaximumHP = this.MaximumHP,
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }
    }
}
