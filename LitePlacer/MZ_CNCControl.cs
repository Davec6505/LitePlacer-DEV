using System;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Globalization;

namespace LitePlacer
{
    /// <summary>
    /// PIC32MZ GRBL v1.1 CNC Controller Interface
    /// Firmware: Pic32mzCNC_V3 (200MHz PIC32MZ2048EFH100)
    /// Protocol: GRBL v1.1 compatible with LitePlacer extensions
    /// </summary>
    public class MZ_CNCclass
    {
        FormMain MainForm;
        CNC Cnc;
        SerialComm Com;

        public MZ_CNCclass(FormMain MainF, CNC C, SerialComm ser)
        {
            MainForm = MainF;
            Cnc = C;
            Com = ser;
        }

        public int RegularMoveTimeout { get; set; } // in ms

        // =================================================================================
        #region Communications

        public bool JustConnected()
        {
            MainForm.DisplayText("MZ_CNC Controller Connected - GRBL v1.1 (PIC32MZ)", KnownColor.DarkGreen);
            
            // Verify GRBL firmware identity
            string buildInfo = GetResponse_m("$I", 500, true);
            if (!string.IsNullOrEmpty(buildInfo))
            {
                MainForm.DisplayText("Firmware: " + buildInfo, KnownColor.DarkCyan);
            }
            
            // Load current GRBL settings
            if (!LoadGRBLSettings())
            {
                MainForm.DisplayText("*** Warning: Could not load GRBL settings", KnownColor.DarkOrange);
            }
            
            // Set machine to absolute positioning mode (G90)
            if (!Write_m("G90"))
            {
                return false;
            }
            
            // Set units to millimeters (G21)
            if (!Write_m("G21"))
            {
                return false;
            }
            
            // Select XY plane (G17)
            if (!Write_m("G17"))
            {
                return false;
            }
            
            // Clear any alarm state
            Write_m("$X");
            
            MainForm.DisplayText("MZ_CNC initialization complete", KnownColor.DarkGreen);
            return true;
        }

        private bool LoadGRBLSettings()
        {
            // Request all settings from GRBL
            string response = GetResponse_m("$$", 1000, false);
            if (string.IsNullOrEmpty(response))
            {
                return false;
            }
            
            // Parse and display key settings
            MainForm.DisplayText("=== GRBL Settings Loaded ===", KnownColor.DarkCyan);
            // Settings will be displayed via normal response handling
            return true;
        }

        public void Close()
        {
            Com.Close();
            Cnc.ErrorState = false;
            Cnc.Connected = false;
            Cnc.Homing = false;
            MainForm.UpdateCncConnectionStatus();
        }

        // ===================================================================
        // Read & write
        // ===================================================================

        private bool LineAvailable = false;
        private string ReceivedLine = "";
        private bool WriteBusy = false;
        private bool ExpectingResponse = false;

        private void ClearReceivedLine()
        {
            lock (ReceivedLine)
            {
                ReceivedLine = "";
            }
        }

        // ===================================================================
        // Write_m
        // Normal write, waits until "ok" response is received
        public bool Write_m(string cmd, int Timeout = 500)
        {
            if (!Com.IsOpen)
            {
                MainForm.DisplayText("###" + cmd + " discarded, com not open");
                ClearReceivedLine();
                return false;
            }
            if (Cnc.ErrorState)
            {
                MainForm.DisplayText("###" + cmd + " discarded, error state on");
                ClearReceivedLine();
                return false;
            }

            Timeout = Timeout / 2;
            int i = 0;
            WriteBusy = true;
            bool WriteOk = Com.Write(cmd);
            while (WriteBusy)
            {
                Thread.Sleep(2);
                Application.DoEvents();
                i++;
                if (i > Timeout)
                {
                    MainForm.ShowMessageBox(
                        "MZ_CNC.Write_m: Timeout on command " + cmd,
                        "Timeout",
                        MessageBoxButtons.OK);
                    ClearReceivedLine();
                    return false;
                }
            }
            return WriteOk;
        }

        // ===================================================================
        // GetResponse
        // Writes a command, returns a response. Failed write returns empty response.
        public string GetResponse_m(string cmd, int Timeout = 250, bool report = true)
        {
            string line = "";

            if (!Com.IsOpen)
            {
                if (report)
                {
                    MainForm.DisplayText("###" + cmd + " discarded, com not open");
                }
                ClearReceivedLine();
                return "";
            }
            if (Cnc.ErrorState)
            {
                if (report)
                {
                    MainForm.DisplayText("###" + cmd + " discarded, error state on");
                }
                ClearReceivedLine();
                return "";
            }

            Timeout = Timeout / 2;
            int i = 0;
            LineAvailable = false;
            ExpectingResponse = true;
            Com.Write(cmd);
            while (!LineAvailable)
            {
                Thread.Sleep(2);
                Application.DoEvents();
                i++;
                if (i > Timeout)
                {
                    if (report)
                    {
                        MainForm.ShowMessageBox(
                            "MZ_CNC.GetResponse_m: Timeout on command " + cmd,
                            "Timeout",
                            MessageBoxButtons.OK);
                    }
                    ClearReceivedLine();
                    ExpectingResponse = false;
                    return "";
                }
            }
            lock (ReceivedLine)
            {
                line = ReceivedLine;
                ClearReceivedLine();
                ExpectingResponse = false;
            }
            return line;
        }

