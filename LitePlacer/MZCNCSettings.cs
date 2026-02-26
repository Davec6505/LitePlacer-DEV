using System;
using System.Globalization;
using System.Windows.Forms;

namespace LitePlacer
{
    public partial class FormMain : Form
    {
        /// <summary>
        /// Load GRBL settings from MZ_CNC controller into UI controls
        /// Called from MZ_CNCControl.JustConnected()
        /// </summary>
        public void MZ_CNCSettings_Load()
        {
            DisplayText("=== Populating MZ_CNC UI Controls ===", System.Drawing.KnownColor.DarkCyan);

            if (Cnc?.MZ_CNC?.Settings == null)
            {
                DisplayText("*** Error: MZ_CNC Settings object is null", System.Drawing.KnownColor.DarkRed);
                return;
            }

            // Wire up event handlers programmatically
            WireMZCNCEventHandlers();

            var settings = Cnc.MZ_CNC.Settings;

            // =============== X-AXIS SETTINGS ===============
            // Speed (max rate) - $110
            MZCNCXspeed_maskedTextBox.Text = settings.MaxRateX.ToString("0.0");
            
            // Acceleration - $120
            MZCNCXacceleration_maskedTextBox.Text = settings.AccelX.ToString("0.0");
            
            // Steps per mm - $100
            double stepsPerMm = settings.StepsPerMmX;
            
            // Calculate microsteps and travel per rev from steps/mm
            // Assuming standard stepper (200 steps/rev = 1.8°)
            // steps/mm = (microsteps * 200) / travel_per_rev
            // For now, use default microsteps = 8
            int microsteps = 8;
            MZCNCXmicrosteps_maskedTextBox.Text = microsteps.ToString();
            
            // Calculate travel per revolution
            double travelPerRev = (microsteps * 200.0) / stepsPerMm;
            MZCNCXtravelPerRev_textBox.Text = travelPerRev.ToString("0.000");
            
            // Default to 1.8° stepping
            MZCNCXdeg18_radioButton.Checked = true;
            MZCNCXdeg09_radioButton.Checked = false;
            
            // Motor current - not in standard GRBL, default to 800mA
            MZCNCXCurrent_maskedTextBox.Text = "800";
            
            // Homing speed - $26 (seek rate)
            MZCNCXhomingSpeed_maskedTextBox.Text = settings.HomingSeek.ToString("0.0");
            
            // Homing backoff - $27 (pull-off distance in mm)
            // Standard GRBL v1.1 setting - value is already in millimeters
            MZCNCXHomingBackoff_maskedTextBox.Text = settings.HomingPulloff.ToString("0.000");
            
            // Interpolation - default to off
            MZCNCXinterpolate_checkBox.Checked = false;

            // =============== Y-AXIS SETTINGS ===============
            MZCNCYspeed_maskedTextBox.Text = settings.MaxRateY.ToString("0.0");
            MZCNCYacceleration_maskedTextBox.Text = settings.AccelY.ToString("0.0");
            
            stepsPerMm = settings.StepsPerMmY;
            travelPerRev = (microsteps * 200.0) / stepsPerMm;
            MZCNCYtravelPerRev_textBox.Text = travelPerRev.ToString("0.000");
            MZCNCYmicrosteps_maskedTextBox.Text = microsteps.ToString();
            
            MZCNCYdeg18_radioButton.Checked = true;
            MZCNCYdeg09_radioButton.Checked = false;
            MZCNCYCurrent_maskedTextBox.Text = "800";
            MZCNCYhomingSpeed_maskedTextBox.Text = settings.HomingSeek.ToString("0.0");
            // Homing backoff - $27 (standard GRBL in mm)
            MZCNCYHomingBackoff_maskedTextBox.Text = settings.HomingPulloff.ToString("0.000");
            MZCNCYinterpolate_checkBox.Checked = false;

            // =============== Z-AXIS SETTINGS ===============
            MZCNCZspeed_maskedTextBox.Text = settings.MaxRateZ.ToString("0.0");
            MZCNCZacceleration_maskedTextBox.Text = settings.AccelZ.ToString("0.0");
            
            stepsPerMm = settings.StepsPerMmZ;
            travelPerRev = (microsteps * 200.0) / stepsPerMm;
            MZCNCZtravelPerRev_textBox.Text = travelPerRev.ToString("0.000");
            MZCNCZmicrosteps_maskedTextBox.Text = microsteps.ToString();
            
            MZCNCZdeg18_radioButton.Checked = true;
            MZCNCZdeg09_radioButton.Checked = false;
            MZCNCZCurrent_maskedTextBox.Text = "800";
            MZCNCZhomingSpeed_maskedTextBox.Text = settings.HomingSeek.ToString("0.0");
            // Homing backoff - $27 (standard GRBL in mm)
            MZCNCZHomingBackoff_maskedTextBox.Text = settings.HomingPulloff.ToString("0.000");
            MZCNCZinterpolate_checkBox.Checked = false;

            // =============== A-AXIS SETTINGS ===============
            MZCNCAspeed_maskedTextBox.Text = settings.MaxRateA.ToString("0.0");
            MZCNCAacceleration_maskedTextBox.Text = settings.AccelA.ToString("0.0");
            
            stepsPerMm = settings.StepsPerMmA;
            travelPerRev = (microsteps * 200.0) / stepsPerMm;
            MZCNCAtravelPerRev_textBox.Text = travelPerRev.ToString("0.000");
            MZCNCAmicrosteps_maskedTextBox.Text = microsteps.ToString();
            
            MZCNCAdeg18_radioButton.Checked = true;
            MZCNCAdeg09_radioButton.Checked = false;
            MZCNCACurrent_maskedTextBox.Text = "800";
            MZCNCAinterpolate_checkBox.Checked = false;

            DisplayText("=== MZ_CNC UI Controls Populated Successfully ===", System.Drawing.KnownColor.DarkGreen);
        }

