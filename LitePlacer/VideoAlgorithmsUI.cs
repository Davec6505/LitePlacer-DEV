using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Input;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

using AForge;
using AForge.Video;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

// To add a function: Add its name to the list of KnownFunctions below, and read more at SetFunctionDefaultParameters() 

namespace LitePlacer
{
    public partial class FormMain : Form
    {
        enum Functions_dataGridViewColumns : int { FunctionColumn, ActiveColumn };
        
        // Dynamic function list - populated based on active camera engine (AForge vs EmguCV)
        private List<string> _knownFunctions;
        public List<string> KnownFunctions 
        { 
            get 
            {
                if (_knownFunctions == null)
                {
                    UpdateKnownFunctions();
                }
                return _knownFunctions;
            }
        }


        public VideoAlgorithmsCollection VideoAlgorithms;

        // =====================================================================================
        // interface to main form:
        Camera cam;

        /// <summary>
        /// Updates the available functions list based on current camera engine
        /// </summary>
        private void UpdateKnownFunctions()
        {
            _knownFunctions = new List<string>();
            
            // Get functions from current camera engine
            if (cam?.CurrentEngine != null && cam.CurrentEngine.IsAvailable)
            {
                _knownFunctions.AddRange(cam.CurrentEngine.GetAvailableFunctions());
                DisplayText($"Loaded {_knownFunctions.Count} functions from {cam.CurrentEngine.EngineName}", 
                    System.Drawing.KnownColor.DarkGreen);
            }
            else
            {
                // Fallback to default AForge functions
                _knownFunctions.AddRange(new[] 
                {
                    "Threshold", "Invert", "Meas. zoom", "Histogram",
                    "Grayscale", "Edge detect", "Noise reduction", "Erosion", 
                    "Kill color", "Keep color", "Blur", "Gaussian blur", 
                    "Hough circles", "Filter Features by Size", "Jog before measurement"
                });
                DisplayText("Using default AForge function set", System.Drawing.KnownColor.DarkOrange);
            }
        }

        /// <summary>
        /// Refreshes the function dropdown when camera engine changes
        /// </summary>
        public void RefreshFunctionList()
        {
            _knownFunctions = null; // Force refresh
            
            // Update the DataGridView combobox column
            DataGridViewComboBoxColumn comboboxColumn =
                (DataGridViewComboBoxColumn)Functions_dataGridView.Columns[(int)Functions_dataGridViewColumns.FunctionColumn];
            comboboxColumn.DataSource = null; // Clear existing DataSource first
            comboboxColumn.DataSource = KnownFunctions;
            
            // Force refresh of all cells to pick up new function list
            Functions_dataGridView.Refresh();
            
            DisplayText($"Function list refreshed: {KnownFunctions.Count} functions available", 
                System.Drawing.KnownColor.DarkGreen);
        }

        private void Algorithms_tabPage_Begin()
        {
            DisplayText("Setup Video Processing tab begin");
            VideoProcessingZguard_checkBox.Checked = false;
            NoVideoProcessing_radioButton.Checked = true;
            SetDownCameraDefaults();
            SetUpCameraDefaults();
            if (DownCam_radioButton.Checked)
            {
                ChangeCamera(DownCamera);
            }
            else
            {
                ChangeCamera(UpCamera);
            }
            NoVideoProcessing_radioButton_CheckedChange();

            JigX_textBox.Text = Setting.General_JigOffsetX.ToString("0.00", CultureInfo.InvariantCulture);
            JigY_textBox.Text = Setting.General_JigOffsetY.ToString("0.00", CultureInfo.InvariantCulture);
            PickupCenterX_textBox.Text = Setting.General_PickupCenterX.ToString("0.00", CultureInfo.InvariantCulture);
            PickupCenterY_textBox.Text = Setting.General_PickupCenterY.ToString("0.00", CultureInfo.InvariantCulture);
            NozzleOffsetX_textBox.Text = Setting.DownCam_NozzleOffsetX.ToString("0.00", CultureInfo.InvariantCulture);
            NozzleOffsetY_textBox.Text = Setting.DownCam_NozzleOffsetY.ToString("0.00", CultureInfo.InvariantCulture);
            Z0toPCB_CamerasTab_label.Text = Setting.General_Z0toPCB.ToString("0.00", CultureInfo.InvariantCulture) + " mm";
            UpcamPositionX_textBox.Text = Setting.UpCam_PositionX.ToString("0.00", CultureInfo.InvariantCulture);
            UpcamPositionY_textBox.Text = Setting.UpCam_PositionY.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void Algorithms_tabPage_End()
        {
            ZGuardOn();
        }

        public void InitVideoAlgorithmsUI()
        {
            DownCam_radioButton.Checked = true; // default to Downcamera
            Functions_dataGridView.Rows.Clear();
            
            // Initialize camera engine selection listbox
            if (listBoxCameraEngin != null)
            {
                listBoxCameraEngin.Items.Clear();
                listBoxCameraEngin.Items.Add("AForge.NET");
                listBoxCameraEngin.Items.Add("EmguCV (OpenCV)");
                
                // Select engine based on saved setting
                string savedEngine = Setting.CameraEngine ?? "AForge";
                if (savedEngine.Contains("EmguCV") || savedEngine.Contains("OpenCV"))
                {
                    listBoxCameraEngin.SelectedIndex = 1; // EmguCV
                }
                else
                {
                    listBoxCameraEngin.SelectedIndex = 0; // AForge
                }
                
                DisplayText($"Camera engine selector initialized: {listBoxCameraEngin.SelectedItem}", 
                    System.Drawing.KnownColor.DarkCyan);
            }
            
            // Initialize function list from camera engine (EmguCV or AForge)
            RefreshFunctionList();

            VideoAlgorithms = new VideoAlgorithmsCollection();
            LoadVideoAlgorithms(VideoAlgorithms); // causes updating of Functions_dataGridView and Function parameters
        }

        // camera change
        private void ChangeCamera(Camera NewCam)
        {
            cam = NewCam;
            SelectCamera(NewCam);
            RefreshFunctionList(); // Update available functions based on camera engine
            AlgorithmsTab_RestoreBehaviour();
        }

        private void DownCam_radioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (StartingUp)
            {
                return;
            }
            // Both radiobuttons CheckedChanged events fire; we need to react only once
            if (DownCam_radioButton.Checked)
            {
                ChangeCamera(DownCamera);
            }
            else
            {
                ChangeCamera(UpCamera);
            }
            SpecialProcessing_button.Visible = true;
            AdvancedProcessing_tabControl.Visible = false;
        }

        private void UpCam_radioButton_CheckedChanged(object sender, EventArgs e)
        {
            // ChangeCamera(UpCamera);
        }


        private void AlgorithmsTab_RestoreBehaviour()
        {
            if (NoVideoProcessing_radioButton.Checked)
            {
                StopVideoProcessing();
            }
            else
            {
                UpdateVideoProcessing();
            }

        }

        // =====================================================================================

        // =====================================================================================
        #region Algorithms Load and Save

        // Load:
        private bool AlgorithmChange = false;   // to prevent parameters etc updates in the middle of process

        private void AlgorithmsLoad_button_Click(object sender, EventArgs e)
        {
            LoadVideoAlgorithms(VideoAlgorithms);
        }

        private void LoadVideoAlgorithms(VideoAlgorithmsCollection Collection)
        {
            AlgorithmChange = true;
            string path = GetPath();
            
            // Determine which file to load based on current camera engine
            string engineSuffix = "";
            string engineName = "AForge.NET"; // Default to AForge if camera not initialized
            
            if (cam?.CurrentEngine != null)
            {
                engineName = cam.CurrentEngine.EngineName;
                // Use engine name as suffix (e.g., "AForge.NET" becomes ".AForge")
                engineSuffix = "." + engineName.Replace(" ", "").Replace("(", "").Replace(")", "").Replace(".NET", "");
                DisplayText($"Loading algorithms for engine: {engineName}", KnownColor.DarkCyan);
            }
            else
            {
                // Camera not initialized yet, default to AForge
                engineSuffix = ".AForge";
                DisplayText("Loading algorithms for engine: AForge.NET (default)", KnownColor.DarkCyan);
            }
            
            string FileName = path + VIDEOALGORITHMS_DATAFILE + engineSuffix;
            
            if (File.Exists(FileName))
            {
                DisplayText("LoadVideoAlgorithms from " + FileName);
                List<VideoAlgorithmsCollection.FullAlgorithmDescription> NewList = new List<VideoAlgorithmsCollection.FullAlgorithmDescription>();
                NewList = JsonConvert.DeserializeObject<List<VideoAlgorithmsCollection.FullAlgorithmDescription>>(File.ReadAllText(FileName));
                Collection.AllAlgorithms = NewList;
                if (NewList.Count == 0)
                {
                    // This should never happen, but there was a customer case where this turned out to be the issue...
                    HandleEmptyVAlist(path, Collection);
                }
            }
            else
            {
                // Try loading from old non-engine-specific file for backward compatibility
                string oldFileName = path + VIDEOALGORITHMS_DATAFILE;
                if (File.Exists(oldFileName))
                {
                    try
                    {
                        DisplayText($"Migrating algorithms from {oldFileName} to {FileName}", KnownColor.DarkOrange);
                        List<VideoAlgorithmsCollection.FullAlgorithmDescription> NewList = new List<VideoAlgorithmsCollection.FullAlgorithmDescription>();
                        NewList = JsonConvert.DeserializeObject<List<VideoAlgorithmsCollection.FullAlgorithmDescription>>(File.ReadAllText(oldFileName));
                        
                        // Set engine name for all algorithms
                        foreach (var algorithm in NewList)
                        {
                            if (string.IsNullOrEmpty(algorithm.EngineName))
                            {
                                algorithm.EngineName = engineName; // Use the engineName we determined earlier
                            }
                        }
                        
                        Collection.AllAlgorithms = NewList;
                        
                        // Try to save to engine-specific file (don't fail startup if this fails)
                        try
                        {
                            SaveVideoAlgorithms(FileName, Collection);
                            DisplayText($"Migration complete: Saved to {FileName}", KnownColor.DarkGreen);
                        }
                        catch (Exception saveEx)
                        {
                            DisplayText($"Warning: Could not save migrated algorithms: {saveEx.Message}", KnownColor.DarkOrange);
                            // Continue anyway - we have the data loaded
                        }
                    }
                    catch (Exception migrateEx)
                    {
                        DisplayText($"Error migrating algorithms: {migrateEx.Message}", KnownColor.DarkRed);
                        LoadOldVideoAlgorithms(path, Collection);
                    }
                }
                else
                {
                    LoadOldVideoAlgorithms(path, Collection);
                }
            }
            // fill Algorithm_comboBox
            Algorithm_comboBox.Items.Clear();
            foreach (VideoAlgorithmsCollection.FullAlgorithmDescription Algorithm in Collection.AllAlgorithms)
            {
                Algorithm_comboBox.Items.Add(Algorithm.Name);
            }
            ClearFunctionParameters();
            Algorithm_comboBox.SelectedIndex = 0;
            Functions_dataGridView.CurrentCell = null;
            AlgorithmChange = false;
        }