        // ===================================================================
        // LineReceived
        // Called from SerialComm when data arrives
        public void LineReceived(string line)
        {
            MainForm.DisplayText("<== " + line);
            
            // Handle "ok" response (command completed)
            if (line == "ok")
            {
                WriteBusy = false;
                return;
            }
            
            // Handle error responses
            if (line.StartsWith("error:"))
            {
                MainForm.DisplayText("*** GRBL Error: " + line, KnownColor.DarkRed, true);
                WriteBusy = false;
                return;
            }
            
            // Handle alarm responses
            if (line.StartsWith("ALARM:"))
            {
                MainForm.DisplayText("*** GRBL ALARM: " + line, KnownColor.DarkRed, true);
                MainForm.DisplayText("*** Send $X to clear alarm", KnownColor.DarkOrange);
                WriteBusy = false;
                Cnc.ErrorState = true;
                return;
            }
            
            // Accumulate multi-line responses
            lock (ReceivedLine)
            {
                if (ReceivedLine == "")
                {
                    ReceivedLine = line;
                }
                else
                {
                    ReceivedLine += MainForm.Setting.Serial_EndCharacters + line;
                }
                LineAvailable = true;
            }
        }

        // ===================================================================
        // RawWrite - For operations that don't give response
        public bool RawWrite(string command)
        {
            if (!Com.IsOpen)
            {
                MainForm.DisplayText("###" + command + " discarded, com not open");
                return false;
            }
            if (Cnc.ErrorState)
            {
                MainForm.DisplayText("###" + command + " discarded, error state on");
                return false;
            }
            return Com.Write(command);
        }

        #endregion Communications

        // =================================================================================
        // Movement, position:
        #region Movement

        // Set X position using G92
        public bool SetXposition(string pos)
        {
            double val;
            if (!double.TryParse(pos.Replace(',', '.'), out val))
            {
                MainForm.ShowMessageBox(
                    "MZ_CNC.SetXposition() called with bad value " + pos,
                    "BUG",
                    MessageBoxButtons.OK);
                return false;
            }
            Cnc.SetCurrentX(val);
            if (!Write_m("G92 X" + pos))
            {
                MainForm.ShowMessageBox(
                    "MZ_CNC G92 X" + pos + " failed",
                    "comm err?",
                    MessageBoxButtons.OK);
                return false;
            }
            return true;
        }

        public bool SetYposition(string pos)
        {
            double val;
            if (!double.TryParse(pos.Replace(',', '.'), out val))
            {
                MainForm.ShowMessageBox(
                    "MZ_CNC.SetYposition() called with bad value " + pos,
                    "BUG",
                    MessageBoxButtons.OK);
                return false;
            }
            Cnc.SetCurrentY(val);
            if (!Write_m("G92 Y" + pos))
            {
                MainForm.ShowMessageBox(
                    "MZ_CNC G92 Y" + pos + " failed",
                    "comm err?",
                    MessageBoxButtons.OK);
                return false;
            }
            return true;
        }

        public bool SetZposition(string pos)
        {
            double val;
            if (!double.TryParse(pos.Replace(',', '.'), out val))
            {
                MainForm.ShowMessageBox(
                    "MZ_CNC.SetZposition() called with bad value " + pos,
                    "BUG",
                    MessageBoxButtons.OK);
                return false;
            }
            Cnc.SetCurrentZ(val);
            if (!Write_m("G92 Z" + pos))
            {
                MainForm.ShowMessageBox(
                    "MZ_CNC G92 Z" + pos + " failed",
                    "comm err?",
                    MessageBoxButtons.OK);
                return false;
            }
            return true;
        }

        public bool SetAposition(string pos)
        {
            double val;
            if (!double.TryParse(pos.Replace(',', '.'), out val))
            {
                MainForm.ShowMessageBox(
                    "MZ_CNC.SetAposition() called with bad value " + pos,
                    "BUG",
                    MessageBoxButtons.OK);
                return false;
            }
            Cnc.SetCurrentA(val);
            if (!Write_m("G92 A" + pos))
            {
                MainForm.ShowMessageBox(
                    "MZ_CNC G92 A" + pos + " failed",
                    "comm err?",
                    MessageBoxButtons.OK);
                return false;
            }
            return true;
        }

