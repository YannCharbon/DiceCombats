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
    public class DCCreatureTextGridField : DCCreatureCustomField
    {
        public override string FieldType => "TextGrid";
        public override string Discriminator => nameof(DCCreatureTextGridField);

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
        public List<List<bool>> HasTextLabels { get; set; } = new List<List<bool>>();
        public List<List<string>> TextLabels { get; set; } = new List<List<string>>();
        public List<List<string>> TextValues { get; set; } = new List<List<string>>();

        public DCCreatureTextGridField()
        {
            Debug.WriteLine("DCCreatureTextGridField");
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            for (int i = 0; i < rows; i++)
            {
                HasTextLabels.Add(new List<bool>(new bool[columns]));
                TextLabels.Add(new List<string>(new string[columns]));
                TextValues.Add(new List<string>( new string[columns]));
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
            var oldTextValues = TextValues;
            var oldHasTextLabels = HasTextLabels;
            var oldTextLabels = TextLabels;
            HasTextLabels = new List<List<bool>>();
            TextLabels = new List<List<string>>();
            TextValues = new List<List<string>>();
            for (int i = 0; i < rows; i++)
            {
                HasTextLabels.Add(new List<bool>(new bool[columns]));
                TextLabels.Add(new List<string>(new string[columns]));
                TextValues.Add(new List<string>(new string[columns]));
            }

            for (int i = 0; i < Math.Min(Rows, oldTextValues.Count); i++)
            {
                for (int j = 0; j < Math.Min(Columns, oldTextValues[0].Count); j++)
                {
                    HasTextLabels[i][j] = oldHasTextLabels[i][j];
                    TextValues[i][j] = oldTextValues[i][j];
                    TextLabels[i][j] = oldTextLabels[i][j];

                }
            }

            InitializeHeaders();
        }

        public override object GetValue()
        {
            return TextValues;
        }

        public override DCCreatureCustomField Clone()
        {
            return new DCCreatureTextGridField
            {
                Title = this.Title,
                Rows = this.Rows,
                Columns = this.Columns,
                HasRowHeaders = this.HasRowHeaders,
                RowHeaders = new List<string>(this.RowHeaders),
                HasColumnHeaders = this.HasColumnHeaders,
                ColumnHeaders = new List<string>(this.ColumnHeaders),
                HasTextLabels = this.HasTextLabels,
                TextLabels = new List<List<string>>(this.TextLabels),
                TextValues = new List<List<string>>(this.TextValues),
                SharedAcrossCreatureInstances = this.SharedAcrossCreatureInstances
            };
        }
    }
}
