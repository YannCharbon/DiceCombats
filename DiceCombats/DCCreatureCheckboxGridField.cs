/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DiceCombats
{
    public class DCCreatureCheckboxGridField : DCCreatureCustomField
    {
        public override string FieldType => "CheckboxGrid";
        public override string Discriminator => nameof(DCCreatureCheckboxGridField);

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
        public List<List<bool>> GridState { get; set; } = new List<List<bool>>();

        public DCCreatureCheckboxGridField()
        {
            Debug.WriteLine("DCCreatureCheckboxGridField");
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            for (int i = 0; i < rows; i++)
            {
                GridState.Add(new List<bool>( new bool[columns]));
            }
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
            GridState = new List<List<bool>>();
            for (int i = 0; i < rows; i++)
            {
                GridState.Add(new List<bool>(new bool[columns]));
            }

            for (int i = 0; i < Math.Min(Rows, oldGrid.Count); i++)
            {
                for (int j = 0; j < Math.Min(Columns, oldGrid[0].Count); j++)
                {
                    GridState[i][j] = oldGrid[i][j];
                }
            }

            InitializeHeaders();
        }

        public override object GetValue()
        {
            return GridState;
        }

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureCheckboxGridField
            {
                Title = this.Title,
                Rows = this.Rows,
                Columns = this.Columns,
                HasRowHeaders = this.HasRowHeaders,
                RowHeaders = new List<string>(this.RowHeaders),
                HasColumnHeaders = this.HasColumnHeaders,
                ColumnHeaders = new List<string>(this.ColumnHeaders),
                GridState = new List<List<bool>>(this.GridState),
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }
    }
}
