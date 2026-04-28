using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace LitePlacer
{
    // Modeless dialog — opened with Show() so the main form jog controls remain active.
    public partial class NozzleManualCalForm : Form
    {
        public FormMain MainForm { get; set; }
        public int NozzleIndex { get; set; }   // 0-based

        private double _refX;
        private double _refY;
        private int _currentStep;              // 0..16
        private bool _referenceSet;
        private readonly List<NozzleCalibrationClass.NozzlePoint> _workingPoints =
            new List<NozzleCalibrationClass.NozzlePoint>();

        // Matches auto-calibration loop: i = 0, 225, 450 ... 3600  -> angle = i / 10.0
        private static readonly double[] _angles = BuildAngles();

        private static double[] BuildAngles()
        {
            double[] a = new double[17];
            for (int i = 0; i < 17; i++)
                a[i] = i * 22.5;
            return a;
        }

        public NozzleManualCalForm()
        {
            InitializeComponent();
        }

        private void NozzleManualCalForm_Load(object sender, EventArgs e)
        {
            Text = "Manual Nozzle Calibration – Nozzle " + (NozzleIndex + 1);
            _currentStep = 0;
            _referenceSet = false;
            _workingPoints.Clear();

            // Pre-fill grid with all 17 angle rows, loading any existing calibration data
            CalGrid_dataGridView.Rows.Clear();

            NozzleCalibrationClass.NozzleData existing = null;
            var allNozzles = MainForm.NozzleCalibration.NozzleDataAllNozzles;
            if (NozzleIndex >= 0 && NozzleIndex < allNozzles.Count &&
                allNozzles[NozzleIndex].Calibrated &&
                allNozzles[NozzleIndex].CalibrationPoints != null)
            {
                existing = allNozzles[NozzleIndex];
            }

            foreach (double angle in _angles)
            {
                string xVal = "";
                string yVal = "";
                Color rowColor = Color.LightGray;

                if (existing != null)
                {
                    NozzleCalibrationClass.NozzlePoint pt =
                        existing.CalibrationPoints.Find(p => Math.Abs(p.Angle - angle) < 0.01);
                    if (pt != null)
                    {
                        xVal = pt.X.ToString("0.000", CultureInfo.InvariantCulture);
                        yVal = pt.Y.ToString("0.000", CultureInfo.InvariantCulture);
                        rowColor = Color.LightGreen;
                    }
                }

                int idx = CalGrid_dataGridView.Rows.Add(
                    angle.ToString("0.0", CultureInfo.InvariantCulture), xVal, yVal);
                CalGrid_dataGridView.Rows[idx].DefaultCellStyle.BackColor = rowColor;
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (!_referenceSet)
            {
                Status_label.Text =
                    "Jog the nozzle tip onto a reference point on the PCB, then click Set Reference.";
                CurrentAngle_label.Text = "Current angle: --";
            }
            else
            {
                double angle = _angles[_currentStep];
                Status_label.Text = string.Format(
                    "Step {0} of 17  –  Angle {1}°\r\nJog tip back over the reference point, then click Set Point.",
                    _currentStep + 1,
                    angle.ToString("0.0", CultureInfo.InvariantCulture));
                CurrentAngle_label.Text = "Current angle: " +
                    angle.ToString("0.0", CultureInfo.InvariantCulture) + "°";

                // Highlight the current row; preserve green (machine-captured) and blue (typed) rows
                for (int i = 0; i < CalGrid_dataGridView.Rows.Count; i++)
                {
                    Color c = CalGrid_dataGridView.Rows[i].DefaultCellStyle.BackColor;
                    if (c != Color.LightGreen && c != Color.LightBlue)
                        CalGrid_dataGridView.Rows[i].DefaultCellStyle.BackColor = Color.LightGray;
                }
                CalGrid_dataGridView.Rows[_currentStep].DefaultCellStyle.BackColor = Color.LightYellow;
            }
        }

        private void SetReference_button_Click(object sender, EventArgs e)
        {
            _refX = MainForm.Cnc.CurrentX;
            _refY = MainForm.Cnc.CurrentY;
            _referenceSet = true;
            _currentStep = 0;
            _workingPoints.Clear();

            // Reset all rows to unset state
            foreach (DataGridViewRow row in CalGrid_dataGridView.Rows)
            {
                row.Cells[1].Value = "";
                row.Cells[2].Value = "";
                row.DefaultCellStyle.BackColor = Color.LightGray;
            }

            if (!MainForm.CNC_A_m(_angles[0]))
            {
                MainForm.DisplayText("Manual nozzle cal: rotation to 0° failed.", KnownColor.DarkRed);
                _referenceSet = false;
                return;
            }
            MainForm.DisplayText(string.Format(
                "Manual nozzle cal: reference set at X={0:0.000} Y={1:0.000}. Rotated to 0°.",
                _refX, _refY));
            UpdateUI();
        }

        private void SetPoint_button_Click(object sender, EventArgs e)
        {
            double dx = MainForm.Cnc.CurrentX - _refX;
            double dy = MainForm.Cnc.CurrentY - _refY;

            NozzleCalibrationClass.NozzlePoint pt = new NozzleCalibrationClass.NozzlePoint();
            pt.Angle = _angles[_currentStep];
            pt.X = dx;
            pt.Y = dy;
            _workingPoints.Add(pt);

            // Update grid row — green = captured
            DataGridViewRow row = CalGrid_dataGridView.Rows[_currentStep];
            row.Cells[1].Value = dx.ToString("0.000", CultureInfo.InvariantCulture);
            row.Cells[2].Value = dy.ToString("0.000", CultureInfo.InvariantCulture);
            row.DefaultCellStyle.BackColor = Color.LightGreen;

            MainForm.DisplayText(string.Format(
                "Manual nozzle cal: A={0:0.0}° X={1:0.000} Y={2:0.000}",
                pt.Angle, pt.X, pt.Y));

            if (_currentStep >= 16)
                Status_label.Text = "All 17 points captured. Click Save & Complete to write calibration.";
            else
                Status_label.Text = "Point recorded. Click Next Angle to advance.";
        }

        private void NextAngle_button_Click(object sender, EventArgs e)
        {
            if (_currentStep >= 16)
            {
                Status_label.Text = "All 17 points captured. Click Save & Complete to write calibration.";
                return;
            }
            _currentStep++;
            if (!MainForm.CNC_A_m(_angles[_currentStep]))
            {
                MainForm.DisplayText("Manual nozzle cal: rotation failed.", KnownColor.DarkRed);
                _currentStep--;
                return;
            }
            UpdateUI();
        }

        private void CalGrid_dataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex == 0) return;  // Angle column is read-only
            string raw = e.FormattedValue?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(raw)) return;  // Empty is allowed
            double val;
            if (!double.TryParse(raw.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                CultureInfo.InvariantCulture, out val))
            {
                e.Cancel = true;
                CalGrid_dataGridView.Rows[e.RowIndex].ErrorText = "Must be a number";
            }
            else
            {
                CalGrid_dataGridView.Rows[e.RowIndex].ErrorText = string.Empty;
            }
        }

        private void CalGrid_dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0) return;
            // Mark as manually typed (blue), regardless of whether it was previously machine-captured
            CalGrid_dataGridView.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightBlue;
        }

        private void SaveComplete_button_Click(object sender, EventArgs e)
        {
            // Commit any pending cell edit first
            CalGrid_dataGridView.EndEdit();

            var points = new NozzleCalibrationClass.CalibrationPointsList();
            int skipped = 0;
            for (int i = 0; i < CalGrid_dataGridView.Rows.Count; i++)
            {
                string rawX = CalGrid_dataGridView.Rows[i].Cells[1].Value?.ToString() ?? "";
                string rawY = CalGrid_dataGridView.Rows[i].Cells[2].Value?.ToString() ?? "";
                double x, y;
                bool xOk = double.TryParse(rawX.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out x);
                bool yOk = double.TryParse(rawY.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out y);
                if (xOk && yOk)
                {
                    points.Add(new NozzleCalibrationClass.NozzlePoint { Angle = _angles[i], X = x, Y = y });
                }
                else if (!string.IsNullOrWhiteSpace(rawX) || !string.IsNullOrWhiteSpace(rawY))
                {
                    skipped++;
                }
            }

            if (points.Count == 0)
            {
                MainForm.ShowMessageBox(
                    "No valid calibration points to save.",
                    "Nothing to save", MessageBoxButtons.OK);
                return;
            }

            if (skipped > 0)
            {
                DialogResult dr = MainForm.ShowMessageBox(
                    skipped + " row(s) had invalid values and will be skipped.\nSave the remaining " + points.Count + " point(s)?",
                    "Invalid data", MessageBoxButtons.YesNo);
                if (dr != DialogResult.Yes) return;
            }

            NozzleCalibrationClass.NozzleData data = new NozzleCalibrationClass.NozzleData();
            data.CalibrationPoints = points;
            data.Calibrated = true;

            MainForm.NozzleCalibration.NozzleDataAllNozzles[NozzleIndex] = data;
            MainForm.NozzleCalibration.UpdateNozzleGridView();
            MainForm.NozzleCalibration.SaveNozzlesCalibration(
                MainForm.GetPath() + FormMain.NOZZLES_CALIBRATION_DATAFILE);
            MainForm.DisplayText(
                "Nozzle " + (NozzleIndex + 1) + " calibrated and saved (" + points.Count + " points).",
                KnownColor.DarkGreen);
            Close();
        }

        private void Cancel_button_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
