using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PickleCalLG.Meters;

namespace PickleCalLG.Views
{
    /// <summary>
    /// Data grid displaying measurement results with color-coded ΔE cells.
    /// Equivalent to HCFR's MainView data grid.
    /// </summary>
    public sealed class MeasurementDataGrid : UserControl
    {
        private readonly DataGridView _grid;

        public MeasurementDataGrid()
        {
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                GridColor = Color.FromArgb(50, 50, 54),
                BorderStyle = BorderStyle.None,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 8),
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = Color.FromArgb(220, 220, 220),
                    SelectionBackColor = Color.FromArgb(0, 88, 160),
                    SelectionForeColor = Color.White
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.FromArgb(200, 200, 200)
                },
                EnableHeadersVisualStyles = false
            };

            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "IRE", HeaderText = "IRE %", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "Lum", HeaderText = "Y (cd/m²)", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "x", HeaderText = "x", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "y", HeaderText = "y", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "CCT", HeaderText = "CCT (K)", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "dE76", HeaderText = "ΔE₇₆", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "dE94", HeaderText = "ΔE₉₄", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "dE2000", HeaderText = "ΔE₂₀₀₀", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "Gamma", HeaderText = "γ", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Type", FillWeight = 10 }
            });

            _grid.CellFormatting += Grid_CellFormatting;
            Controls.Add(_grid);
        }

        public void SetPoints(IEnumerable<CalibrationPoint> points)
        {
            _grid.Rows.Clear();
            foreach (var p in points)
            {
                int idx = _grid.Rows.Add();
                var row = _grid.Rows[idx];
                row.Cells["Name"].Value = p.Name;
                row.Cells["IRE"].Value = p.TargetIre.ToString("F0");
                row.Cells["Lum"].Value = p.Luminance.ToString("F2");
                row.Cells["x"].Value = p.MeasuredXy.X.ToString("F4");
                row.Cells["y"].Value = p.MeasuredXy.Y.ToString("F4");
                row.Cells["CCT"].Value = p.CCT.ToString("F0");
                row.Cells["dE76"].Value = p.DeltaE76.ToString("F2");
                row.Cells["dE94"].Value = p.DeltaE94.ToString("F2");
                row.Cells["dE2000"].Value = p.DeltaE2000.ToString("F2");
                row.Cells["Gamma"].Value = p.EffectiveGamma.HasValue ? p.EffectiveGamma.Value.ToString("F2") : "—";
                row.Cells["Category"].Value = p.Category.ToString();
                row.Tag = p;
            }
        }

        public void ClearPoints()
        {
            _grid.Rows.Clear();
        }

        public CalibrationPoint? SelectedPoint
        {
            get
            {
                if (_grid.SelectedRows.Count > 0)
                    return _grid.SelectedRows[0].Tag as CalibrationPoint;
                return null;
            }
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // Color-code ΔE columns
            var colName = _grid.Columns[e.ColumnIndex].Name;
            if (colName is "dE76" or "dE94" or "dE2000" && e.Value is string valStr && e.CellStyle != null)
            {
                if (double.TryParse(valStr, out double de))
                {
                    if (de < 1.0)
                        e.CellStyle!.BackColor = Color.FromArgb(20, 60, 20);
                    else if (de < 3.0)
                        e.CellStyle!.BackColor = Color.FromArgb(60, 55, 10);
                    else
                        e.CellStyle!.BackColor = Color.FromArgb(70, 20, 20);
                }
            }
        }
    }
}
