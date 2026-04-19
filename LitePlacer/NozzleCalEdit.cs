using System;
using System.Globalization;
using System.Windows.Forms;

namespace LitePlacer
{
    public partial class NozzleCalEditForm : Form
    {
        public FormMain MainForm { get; set; }
        public int NozzleIndex { get; set; }   // 0-based

        public NozzleCalEditForm()
        {
            InitializeComponent();
        }

        private void NozzleCalEditForm_Load(object sender, EventArgs e)
        {
            Text = "Nozzle " + (NozzleIndex + 1) + " Calibration Points";
            FillGrid();
        }

        private void FillGrid()
        {
            CalPoints_dataGridView.Rows.Clear();
            var points = MainForm.NozzleCalibration.NozzleDataAllNozzles[NozzleIndex].CalibrationPoints;
            if (points == null || points.Count == 0)
            {
                MessageBox.Show(
                    "No calibration data for nozzle " + (NozzleIndex + 1) + ".\n" +
                    "Run calibration first.",
                    "No data", MessageBoxButtons.OK);
                DialogResult = DialogResult.Cancel;
                return;
            }
            foreach (NozzleCalibrationClass.NozzlePoint p in points)
            {
                CalPoints_dataGridView.Rows.Add(
                    p.Angle.ToString("0.0", CultureInfo.InvariantCulture),
                    p.X.ToString("0.000", CultureInfo.InvariantCulture),
                    p.Y.ToString("0.000", CultureInfo.InvariantCulture));
            }
        }

        // Validate X/Y cells as numeric; flag invalid cells red.
        private void CalPoints_dataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex == 0) return;   // Angle column is read-only
            double val;
            string raw = e.FormattedValue.ToString().Replace(',', '.');
            if (!double.TryParse(raw, System.Globalization.NumberStyles.Any,
                CultureInfo.InvariantCulture, out val))
            {
                e.Cancel = true;
                CalPoints_dataGridView.Rows[e.RowIndex].ErrorText = "Must be a number";
            }
            else
            {
                CalPoints_dataGridView.Rows[e.RowIndex].ErrorText = string.Empty;
            }
        }

        private void Save_button_Click(object sender, EventArgs e)
        {
            // Commit any pending edit
            CalPoints_dataGridView.EndEdit();

            var points = MainForm.NozzleCalibration.NozzleDataAllNozzles[NozzleIndex].CalibrationPoints;
            for (int i = 0; i < CalPoints_dataGridView.Rows.Count && i < points.Count; i++)
            {
                double x, y;
                string rawX = CalPoints_dataGridView.Rows[i].Cells[1].Value?.ToString().Replace(',', '.') ?? "0";
                string rawY = CalPoints_dataGridView.Rows[i].Cells[2].Value?.ToString().Replace(',', '.') ?? "0";
                if (double.TryParse(rawX, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out x))
                    points[i].X = x;
                if (double.TryParse(rawY, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out y))
                    points[i].Y = y;
            }
            // Calibrated flag is intentionally left unchanged — this is a refinement, not a reset.
            MainForm.NozzleCalibration.SaveNozzlesCalibration(MainForm.GetPath() + FormMain.NOZZLES_CALIBRATION_DATAFILE);
            MainForm.DisplayText(
                "Nozzle " + (NozzleIndex + 1) + " calibration points updated and saved.",
                System.Drawing.KnownColor.DarkGreen);
            DialogResult = DialogResult.OK;
        }

        private void Cancel_button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