        private void HandleEmptyVAlist(string path, VideoAlgorithmsCollection Collection)
        {
            DialogResult dialogResult = ShowMessageBox(
                "Stored video algorithms data was unusable, and saved data has been lost.\n\r " +
                "There is dated data backup directories under your LitePlacer directory.\n\r\n\r " +
                "To recover: Exit the program. Copy the datafiles from a backup directory to the \n\r " +
                "litePlacer main directory, overwriting existing files.\n\r\n\r " +
                "Exit now? (Yes: Exit; No: Continue with empty placeholder algorithm list",

                "*** Data loss! ***", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                Application.Exit();
            }
            LoadOldVideoAlgorithms(path, Collection);
        }

        private void LoadOldVideoAlgorithms(string path, VideoAlgorithmsCollection Collection)
        {
            // For now (maybe for good?), build an placeholder list
            string currentEngineName = cam?.CurrentEngine?.EngineName ?? "AForge.NET";
            
            List<VideoAlgorithmsCollection.FullAlgorithmDescription> NewList = new List<VideoAlgorithmsCollection.FullAlgorithmDescription>();
            NewList.Add(new VideoAlgorithmsCollection.FullAlgorithmDescription { Name = "Homing", EngineName = currentEngineName });
            NewList.Add(new VideoAlgorithmsCollection.FullAlgorithmDescription { Name = "Fiducials", EngineName = currentEngineName });
            NewList.Add(new VideoAlgorithmsCollection.FullAlgorithmDescription { Name = "Paper tape", EngineName = currentEngineName });
            NewList.Add(new VideoAlgorithmsCollection.FullAlgorithmDescription { Name = "Black tape", EngineName = currentEngineName });
            NewList.Add(new VideoAlgorithmsCollection.FullAlgorithmDescription { Name = "Clear tape", EngineName = currentEngineName });
            NewList.Add(new VideoAlgorithmsCollection.FullAlgorithmDescription { Name = "Components", EngineName = currentEngineName });
            NewList.Add(new VideoAlgorithmsCollection.FullAlgorithmDescription { Name = "Nozzle tip", EngineName = currentEngineName });
            Collection.AllAlgorithms = NewList;
        }

        // ========= Save

        private bool SaveVideoAlgorithms(string FileName, VideoAlgorithmsCollection Collection)
        {
            try
            {
                DisplayText("SaveVideoAlgorithms to " + FileName);
                File.WriteAllText(FileName, JsonConvert.SerializeObject(VideoAlgorithms.AllAlgorithms, Formatting.Indented));
                return true;
            }
            catch (System.Exception excep)
            {
                DisplayText("Saving Video algorithms failed. " + excep.Message);
                return false;
            }
        }

        private void AlgorithmsSave_button_Click(object sender, EventArgs e)
        {
            string path = GetPath();
            
            // Determine which file to save based on current camera engine
            string engineSuffix = "";
            if (cam?.CurrentEngine != null)
            {
                engineSuffix = "." + cam.CurrentEngine.EngineName.Replace(" ", "").Replace("(", "").Replace(")", "").Replace(".NET", "");
                
                // Update engine name for all algorithms before saving
                foreach (var algorithm in VideoAlgorithms.AllAlgorithms)
                {
                    algorithm.EngineName = cam.CurrentEngine.EngineName;
                }
                
                DisplayText($"Saving algorithms for engine: {cam.CurrentEngine.EngineName}", KnownColor.DarkCyan);
            }
            
            SaveVideoAlgorithms(path + VIDEOALGORITHMS_DATAFILE + engineSuffix, VideoAlgorithms);
        }

        #endregion Algorithms Load and Save
        // =====================================================================================

        // =====================================================================================
        #region Current video algorithm

        private void Algorithm_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            AlgorithmChange = true;
            string AlgorithmName = Algorithm_comboBox.SelectedItem.ToString();
            // DisplayText("Algorithm_comboBox_SelectedIndexChanged(), func: " + AlgorithmName);
            VideoAlgorithms.SelectedAlgorithmChanged(AlgorithmName);
            FillFunctionTable(AlgorithmName);
            FillMeasurementValues(AlgorithmName);
            ClearFunctionParameters();
            AlgorithmChange = false;
            if (!NoVideoProcessing_radioButton.Checked)
            {
                UpdateVideoProcessing();
            }

        }

        bool ChangeYwithX = false;

        private void FillMeasurementValues(string ListName)
        {
            // User changed the current algorithm. This function fills the measurement value boxes
            MeasurementParametersClass values = VideoAlgorithms.CurrentAlgorithm.MeasurementParameters;
            SearchRound_checkBox.Checked = values.SearchRounds;
            SearchRectangles_checkBox.Checked = values.SearchRectangles;
            SearchComponentOutlines_checkBox.Checked = values.SearchComponentOutlines;
            SearchComponentPads_checkBox.Checked = values.SearchComponentPads;
            // for some reason(??), the order of things to set are not honored. A workaround:
            ChangeYwithX = false;
            Xmin_textBox.Text = values.Xmin.ToString("0.00", CultureInfo.InvariantCulture);
            Xmax_textBox.Text = values.Xmax.ToString("0.00", CultureInfo.InvariantCulture);
            Ymin_textBox.Text = values.Ymin.ToString("0.00", CultureInfo.InvariantCulture);
            Ymax_textBox.Text = values.Ymax.ToString("0.00", CultureInfo.InvariantCulture);
            XmaxDistance_textBox.Text = values.XUniqueDistance.ToString("0.00", CultureInfo.InvariantCulture);
            YmaxDistance_textBox.Text = values.XUniqueDistance.ToString("0.00", CultureInfo.InvariantCulture);
            ChangeYwithX = true;
        }
        // =====================================================================================
        // Add, Remove, Duplicate, Rename

        // ==================
        // helpers
        public string GetName(string StartName, bool rename)
        {
            AlgorithmNameForm GetNameDialog = new AlgorithmNameForm(StartName);
            GetNameDialog.Algorithms = VideoAlgorithms.AllAlgorithms;
            GetNameDialog.Renaming = rename;
            GetNameDialog.StartPosition = FormStartPosition.CenterParent;
            GetNameDialog.ShowDialog(this);
            if (GetNameDialog.OK)
            {
                return GetNameDialog.NewName;
            }
            else
            {
                return null;
            }
        }

        private bool FindLocation(string AlgorithmName, out int loc)
        {
            // returns the index of the named algorithm in AllAlgorithms
            for (int i = 0; i < VideoAlgorithms.AllAlgorithms.Count; i++)
            {
                if (VideoAlgorithms.AllAlgorithms[i].Name == AlgorithmName)
                {
                    loc = i;
                    return true;
                }
            }
            loc = -1;
            return false;
        }

        public static T DeepClone<T>(T obj)
        {
            string clone = JsonConvert.SerializeObject(obj, Formatting.Indented);
            return JsonConvert.DeserializeObject<T>(clone);
        }

        // ==================
        // Button clicks

        private void AddAlgorithm(string NewName)
        {
            DisplayText("Add algorithm " + NewName);
            VideoAlgorithmsCollection.FullAlgorithmDescription Alg = new VideoAlgorithmsCollection.FullAlgorithmDescription();
            Alg.Name = NewName;
            VideoAlgorithms.AllAlgorithms.Add(Alg);
            Algorithm_comboBox.Items.Add(NewName);
            // tapes grid
            for (int i = 0; i < Tapes_dataGridView.RowCount; i++)
            {
                string OrgValue = Tapes_dataGridView.Rows[i].Cells["Type_Column"].Value.ToString();
                // rebuild the selection cell
                DataGridViewComboBoxCell c = new DataGridViewComboBoxCell();
                BuildAlgorithmsCombox(out c);
                Tapes_dataGridView.Rows[i].Cells["Type_Column"] = c;
                Tapes_dataGridView.Rows[i].Cells["Type_Column"].Value = OrgValue;
            }
        }

        private void AddAlgorithm_button_Click(object sender, EventArgs e)
        {
            string NewName = GetName("", false);
            if (NewName == null)
            {
                DisplayText("Add algorithm canceled");
                return;
            }
            AddAlgorithm(NewName);
            Algorithm_comboBox.SelectedIndex = Algorithm_comboBox.Items.Count - 1;
        }

