using System;
using System.Threading;
using System.Windows.Forms;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Globalization;
using AForge.Math.Geometry;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;

namespace LitePlacer
{
    public class SKR3class
    {
        FormMain MainForm;
        CNC Cnc;
        SerialComm Com;

        public SKR3class(FormMain MainF, CNC C, SerialComm ser)
        {
            MainForm = MainF;
            Cnc = C;
            Com = ser;
        }

        public int RegularMoveTimeout { get; set; } // in ms
        
        // CONCURRENCY FIX #1: Add lock object for thread-safe access to shared state
        private readonly object writeLock = new object();

        // =================================================================================
        #region Communications

        public bool JustConnected()
        {
            MainForm.DisplayText("SKR3class.JustConnected()");
            MainForm.SKR3Settings_Load();
            return true;
        }


        // ===================================================================
        // Read & write
        // ===================================================================

        // CONCURRENCY FIX #2: Protected by writeLock
        private bool LineAvailable = false;
        private string ReceivedLine = "";
        private bool WriteBusy = false;
        private bool ExpectingResponse = false;

        // so that we don't need to write lock... so many times
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

            lock (writeLock)
            {
                WriteBusy = true;
            }
            
            Timeout = Timeout / 2;
            int i = 0;
            bool WriteOk = Com.Write(cmd);
            
