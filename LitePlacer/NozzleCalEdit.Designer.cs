namespace LitePlacer
{
    partial class NozzleCalEditForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            _calPointsDataGridView = new System.Windows.Forms.DataGridView();
            _colAngle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _saveButton = new System.Windows.Forms.Button();
            _cancelButton = new System.Windows.Forms.Button();
            _labelInfo = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)_calPointsDataGridView).BeginInit();
            SuspendLayout();

            // _labelInfo
            _labelInfo.AutoSize = true;
            _labelInfo.Location = new System.Drawing.Point(12, 9);
            _labelInfo.Name = "_labelInfo";
            _labelInfo.Size = new System.Drawing.Size(280, 13);
            _labelInfo.Text = "Edit X and Y offset values (mm). Angle column is read-only.";

            // _colAngle
            _colAngle.HeaderText = "Angle (°)";
            _colAngle.Name = "_colAngle";
            _colAngle.ReadOnly = true;
            _colAngle.Width = 80;

            // _colX
            _colX.HeaderText = "X (mm)";
            _colX.Name = "_colX";
            _colX.Width = 90;

            // _colY
            _colY.HeaderText = "Y (mm)";
            _colY.Name = "_colY";
            _colY.Width = 90;

            // _calPointsDataGridView
            _calPointsDataGridView.AllowUserToAddRows = false;
            _calPointsDataGridView.AllowUserToDeleteRows = false;
            _calPointsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _calPointsDataGridView.Columns.Add(_colAngle);
            _calPointsDataGridView.Columns.Add(_colX);
            _calPointsDataGridView.Columns.Add(_colY);
            _calPointsDataGridView.Location = new System.Drawing.Point(12, 30);
            _calPointsDataGridView.Name = "_calPointsDataGridView";
            _calPointsDataGridView.RowHeadersWidth = 30;
            _calPointsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            _calPointsDataGridView.Size = new System.Drawing.Size(290, 352);
            _calPointsDataGridView.TabIndex = 0;
            _calPointsDataGridView.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(CalPoints_dataGridView_CellValidating);

            // _saveButton
            _saveButton.Location = new System.Drawing.Point(146, 396);
            _saveButton.Name = "_saveButton";
            _saveButton.Size = new System.Drawing.Size(75, 23);
            _saveButton.TabIndex = 1;
            _saveButton.Text = "Save";
            _saveButton.Click += new System.EventHandler(Save_button_Click);

            // _cancelButton
            _cancelButton.Location = new System.Drawing.Point(227, 396);
            _cancelButton.Name = "_cancelButton";
            _cancelButton.Size = new System.Drawing.Size(75, 23);
            _cancelButton.TabIndex = 2;
            _cancelButton.Text = "Cancel";
            _cancelButton.Click += new System.EventHandler(Cancel_button_Click);

            // NozzleCalEditForm
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(316, 431);
            Controls.Add(_labelInfo);
            Controls.Add(_calPointsDataGridView);
            Controls.Add(_saveButton);
            Controls.Add(_cancelButton);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "NozzleCalEditForm";
            Text = "Nozzle Calibration Points";
            Load += new System.EventHandler(NozzleCalEditForm_Load);

            ((System.ComponentModel.ISupportInitialize)_calPointsDataGridView).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.DataGridView _calPointsDataGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colAngle;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colX;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colY;
        private System.Windows.Forms.Button _saveButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Label _labelInfo;

        // Public accessor used by FillGrid / Save
        public System.Windows.Forms.DataGridView CalPoints_dataGridView { get { return _calPointsDataGridView; } }
    }
}