        private void RemoveAlgorithm_button_Click(object sender, EventArgs e)
        {
            int pos = 0;
            string Alg = Algorithm_comboBox.SelectedItem.ToString();
            if (!FindLocation(Alg, out pos))
            {
                DisplayText("Remove algorithm, algorithm not found!");
                return;
            }
            if (pos == 0)
            {
                DisplayText("Homing cannot be removed.");
                return;
            }
            bool AlgInUse = AlgorithmUsed(Alg);
            if (AlgInUse)
            {
                DialogResult dialogResult = ShowMessageBox(
                    "Algorithm " + Alg + " is in use (see log window). Remove anyway?" +
                    "\r\n(Used instances are replaced with Homing)",
                    "OK to remove algorithm?", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.No)
                {
                    return;
                };
            }
            VideoAlgorithms.AllAlgorithms.RemoveAt(pos);
            Algorithm_comboBox.Items.RemoveAt(Algorithm_comboBox.SelectedIndex);
            AlgorithmChange = true;
            ClearFunctionParameters();
            Algorithm_comboBox.SelectedIndex = 0;
            Functions_dataGridView.CurrentCell = null;
            AlgorithmChange = false;
            UpdateVideoProcessing();
            if (AlgInUse)
            {
                RemoveAlgorithmFromUse(Alg);
            }
        }

        private void DuplicateAlgorithm_button_Click(object sender, EventArgs e)
        {
            int loc;
            if (!FindLocation(Algorithm_comboBox.SelectedItem.ToString(), out loc))
            {
                DisplayText("Duplicate algorithm, algorithm not found!");
                return;
            }
            DisplayText("Duplicate algorithm " + VideoAlgorithms.AllAlgorithms[loc].Name);
            VideoAlgorithmsCollection.FullAlgorithmDescription Alg =
                DeepClone<VideoAlgorithmsCollection.FullAlgorithmDescription>(VideoAlgorithms.AllAlgorithms[loc]);
            Alg.Name = VideoAlgorithms.AllAlgorithms[loc].Name + " (duplicate)";
            VideoAlgorithms.AllAlgorithms.Add(Alg);
            Algorithm_comboBox.Items.Add(Alg.Name);
            Algorithm_comboBox.SelectedIndex = Algorithm_comboBox.Items.Count - 1;
            // User propably doesn't want the name+(duplicate), so let's click the rename button automatically
            RenameAlgorithm_button_Click(sender, e);
        }

        private void RenameAlgorithm_button_Click(object sender, EventArgs e)
        {
            if (Algorithm_comboBox.SelectedIndex == 0)
            {
                DisplayText("Homing can't be renamed.");
                return;
            }
            string OldName = Algorithm_comboBox.SelectedItem.ToString();
            string NewName = GetName(OldName, true);
            if (NewName == null)
            {
                DisplayText("Rename algorithm canceled");
                return;
            }
            DisplayText("Rename algorithm to " + NewName);
            int AlgPos;
            if (!FindLocation(OldName, out AlgPos))
            {
                DisplayText("***Rename algorithm, algorithm not found!");
                return;
            }
            VideoAlgorithms.AllAlgorithms[AlgPos].Name = NewName;
            int NamePos = Algorithm_comboBox.SelectedIndex;
            Algorithm_comboBox.Items.RemoveAt(NamePos);
            Algorithm_comboBox.Items.Insert(NamePos, NewName);
            Algorithm_comboBox.SelectedIndex = NamePos;

            // Rename algorithm names if already in use
            // Fiducials on the job table
            foreach (DataGridViewRow row in JobData_GridView.Rows)
            {
                if (row.Cells["JobdataMethodColumn"].Value.ToString() == "Fiducials")
                {
                    row.Cells["JobdataMethodParametersColumn"].Value = NewName;
                }
            }
            // Tapes data table
            foreach (DataGridViewRow row in Tapes_dataGridView.Rows)
            {
                string CurrentValue = row.Cells["Type_Column"].Value.ToString();
                row.Cells["Type_Column"].Value = null;
                DataGridViewComboBoxCell c = new DataGridViewComboBoxCell();
                BuildAlgorithmsCombox(out c);
                row.Cells["Type_Column"] = c;
                if (VideoAlgorithms.AlgorithmExists(CurrentValue))
                {
                    row.Cells["Type_Column"].Value = CurrentValue;
                }
                else
                {
                    // if CurrentValue does not exist anymore, the name was changed
                    if (CurrentValue != OldName)
                    {
                        DisplayText("*** Rename algorithm: Assert failed!");  // or something else is off
                    }
                    row.Cells["Type_Column"].Value = NewName;
                }
            }
            // Nozzles

        }

        private void NoVideoProcessing_radioButton_CheckedChange()
        {
            if (StartingUp)
            {
                return;
            }
            if (NoVideoProcessing_radioButton.Checked)
            {
                StopVideoProcessing();
            }
            else
            {
                UpdateVideoProcessing();
            }
        }

        private void NoVideoProcessing_radioButton_CheckedChanged(object sender, EventArgs e)
        {
            NoVideoProcessing_radioButton_CheckedChange();
        }


