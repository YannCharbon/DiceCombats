using BootstrapBlazor.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DiceCombats
{
    public class DCCreatureStatsField : DCCreatureCustomField
    {
        public override string FieldType => "Stats";
        public override string Discriminator => nameof(DCCreatureStatsField);

        private int count = 6;

        public int Count
        {
            get => count;
            set
            {
                if (value != count)
                {
                    count = value;
                    InitializeStats();
                }
            }
        }

        public List<string> StatsLabels { get; set; } = new List<string>();
        public List<int> StatsValues { get; set; } = new List<int>();
        public bool ValuesAreEditable { get; set; } = false;

        public DCCreatureStatsField()
        {
            Debug.WriteLine("DCCreatureStatsField");
            InitializeStats();
        }

        public override object GetValue()
        {
            return StatsValues;
        }

        private void InitializeStats()
        {
            while (StatsLabels.Count < Count) StatsLabels.Add($"Stat {StatsLabels.Count + 1}");
            while (StatsValues.Count < Count) StatsValues.Add(0);
        }

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureStatsField
            {
                Title = this.Title,
                Count = this.Count,
                StatsLabels = this.StatsLabels,
                StatsValues = this.StatsValues,
                ValuesAreEditable = this.ValuesAreEditable,
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }
    }
}
