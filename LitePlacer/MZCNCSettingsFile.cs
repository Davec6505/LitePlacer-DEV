using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace LitePlacer
{
#pragma warning disable CA1031 // Do not catch general exception types (see MainForm.cs beginning)

    // =================================================================================
    // MZ_CNC Settings File Management
    // =================================================================================
    // This file handles save/load of MZ_CNC controller settings to/from JSON files
    // Following the same pattern as TinyG settings for consistency
    // =================================================================================

    public partial class FormMain : Form
    {
        // =================================================================================
        // MZ_CNC Settings Save/Load (follows TinyG pattern)
        // =================================================================================

        // Board settings file format: Text file starting with 8 characters board name and \n\r,
        // then the settings in Json format
        private bool SaveMZCNCSettings(MZCNCSettings settings, string fileName)
        {
            try
            {
                DisplayText("Writing MZ_CNC settings file: " + fileName);
                string json = "MZ_CNC  \n\r" + JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(fileName, json);
                DisplayText("Done.", KnownColor.DarkGreen);
                return true;
            }
            catch (Exception excep)
            {
                DisplayText("*** Saving MZ_CNC settings to " + fileName + " failed:\n" + excep.Message, KnownColor.DarkRed, true);
                return false;
            }
        }

        private bool LoadMZCNCSettings(ref MZCNCSettings settings, string fileName)
        {
            string content = "";
            try
            {
                if (File.Exists(fileName))
                {
                    DisplayText("Reading MZ_CNC settings from " + fileName);
                    content = File.ReadAllText(fileName);
                }
                else
                {
                    DisplayText("MZ_CNC settings file " + fileName + " not found, using default values.", KnownColor.DarkOrange);
                    return false;
                }

                if (content.Length < 10)
                {
                    ShowMessageBox(
                       "Problem loading MZ_CNC settings: File is too short.\nUsing built in defaults.",
                       "Settings not loaded",
                       MessageBoxButtons.OK);
                    return false;
                }

                string boardType = content.Substring(0, 10);
                content = content.Remove(0, 10);

                if (boardType == "MZ_CNC  \n\r")
                {
                    settings = JsonConvert.DeserializeObject<MZCNCSettings>(content);
                    DisplayText("MZ_CNC settings loaded successfully.", KnownColor.DarkGreen);
                }
                else
                {
                    ShowMessageBox(
                       "Unknown / wrong board type (" + boardType + ") in file, MZ_CNC settings not changed.",
                       "Settings not loaded",
                       MessageBoxButtons.OK);
                    return false;
                }
                return true;
            }
            catch (Exception excep)
            {
                ShowMessageBox(
                    "Problem loading MZ_CNC settings:\n" + excep.Message + "\nMZ_CNC settings not changed.",
                    "Settings not loaded",
                    MessageBoxButtons.OK);
                return false;
            }
        }

        // Write all settings to controller (called when "Restore Settings" button is clicked)
        private void WriteAllMZCNCSettings_m()
        {
            if (StartingUp)
            {
                return;
            }

            if (Setting.Controlboard != ControlBoardType.MZ_CNC)
            {
                DisplayText("Cannot restore MZ_CNC settings: MZ_CNC board not connected.", KnownColor.DarkRed);
                return;
            }

            DialogResult dialogResult = ShowMessageBox(
               "Settings currently stored on your MZ_CNC board will be overwritten.\n" +
               "Current values will be permanently lost if you haven't stored a backup copy.\n" +
               "Continue?",
               "Overwrite current settings?", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.No)
            {
                return;
            }

            // Load settings from file
            MZCNCSettings settingsToRestore = new MZCNCSettings();
            string fileName = Setting.MZCNCsettingsFile;
            if (!LoadMZCNCSettings(ref settingsToRestore, fileName))
            {
                DisplayText("Failed to load MZ_CNC settings file for restore.", KnownColor.DarkRed);
                return;
            }

            // Write all GRBL-writable settings to the controller
            if (!WriteMZCNCSettingsToController(settingsToRestore))
            {
                DisplayText("Writing MZ_CNC settings to controller failed.", KnownColor.DarkRed);
                ShowMessageBox(
                    "Problem writing board settings. Board may be in undefined state, fix the problem before continuing!",
                    "Settings not restored",
                    MessageBoxButtons.OK);
            }
            else
            {
                DisplayText("MZ_CNC settings restored successfully.", KnownColor.DarkGreen);
                // Reload settings from controller to update UI
                Cnc.MZ_CNC.JustConnected();
                MZ_CNCSettings_Load();
            }
        }

        private bool WriteMZCNCSettingsToController(MZCNCSettings settings)
        {
            DisplayText("Writing settings to MZ_CNC board...");

            // Write GRBL settings using $xxx=value format
            // Speed settings (max rate)
            if (!WriteOneMZCNCSetting("$110", settings.XSpeed.ToString("0.0", CultureInfo.InvariantCulture))) return false;
            if (!WriteOneMZCNCSetting("$111", settings.YSpeed.ToString("0.0", CultureInfo.InvariantCulture))) return false;
            if (!WriteOneMZCNCSetting("$112", settings.ZSpeed.ToString("0.0", CultureInfo.InvariantCulture))) return false;
            if (!WriteOneMZCNCSetting("$113", settings.ASpeed.ToString("0.0", CultureInfo.InvariantCulture))) return false;

            // Acceleration settings
            if (!WriteOneMZCNCSetting("$120", settings.XAccel.ToString("0.0", CultureInfo.InvariantCulture))) return false;
            if (!WriteOneMZCNCSetting("$121", settings.YAccel.ToString("0.0", CultureInfo.InvariantCulture))) return false;
            if (!WriteOneMZCNCSetting("$122", settings.ZAccel.ToString("0.0", CultureInfo.InvariantCulture))) return false;
            if (!WriteOneMZCNCSetting("$123", settings.AAccel.ToString("0.0", CultureInfo.InvariantCulture))) return false;

            // Steps per mm
            if (!WriteOneMZCNCSetting("$100", settings.StepsPerMmX.ToString("0.000", CultureInfo.InvariantCulture))) return false;
            if (!WriteOneMZCNCSetting("$101", settings.StepsPerMmY.ToString("0.000", CultureInfo.InvariantCulture))) return false;
            if (!WriteOneMZCNCSetting("$102", settings.StepsPerMmZ.ToString("0.000", CultureInfo.InvariantCulture))) return false;
            if (!WriteOneMZCNCSetting("$103", settings.StepsPerMmA.ToString("0.000", CultureInfo.InvariantCulture))) return false;

            // Homing settings
            if (!WriteOneMZCNCSetting("$26", settings.HomingSeek.ToString("0.0", CultureInfo.InvariantCulture))) return false;
            if (!WriteOneMZCNCSetting("$27", settings.HomingPulloff.ToString("0.000", CultureInfo.InvariantCulture))) return false;

            // Explicitly save settings to NVM flash (PIC32MZ-specific command)
            DisplayText("Saving settings to NVM flash...", KnownColor.DarkCyan);
            if (!Cnc.MZ_CNC.Write_m("$SAVE", 1000))
            {
                DisplayText("*** Warning: $SAVE command failed. Settings may not persist after power cycle!", KnownColor.DarkOrange);
                // Don't return false - settings may still be in RAM and functional
            }
            else
            {
                DisplayText("Settings saved to flash successfully.", KnownColor.DarkGreen);
            }

            DisplayText("All MZ_CNC settings written to controller successfully.", KnownColor.DarkGreen);
            return true;
        }

        private bool WriteOneMZCNCSetting(string setting, string value)
        {
            string cmd = setting + "=" + value;
            DisplayText("  Write: " + cmd, KnownColor.DarkCyan);
            if (!Cnc.MZ_CNC.Write_m(cmd, 500))
            {
                DisplayText("*** Failed to write " + setting, KnownColor.DarkRed);
                return false;
            }
            System.Threading.Thread.Sleep(50);
            return true;
        }

        // Capture current UI settings into MZCNCSettings object for saving
        private MZCNCSettings CaptureMZCNCSettingsFromUI()
        {
            MZCNCSettings settings = new MZCNCSettings();
            
            // Capture GRBL-writable settings from controller (already downloaded)
            if (Cnc.MZ_CNC != null && Cnc.MZ_CNC.Settings != null)
            {
                settings.XSpeed = Cnc.MZ_CNC.Settings.MaxRateX;
                settings.YSpeed = Cnc.MZ_CNC.Settings.MaxRateY;
                settings.ZSpeed = Cnc.MZ_CNC.Settings.MaxRateZ;
                settings.ASpeed = Cnc.MZ_CNC.Settings.MaxRateA;

                settings.XAccel = Cnc.MZ_CNC.Settings.AccelX;
                settings.YAccel = Cnc.MZ_CNC.Settings.AccelY;
                settings.ZAccel = Cnc.MZ_CNC.Settings.AccelZ;
                settings.AAccel = Cnc.MZ_CNC.Settings.AccelA;

                settings.StepsPerMmX = Cnc.MZ_CNC.Settings.StepsPerMmX;
                settings.StepsPerMmY = Cnc.MZ_CNC.Settings.StepsPerMmY;
                settings.StepsPerMmZ = Cnc.MZ_CNC.Settings.StepsPerMmZ;
                settings.StepsPerMmA = Cnc.MZ_CNC.Settings.StepsPerMmA;

                settings.HomingSeek = Cnc.MZ_CNC.Settings.HomingSeek;
                settings.HomingPulloff = Cnc.MZ_CNC.Settings.HomingPulloff;
            }

            // Capture AppSettings-only values (not stored in GRBL)
            settings.XCurrent = Setting.MZCNC_XCurrent;
            settings.YCurrent = Setting.MZCNC_YCurrent;
            settings.ZCurrent = Setting.MZCNC_ZCurrent;
            settings.ACurrent = Setting.MZCNC_ACurrent;

            settings.XMicrosteps = Setting.MZCNC_XMicroStep;
            settings.YMicrosteps = Setting.MZCNC_YMicroStep;
            settings.ZMicrosteps = Setting.MZCNC_ZMicroStep;
            settings.AMicrosteps = Setting.MZCNC_AMicroStep;

            settings.XDeg18 = Setting.MZCNC_XDeg18;
            settings.YDeg18 = Setting.MZCNC_YDeg18;
            settings.ZDeg18 = Setting.MZCNC_ZDeg18;
            settings.ADeg18 = Setting.MZCNC_ADeg18;

            settings.XInterpolate = Setting.MZCNC_XInterpolate;
            settings.YInterpolate = Setting.MZCNC_YInterpolate;
            settings.ZInterpolate = Setting.MZCNC_ZInterpolate;
            settings.AInterpolate = Setting.MZCNC_AInterpolate;

            return settings;
        }
    }

    // =================================================================================
    // MZ_CNC Settings Class
    // =================================================================================
    // Stores all settings for MZ_CNC controller
    // Mix of GRBL-writable settings and AppSettings-only values
    // =================================================================================

    public class MZCNCSettings
    {
        // ==========  GRBL-writable settings  ==========
        // These can be written to the controller via $xxx=value commands

        // Speed settings (max rate) - $110-$113 (mm/min)
        public double XSpeed { get; set; } = 5000.0;
        public double YSpeed { get; set; } = 5000.0;
        public double ZSpeed { get; set; } = 2000.0;   // Updated to match PIC32MZ default
        public double ASpeed { get; set; } = 5000.0;   // Updated to match PIC32MZ default

        // Acceleration settings - $120-$123 (mm/sec^2)
        public double XAccel { get; set; } = 500.0;
        public double YAccel { get; set; } = 500.0;
        public double ZAccel { get; set; } = 200.0;
        public double AAccel { get; set; } = 500.0;    // Updated to match PIC32MZ default

        // Steps per mm - $100-$103
        public double StepsPerMmX { get; set; } = 156.0;   // Updated to match PIC32MZ default
        public double StepsPerMmY { get; set; } = 156.0;   // Updated to match PIC32MZ default
        public double StepsPerMmZ { get; set; } = 156.0;   // Updated to match PIC32MZ default
        public double StepsPerMmA { get; set; } = 156.0;   // Updated to match PIC32MZ default

        // Homing settings
        public double HomingSeek { get; set; } = 500.0;       // $26 (homing seek rate in mm/min)
        public double HomingPulloff { get; set; } = 2.0;  // $27 

        // ==========  AppSettings-only values  ==========
        // These are NOT stored in GRBL firmware, only in LitePlacer settings

        // Motor current (not supported by GRBL)
        public int XCurrent { get; set; } = 800; //mA
        public int YCurrent { get; set; } = 800; //mA
        public int ZCurrent { get; set; } = 800; //mA
        public int ACurrent { get; set; } = 800; //mA

        // Microsteps (calculated from steps/mm, not directly configurable in GRBL)
        public int XMicrosteps { get; set; } = 8;
        public int YMicrosteps { get; set; } = 8;
        public int ZMicrosteps { get; set; } = 8;
        public int AMicrosteps { get; set; } = 8;

        // Step angle (1.8° or 0.9°)
        public bool XDeg18 { get; set; } = true;  // true=1.8°, false=0.9°
        public bool YDeg18 { get; set; } = true;
        public bool ZDeg18 { get; set; } = true;
        public bool ADeg18 { get; set; } = true;

        // Interpolation
        public bool XInterpolate { get; set; } = false;
        public bool YInterpolate { get; set; } = false;
        public bool ZInterpolate { get; set; } = false;
        public bool AInterpolate { get; set; } = false;
    }
}