        private void ShowVideoProcessing_radioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (DownCam_radioButton.Checked)
            {
                DownCamera.ShowProcessing = ShowVideoProcessing_radioButton.Checked;
            }
            if (UpCam_radioButton.Checked)
            {
                UpCamera.ShowProcessing = ShowVideoProcessing_radioButton.Checked;
            }
        }

        private void ShowVideoResults_radioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (DownCam_radioButton.Checked)
            {
                DownCamera.ShowProcessing = !ShowVideoResults_radioButton.Checked;
            }
            if (UpCam_radioButton.Checked)
            {
                UpCamera.ShowProcessing = !ShowVideoResults_radioButton.Checked;
            }
        }



        // =====================================================================================
        // Check if Algorithm is used somewhere
        private bool AlgorithmUsed(string Alg)
        {
            bool ret = false;
            for (int i = 0; i < JobData_GridView.RowCount; i++)
            {
                if (JobData_GridView.Rows[i].Cells["JobdataMethodParametersColumn"].Value != null)
                {
                    if (JobData_GridView.Rows[i].Cells["JobdataMethodParametersColumn"].Value.ToString() == Alg)
                    {
                        DisplayText("Algorithm \"" + Alg + "\" used in Job data, row " + (i + 1).ToString());
                        ret = true;
                    }
                }
            }

            for (int i = 0; i < Tapes_dataGridView.RowCount; i++)
            {
                if (Tapes_dataGridView.Rows[i].Cells["Type_Column"].Value != null)
                {
                    if (Tapes_dataGridView.Rows[i].Cells["Type_Column"].Value.ToString() == Alg)
                    {
                        DisplayText("Algorithm \"" + Alg + "\" used in Tapes data, row " + (i + 1).ToString());
                        ret = true;
                    }
                }
            }

            for (int i = 0; i < NozzlesParameters_dataGridView.RowCount; i++)
            {
                if (NozzlesParameters_dataGridView.Rows[i].Cells["VisionAlgorithm_column"].Value != null)
                {
                    if (NozzlesParameters_dataGridView.Rows[i].Cells["VisionAlgorithm_column"].Value.ToString() == Alg)
                    {
                        DisplayText("Algorithm \"" + Alg + "\" used in Nozzles setup, row " + (i + 1).ToString());
                        ret = true;
                    }

                }
            }
            return ret;
        }

        // =====================================================================================
        // And if algorithm was in use and was deleted, we need to replace it with the (always existing) homing
        private void RemoveAlgorithmFromUse(string Alg)
        {
            // Job data
            for (int i = 0; i < JobData_GridView.RowCount; i++)
            {
                if (JobData_GridView.Rows[i].Cells["JobdataMethodParametersColumn"].Value != null)
                {
                    if (JobData_GridView.Rows[i].Cells["JobdataMethodParametersColumn"].Value.ToString() == Alg)
                    {
                        JobData_GridView.Rows[i].Cells["JobdataMethodParametersColumn"].Value = VideoAlgorithms.AllAlgorithms[0].Name;
                    }
                }
            }

            // Tapes grid
            for (int i = 0; i < Tapes_dataGridView.RowCount; i++)
            {
                string OrgValue = Tapes_dataGridView.Rows[i].Cells["Type_Column"].Value.ToString();
                // rebuild the selection cell
                DataGridViewComboBoxCell c = new DataGridViewComboBoxCell();
                BuildAlgorithmsCombox(out c);
                Tapes_dataGridView.Rows[i].Cells["Type_Column"] = c;
                if (OrgValue == Alg)
                {
                    Tapes_dataGridView.Rows[i].Cells["Type_Column"].Value = VideoAlgorithms.AllAlgorithms[0].Name;
                }
                else
                {
                    Tapes_dataGridView.Rows[i].Cells["Type_Column"].Value = OrgValue;
                }
            }

            // Nozzles
            for (int i = 0; i < NozzlesParameters_dataGridView.RowCount; i++)
            {
                if (NozzlesParameters_dataGridView.Rows[i].Cells["VisionAlgorithm_column"].Value != null)
                {
                    if (NozzlesParameters_dataGridView.Rows[i].Cells["VisionAlgorithm_column"].Value.ToString() == Alg)
                    {
                        NozzlesParameters_dataGridView.Rows[i].Cells["VisionAlgorithm_column"].Value = VideoAlgorithms.AllAlgorithms[0].Name;
                    }

                }
            }
        }



        #endregion Current video algorithm
        // =====================================================================================

        // =====================================================================================
        #region Functions and parameters

        // =====================================================================================
        // Buttons:

        private void AddFunction_button_Click(object sender, EventArgs e)
        {
            // adding a row will change current cell; we don't want false update of function parameters
            AlgorithmChange = true;
            DataGridViewSelectedRowCollection SelectedRows = Functions_dataGridView.SelectedRows;
            int row = 0;
            if (Functions_dataGridView.Rows.Count == 0)
            {
                // grid is empty:
                Functions_dataGridView.Rows.Insert(0);
            }
            else
            {
                // insert at end
                Functions_dataGridView.Rows.Insert(Functions_dataGridView.Rows.Count);
                row = Functions_dataGridView.Rows.Count - 1;
            };

            int FunctCol = (int)Functions_dataGridViewColumns.FunctionColumn;
            // int ActiveCol = (int)Functions_dataGridViewColumns.ActiveColumn;


            AForgeFunctionDefinition Newfunct = new AForgeFunctionDefinition();
            Newfunct.Name = KnownFunctions[0].ToString();    // default to the first in list
            VideoAlgorithms.CurrentAlgorithm.FunctionList.Insert(row, Newfunct);
            AlgorithmChange = false; // next triggers cell changed event
            Functions_dataGridView.CurrentCell = Functions_dataGridView.Rows[row].Cells[FunctCol];
            Functions_dataGridView.Rows[row].Cells[FunctCol].Value = Newfunct.Name;

        }

        // ===================
        private void RemoveFunction_button_Click(object sender, EventArgs e)
        {
            // If function exists, remove it.
            if (VideoAlgorithms.CurrentFunctionIndex >= 0)
            {
                bool WasActive =
                    VideoAlgorithms.CurrentAlgorithm.FunctionList[VideoAlgorithms.CurrentFunctionIndex].Active;
                VideoAlgorithms.CurrentAlgorithm.FunctionList.RemoveAt(VideoAlgorithms.CurrentFunctionIndex);
                VideoAlgorithms.CurrentFunctionIndex = -1;
                // We don't want false update of function parameters
                AlgorithmChange = true;
                FillFunctionTable(VideoAlgorithms.CurrentAlgorithm.Name);
                ClearFunctionParameters();
                Functions_dataGridView.CurrentCell = null;
                AlgorithmChange = false;
                if (WasActive)
                {
                    UpdateVideoProcessing();
                }
            }
        }

        // ===================
        private void MoveUp_button_Click(object sender, EventArgs e)
        {
            if (Functions_dataGridView.CurrentCell == null)
            {
                DisplayText("Move up, cell=null");
                return;
            }
            int OldPos = Functions_dataGridView.CurrentCell.RowIndex;
            if (OldPos == 0)
            {
                DisplayText("Move up, at top (row==0)");
                return;
            }
            MoveFunction(OldPos, OldPos - 1);
        }

        // ===================
        private void MoveDown_button_Click(object sender, EventArgs e)
        {
            if (Functions_dataGridView.CurrentCell == null)
            {
                DisplayText("Move down, cell=null");
                return;
            }
            int OldPos = Functions_dataGridView.CurrentCell.RowIndex;
            if (OldPos >= Functions_dataGridView.RowCount - 1)
            {
                DisplayText("Move down, at bottom)");
                return;
            }
            MoveFunction(OldPos, OldPos + 1);
        }

        // ===================
        private void MoveFunction(int OldPos, int NewPos)
        {
            // Re-arranges functions in the UI
            AForgeFunctionDefinition funct = VideoAlgorithms.CurrentAlgorithm.FunctionList[OldPos];
            VideoAlgorithms.CurrentAlgorithm.FunctionList.RemoveAt(OldPos);
            VideoAlgorithms.CurrentAlgorithm.FunctionList.Insert(NewPos, funct);
            int col = Functions_dataGridView.CurrentCell.ColumnIndex;
            AlgorithmChange = true;
            FillFunctionTable(VideoAlgorithms.CurrentAlgorithm.Name);
            ClearFunctionParameters();
            Functions_dataGridView.CurrentCell = null; // to force the change event at last statement
            AlgorithmChange = false;
            Functions_dataGridView.CurrentCell = Functions_dataGridView[col, NewPos];
            if (funct.Active)
            {
                UpdateVideoProcessing();
            }
        }

        // ===================
        private void Algorithm_Measure_button_Click(object sender, EventArgs e)
        {
            Camera cam = UpCamera;
            if (DownCam_radioButton.Checked)
            {
                cam = DownCamera;
            }
            cam.BuildMeasurementFunctionsList(VideoAlgorithms.CurrentAlgorithm.FunctionList);
            cam.MeasurementParameters = VideoAlgorithms.CurrentAlgorithm.MeasurementParameters;
            cam.Measure(out double X, out double Y, out double Ares, true);
        }

        private void Measure10x_button_Click(object sender, EventArgs e)
        {
            if (DownCam_radioButton.Checked)
            {
                cam = DownCamera;
            }
            cam.BuildMeasurementFunctionsList(VideoAlgorithms.CurrentAlgorithm.FunctionList);
            cam.MeasurementParameters = VideoAlgorithms.CurrentAlgorithm.MeasurementParameters;
            double Xmax = 0.0;
            double Xmin = 9999999.0;
            double Xsum = 0.0;
            double Ymax = 0.0;
            double Ymin = 9999999.0;
            double Ysum = 0.0;
            double Amax = -9999999.0;
            double Amin = 99999999.0;
            double Asum = 0.0;
            for (int i = 0; i < 10; i++)
            {
                cam.Measure(out double X, out double Y, out double A, false);
                DisplayText("X: " + X.ToString("0.000") + ", Y:" + Y.ToString("0.000") + ", A:" + A.ToString("0.000"));
                if (X > Xmax) Xmax = X;
                if (X < Xmin) Xmin = X;
                Xsum = Xsum + X;
                if (Y > Ymax) Ymax = Y;
                if (Y < Ymin) Ymin = Y;
                Ysum = Ysum + Y;
                if (A > Amax) Amax = A;
                if (A < Amin) Amin = A;
                Asum = Asum + A;
            }
            Xsum = Xsum / 10.0;
            Ysum = Ysum / 10.0;
            Asum = Asum / 10.0;
            DisplayText("Results:");
            DisplayText("Xmax: " + Xmax.ToString("0.000") + ", Xmin: " + Xmin.ToString("0.000") + ", Avg: " + Xsum.ToString("0.000")
                + ", Diff: " + (Xmax - Xmin).ToString("0.000"));
            DisplayText("Ymax: " + Ymax.ToString("0.000") + ", Ymin: " + Ymin.ToString("0.000") + ", Avg: " + Ysum.ToString("0.000")
                + ", Diff: " + (Ymax - Ymin).ToString("0.000"));
            DisplayText("Amax: " + Amax.ToString("0.000") + ", Amin: " + Amin.ToString("0.000") + ", Avg: " + Asum.ToString("0.000")
                + ", Diff: " + (Amax - Amin).ToString("0.000"));
        }

        // =====================================================================================
        // Functions_dataGridView 

        // =====================================================================================
        // Helper functions:

        public void BuildAlgorithmsCombox(out DataGridViewComboBoxCell Cout)
        {
            DataGridViewComboBoxCell c = new DataGridViewComboBoxCell();
            for (int i = 0; i < VideoAlgorithms.AllAlgorithms.Count; i++)
            {
                c.Items.Add(VideoAlgorithms.AllAlgorithms[i].Name);
            }
            Cout = c;
        }


        void FillFunctionTable(string AlgorithmName)
        {
            // User changed the current algorithm or deleted a fuction. 
            // This function (re-)fills Algorithms_dataGridView function column
            Functions_dataGridView.Rows.Clear();
            int row = 0;
            AForgeFunctionDefinition func = new AForgeFunctionDefinition();

            for (int i = 0; i < VideoAlgorithms.CurrentAlgorithm.FunctionList.Count; i++)
            {
                func = VideoAlgorithms.CurrentAlgorithm.FunctionList[i];
                if (KnownFunctions.Contains(func.Name))
                {
                    int index = KnownFunctions.IndexOf(func.Name);
                    Functions_dataGridView.Rows.Add();
                    Functions_dataGridView.Rows[row].Cells[(int)Functions_dataGridViewColumns.FunctionColumn].Value =
                        func.Name;
                    Functions_dataGridView.Rows[row].Cells[(int)Functions_dataGridViewColumns.ActiveColumn].Value =
                        func.Active;
                    row++;
                }
            }
            Update_GridView(Functions_dataGridView);
        }

        // =====================================================================================
        // Grid cell events:

        private void Functions_dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (AlgorithmChange)
            {
                //DisplayText("Functions_dataGridView_CellValueChanged(), AlgorithmChange");
                return;
            }
            if (Functions_dataGridView.CurrentCell == null)
            {
                //DisplayText("Functions_dataGridView_CellValueChanged(), cell=null");
                return;
            }
            int row = Functions_dataGridView.CurrentCell.RowIndex;
            int col = Functions_dataGridView.CurrentCell.ColumnIndex;
            //DisplayText("Functions_dataGridView_CellValueChanged(), " + row.ToString() + ", " + col.ToString());
            int FunctCol = (int)Functions_dataGridViewColumns.FunctionColumn;
            int ActiveCol = (int)Functions_dataGridViewColumns.ActiveColumn;

            if (Functions_dataGridView.Rows[row].Cells[FunctCol].Value == null)
            {
                //DisplayText("value: null");
                return;
            };
            //DisplayText("value: " + Functions_dataGridView.Rows[row].Cells[FunctCol].Value.ToString());
            Update_GridView(Functions_dataGridView);

            if (col == FunctCol)
            {
                // Function column changed
                string FunctionName = Functions_dataGridView.Rows[row].Cells[FunctCol].Value.ToString();
                VideoAlgorithms.CurrentFunctionIndex = row;
                VideoAlgorithms.CurrentAlgorithm.FunctionList[row].Name = FunctionName;
                SetFunctionDefaultParameters(FunctionName);
                VideoAlgorithms.CurrentAlgorithm.FunctionList[row].Active = false;  // newly selected function is inactive by default
                Functions_dataGridView.Rows[row].Cells[ActiveCol].Value = false;
                Update_GridView(Functions_dataGridView);
                UpdateParameterTargets(FunctionName);
                UpdateVideoProcessing();
            }
            else
            {
                // active column changed
                VideoAlgorithms.CurrentAlgorithm.FunctionList[row].Active =
                    (bool)Functions_dataGridView.Rows[row].Cells[ActiveCol].Value;
                UpdateVideoProcessing();
            }
        }

        private void Functions_dataGridView_CurrentCellChanged(object sender, EventArgs e)
        {
            if (AlgorithmChange)
            {
                DisplayText("Functions_dataGridView_CurrentCellChanged(), AlgorithmChange");
                return;
            }
            if (Functions_dataGridView.CurrentCell == null)
            {
                DisplayText("Functions_dataGridView_CurrentCellChanged(), cell=null");
                return;
            }
            int row = Functions_dataGridView.CurrentCell.RowIndex;
            int col = Functions_dataGridView.CurrentCell.ColumnIndex;
            DisplayText("Functions_dataGridView_CurrentCellChanged(), " + row.ToString() + ", " + col.ToString());
            VideoAlgorithms.CurrentFunctionIndex = row;
            if (Functions_dataGridView.Rows[row].Cells[(int)Functions_dataGridViewColumns.FunctionColumn].Value == null)
            {
                // user is creating new function and has not yet selected the value
                return;
            }
            UpdateParameterTargets(Functions_dataGridView.Rows[row].Cells[(int)Functions_dataGridViewColumns.FunctionColumn].Value.ToString());
        }

        private void Functions_dataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (Functions_dataGridView.CurrentCell == null)
            {
                DisplayText("Functions_dataGridView_CurrentCellDirtyStateChanged(), cell=null");
                return;
            }

            int row = Functions_dataGridView.CurrentCell.RowIndex;
            int col = Functions_dataGridView.CurrentCell.ColumnIndex;

            DisplayText("Functions_dataGridView_CurrentCellDirtyStateChanged: "
                + row.ToString() + ", " + col.ToString());
            // Return if not dirty; otherwise the stuff is executed twice (once when it changes, once when it becomes clean)
            if (!Functions_dataGridView.IsCurrentCellDirty)
            {
                return;
            }
            Update_GridView(Functions_dataGridView);
        }


        // =====================================================================================
        // Functions parameters 
        // =====================================================================================

        // To add a function to UI: Add its name and default parameters to the case clause in function SetFunctionDefaultParameters().
        // Enable the required parameters in UpdateParameterTargets() and write the explanation text there.
        // Add the function to Camera.cs, section "Functions compatible with lists:"


        private void SetFunctionDefaultParameters(string FunctionName)
        {
            // The function is just created. Function is 
            AForgeFunctionDefinition funct = VideoAlgorithms.CurrentAlgorithm.FunctionList[VideoAlgorithms.CurrentFunctionIndex];
            switch (FunctionName)
            {
                // switch by the selected algorithm:  
                case "Filter Features by Size":
                    funct.parameterDoubleA = 1.0;
                    funct.parameterDoubleB = 4.0;
                    break;

                case "Blur":
                    break;		// no parameters

                case "Histogram":
                    break;		// no parameters

                case "Grayscale":
                    break;		// no parameters

                case "Invert":
                    break;      // no parameters

                case "Erosion":
                    break;      // no parameters

                case "Edge detect":
                    // Type, maximum difference of processing pixel with neighboring pixels in 8 direction
                    funct.parameterInt = 3;
                    break;

                case "Kill color":
                    // radius and color
                    funct.parameterInt = 50;
                    funct.R = 128;
                    funct.G = 128;
                    funct.B = 128;
                    break;

                case "Keep color":
                    // radius and color
                    funct.parameterInt = 50;
                    funct.R = 128;
                    funct.G = 128;
                    funct.B = 128;
                    break;

                case "Meas. zoom":
                    funct.parameterDouble = 2.0;
                    break;

                case "Gaussian blur":
                    funct.parameterDouble = 2.0;
                    break;

                case "Threshold":
                    funct.parameterInt = 128;
                    break;

                case "Hough circles":
                    funct.parameterInt = 128;
                    break;

                case "Jog before measurement":
                    break;		// no parameters

                // ===== EmguCV Advanced Functions =====
                
                case "Canny edge detection":
                    funct.parameterDoubleA = 100;   // Lower threshold
                    funct.parameterDoubleB = 200;   // Upper threshold
                    funct.parameterInt = 3;         // Aperture size
                    break;

                case "Sobel edge detection":
                    funct.parameterInt = 0;         // Direction (0=Both)
                    funct.parameterDouble = 1.0;    // Scale
                    funct.parameterDoubleA = 0;     // Delta
                    break;

                case "Laplacian edge detection":
                    funct.parameterInt = 3;         // Aperture size
                    funct.parameterDouble = 1.0;    // Scale
                    funct.parameterDoubleA = 0;     // Delta
                    break;

                case "Adaptive threshold":
                    funct.parameterInt = 0;         // Method (0=Mean)
                    funct.parameterDouble = 11;     // Block size (must be odd)
                    funct.parameterDoubleA = 2;     // C constant
                    funct.parameterDoubleB = 255;   // Max value
                    break;

                case "Bilateral filter":
                    funct.parameterInt = 9;         // Diameter
                    funct.parameterDouble = 75;     // Sigma color
                    funct.parameterDoubleA = 75;    // Sigma space
                    break;

                case "CLAHE":
                    funct.parameterDouble = 40.0;   // Clip limit
                    funct.parameterInt = 8;         // Tile grid size
                    break;

                case "Morphological gradient":
                    funct.parameterInt = 3;         // Kernel size
                    funct.parameterDouble = 1;      // Iterations
                    break;

                case "Morphological top hat":
                    funct.parameterInt = 5;         // Kernel size
                    break;

                case "Morphological black hat":
                    funct.parameterInt = 5;         // Kernel size
                    break;

                case "Hough circles (sub-pixel)":
                    funct.parameterInt = 0;         // Method
                    funct.parameterDouble = 1.0;    // DP
                    funct.parameterDoubleA = 20;    // Min distance
                    funct.parameterDoubleB = 100;   // Param1
                    funct.parameterDoubleC = 30;    // Param2
                    funct.R = 10;                   // Min radius
                    funct.G = 100;                  // Max radius
                    break;

                case "Harris corners":
                    funct.parameterInt = 2;         // Block size
                    funct.parameterDouble = 3;      // Aperture
                    funct.parameterDoubleA = 0.04;  // K parameter
                    break;

                case "Shi-Tomasi corners":
                    funct.parameterInt = 100;       // Max corners
                    funct.parameterDouble = 0.01;   // Quality level
                    funct.parameterDoubleA = 10;    // Min distance
                    break;

                case "FAST feature detection":
                    funct.parameterInt = 40;        // Threshold
                    funct.parameterDouble = 1;      // Non-max suppression (1=on)
                    break;

                case "Template matching":
                    funct.parameterInt = 5;         // Method (CCOEFF_NORMED)
                    funct.parameterDouble = 0.8;    // Match threshold
                    break;

                case "Contour detection":
                    funct.parameterInt = 1;         // Retrieval mode (LIST)
                    funct.parameterDouble = 100;    // Min area
                    funct.parameterDoubleA = 10000; // Max area
                    break;

                case "Watershed segmentation":
                    funct.parameterInt = 10;        // Min distance
                    funct.parameterDouble = 0.5;    // Threshold factor
                    break;

                default:
                    break;
            }
            return;
        }

        private void UpdateParameterTargets(string Name)
        {
            // This function is called when user changes a function in Algorithms_dataGridView
            // The function updates the parameter editing boxes
            // make all inactive
            ClearFunctionParameters();
            // activate those that this function needs, with description text, 
            // and put in default values
            switch (Name)
            {
                // switch by the selected algorithm:
                case "Filter Features by Size":
                    FunctionExplanation_textBox.Text = "Removes features that are smaller or larger than  the specified values (mm).\r\n" +
                        "Also discards features that are further away from the center than specified.\r\n" +
                        "Currently used only when searching 'component by pads'.";
                    FunctionExplanation_textBox.Visible = true;
                    EnableDoubleA("Size min:");
                    EnableDoubleB("Size max:");
                    EnableDoubleC("Distance max:");
                    break;

                case "Blur":
                    // no parameters
                    FunctionExplanation_textBox.Text = "Blurs the image, reducing the effects of camera noise " +
                        "and possible imperfections in the target outline";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Histogram":
                    FunctionExplanation_textBox.Text = "Increases contrast in the image";
                    FunctionExplanation_textBox.Visible = true;
                    break;		// no parameters

                case "Grayscale":
                    FunctionExplanation_textBox.Text = "Converts the image to grayscale";
                    FunctionExplanation_textBox.Visible = true;
                    break;		// no parameters

                case "Invert":
                    FunctionExplanation_textBox.Text = "Inverts the image; the detection functions are looking for" +
                        " white image on black background.";
                    FunctionExplanation_textBox.Visible = true;
                    break;		// no parameters

                case "Erosion":
                    FunctionExplanation_textBox.Text = "Assigns minimum value of surrounding pixels to each pixel of the result image." +
                        "  Removes noisy pixels, shrinks objects.";
                    FunctionExplanation_textBox.Visible = true;
                    break;		// no parameters

                case "Edge detect":
                    // single int parameter, 1..4
                    EnableInt(1, 4, "Operator type:");
                    FunctionExplanation_textBox.Text = "Finds edges in the image:\r\n" +
                        "1: Using Sobel operator.\r\n" +
                        "2: Calculating maximum difference between pixels in 4 directions around the processing pixel.\r\n" +
                        "3: Calculating maximum difference of processing pixel with neighboring pixels in 8 direction.\r\n" +
                        "4: Applying Canny edge detector";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Kill color":
                    // int and RGB parameter
                    EnableInt(0, 450, "Radius:");
                    EnableRGB("Color to remove: ");
                    FunctionExplanation_textBox.Text = "Removes color that is inside of RGB sphere " +
                        "with specified center color and radius.";
                    FunctionExplanation_textBox.Visible = true;
                    EnableColorBox();
                    break;

                case "Keep color":
                    // int and RGB parameter
                    EnableInt(0, 450, "Radius:");
                    EnableRGB("Color to keep: ");
                    FunctionExplanation_textBox.Text = "Keeps color that is outside of RGB sphere " +
                        "with specified center color and radius.";
                    FunctionExplanation_textBox.Visible = true;
                    EnableColorBox();
                    break;

                case "Meas. zoom":
                    // one double parameter
                    EnableDouble("Zoom factor:");
                    FunctionExplanation_textBox.Text = "Enlargens the image that is used for measurements.";
                    FunctionExplanation_textBox.Visible = true;

                    break;

                case "Gaussian blur":
                    // one double parameter
                    EnableDouble("Sigma:");
                    FunctionExplanation_textBox.Text = "Another method to blur the image: gaussian blur with kernel size of 11.";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Hough circles":
                    // int and double parameter
                    EnableInt(0, 255, "Diameter:");
                    EnableDouble("Intensity:");
                    FunctionExplanation_textBox.Text = "Finds partial circles with specified diameter.\r\n"
                        + "Result intensity correlates to match quality.";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Threshold":
                    // one int parameter
                    EnableInt(0, 255, "Threshold:");
                    FunctionExplanation_textBox.Text = "Makes the image black and white.";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Jog before measurement":
                    // no parameters
                    FunctionExplanation_textBox.Text = "Jog machine to position before continuing.\r\n"
                        + "(useful to target fiducials on a very tight board, for example)";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                // ===== EmguCV Advanced Functions UI =====

                case "Canny edge detection":
                    EnableDoubleA("Lower threshold:");
                    EnableDoubleB("Upper threshold:");
                    EnableInt(3, 7, "Aperture size:");
                    FunctionExplanation_textBox.Text =
                        "Detects edges using Canny algorithm with hysteresis.\r\n" +
                        "Lower threshold: Weak edges below this are discarded.\r\n" +
                        "Upper threshold: Strong edges above this are kept.\r\n" +
                        "Aperture: Sobel kernel size (3, 5, or 7)";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Sobel edge detection":
                    EnableInt(0, 3, "Direction:");
                    EnableDouble("Scale:");
                    EnableDoubleA("Delta:");
                    FunctionExplanation_textBox.Text =
                        "Detects edges using Sobel operator.\r\n" +
                        "Direction: 0=Both XY, 1=X only, 2=Y only, 3=Magnitude\r\n" +
                        "Scale: Multiplier for gradient values\r\n" +
                        "Delta: Value added to results";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Laplacian edge detection":
                    EnableInt(1, 7, "Aperture:");
                    EnableDouble("Scale:");
                    EnableDoubleA("Delta:");
                    FunctionExplanation_textBox.Text =
                        "Detects edges using second derivative (Laplacian).\r\n" +
                        "Good for finding zero-crossings and fine details.\r\n" +
                        "Aperture: Kernel size (1, 3, 5, or 7)";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Adaptive threshold":
                    EnableInt(0, 1, "Method:");
                    EnableDouble("Block size:");
                    EnableDoubleA("C constant:");
                    EnableDoubleB("Max value:");
                    FunctionExplanation_textBox.Text =
                        "Threshold that adapts to local illumination.\r\n" +
                        "Method: 0=Mean, 1=Gaussian weighted\r\n" +
                        "Block size: Neighborhood size (must be odd)\r\n" +
                        "C: Constant subtracted from mean/weighted mean";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Bilateral filter":
                    EnableInt(1, 50, "Diameter:");
                    EnableDouble("Sigma color:");
                    EnableDoubleA("Sigma space:");
                    FunctionExplanation_textBox.Text =
                        "Edge-preserving noise reduction.\r\n" +
                        "Diameter: Filter size (larger = slower but smoother)\r\n" +
                        "Sigma color: Color difference sensitivity\r\n" +
                        "Sigma space: Spatial distance sensitivity";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "CLAHE":
                    EnableDouble("Clip limit:");
                    EnableInt(2, 32, "Tile size:");
                    FunctionExplanation_textBox.Text =
                        "Contrast enhancement with clipping to prevent over-amplification.\r\n" +
                        "Clip limit: Controls contrast amplification (higher = more contrast)\r\n" +
                        "Tile size: Grid size for local histogram equalization";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Morphological gradient":
                    EnableInt(1, 21, "Kernel size:");
                    EnableDouble("Iterations:");
                    FunctionExplanation_textBox.Text =
                        "Edge detection via morphology (dilation - erosion).\r\n" +
                        "Highlights object boundaries.\r\n" +
                        "Kernel size: Structuring element size (odd numbers)";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Morphological top hat":
                    EnableInt(1, 21, "Kernel size:");
                    FunctionExplanation_textBox.Text =
                        "Extracts bright features smaller than structuring element.\r\n" +
                        "Useful for finding small bright objects on dark background.";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Morphological black hat":
                    EnableInt(1, 21, "Kernel size:");
                    FunctionExplanation_textBox.Text =
                        "Extracts dark features smaller than structuring element.\r\n" +
                        "Useful for finding small dark objects on bright background.";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Hough circles (sub-pixel)":
                    EnableInt(0, 1, "Method:");
                    EnableDouble("DP:");
                    EnableDoubleA("Min distance:");
                    EnableDoubleB("Edge threshold:");
                    EnableDoubleC("Center threshold:");
                    R_label.Text = "Min radius:";
                    R_label.Visible = true;
                    DoubleParA_textBox.Text = VideoAlgorithms.CurrentAlgorithm.FunctionList[VideoAlgorithms.CurrentFunctionIndex].R.ToString();
                    DoubleParA_textBox.Visible = true;
                    G_label.Text = "Max radius:";
                    G_label.Visible = true;
                    DoubleParB_textBox.Text = VideoAlgorithms.CurrentAlgorithm.FunctionList[VideoAlgorithms.CurrentFunctionIndex].G.ToString();
                    DoubleParB_textBox.Visible = true;
                    FunctionExplanation_textBox.Text =
                        "Circle detection with sub-pixel accuracy.\r\n" +
                        "DP: Inverse accumulator resolution\r\n" +
                        "Min distance: Minimum distance between circle centers\r\n" +
                        "Edge threshold: Canny edge detector threshold\r\n" +
                        "Center threshold: Accumulator threshold for centers";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Harris corners":
                    EnableInt(1, 10, "Block size:");
                    EnableDouble("Aperture:");
                    EnableDoubleA("K parameter:");
                    FunctionExplanation_textBox.Text =
                        "Detects corner points (Harris detector).\r\n" +
                        "Block size: Neighborhood size\r\n" +
                        "Aperture: Sobel derivative aperture\r\n" +
                        "K: Harris detector free parameter (0.04-0.06 typical)";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Shi-Tomasi corners":
                    EnableInt(1, 500, "Max corners:");
                    EnableDouble("Quality level:");
                    EnableDoubleA("Min distance:");
                    FunctionExplanation_textBox.Text =
                        "Detects good features to track (Shi-Tomasi).\r\n" +
                        "Max corners: Maximum number of corners to return\r\n" +
                        "Quality level: Minimal accepted quality (0.01 typical)\r\n" +
                        "Min distance: Minimum distance between corners";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "FAST feature detection":
                    EnableInt(1, 100, "Threshold:");
                    EnableDouble("Non-max suppress:");
                    FunctionExplanation_textBox.Text =
                        "Fast corner detection (FAST algorithm).\r\n" +
                        "Threshold: Detection sensitivity (lower = more corners)\r\n" +
                        "Non-max suppress: 1=Remove adjacent weak corners, 0=Keep all";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Template matching":
                    EnableInt(0, 5, "Method:");
                    EnableDouble("Threshold:");
                    FunctionExplanation_textBox.Text =
                        "Find template pattern in image.\r\n" +
                        "Method: 0-1=SQDIFF, 2-3=CCORR, 4-5=CCOEFF (5 recommended)\r\n" +
                        "Threshold: Match quality threshold (0-1, higher = stricter)";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Contour detection":
                    EnableInt(0, 3, "Mode:");
                    EnableDouble("Min area:");
                    EnableDoubleA("Max area:");
                    FunctionExplanation_textBox.Text =
                        "Finds and filters contours by area.\r\n" +
                        "Mode: 0=External only, 1=All contours, 2=Two-level, 3=Tree\r\n" +
                        "Min/Max area: Filter contours by pixel area";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                case "Watershed segmentation":
                    EnableInt(1, 50, "Min distance:");
                    EnableDouble("Threshold:");
                    FunctionExplanation_textBox.Text =
                        "Separates touching objects using watershed algorithm.\r\n" +
                        "Min distance: Minimum distance between object seeds\r\n" +
                        "Threshold: Controls segmentation sensitivity";
                    FunctionExplanation_textBox.Visible = true;
                    break;

                default:
                    break;
            }
            return;

            // local functions for UpdateParameterTargets():
            void EnableInt(int min, int max, string label)
            {
                IntParameter_numericUpDown.Minimum = min;
                IntParameter_numericUpDown.Maximum = max;
                IntParameter_numericUpDown.Value =
                    VideoAlgorithms.CurrentAlgorithm.FunctionList[VideoAlgorithms.CurrentFunctionIndex].parameterInt;
                IntParameter_numericUpDown.Visible = true;
                IntParameter_label.Text = label;
                IntParameter_label.Visible = true;
                if (max>19)
                {
                    IntParameterUp10_button.Visible = true;
                    IntParameterDown10_button.Visible = true;
                }
            }

            void EnableDouble(string label)
            {
                DoubleParameter_textBox.Text =
                    VideoAlgorithms.CurrentAlgorithm.FunctionList[VideoAlgorithms.CurrentFunctionIndex].parameterDouble.ToString();
                DoubleParameter_textBox.Visible = true;
                DoubleParameter_label.Text = label;
                DoubleParameter_label.Visible = true;
            }

            void EnableDoubleA(string label)
            {
                DoubleParA_textBox.Text =
                    VideoAlgorithms.CurrentAlgorithm.FunctionList[VideoAlgorithms.CurrentFunctionIndex].parameterDoubleA.ToString();
                DoubleParA_textBox.Visible = true;
                R_label.Text = label;
                R_label.Visible = true;
            }

            void EnableDoubleB(string label)
            {
                DoubleParB_textBox.Text =
                    VideoAlgorithms.CurrentAlgorithm.FunctionList[VideoAlgorithms.CurrentFunctionIndex].parameterDoubleB.ToString();
                DoubleParB_textBox.Visible = true;
                G_label.Text = label;
                G_label.Visible = true;
            }

            void EnableDoubleC(string label)
            {
                DoubleParC_textBox.Text =
                    VideoAlgorithms.CurrentAlgorithm.FunctionList[VideoAlgorithms.CurrentFunctionIndex].parameterDoubleC.ToString();
                DoubleParC_textBox.Visible = true;
                B_label.Text = label;
                B_label.Visible = true;
            }

            void EnableRGB(string label)
            {
                R_label.Text = "R";
                R_label.Visible = true;
                G_label.Text = "G";
                G_label.Visible = true;
                B_label.Text = "B";
                B_label.Visible = true;
                R_numericUpDown.Visible = true;
                G_numericUpDown.Visible = true;
                B_numericUpDown.Visible = true;
                R_numericUpDown.Value =
                    VideoAlgorithms.CurrentAlgorithm.FunctionList[VideoAlgorithms.CurrentFunctionIndex].R;
                G_numericUpDown.Value =
                    VideoAlgorithms.CurrentAlgorithm.FunctionList[VideoAlgorithms.CurrentFunctionIndex].G;
                B_numericUpDown.Value =
                    VideoAlgorithms.CurrentAlgorithm.FunctionList[VideoAlgorithms.CurrentFunctionIndex].B;
                RGBParameter_label.Text = label;
                RGBParameter_label.Visible = true;
            }

            void EnableColorBox()
            {
                Color_Box.Visible = true;
                ColorHelp_label.Visible = true;
            }

        }       // UpdateParameterTargets() end


        private void ClearFunctionParameters()
        {
            IntParameter_label.Text = "--";
            IntParameter_label.Visible = false;
            IntParameter_numericUpDown.Visible = false;
            IntParameterUp10_button.Visible = false;
            IntParameterDown10_button.Visible = false;


            DoubleParameter_label.Text = "--";
            DoubleParameter_label.Visible = false;
            DoubleParameter_textBox.Text = "";
            DoubleParameter_textBox.Visible = false;

            DoubleParA_textBox.Text = "";
            DoubleParA_textBox.Visible = false;
            DoubleParB_textBox.Text = "";
            DoubleParB_textBox.Visible = false;
            DoubleParC_textBox.Text = "";
            DoubleParC_textBox.Visible = false;

            RGBParameter_label.Text = "--";
            RGBParameter_label.Visible = false;
            R_numericUpDown.Visible = false;
            G_numericUpDown.Visible = false;
            B_numericUpDown.Visible = false;
            R_label.Visible = false;
            G_label.Visible = false;
            B_label.Visible = false;
            Color_Box.Visible = false;
            ColorHelp_label.Visible = false;

            FunctionExplanation_textBox.Text = "";
            FunctionExplanation_textBox.Visible = false;
        }

        private void UpdateVideoProcessing()
        {
            // User changed something, that (potentially) affects the current video processing
            // Note, that there might not be a current algorithm
            if (NoVideoProcessing_radioButton.Checked)
            {
                return;
            }
            if (VideoAlgorithms.CurrentAlgorithm == null)
            {
                DisplayText("UpdateVideoProcessing(), no current algorithm");
                return;
            }
            // Pass CurrentAlgorithm to camera
            DisplayText("UpdateVideoProcessing()");
            if (DownCam_radioButton.Checked)
            {
                DownCamera.BuildDisplayFunctionsList(VideoAlgorithms.CurrentAlgorithm.FunctionList);
            }
            else
            {
                UpCamera.BuildDisplayFunctionsList(VideoAlgorithms.CurrentAlgorithm.FunctionList);
            }
            UpdateSearchFunctions();
        }

        private void StopVideoProcessing()
        {
            DisplayText("StopVideoProcessing()");
            if (DownCam_radioButton.Checked)
            {
                DownCamera.ClearDisplayFunctionsList();
            }
            if (UpCam_radioButton.Checked)
            {
                UpCamera.ClearDisplayFunctionsList();
            }
        }


        // =====================================================================================
        // Functions parameter changes:
        private void IntParameter_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            VideoAlgorithms.CurrentFunction_NewInt((int)IntParameter_numericUpDown.Value);
            UpdateVideoProcessing();
        }

        // Separate buttons for 10 increment
        private void IntParameterUp10_button_Click(object sender, EventArgs e)
        {
            if (IntParameter_numericUpDown.Value <= (IntParameter_numericUpDown.Maximum-10))
            {
                IntParameter_numericUpDown.Value = IntParameter_numericUpDown.Value + 10;
            }
            else
            {
                IntParameter_numericUpDown.Value = IntParameter_numericUpDown.Maximum;
            }

            VideoAlgorithms.CurrentFunction_NewInt((int)IntParameter_numericUpDown.Value);
            UpdateVideoProcessing();
        }
        private void IntParameterDown10_button_Click(object sender, EventArgs e)
        {
            if (IntParameter_numericUpDown.Value>10)
            {
                IntParameter_numericUpDown.Value = IntParameter_numericUpDown.Value - 10;
            }
            else
            {
                IntParameter_numericUpDown.Value = IntParameter_numericUpDown.Value = 0;
            }
            VideoAlgorithms.CurrentFunction_NewInt((int)IntParameter_numericUpDown.Value);
            UpdateVideoProcessing();
        }

        private bool DoubletextBox_TextChanged(TextBox box, ref double val)
        {
            CommasToPoints(box);
            if (double.TryParse(box.Text, out val))
            {
                box.ForeColor = Color.Black;
                return true;
            }
            else
            {
                box.ForeColor = Color.Red;
                return false;
            }
        }



        private void DoubleParameter_textBox_TextChanged(object sender, EventArgs e)
        {
            double val = 0.0;
            if (DoubletextBox_TextChanged(DoubleParameter_textBox, ref val))
            {
                VideoAlgorithms.CurrentFunction_NewDouble(val);
                UpdateVideoProcessing();
            }
        }

        private void DoubleParA_textBox_TextChanged(object sender, EventArgs e)
        {
            double val = 0.0;
            if (DoubletextBox_TextChanged(DoubleParA_textBox, ref val))
            {
                VideoAlgorithms.CurrentFunction_NewDoubleParA(val);
                UpdateVideoProcessing();
            }
        }

        private void DoubleParB_textBox_TextChanged(object sender, EventArgs e)
        {
            double val = 0.0;
            if (DoubletextBox_TextChanged(DoubleParB_textBox, ref val))
            {
                VideoAlgorithms.CurrentFunction_NewDoubleParB(val);
                UpdateVideoProcessing();
            }
        }

        private void DoubleParC_textBox_TextChanged(object sender, EventArgs e)
        {
            double val = 0.0;
            if (DoubletextBox_TextChanged(DoubleParC_textBox, ref val))
            {
                VideoAlgorithms.CurrentFunction_NewDoubleParC(val);
                UpdateVideoProcessing();
            }
        }

        private void R_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            VideoAlgorithms.CurrentFunction_NewR((int)R_numericUpDown.Value);
            UpdateColorBoxColor();
            UpdateVideoProcessing();
        }

        private void G_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            VideoAlgorithms.CurrentFunction_NewG((int)G_numericUpDown.Value);
            UpdateColorBoxColor();
            UpdateVideoProcessing();
        }

        private void B_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            VideoAlgorithms.CurrentFunction_NewB((int)B_numericUpDown.Value);
            UpdateColorBoxColor();
            UpdateVideoProcessing();
        }

        private void Functions_dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            DisplayText("Functions_dataGridView_DataError ");
        }

        // =====================================================================================
        // Color box
        private void UpdateColorBoxColor()
        {
            Color_Box.BackColor = Color.FromArgb((int)R_numericUpDown.Value, (int)G_numericUpDown.Value, (int)B_numericUpDown.Value);
        }

        //  Alt-click on video box calls this, used to pick color for processing
        private void PickColor(int X, int Y)
        {
            if (X < 2)
                X = 2;
            if (X > (Cam_pictureBox.Width - 2))
                X = Cam_pictureBox.Width - 2;
            if (Y < 2)
                Y = 2;
            if (Y > (Cam_pictureBox.Height - 2))
                X = Cam_pictureBox.Height - 2;

            byte R = 0;
            byte G = 0;
            byte B = 0;
            Color pixelColor;
            Bitmap img = (Bitmap)Cam_pictureBox.Image.Clone();
            // X, Y are from the Cam_pictureBox.Cam_pictureBox resolution is not the same as image resolution
            double dX = (double)X / (double)Cam_pictureBox.Width;
            double DY = (double)Y / (double)Cam_pictureBox.Height;
            X = (int)(dX * (double)img.Width);
            Y = (int)(DY * (double)img.Height);

            /*
            int Rsum = 0;
            int Gsum = 0;
            int Bsum = 0;
            if (img != null)
            {
                for (int ix = X - 2; ix <= X + 2; ix++)
                {
                    for (int iy = Y - 2; iy <= Y + 2; iy++)
                    {
                        pixelColor = img.GetPixel(ix, iy);
                        Rsum += pixelColor.R;
                        Gsum += pixelColor.G;
                        Bsum += pixelColor.B;
                    }
                }
                R = (byte)(Rsum / 25);
                G = (byte)(Gsum / 25);
                B = (byte)(Bsum / 25);
                img.Dispose();
            }
            */

            if (img != null)
            {
                pixelColor = img.GetPixel(X, Y);
                R = pixelColor.R;
                G = pixelColor.G;
                B = pixelColor.B;
                img.Dispose();
            }

            R_numericUpDown.Value = R;
            G_numericUpDown.Value = G;
            B_numericUpDown.Value = B;
            Color_Box.BackColor = Color.FromArgb(R, G, B);
        }


        #endregion Functions and parameters
        // =====================================================================================

        // =====================================================================================
        #region search, size and distance

        private void UpdateSearchFunctions()
        {
            Camera cam;
            if (DownCam_radioButton.Checked)
            {
                cam = DownCamera;
            }
            else
            {
                cam = UpCamera;
            }
            cam.FindCircles = VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.SearchRounds;
            cam.FindRectangles = VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.SearchRectangles;
            cam.FindComponentByOutlines = VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.SearchComponentOutlines;
            cam.FindComponentByPads = VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.SearchComponentPads;
        }


        // search boxes
        private void SearchRound_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.SearchRounds = SearchRound_checkBox.Checked;
            UpdateSearchFunctions();
        }

        private void SearchRectangles_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.SearchRectangles = SearchRectangles_checkBox.Checked;
            UpdateSearchFunctions();
        }

        private void SearchComponentsOutlines_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.SearchComponentOutlines = SearchComponentOutlines_checkBox.Checked;
            UpdateSearchFunctions();
        }

        private void SearchComponentPads_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.SearchComponentPads = SearchComponentPads_checkBox.Checked;
            UpdateSearchFunctions();
        }

        private void ShowPixels_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            /*
            if (ShowPixels_checkBox.Checked)
            {
                Cam_pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
            }
            else
            {
                Cam_pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            }
            */

            UpCamera.ShowPixels = ShowPixels_checkBox.Checked;
            DownCamera.ShowPixels = ShowPixels_checkBox.Checked;
            Setting.Cam_ShowPixels = ShowPixels_checkBox.Checked;
            UpdateDownCamBoxXSizeText();
            UpdateDownCamBoxYSizeText();
            UpdateUpCamBoxXSizeText();
            UpdateUpCamBoxYSizeText();
            InitDownCamFpsMeasurement();
            InitUpCamFpsMeasurement();
        }

        // =====================================================================================
        // Textbox values and interaction
        void CommasToPoints(TextBox box)
        {
            int pos = box.SelectionStart;
            box.Text = box.Text.Replace(',', '.');
            box.SelectionStart = pos;
        }



        private void Xmin_textBox_TextChanged(object sender, EventArgs e)
        {
            double val = 0.0;
            CommasToPoints(Xmin_textBox);
            if (double.TryParse(Xmin_textBox.Text, out val))
            {
                if (val >= 0.0)
                {
                    if (ChangeYwithX)
                    {
                        Ymin_textBox.Text = Xmin_textBox.Text;
                    }
                    Xmin_textBox.ForeColor = Color.Black;
                    VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.Xmin = val;
                    return;
                }
            }
            Xmin_textBox.ForeColor = Color.Red;
        }



        private void Xmax_textBox_TextChanged(object sender, EventArgs e)
        {
            double val = 0.0;
            CommasToPoints(Xmax_textBox);
            if (double.TryParse(Xmax_textBox.Text, out val))
            {
                if (val >= 0.0)
                {
                    if (ChangeYwithX)
                    {
                        Ymax_textBox.Text = Xmax_textBox.Text;
                    }
                    Xmax_textBox.ForeColor = Color.Black;
                    VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.Xmax = val;
                    return;
                }
            }
            Xmax_textBox.ForeColor = Color.Red;
        }

        private void Ymin_textBox_TextChanged(object sender, EventArgs e)
        {
            double val = 0.0;
            CommasToPoints(Ymin_textBox);
            if (double.TryParse(Ymin_textBox.Text, out val))
            {
                if (val >= 0.0)
                {
                    Ymin_textBox.ForeColor = Color.Black;
                    VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.Ymin = val;
                    return;
                }
            }
            Ymin_textBox.ForeColor = Color.Red;
        }

        private void Ymax_textBox_TextChanged(object sender, EventArgs e)
        {
            double val = 0.0;
            CommasToPoints(Ymax_textBox);
            if (double.TryParse(Ymax_textBox.Text, out val))
            {
                if (val >= 0.0)
                {
                    Ymax_textBox.ForeColor = Color.Black;
                    VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.Ymax = val;
                    return;
                }
            }
            Ymax_textBox.ForeColor = Color.Red;
        }


        private void XmaxDistance_textBox_TextChanged(object sender, EventArgs e)
        {
            double val = 0.0;
            CommasToPoints(XmaxDistance_textBox);
            if (double.TryParse(XmaxDistance_textBox.Text, out val))
            {
                if (val >= 0.0)
                {
                    if (ChangeYwithX)
                    {
                        YmaxDistance_textBox.Text = XmaxDistance_textBox.Text;
                    }
                    XmaxDistance_textBox.ForeColor = Color.Black;
                    VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.XUniqueDistance = val;
                    return;
                }
            }
            XmaxDistance_textBox.ForeColor = Color.Red;
        }



        private void YmaxDistance_textBox_TextChanged(object sender, EventArgs e)
        {
            double val = 0.0;
            CommasToPoints(YmaxDistance_textBox);
            if (double.TryParse(YmaxDistance_textBox.Text, out val))
            {
                if (val >= 0.0)
                {
                    YmaxDistance_textBox.ForeColor = Color.Black;
                    VideoAlgorithms.CurrentAlgorithm.MeasurementParameters.YUniqueDistance = val;
                    return;
                }
            }
            YmaxDistance_textBox.ForeColor = Color.Red;
        }
        #endregion search, size and distance
        // =====================================================================================

        #region Camera Engine Selection
        // =====================================================================================
        
        /// <summary>
        /// Event handler for camera engine selection (AForge vs EmguCV)
        /// Wire this up to your listBoxCameraEngine.SelectedIndexChanged event in Designer
        /// </summary>
        private void listBoxCameraEngine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (StartingUp)
            {
                return;
            }

            ListBox listBox = sender as ListBox;
            if (listBox == null || listBox.SelectedItem == null)
            {
                return;
            }

            string selectedEngine = listBox.SelectedItem.ToString();
            
            CameraEngines.ICameraEngine engine = null;
            
            switch (selectedEngine)
            {
                case "AForge":
                case "AForge.NET":
                    // Switch to AForge engine
                    engine = new CameraEngines.AForgeEngine(DownCamera);
                    DisplayText("Switched to AForge.NET engine", KnownColor.DarkCyan);
                    DisplayText("Using standard image processing functions", KnownColor.DarkCyan);
                    break;
                    
                case "EmguCV":
                case "EmguCV (OpenCV)":
                    // Check if EmguCV is available
                    var testEngine = new CameraEngines.EmguCVEngine(this);
                    if (!testEngine.IsAvailable)
                    {
                        ShowMessageBox(
                            "EmguCV library not found or not functional.\n\n" +
                            "Please ensure Emgu.CV NuGet packages are installed:\n" +
                            "- Emgu.CV\n" +
                            "- Emgu.CV.runtime.windows\n\n" +
                            "The packages should already be installed. Try rebuilding the solution.",
                            "EmguCV Not Available",
                            MessageBoxButtons.OK);
                        
                        // Revert selection to AForge
                        listBox.SelectedIndex = 0;
                        return;
                    }
                    
                    engine = testEngine;
                    DisplayText("Switched to EmguCV (OpenCV) engine", KnownColor.DarkGreen);
                    DisplayText($"Version: {engine.Version}", KnownColor.DarkGreen);
                    DisplayText("Advanced features enabled: Canny, Sobel, Adaptive threshold, etc.", KnownColor.DarkGreen);
                    break;
                    
                default:
                    DisplayText($"Unknown engine: {selectedEngine}", KnownColor.DarkRed);
                    return;
            }
            
            if (engine == null)
            {
                return;
            }
            
            // Apply engine to both cameras
            DownCamera.SetEngine(engine);
            UpCamera.SetEngine(engine);
            
            // Save selection to settings
            Setting.CameraEngine = selectedEngine;
            
            // Reload algorithms for new engine (engine-specific algorithm files)
            LoadVideoAlgorithms(VideoAlgorithms);
            
            // Refresh available functions list if the video algorithms UI is visible
            // This will update the Functions_dataGridView with engine-specific functions
            RefreshAvailableFunctions();
            
            DisplayText($"Both cameras now using {engine.EngineName}", KnownColor.DarkGreen);
        }
        
        /// <summary>
        /// Refreshes the available functions list based on current camera engine
        /// </summary>
        private void RefreshAvailableFunctions()
        {
            // Get current camera engine
            CameraEngines.ICameraEngine engine = DownCamera.CurrentEngine;
            if (engine == null)
            {
                return;
            }
            
            // Get available functions from engine
            var functions = engine.GetAvailableFunctions();
            
            // Update the Functions ComboBox column
            DataGridViewComboBoxColumn comboboxColumn =
                (DataGridViewComboBoxColumn)Functions_dataGridView.Columns[(int)Functions_dataGridViewColumns.FunctionColumn];
            
            comboboxColumn.Items.Clear();
            foreach (string function in functions)
            {
                comboboxColumn.Items.Add(function);
            }
            
            DisplayText($"Loaded {functions.Count} functions from {engine.EngineName}", KnownColor.DarkCyan);
        }
        
        #endregion Camera Engine Selection
        // =====================================================================================
    }

}
