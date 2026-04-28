namespace LitePlacer
{
    partial class NozzleManualCalForm
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
            this._statusLabel = new System.Windows.Forms.Label();
            this._currentAngleLabel = new System.Windows.Forms.Label();
            this._calGridDataGridView = new System.Windows.Forms.DataGridView();
            this._colAngle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._colX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._colY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._setReferenceButton = new System.Windows.Forms.Button();
            this._setPointButton = new System.Windows.Forms.Button();
            this._nextAngleButton = new System.Windows.Forms.Button();
            this._saveCompleteButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._instructionsLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this._calGridDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // _statusLabel
            // 
            this._statusLabel.Location = new System.Drawing.Point(16, 54);
            this._statusLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this._statusLabel.Name = "_statusLabel";
            this._statusLabel.Size = new System.Drawing.Size(501, 44);
            this._statusLabel.TabIndex = 1;
            this._statusLabel.Text = "Ready.";
            // 
            // _currentAngleLabel
            // 
            this._currentAngleLabel.AutoSize = true;
            this._currentAngleLabel.Location = new System.Drawing.Point(16, 105);
            this._currentAngleLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this._currentAngleLabel.Name = "_currentAngleLabel";
            this._currentAngleLabel.Size = new System.Drawing.Size(100, 16);
            this._currentAngleLabel.TabIndex = 2;
            this._currentAngleLabel.Text = "Current angle: --";
            // 
            // _calGridDataGridView
            // 
            this._calGridDataGridView.AllowUserToAddRows = false;
            this._calGridDataGridView.AllowUserToDeleteRows = false;
            this._calGridDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._calGridDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colAngle,
            this._colX,
            this._colY});
            this._calGridDataGridView.Location = new System.Drawing.Point(16, 129);
            this._calGridDataGridView.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._calGridDataGridView.Name = "_calGridDataGridView";
            this._calGridDataGridView.RowHeadersWidth = 30;
            this._calGridDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._calGridDataGridView.Size = new System.Drawing.Size(501, 460);
            this._calGridDataGridView.TabIndex = 0;
            this._calGridDataGridView.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.CalGrid_dataGridView_CellEndEdit);
            this._calGridDataGridView.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.CalGrid_dataGridView_CellValidating);
            // 
            // _colAngle
            // 
            this._colAngle.HeaderText = "Angle";
            this._colAngle.MinimumWidth = 6;
            this._colAngle.Name = "_colAngle";
            this._colAngle.ReadOnly = true;
            this._colAngle.Width = 70;
            // 
            // _colX
            // 
            this._colX.HeaderText = "X offset (mm)";
            this._colX.MinimumWidth = 6;
            this._colX.Name = "_colX";
            this._colX.Width = 120;
            // 
            // _colY
            // 
            this._colY.HeaderText = "Y offset (mm)";
            this._colY.MinimumWidth = 6;
            this._colY.Name = "_colY";
            this._colY.Width = 120;
            // 
            // _setReferenceButton
            // 
            this._setReferenceButton.Location = new System.Drawing.Point(16, 606);
            this._setReferenceButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._setReferenceButton.Name = "_setReferenceButton";
            this._setReferenceButton.Size = new System.Drawing.Size(160, 34);
            this._setReferenceButton.TabIndex = 1;
            this._setReferenceButton.Text = "Set Reference";
            this._setReferenceButton.Click += new System.EventHandler(this.SetReference_button_Click);
            // 
            // _setPointButton
            // 
            this._setPointButton.Location = new System.Drawing.Point(184, 606);
            this._setPointButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._setPointButton.Name = "_setPointButton";
            this._setPointButton.Size = new System.Drawing.Size(160, 34);
            this._setPointButton.TabIndex = 2;
            this._setPointButton.Text = "Set Point";
            this._setPointButton.Click += new System.EventHandler(this.SetPoint_button_Click);
            // 
            // _nextAngleButton
            // 
            this._nextAngleButton.Location = new System.Drawing.Point(16, 647);
            this._nextAngleButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._nextAngleButton.Name = "_nextAngleButton";
            this._nextAngleButton.Size = new System.Drawing.Size(160, 34);
            this._nextAngleButton.TabIndex = 3;
            this._nextAngleButton.Text = "Next Angle";
            this._nextAngleButton.Click += new System.EventHandler(this.NextAngle_button_Click);
            // 
            // _saveCompleteButton
            // 
            this._saveCompleteButton.Location = new System.Drawing.Point(184, 647);
            this._saveCompleteButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._saveCompleteButton.Name = "_saveCompleteButton";
            this._saveCompleteButton.Size = new System.Drawing.Size(160, 34);
            this._saveCompleteButton.TabIndex = 4;
            this._saveCompleteButton.Text = "Save && Complete";
            this._saveCompleteButton.Click += new System.EventHandler(this.SaveComplete_button_Click);
            // 
            // _cancelButton
            // 
            this._cancelButton.Location = new System.Drawing.Point(357, 647);
            this._cancelButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(160, 34);
            this._cancelButton.TabIndex = 5;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.Click += new System.EventHandler(this.Cancel_button_Click);
            // 
            // _instructionsLabel
            // 
            this._instructionsLabel.Location = new System.Drawing.Point(16, 11);
            this._instructionsLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this._instructionsLabel.Name = "_instructionsLabel";
            this._instructionsLabel.Size = new System.Drawing.Size(501, 37);
            this._instructionsLabel.TabIndex = 0;
            this._instructionsLabel.Text = "Jog tip over a reference mark and Set Reference, or type X/Y offsets directly int" +
    "o the grid.";
            // 
            // NozzleManualCalForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(533, 697);
            this.Controls.Add(this._instructionsLabel);
            this.Controls.Add(this._statusLabel);
            this.Controls.Add(this._currentAngleLabel);
            this.Controls.Add(this._calGridDataGridView);
            this.Controls.Add(this._setReferenceButton);
            this.Controls.Add(this._setPointButton);
            this.Controls.Add(this._nextAngleButton);
            this.Controls.Add(this._saveCompleteButton);
            this.Controls.Add(this._cancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NozzleManualCalForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Manual Nozzle Calibration";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.NozzleManualCalForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this._calGridDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label _instructionsLabel;
        private System.Windows.Forms.Label _statusLabel;
        private System.Windows.Forms.Label _currentAngleLabel;
        private System.Windows.Forms.DataGridView _calGridDataGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colAngle;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colX;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colY;
        private System.Windows.Forms.Button _setReferenceButton;
        private System.Windows.Forms.Button _setPointButton;
        private System.Windows.Forms.Button _nextAngleButton;
        private System.Windows.Forms.Button _saveCompleteButton;
        private System.Windows.Forms.Button _cancelButton;

        // Public accessors used by the form logic
        public System.Windows.Forms.Label Status_label { get { return _statusLabel; } }
        public System.Windows.Forms.Label CurrentAngle_label { get { return _currentAngleLabel; } }
        public System.Windows.Forms.DataGridView CalGrid_dataGridView { get { return _calGridDataGridView; } }
        public System.Windows.Forms.Button SetReference_button { get { return _setReferenceButton; } }
        public System.Windows.Forms.Button SetPoint_button { get { return _setPointButton; } }
        public System.Windows.Forms.Button NextAngle_button { get { return _nextAngleButton; } }
        public System.Windows.Forms.Button SaveComplete_button { get { return _saveCompleteButton; } }
    }
}