        // ==================================================================================
        // Event Handlers for MZ_CNC Settings Changes
        // ==================================================================================

        #region X-Axis Event Handlers

        private void MZCNCXspeed_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                if (double.TryParse(MZCNCXspeed_maskedTextBox.Text, out double speed))
                {
                    string cmd = $"$110={speed.ToString("0.0", CultureInfo.InvariantCulture)}";
                    if (Cnc.MZ_CNC.Write_m(cmd))
                    {
                        DisplayText($"X max rate set to {speed} mm/min", System.Drawing.KnownColor.DarkGreen);
                    }
                }
            }
        }

        private void MZCNCXacceleration_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                if (double.TryParse(MZCNCXacceleration_maskedTextBox.Text, out double accel))
                {
                    string cmd = $"$120={accel.ToString("0.0", CultureInfo.InvariantCulture)}";
                    if (Cnc.MZ_CNC.Write_m(cmd))
                    {
                        DisplayText($"X acceleration set to {accel} mm/s²", System.Drawing.KnownColor.DarkGreen);
                    }
                }
            }
        }

        private void MZCNCXhomingSpeed_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                if (double.TryParse(MZCNCXhomingSpeed_maskedTextBox.Text, out double speed))
                {
                    string cmd = $"$26={speed.ToString("0.0", CultureInfo.InvariantCulture)}";
                    if (Cnc.MZ_CNC.Write_m(cmd))
                    {
                        DisplayText($"Homing seek rate set to {speed} mm/min", System.Drawing.KnownColor.DarkGreen);
                    }
                }
            }
        }

        private void MZCNCXHomingBackoff_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                if (double.TryParse(MZCNCXHomingBackoff_maskedTextBox.Text, out double backoff))
                {
                    // Standard GRBL $27 setting (homing pull-off in mm)
                    string cmd = $"$27={backoff.ToString("0.000", CultureInfo.InvariantCulture)}";
                    if (Cnc.MZ_CNC.Write_m(cmd))
                    {
                        DisplayText($"Homing pull-off set to {backoff} mm", System.Drawing.KnownColor.DarkGreen);
                    }
                }
            }
        }

        #endregion

        #region Y-Axis Event Handlers

        private void MZCNCYspeed_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                if (double.TryParse(MZCNCYspeed_maskedTextBox.Text, out double speed))
                {
                    string cmd = $"$111={speed.ToString("0.0", CultureInfo.InvariantCulture)}";
                    if (Cnc.MZ_CNC.Write_m(cmd))
                    {
                        DisplayText($"Y max rate set to {speed} mm/min", System.Drawing.KnownColor.DarkGreen);
                    }
                }
            }
        }

        private void MZCNCYacceleration_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                if (double.TryParse(MZCNCYacceleration_maskedTextBox.Text, out double accel))
                {
                    string cmd = $"$121={accel.ToString("0.0", CultureInfo.InvariantCulture)}";
                    if (Cnc.MZ_CNC.Write_m(cmd))
                    {
                        DisplayText($"Y acceleration set to {accel} mm/s²", System.Drawing.KnownColor.DarkGreen);
                    }
                }
            }
        }

        private void MZCNCYhomingSpeed_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                // Y uses same $26 as X (global homing seek rate)
                MZCNCXhomingSpeed_maskedTextBox_KeyPress(sender, e);
            }
        }

        private void MZCNCYHomingBackoff_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                // Y uses same $27 as X (global homing pull-off in mm)
                MZCNCXHomingBackoff_maskedTextBox_KeyPress(sender, e);
            }
        }

        #endregion

        #region Z-Axis Event Handlers

        private void MZCNCZspeed_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                if (double.TryParse(MZCNCZspeed_maskedTextBox.Text, out double speed))
                {
                    string cmd = $"$112={speed.ToString("0.0", CultureInfo.InvariantCulture)}";
                    if (Cnc.MZ_CNC.Write_m(cmd))
                    {
                        DisplayText($"Z max rate set to {speed} mm/min", System.Drawing.KnownColor.DarkGreen);
                    }
                }
            }
        }

        private void MZCNCZacceleration_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                if (double.TryParse(MZCNCZacceleration_maskedTextBox.Text, out double accel))
                {
                    string cmd = $"$122={accel.ToString("0.0", CultureInfo.InvariantCulture)}";
                    if (Cnc.MZ_CNC.Write_m(cmd))
                    {
                        DisplayText($"Z acceleration set to {accel} mm/s²", System.Drawing.KnownColor.DarkGreen);
                    }
                }
            }
        }

        private void MZCNCZhomingSpeed_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                // Z uses same $26 as X (global homing seek rate)
                MZCNCXhomingSpeed_maskedTextBox_KeyPress(sender, e);
            }
        }

        private void MZCNCZHomingBackoff_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                // Z uses same $27 as X (global homing pull-off in mm)
                MZCNCXHomingBackoff_maskedTextBox_KeyPress(sender, e);
            }
        }

        #endregion

        #region A-Axis Event Handlers

        private void MZCNCAspeed_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                if (double.TryParse(MZCNCAspeed_maskedTextBox.Text, out double speed))
                {
                    string cmd = $"$113={speed.ToString("0.0", CultureInfo.InvariantCulture)}";
                    if (Cnc.MZ_CNC.Write_m(cmd))
                    {
                        DisplayText($"A max rate set to {speed} mm/min", System.Drawing.KnownColor.DarkGreen);
                    }
                }
            }
        }

        private void MZCNCAacceleration_maskedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                if (double.TryParse(MZCNCAacceleration_maskedTextBox.Text, out double accel))
                {
                    string cmd = $"$123={accel.ToString("0.0", CultureInfo.InvariantCulture)}";
                    if (Cnc.MZ_CNC.Write_m(cmd))
                    {
                        DisplayText($"A acceleration set to {accel} mm/s²", System.Drawing.KnownColor.DarkGreen);
                    }
                }
            }
        }

        #endregion

        // ==================================================================================
        // Wire Up Event Handlers Programmatically
        // ==================================================================================
        private bool _mzcncEventHandlersWired = false;

        private void WireMZCNCEventHandlers()
        {
            // Prevent double-wiring if already connected
            if (_mzcncEventHandlersWired)
            {
                DisplayText("MZ_CNC event handlers already wired", System.Drawing.KnownColor.DarkCyan);
                return;
            }

            try
            {
                // X-Axis
                MZCNCXspeed_maskedTextBox.KeyPress -= MZCNCXspeed_maskedTextBox_KeyPress; // Remove first (safe if not exists)
                MZCNCXspeed_maskedTextBox.KeyPress += MZCNCXspeed_maskedTextBox_KeyPress;
                
                MZCNCXacceleration_maskedTextBox.KeyPress -= MZCNCXacceleration_maskedTextBox_KeyPress;
                MZCNCXacceleration_maskedTextBox.KeyPress += MZCNCXacceleration_maskedTextBox_KeyPress;
                
                MZCNCXhomingSpeed_maskedTextBox.KeyPress -= MZCNCXhomingSpeed_maskedTextBox_KeyPress;
                MZCNCXhomingSpeed_maskedTextBox.KeyPress += MZCNCXhomingSpeed_maskedTextBox_KeyPress;
                
                MZCNCXHomingBackoff_maskedTextBox.KeyPress -= MZCNCXHomingBackoff_maskedTextBox_KeyPress;
                MZCNCXHomingBackoff_maskedTextBox.KeyPress += MZCNCXHomingBackoff_maskedTextBox_KeyPress;

                // Y-Axis
                MZCNCYspeed_maskedTextBox.KeyPress -= MZCNCYspeed_maskedTextBox_KeyPress;
                MZCNCYspeed_maskedTextBox.KeyPress += MZCNCYspeed_maskedTextBox_KeyPress;
                
                MZCNCYacceleration_maskedTextBox.KeyPress -= MZCNCYacceleration_maskedTextBox_KeyPress;
                MZCNCYacceleration_maskedTextBox.KeyPress += MZCNCYacceleration_maskedTextBox_KeyPress;
                
                MZCNCYhomingSpeed_maskedTextBox.KeyPress -= MZCNCYhomingSpeed_maskedTextBox_KeyPress;
                MZCNCYhomingSpeed_maskedTextBox.KeyPress += MZCNCYhomingSpeed_maskedTextBox_KeyPress;
                
                MZCNCYHomingBackoff_maskedTextBox.KeyPress -= MZCNCYHomingBackoff_maskedTextBox_KeyPress;
                MZCNCYHomingBackoff_maskedTextBox.KeyPress += MZCNCYHomingBackoff_maskedTextBox_KeyPress;

                // Z-Axis
                MZCNCZspeed_maskedTextBox.KeyPress -= MZCNCZspeed_maskedTextBox_KeyPress;
                MZCNCZspeed_maskedTextBox.KeyPress += MZCNCZspeed_maskedTextBox_KeyPress;
                
                MZCNCZacceleration_maskedTextBox.KeyPress -= MZCNCZacceleration_maskedTextBox_KeyPress;
                MZCNCZacceleration_maskedTextBox.KeyPress += MZCNCZacceleration_maskedTextBox_KeyPress;
                
                MZCNCZhomingSpeed_maskedTextBox.KeyPress -= MZCNCZhomingSpeed_maskedTextBox_KeyPress;
                MZCNCZhomingSpeed_maskedTextBox.KeyPress += MZCNCZhomingSpeed_maskedTextBox_KeyPress;
                
                MZCNCZHomingBackoff_maskedTextBox.KeyPress -= MZCNCZHomingBackoff_maskedTextBox_KeyPress;
                MZCNCZHomingBackoff_maskedTextBox.KeyPress += MZCNCZHomingBackoff_maskedTextBox_KeyPress;

                // A-Axis
                MZCNCAspeed_maskedTextBox.KeyPress -= MZCNCAspeed_maskedTextBox_KeyPress;
                MZCNCAspeed_maskedTextBox.KeyPress += MZCNCAspeed_maskedTextBox_KeyPress;
                
                MZCNCAacceleration_maskedTextBox.KeyPress -= MZCNCAacceleration_maskedTextBox_KeyPress;
                MZCNCAacceleration_maskedTextBox.KeyPress += MZCNCAacceleration_maskedTextBox_KeyPress;

                _mzcncEventHandlersWired = true;
                DisplayText("MZ_CNC event handlers wired successfully", System.Drawing.KnownColor.DarkGreen);
            }
            catch (Exception ex)
            {
                DisplayText($"*** Error wiring MZ_CNC event handlers: {ex.Message}", System.Drawing.KnownColor.DarkRed);
            }
        }
    }
}