        // Move to absolute position
        public bool XY(double X, double Y)
        {
            string command = "G0 X" + X.ToString("0.000", CultureInfo.InvariantCulture)
                           + " Y" + Y.ToString("0.000", CultureInfo.InvariantCulture);
            if (!Write_m(command, RegularMoveTimeout))
            {
                return false;
            }
            Cnc.SetCurrentX(X);
            Cnc.SetCurrentY(Y);
            return true;
        }

        public bool XYA(double X, double Y, double A)
        {
            string command = "G0 X" + X.ToString("0.000", CultureInfo.InvariantCulture)
                           + " Y" + Y.ToString("0.000", CultureInfo.InvariantCulture)
                           + " A" + A.ToString("0.000", CultureInfo.InvariantCulture);
            if (!Write_m(command, RegularMoveTimeout))
            {
                return false;
            }
            Cnc.SetCurrentX(X);
            Cnc.SetCurrentY(Y);
            Cnc.SetCurrentA(A);
            return true;
        }

        public bool Z(double Z)
        {
            string command = "G0 Z" + Z.ToString("0.000", CultureInfo.InvariantCulture);
            if (!Write_m(command, RegularMoveTimeout))
            {
                return false;
            }
            Cnc.SetCurrentZ(Z);
            return true;
        }

        public bool A(double A)
        {
            string command = "G0 A" + A.ToString("0.000", CultureInfo.InvariantCulture);
            if (!Write_m(command, RegularMoveTimeout))
            {
                return false;
            }
            Cnc.SetCurrentA(A);
            return true;
        }

        public bool XYZA(double X, double Y, double Z, double A)
        {
            string command = "G0 X" + X.ToString("0.000", CultureInfo.InvariantCulture)
                           + " Y" + Y.ToString("0.000", CultureInfo.InvariantCulture)
                           + " Z" + Z.ToString("0.000", CultureInfo.InvariantCulture)
                           + " A" + A.ToString("0.000", CultureInfo.InvariantCulture);
            if (!Write_m(command, RegularMoveTimeout))
            {
                return false;
            }
            Cnc.SetCurrentX(X);
            Cnc.SetCurrentY(Y);
            Cnc.SetCurrentZ(Z);
            Cnc.SetCurrentA(A);
            return true;
        }

        #endregion Movement

        // =================================================================================
        // Probing operations (CRITICAL for LitePlacer):
        #region Probing

        /// <summary>
        /// Probe down using G38.2 (probe toward workpiece, stop on contact, error if failed)
        /// Z-max limit switch is used as probe input (standard GRBL convention)
        /// </summary>
        public bool ProbeZ(double targetZ, double feedrate)
        {
            MainForm.DisplayText($"[PROBE] Starting Z probe to {targetZ}mm at F{feedrate}", KnownColor.DarkCyan);
            
            // Send G38.2 probe command
            string command = $"G38.2 Z{targetZ.ToString("0.000", CultureInfo.InvariantCulture)} F{feedrate.ToString("0.0", CultureInfo.InvariantCulture)}";
            
            if (!Write_m(command, RegularMoveTimeout * 2)) // Double timeout for probing
            {
                MainForm.DisplayText("*** Probe command failed", KnownColor.DarkRed);
                return false;
            }
            
            // Request probe result: [PRB:x,y,z,a:1]
            string probeResult = GetResponse_m("?", 500, true);
            
            // Parse probe position from status or PRB response
            if (!string.IsNullOrEmpty(probeResult))
            {
                MainForm.DisplayText("[PROBE] Result: " + probeResult, KnownColor.DarkGreen);
                // TODO: Parse probe position and update Cnc.CurrentZ
                // Format: [PRB:x.xxx,y.yyy,z.zzz,a.aaa:1]
            }
            else
            {
                MainForm.DisplayText("*** Probe result not received", KnownColor.DarkOrange);
            }
            
            return true;
        }

        /// <summary>
        /// Get current machine position via status query (?)
        /// </summary>
        public bool UpdatePosition()
        {
            string status = GetResponse_m("?", 250, false);
            if (string.IsNullOrEmpty(status))
            {
                return false;
            }
            
            // Parse GRBL status: <Idle|MPos:x,y,z,a|WPos:x,y,z,a|FS:f,s>
            // TODO: Parse and update Cnc.CurrentX/Y/Z/A
            MainForm.DisplayText(status);
            return true;
        }

        #endregion Probing

        // =================================================================================
        // Homing:
        #region Homing

        public bool Homing()
        {
            MainForm.DisplayText("Starting homing cycle ($H)...");
            Cnc.Homing = true;
            
            if (!Write_m("$H", 30000)) // 30 second timeout for homing
            {
                Cnc.Homing = false;
                return false;
            }
            
            Cnc.Homing = false;
            MainForm.DisplayText("Homing complete", KnownColor.DarkGreen);
            
            // Update position after homing (machine zero)
            Cnc.SetCurrentX(0);
            Cnc.SetCurrentY(0);
            Cnc.SetCurrentZ(0);
            Cnc.SetCurrentA(0);
            
            return true;
        }

        #endregion Homing
    }
}
