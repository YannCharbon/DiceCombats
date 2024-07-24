using System.Collections.Generic;

namespace DiceCombats
{
    public class DCCreatureCheckboxGridField : DCCreatureCustomField
    {
        public override string FieldType => "CheckboxArray";
        public int HorizontalCount {
            get { return _horizontalCount; }
            set {
                _horizontalCount = value;
                HorizontalLabels.Clear();
                for (int i = 0; i < _horizontalCount; i++)
                {
                    HorizontalLabels.Add("Label");
                }
            }
        }
        public int VerticalCount
        {
            get { return _verticalCount; }
            set
            {
                _verticalCount = value;
                VerticalLabels.Clear();
                for (int i = 0; i < _verticalCount; i++) {
                    VerticalLabels.Add("Label");
                }
            }
        }
        public List<string> HorizontalLabels { get; set; } = new List<string> { "A", "B", "C" };
        public List<string> VerticalLabels { get; set; } = new List<string> { "A", "B", "C" };
        public bool HasHorizontalLabels { get;set; } = true;
        public bool HasVerticalLabels { get; set; } = true;
        public List<List<bool>> SelectedOptions { get; set; } = new List<List<bool>>();

        private int _horizontalCount = 3;
        private int _verticalCount = 3;

        public override object GetValue()
        {
            return SelectedOptions;
        }
    }
}
