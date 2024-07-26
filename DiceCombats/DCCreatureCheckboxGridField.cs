using System;
using System.Collections.Generic;

namespace DiceCombats
{
    public class DCCreatureCheckboxGridField : DCCreatureCustomField
    {
        public override string FieldType => "CheckboxGrid";

        private int rows = 3;
        private int columns = 3;

        public int Rows
        {
            get => rows;
            set
            {
                if (value != rows)
                {
                    rows = value;
                    ResizeGrid();
                }
            }
        }

        public int Columns
        {
            get => columns;
            set
            {
                if (value != columns)
                {
                    columns = value;
                    ResizeGrid();
                }
            }
        }

        public bool HasColumnHeaders { get; set; } = true;
        public bool HasRowHeaders { get; set; } = true;

        public List<string> RowHeaders { get; set; } = new List<string>();
        public List<string> ColumnHeaders { get; set; } = new List<string>();
        public bool[,] GridState { get; private set; } = default!;

        public DCCreatureCheckboxGridField()
        {
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            GridState = new bool[Rows, Columns];
            InitializeHeaders();
        }

        private void InitializeHeaders()
        {
            while (RowHeaders.Count < Rows) RowHeaders.Add($"Row {RowHeaders.Count + 1}");
            while (RowHeaders.Count > Rows) RowHeaders.RemoveAt(RowHeaders.Count - 1);

            while (ColumnHeaders.Count < Columns) ColumnHeaders.Add($"Col {ColumnHeaders.Count + 1}");
            while (ColumnHeaders.Count > Columns) ColumnHeaders.RemoveAt(ColumnHeaders.Count - 1);
        }

        private void ResizeGrid()
        {
            var oldGrid = GridState;
            GridState = new bool[Rows, Columns];

            for (int i = 0; i < Math.Min(Rows, oldGrid.GetLength(0)); i++)
            {
                for (int j = 0; j < Math.Min(Columns, oldGrid.GetLength(1)); j++)
                {
                    GridState[i, j] = oldGrid[i, j];
                }
            }

            InitializeHeaders();
        }

        public bool GetCheckboxState(int row, int column)
        {
            if (row >= 0 && row < Rows && column >= 0 && column < Columns)
            {
                return GridState[row, column];
            }
            return false;
        }

        public void SetCheckboxState(int row, int column, bool state)
        {
            if (row >= 0 && row < Rows && column >= 0 && column < Columns)
            {
                GridState[row, column] = state;
            }
        }

        public override object GetValue()
        {
            return GridState;
        }
    }
}
