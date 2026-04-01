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
    public class MZ_CNCControl
    {
        FormMain MainForm;
        CNC Cnc;
        SerialComm Com;

        public MZ_CNCControl(FormMain MainF, CNC C, SerialComm ser)
        {
            MainForm = MainF;
            Cnc = C;
            Com = ser;
        }

        public int RegularMoveTimeout { get; set; } // in ms
        
        // CONCURRENCY FIX #1: Instance-based event (already correct)
        private ManualResetEvent responseReceivedEvent = new ManualResetEvent(false);
        
        // CONCURRENCY FIX #2: Add lock object for thread-safe access to shared state
        private readonly object writeLock = new object();
        
        // GRBL Settings Storage - parsed from $$ command
        public class GRBLSettings
        {
            public double StepPulseTime { get; set; }           // $0
            public double StepIdleDelay { get; set; }           // $1
            public int StepPortInvert { get; set; }             // $2
            public int DirPortInvert { get; set; }              // $3
            public int StepEnableInvert { get; set; }           // $4
            public int LimitPinsInvert { get; set; }            // $5
            public double JunctionDeviation { get; set; }       // $11
            public double ArcTolerance { get; set; }            // $12
            public double ReportInches { get; set; }            // $13
            public int SoftLimits { get; set; }                 // $21
            public int HardLimits { get; set; }                 // $22
            public int HomingEnable { get; set; }               // $23
            public double HomingDirInvert { get; set; }         // $24
            public double HomingFeed { get; set; }              // $25
            public double HomingSeek { get; set; }              // $26
            public double HomingDebounce { get; set; }          // $27
            public double HomingPulloff { get; set; }           // $30
            public double MaxSpindleSpeed { get; set; }         // $31
            public double StepsPerMmX { get; set; }             // $100
            public double StepsPerMmY { get; set; }             // $101
            public double StepsPerMmZ { get; set; }             // $102
            public double StepsPerMmA { get; set; }             // $103
            public double MaxRateX { get; set; }                // $110
            public double MaxRateY { get; set; }                // $111
            public double MaxRateZ { get; set; }                // $112
            public double MaxRateA { get; set; }                // $113
            public double AccelX { get; set; }                  // $120
            public double AccelY { get; set; }                  // $121
            public double AccelZ { get; set; }                  // $122
            public double AccelA { get; set; }                  // $123
            public double MaxTravelX { get; set; }              // $130
            public double MaxTravelY { get; set; }              // $131
            public double MaxTravelZ { get; set; }              // $132
        }
        
        public GRBLSettings Settings { get; private set; } = new GRBLSettings();
        
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
                return false; // Fail if settings don't load properly
            }
            
            // Small delay to ensure all setting responses are fully processed
            Thread.Sleep(100);
            
            // Set machine to absolute positioning mode (G90)
            if (!Write_m("G90"))
            {
                MainForm.DisplayText("*** G90 command failed", KnownColor.DarkRed);
                return false;
            }
            
            // Set units to millimeters (G21)
            if (!Write_m("G21"))
            {
                MainForm.DisplayText("*** G21 command failed", KnownColor.DarkRed);
                return false;
            }
            
            // Select XY plane (G17)
            if (!Write_m("G17"))
            {
                MainForm.DisplayText("*** G17 command failed", KnownColor.DarkRed);
                return false;
            }
            
            // Clear any alarm state (don't fail if this doesn't work)
            Write_m("$X", 500);
            
            // Load settings UI
            MainForm.MZ_CNCSettings_Load();
            
            MainForm.DisplayText("MZ_CNC initialization complete", KnownColor.DarkGreen);
            return true;
        }

        private bool LoadGRBLSettings()
        {
            // Request all settings from GRBL
            // Use Write_m() to properly wait for "ok" completion
            MainForm.DisplayText("=== Loading GRBL Settings ($$) ===", KnownColor.DarkCyan);
            
            // Write_m waits for "ok" which comes AFTER all setting lines
            // Need longer timeout - 40+ settings lines take time
            if (!Write_m("$$", 3000)) // 3 second timeout for all settings to arrive
            {
                MainForm.DisplayText("*** Could not load GRBL settings", KnownColor.DarkRed);
                return false;
            }
            
            // Display key parsed settings
            MainForm.DisplayText($"=== GRBL Settings Loaded ===", KnownColor.DarkGreen);
            MainForm.DisplayText($"  Machine Size: X={Settings.MaxTravelX:0.0}, Y={Settings.MaxTravelY:0.0}, Z={Settings.MaxTravelZ:0.0} mm", KnownColor.DarkCyan);
            MainForm.DisplayText($"  Max Rates: X={Settings.MaxRateX:0.0}, Y={Settings.MaxRateY:0.0}, Z={Settings.MaxRateZ:0.0}, A={Settings.MaxRateA:0.0} mm/min", KnownColor.DarkCyan);
            MainForm.DisplayText($"  Steps/mm: X={Settings.StepsPerMmX:0.0}, Y={Settings.StepsPerMmY:0.0}, Z={Settings.StepsPerMmZ:0.0}, A={Settings.StepsPerMmA:0.0}", KnownColor.DarkCyan);
            MainForm.DisplayText($"  Homing: Enable={Settings.HomingEnable}, Hard Limits={Settings.HardLimits}", KnownColor.DarkCyan);
            
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
        private readonly object responseLock = new object();
        
        // CONCURRENCY FIX #3: Protected by writeLock
        private bool LineAvailable = false;
        private string ReceivedLine = "";
        private bool WriteBusy = false;
       

        private void ClearReceivedLine()
        {
            lock (responseLock)
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

            lock (writeLock)
            {
                WriteBusy = true;
            }
            
            Timeout = Timeout / 2;
            int i = 0;
            responseReceivedEvent.Reset();
            bool WriteOk = Com.Write(cmd);
            
            bool busy = true;
            while (busy)
            {
                Thread.Sleep(2);
                // CONCURRENCY FIX #4: Removed Application.DoEvents() to prevent reentrancy
                
                lock (writeLock)
                {
                    busy = WriteBusy;
                }
                
                i++;
                if (i > Timeout)
                {
                    // Use DisplayText instead of ShowMessageBox to avoid UI freeze
                    MainForm.DisplayText("*** MZ_CNC.Write_m: Timeout on command " + cmd, KnownColor.DarkRed, true);
                    ClearReceivedLine();
                    lock (writeLock)
                    {
                        WriteBusy = false; // Reset state on timeout
                    }
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

            lock (writeLock)
            {
                LineAvailable = false;
            }
            
            Timeout = Timeout / 2;
            int i = 0;
          
            Com.Write(cmd);
            
            bool available = false;
            while (!available)
            {
                Thread.Sleep(2);
                // CONCURRENCY FIX #5: Removed Application.DoEvents() to prevent reentrancy
                
                lock (writeLock)
                {
                    available = LineAvailable;
                }
                
                i++;
                if (i > Timeout)
                {
                    if (report)
                    {
                        // Use DisplayText instead of ShowMessageBox to avoid UI freeze
                        MainForm.DisplayText("*** MZ_CNC.GetResponse_m: Timeout on command " + cmd, KnownColor.DarkRed, true);
                    }
                    ClearReceivedLine();
                  
                    return "";
                }
            }
            lock (responseLock)
            {
                line = ReceivedLine;
                ClearReceivedLine();
               
            }
            return line;
        }

        // ===================================================================
        // LineReceived
        // Called from SerialComm when data arrives
        public void LineReceived(string line)
        {
            // CONCURRENCY FIX #6: Use BeginInvoke() for non-blocking UI updates
            MainForm.BeginInvoke((MethodInvoker)delegate
            {
                MainForm.DisplayText("<== " + line);
            });
            
            // Handle "ok" response (command completed)
            if (line == "ok")
            {
                lock (writeLock)
                {
                    WriteBusy = false;
                }
                return;
            }
            
            // Parse GRBL settings lines (format: $120=500.000)
            if (line.StartsWith("$") && line.Contains("="))
            {
                ParseGRBLSetting(line);
                // Don't return - let it accumulate for GetResponse_m too
            }
            
            // Handle error responses
            if (line.StartsWith("error:"))
            {
                MainForm.BeginInvoke((MethodInvoker)delegate
                {
                    MainForm.DisplayText("*** GRBL Error: " + line, KnownColor.DarkRed, true);
                });
                lock (writeLock)
                {
                    WriteBusy = false;
                }
                return;
            }
            
            // Handle alarm responses
            if (line.StartsWith("ALARM:"))
            {
                MainForm.BeginInvoke((MethodInvoker)delegate
                {
                    MainForm.DisplayText("*** GRBL ALARM: " + line, KnownColor.DarkRed, true);
                    MainForm.DisplayText("*** Send $X to clear alarm", KnownColor.DarkOrange);
                });
                lock (writeLock)
                {
                    WriteBusy = false;
                }
                Cnc.ErrorState = true;
                return;
            }
            
            // Accumulate multi-line responses
            lock (responseLock)
            {
                if (ReceivedLine == "")
                {
                    ReceivedLine = line;
                }
                else
                {
                    ReceivedLine += MainForm.Setting.Serial_EndCharacters + line;
                }
            }
            lock (writeLock)
            {
                LineAvailable = true;
            }
        }
        
        /// <summary>
        /// Parse a single GRBL setting line (e.g., "$120=500.000")
        /// </summary>
        private void ParseGRBLSetting(string line)
        {
            try
            {
                // Format: $NNN=value
                int equalsPos = line.IndexOf('=');
                if (equalsPos < 2) return; // Need at least "$N="
                
                string numberPart = line.Substring(1, equalsPos - 1); // Skip '$', get number
                string valuePart = line.Substring(equalsPos + 1);
                
                if (!int.TryParse(numberPart, out int settingNum)) return;
                if (!double.TryParse(valuePart, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) return;
                
                // Map setting number to property
                switch (settingNum)
                {
                    case 0: Settings.StepPulseTime = value; break;
                    case 1: Settings.StepIdleDelay = value; break;
                    case 2: Settings.StepPortInvert = (int)value; break;
                    case 3: Settings.DirPortInvert = (int)value; break;
                    case 4: Settings.StepEnableInvert = (int)value; break;
                    case 5: Settings.LimitPinsInvert = (int)value; break;
                    case 11: Settings.JunctionDeviation = value; break;
                    case 12: Settings.ArcTolerance = value; break;
                    case 13: Settings.ReportInches = value; break;
                    case 21: Settings.SoftLimits = (int)value; break;
                    case 22: Settings.HardLimits = (int)value; break;
                    case 23: Settings.HomingEnable = (int)value; break;
                    case 24: Settings.HomingDirInvert = value; break;
                    case 25: Settings.HomingFeed = value; break;
                    case 26: Settings.HomingSeek = value; break;
                    case 27: Settings.HomingPulloff = value; break;  // *** CRITICAL FIX: $27 is pull-off in mm
                    case 30: Settings.MaxSpindleSpeed = value; break;  // *** CRITICAL FIX: $30 is spindle RPM
                    case 31: break;  // Reserved
                    case 100: Settings.StepsPerMmX = value; break;
                    case 101: Settings.StepsPerMmY = value; break;
                    case 102: Settings.StepsPerMmZ = value; break;
                    case 103: Settings.StepsPerMmA = value; break;
                    case 110: Settings.MaxRateX = value; break;
                    case 111: Settings.MaxRateY = value; break;
                    case 112: Settings.MaxRateZ = value; break;
                    case 113: Settings.MaxRateA = value; break;
                    case 120: Settings.AccelX = value; break;
                    case 121: Settings.AccelY = value; break;
                    case 122: Settings.AccelZ = value; break;
                    case 123: Settings.AccelA = value; break;
                    case 130: Settings.MaxTravelX = value; break;
                    case 131: Settings.MaxTravelY = value; break;
                    case 132: Settings.MaxTravelZ = value; break;
                }
            }
            catch (Exception ex)
            {
                MainForm.DisplayText("*** Error parsing GRBL setting: " + ex.Message, KnownColor.DarkOrange);
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

        // Combined position set for all axes - CHANGED TO VOID to match CNC.cs signature
        public void SetPosition(string X, string Y, string Z, string A)
        {
            string command = "G92";
            bool hasValues = false;

            if (!string.IsNullOrEmpty(X))
            {
                double val;
                if (!double.TryParse(X.Replace(',', '.'), out val))
                {
                    MainForm.ShowMessageBox(
                        "MZ_CNC.SetPosition() called with bad X value " + X,
                        "BUG",
                        MessageBoxButtons.OK);
                    return;
                }
                command += " X" + X;
                Cnc.SetCurrentX(val);
                hasValues = true;
            }

            if (!string.IsNullOrEmpty(Y))
            {
                double val;
                if (!double.TryParse(Y.Replace(',', '.'), out val))
                {
                    MainForm.ShowMessageBox(
                        "MZ_CNC.SetPosition() called with bad Y value " + Y,
                        "BUG",
                        MessageBoxButtons.OK);
                    return;
                }
                command += " Y" + Y;
                Cnc.SetCurrentY(val);
                hasValues = true;
            }

            if (!string.IsNullOrEmpty(Z))
            {
                double val;
                if (!double.TryParse(Z.Replace(',', '.'), out val))
                {
                    MainForm.ShowMessageBox(
                        "MZ_CNC.SetPosition() called with bad Z value " + Z,
                        "BUG",
                        MessageBoxButtons.OK);
                    return;
                }
                command += " Z" + Z;
                Cnc.SetCurrentZ(val);
                hasValues = true;
            }

            if (!string.IsNullOrEmpty(A))
            {
                double val;
                if (!double.TryParse(A.Replace(',', '.'), out val))
                {
                    MainForm.ShowMessageBox(
                        "MZ_CNC.SetPosition() called with bad A value " + A,
                        "BUG",
                        MessageBoxButtons.OK);
                    return;
                }
                command += " A" + A;
                Cnc.SetCurrentA(val);
                hasValues = true;
            }

            if (!hasValues)
            {
                MainForm.DisplayText("*** MZ_CNC.SetPosition() called with no values", KnownColor.DarkRed);
                return;
            }

            if (!Write_m(command))
            {
                MainForm.ShowMessageBox(
                    "MZ_CNC " + command + " failed",
                    "comm err?",
                    MessageBoxButtons.OK);
            }
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

        // XYA with speed and move type (called from CNC.Execute_XYA)
        public bool XYA(double X, double Y, double A, double speed, string MoveType)
        {
            string command = MoveType; // "G0" for rapid, "G1" for feed
            command += " X" + X.ToString("0.000", CultureInfo.InvariantCulture);
            command += " Y" + Y.ToString("0.000", CultureInfo.InvariantCulture);
            command += " A" + A.ToString("0.000", CultureInfo.InvariantCulture);
            
            if (MoveType == "G1") // Feed move - include feedrate
            {
                command += " F" + speed.ToString("0.0", CultureInfo.InvariantCulture);
            }
            
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

        // Overload to match CNC.Execute_Z(double Z, double speed, string MoveType)
        public bool Z(double Z, double speed, string MoveType)
        {
            string command;
            if (MoveType == "G1")
            {
                command = "G1 Z" + Z.ToString("0.000", CultureInfo.InvariantCulture) + " F" + speed.ToString("0.0", CultureInfo.InvariantCulture);
            }
            else
            {
                command = "G0 Z" + Z.ToString("0.000", CultureInfo.InvariantCulture);
            }
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

        // Overload to match CNC.Execute_A(double A, double speed, string MoveType)
        public bool A(double A, double speed, string MoveType)
        {
            string command;
            if (MoveType == "G1")
            {
                command = "G1 A" + A.ToString("0.000", CultureInfo.InvariantCulture) + " F" + speed.ToString("0.0", CultureInfo.InvariantCulture);
            }
            else
            {
                command = "G0 A" + A.ToString("0.000", CultureInfo.InvariantCulture);
            }
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

        // Jogging support (manual control)
        public void Jog(string Speed, string X, string Y, string Z, string A)
        {
            // GRBL jog mode: $J=G91 X10 Y10 F500
            // G91 = incremental mode for jogging
            string command = "$J=G91";
            
            if (!string.IsNullOrEmpty(X))
            {
                command += " X" + X;
            }
            if (!string.IsNullOrEmpty(Y))
            {
                command += " Y" + Y;
            }
            if (!string.IsNullOrEmpty(Z))
            {
                command += " Z" + Z;
            }
            if (!string.IsNullOrEmpty(A))
            {
                command += " A" + A;
            }
            if (!string.IsNullOrEmpty(Speed))
            {
                command += " F" + Speed;
            }
            
            RawWrite(command); // Jog doesn't wait for completion
            MainForm.DisplayText("[JOG] " + command, KnownColor.DarkCyan);
        }

        public void CancelJog()
        {
            // GRBL jog cancel: Send 0x85 (jog cancel character)
            // Alternative: Feed hold (!) will stop any motion
            RawWrite("\x85"); // Jog cancel
            MainForm.DisplayText("[JOG] Cancelled", KnownColor.DarkOrange);
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
            
            if (!Write_m(command, RegularMoveTimeout * 3)) // Triple timeout for probing
            {
                MainForm.DisplayText("*** Probe command failed", KnownColor.DarkRed);
                return false;
            }
            
            // Get probe result with status query
            Thread.Sleep(100); // Wait for probe to settle
            string status = GetResponse_m("?", 500, false);
            
            // Parse probe result: Look for [PRB:x,y,z,a:1] in response
            if (!string.IsNullOrEmpty(status) && status.Contains("[PRB:"))
            {
                if (ParseProbeResult(status, out double probeZ))
                {
                    MainForm.DisplayText($"[PROBE] Success - Triggered at Z={probeZ:0.000}mm", KnownColor.DarkGreen);
                    Cnc.SetCurrentZ(probeZ);
                    return true;
                }
            }
            
            // Fallback: Update position with standard status query
            UpdatePosition();
            MainForm.DisplayText("[PROBE] Complete - check position", KnownColor.DarkOrange);
            return true;
        }

        /// <summary>
        /// Nozzle probe down for LitePlacer (CRITICAL method)
        /// </summary>
        public bool Nozzle_ProbeDown(double backoff)
        {
            MainForm.DisplayText("[PROBE] Nozzle probing down...", KnownColor.DarkCyan);
            
            // Probe down with safe feedrate (300 mm/min typical for Z probing)
            double probeDistance = -100.0; // Probe down 100mm (negative Z in GRBL)
            double probeFeedrate = 300.0;
            
            if (!ProbeZ(probeDistance, probeFeedrate))
            {
                MainForm.DisplayText("*** Nozzle probe failed", KnownColor.DarkRed);
                return false;
            }
            
            // Back off after probe contact
            if (backoff > 0.001)
            {
                double currentZ = Cnc.CurrentZ;
                double backoffZ = currentZ + backoff; // Move up (positive) by backoff amount
                
                MainForm.DisplayText($"[PROBE] Backing off {backoff}mm to Z={backoffZ:0.000}", KnownColor.DarkCyan);
                if (!Z(backoffZ))
                {
                    MainForm.DisplayText("*** Backoff move failed", KnownColor.DarkOrange);
                }
            }
            
            MainForm.DisplayText("[PROBE] Nozzle probe complete", KnownColor.DarkGreen);
            return true;
        }

        /// <summary>
        /// Parse probe result from GRBL response: [PRB:x,y,z,a:1]
        /// </summary>
        private bool ParseProbeResult(string response, out double probeZ)
        {
            probeZ = 0;
            
            try
            {
                int prbStart = response.IndexOf("[PRB:");
                if (prbStart < 0) return false;
                
                int prbEnd = response.IndexOf("]", prbStart);
                if (prbEnd < 0) return false;
                
                string prbData = response.Substring(prbStart + 5, prbEnd - prbStart - 5);
                // Format: x.xxx,y.yyy,z.zzz,a.aaa:1
                
                string[] parts = prbData.Split(':');
                if (parts.Length < 2) return false;
                
                string[] coords = parts[0].Split(',');
                if (coords.Length < 3) return false;
                
                // Parse Z coordinate (index 2)
                if (double.TryParse(coords[2], NumberStyles.Float, CultureInfo.InvariantCulture, out probeZ))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                MainForm.DisplayText("*** Probe result parse error: " + ex.Message, KnownColor.DarkRed);
            }
            
            return false;
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
            if (ParseGRBLStatus(status, out double x, out double y, out double z, out double a))
            {
                Cnc.SetCurrentX(x);
                Cnc.SetCurrentY(y);
                Cnc.SetCurrentZ(z);
                Cnc.SetCurrentA(a);
                return true;
            }
            
            MainForm.DisplayText("*** Could not parse position from status", KnownColor.DarkOrange);
            return false;
        }

        /// <summary>
        /// Parse GRBL status response: <Idle|MPos:x,y,z,a|...>
        /// </summary>
        private bool ParseGRBLStatus(string status, out double x, out double y, out double z, out double a)
        {
            x = y = z = a = 0;
            
            try
            {
                // Look for MPos:x,y,z,a pattern
                int mposStart = status.IndexOf("MPos:");
                if (mposStart < 0) return false;
                
                int mposEnd = status.IndexOf("|", mposStart);
                if (mposEnd < 0) mposEnd = status.IndexOf(">", mposStart);
                if (mposEnd < 0) return false;
                
                string coordData = status.Substring(mposStart + 5, mposEnd - mposStart - 5);
                string[] coords = coordData.Split(',');
                
                if (coords.Length >= 3)
                {
                    double.TryParse(coords[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x);
                    double.TryParse(coords[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y);
                    double.TryParse(coords[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z);
                    
                    if (coords.Length >= 4)
                    {
                        double.TryParse(coords[3], NumberStyles.Float, CultureInfo.InvariantCulture, out a);
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                MainForm.DisplayText("*** Status parse error: " + ex.Message, KnownColor.DarkRed);
            }
            
            return false;
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

        /// <summary>
        /// Home specific axis or all axes
        /// </summary>
        public bool Home_m(string axis)
        {
            MainForm.DisplayText($"[HOME] Starting homing for {axis}...", KnownColor.DarkCyan);
            Cnc.Homing = true;
            
            // GRBL $H homes all axes - no single-axis homing in standard GRBL
            // For single-axis, we use $H and warn user
            if (axis != "all" && axis != "")
            {
                MainForm.DisplayText($"[HOME] Note: GRBL homes all axes together (no single-axis homing)", KnownColor.DarkOrange);
            }
            
            if (!Write_m("$H", 30000)) // 30 second timeout
            {
                Cnc.Homing = false;
                MainForm.DisplayText("*** Homing failed", KnownColor.DarkRed);
                return false;
            }
            
            Cnc.Homing = false;
            MainForm.DisplayText("[HOME] Complete", KnownColor.DarkGreen);
            
            // Update position to machine zero
            Cnc.SetCurrentX(0);
            Cnc.SetCurrentY(0);
            Cnc.SetCurrentZ(0);
            Cnc.SetCurrentA(0);
            
            return true;
        }

        #endregion Homing

        // =================================================================================
        // Hardware Features - Motor Power, Vacuum, Pump
        #region Hardware Features

        public void MotorPowerOn()
        {
            MainForm.DisplayText("MotorPowerOn(), MZ_CNC");
            // GRBL motors auto-enable on motion commands
            // No explicit enable needed, but we track the state
            MainForm.ResetMotorTimer();
        }

        public void MotorPowerOff()
        {
            MainForm.DisplayText("MotorPowerOff(), MZ_CNC");
            MainForm.TimerDone = true;
            RawWrite("M18");  // Disable all steppers (standard GRBL)
        }

        public void VacuumOn()
        {
            MainForm.DisplayText("VacuumOn(), MZ_CNC");
            RawWrite("M7");  // Mist coolant = Vacuum control
        }

        public void VacuumOff()
        {
            MainForm.DisplayText("VacuumOff(), MZ_CNC");
            RawWrite("M9");  // All coolant off
        }

        public void PumpOn()
        {
            MainForm.DisplayText("PumpOn(), MZ_CNC");
            RawWrite("M8");  // Flood coolant = Pump control
        }

        public void PumpOff()
        {
            MainForm.DisplayText("PumpOff(), MZ_CNC");
            RawWrite("M9");  // All coolant off
        }

        #endregion Hardware Features
    }
}