            bool busy = true;
            while (busy)
            {
                Thread.Sleep(2);
                // CONCURRENCY FIX #3: Removed Application.DoEvents() to prevent reentrancy
                
                lock (writeLock)
                {
                    busy = WriteBusy;
                }
                
                i++;
                if (i > Timeout)
                {
                    MainForm.ShowMessageBox(
                        "SKR3.Write_m: Timeout on command " + cmd,
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
            string line="";

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
                ExpectingResponse = true;
            }
            
            Timeout = Timeout / 2;
            int i = 0;
            Com.Write(cmd);
            
            bool available = false;
            while (!available)
            {
                Thread.Sleep(2);
                // CONCURRENCY FIX #4: Removed Application.DoEvents() to prevent reentrancy
                
                lock (writeLock)
                {
                    available = LineAvailable;
                }
                
                i++;
                if (i > Timeout)
                {
                    if (report)
                    {
                        MainForm.ShowMessageBox(
                            "Cnc.SKR3.Write_m: Timeout on command " + cmd,
                            "Timeout",
                            MessageBoxButtons.OK);
                    }
                    ClearReceivedLine();
                    return "";
                }
            }
            lock (ReceivedLine)
            {
                line = ReceivedLine;
                ClearReceivedLine();
            }
            lock (writeLock)
            {
                ExpectingResponse = false;
            }
            return line;
        }


        // ===================================================================

        public void LineReceived(string line)
        {
            // This is called from Cnc.LineReceived (called from SerialComm dataReceived),
            // and runs in a separate thread than UI
            // The response can be multiline, but we get only one line at a time.
            // I don't want to break TinyG communications, so I add lines here if needed.
            // Not too elegant, but works.

            // CONCURRENCY FIX #5: Use Invoke() for thread-safe UI updates
            MainForm.Invoke((MethodInvoker)delegate
            {
                MainForm.DisplayText("<== " + line);
            });
            
            if (line == "ok")
            {
                lock (writeLock)
                {
                    WriteBusy = false;
                }
                return;
            }
            
            lock (ReceivedLine)
            {
                if (ReceivedLine=="")
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
            
            bool expecting;
            lock (writeLock)
            {
                expecting = ExpectingResponse;
            }
            
            if (!expecting)
            {
                MainForm.Invoke((MethodInvoker)delegate
                {
                    MainForm.DisplayText("*** SKR3() - unsoliticed message", KnownColor.DarkRed, true);
                });
            }
        }


        // ===================================================================
        // For operations that don't give response
        // Caller does waiting, if needed.
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

        private bool SetXposition(string pos)
        {
            double val;
            if (!double.TryParse(pos.Replace(',', '.'), out val))
            {
                MainForm.ShowMessageBox(
                    "SKR3.SetXposition() called with bad value " + pos,
                    "BUG",
                    MessageBoxButtons.OK);
                return false;
            }
            Cnc.SetCurrentX(val);
            if (!Write_m("G92 X" + pos))
            {
                MainForm.ShowMessageBox(
                    "SKR3 G92 X" + pos + " failed",
                    "comm err?",
                    MessageBoxButtons.OK);
                return false;
            }
            return true;
        }

        private bool SetYposition(string pos)
        {
            double val;
            if (!double.TryParse(pos.Replace(',', '.'), out val))
            {
                MainForm.ShowMessageBox(
                    "SKR3.SetYposition() called with bad value " + pos,
                    "BUG",
                    MessageBoxButtons.OK);
                return false;
            }
            Cnc.SetCurrentY(val);
            if (!Write_m("G92 Y" + pos))
            {
                MainForm.ShowMessageBox(
                    "SKR3 G92 Y" + pos + " failed",
                    "comm err?",
                    MessageBoxButtons.OK);
                return false;
            }
            return true;
        }

        private bool SetZposition(string pos)
        {
            double val;
            if (!double.TryParse(pos.Replace(',', '.'), out val))
            {
                MainForm.ShowMessageBox(
                    "SKR3.SetZposition() called with bad value " + pos,
                    "BUG",
                    MessageBoxButtons.OK);
                return false;
            }
            Cnc.SetCurrentZ(val);
            if (!Write_m("G92 Z" + pos))
            {
                MainForm.ShowMessageBox(
                    "SKR3 G92 Z" + pos + " failed",
                    "comm err?",
                    MessageBoxButtons.OK);
                return false;
            }
            return true;
        }

        private bool SetAposition(string pos)
        {
            double val;
            if (!double.TryParse(pos.Replace(',', '.'), out val))
            {
                MainForm.ShowMessageBox(
                    "SKR3.SetAposition() called with bad value " + pos,
                    "BUG",
                    MessageBoxButtons.OK);
                return false;
            }
            Cnc.SetCurrentA(val);
            if (!Write_m("G92 A" + pos))
            {
                MainForm.ShowMessageBox(
                    "SKR3 G92 A" + pos + " failed",
                    "comm err?",
                    MessageBoxButtons.OK);
                return false;
            }
            return true;
        }


        public void SetPosition(string Xstr, string Ystr, string Zstr, string Astr)
        {
            if (Xstr != "")
            {
                SetXposition(Xstr);
            };
            if (Ystr != "")
            {
                SetYposition(Ystr);
            };
            if (Zstr != "")
            {
                SetZposition(Zstr);
            };
            if (Astr != "")
            {
                SetAposition(Astr);
            };
        }



        public void CancelJog()
        {
            MainForm.ShowMessageBox("Unimplemented SKR3 function CancelJog", "Unimplemented function", MessageBoxButtons.OK);
        }



        public void Jog(string Speed, string X, string Y, string Z, string A)
        {
            MainForm.ShowMessageBox("Unimplemented SKR3 function Jog", "Unimplemented function", MessageBoxButtons.OK);
        }

        // =================================================================================
        // homing

        private bool HomingTimeout_m(out int TimeOut, string axis)
        {
            /*
            double Speed;
            double size;
            TimeOut = 0;
            switch (axis)
            {
                case "X":
                    Speed = MainForm.Setting.SKR3_XHomingSpeed;
                    size = MainForm.Setting.General_MachineSizeX;
                    break;

                case "Y":
                    // Speed = MainForm.Setting.SKR3_YHomingSpeed;
                    size = MainForm.Setting.General_MachineSizeY;
                    break;

                case "Z":
                    Speed = MainForm.Setting.SKR3_ZHomingSpeed;
                    size = 100.0;
                    break;

                default:
                    return false;
            }

            Speed = Speed / 60;  // Was mm/min, now in mm / second
            Double MaxTime = (size / Speed) * 1.2 + 4;
            // in seconds for the machine size and some (1.2 to allow acceleration, + 4 for the operarations at end stop
            TimeOut = (int)MaxTime * 1000;  // to ms
            */
            TimeOut = 1;
            return true;
        }


        public bool Home_m(string axis)
        {
            MainForm.ShowMessageBox("Unimplemented SKR3 function Home_m: axis " + axis,
                "Unimplemented function", MessageBoxButtons.OK);
            return false;
        }


        public bool XYA(double X, double Y, double A, double speed, string MoveType)
        {
            string command;
            if (MoveType == "G1")
            {
                command = "G1 F" + speed.ToString() +
                    " X" + X.ToString("0.000", CultureInfo.InvariantCulture) +
                    " Y" + Y.ToString("0.000", CultureInfo.InvariantCulture) +
                    " A" + A.ToString("0.000", CultureInfo.InvariantCulture);
            }
            else
            {
                command = "G0 " +
                    " X" + X.ToString("0.000", CultureInfo.InvariantCulture) +
                    " Y" + Y.ToString("0.000", CultureInfo.InvariantCulture) +
                    " A" + A.ToString("0.000", CultureInfo.InvariantCulture);
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


        public bool A(double A, double speed, string MoveType)
        {
            string command;
            if (MoveType == "G1")
            {
                command = "G1 F" + speed.ToString() +
                    " A" + A.ToString("0.000", CultureInfo.InvariantCulture);
            }
            else
            {
                command = "G0 " +
                    " A" + A.ToString("0.000", CultureInfo.InvariantCulture);
            }
            if (!Write_m(command, RegularMoveTimeout))
            {
                return false;
            }
            Cnc.SetCurrentA(A);
            return true;
        }


        public bool Z(double Z, double speed, string MoveType)
        {
            string command;
            if (MoveType == "G1")
            {
                command = "G1 F" + speed.ToString() +
                    " Z" + Z.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                command = "G0 " +
                    " Z" + Z.ToString(CultureInfo.InvariantCulture);
            }
            if (!Write_m(command, RegularMoveTimeout))
            {
                return false;
            }
            Cnc.SetCurrentZ(Z);
            return true;
        }


        #endregion Movement


        // =================================================================================
        // Hardware features: probing, pump, vacuum, motor power
        #region Features

        public bool SetMachineSizeX()
        {
            int MaxSixe = (int)Math.Round(MainForm.Setting.General_MachineSizeX) + 3;
            int MinSize = (int)Math.Round(MainForm.Setting.General_NegativeX);
            MainForm.DisplayText("SKR3 SetMachineSizeX() not implemented");
            return true;
        }

        public bool SetMachineSizeY()
        {
            int MaxSixe = (int)Math.Round(MainForm.Setting.General_MachineSizeY) + 3;
            int MinSize = (int)Math.Round(MainForm.Setting.General_NegativeY);
            MainForm.DisplayText("SKR3 SetMachineSizeY() not implemented");
            return true;
        }


        public void DisableZswitches()
        {
            MainForm.ShowMessageBox("Unimplemented SKR3 function DisableZswitches", "Unimplemented function", MessageBoxButtons.OK);
        }



        public void EnableZswitches()
        {
            MainForm.DisplayText("SKR3 EnableZswitches() not implemented");
        }


        public bool Nozzle_ProbeDown(double backoff)
        {
            MainForm.ShowMessageBox("Unimplemented SKR3 function Nozzle_ProbeDown", "Unimplemented function", MessageBoxButtons.OK);
            return false;
        }



        public void MotorPowerOn()
        {
            MainForm.DisplayText("SKR3 MotorPowerOn() not implemented");
        }



        public void MotorPowerOff()
        {
            MainForm.DisplayText("SKR3 MotorPowerOff() not implemented");
        }



        public void VacuumOn()
        {
            MainForm.DisplayText("SKR3 VacuumOn() not implemented");
        }



        public void VacuumOff()
        {
            MainForm.DisplayText("SKR3 VacuumOff() not implemented");
        }


        public void PumpOn()
        {
            MainForm.DisplayText("SKR3 PumpOn() not implemented");
        }



        public void PumpOff()
        {
            MainForm.DisplayText("SKR3 PumpOff() not implemented");
        }

        #endregion Features


    }
}
