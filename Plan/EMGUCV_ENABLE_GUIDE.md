# How to Enable EmguCV Advanced Functions

## Problem
The EmguCV advanced functions don't appear in the Video Processing dropdown because:
1. Cameras default to AForge.NET engine
2. The UI control to switch engines (`listBoxCameraEngin`) exists as an event handler but the actual control was never added to the form

## Temporary Solution (Quick Test)

### Method 1: Call from Immediate Window (Debug Mode)
1. Set a breakpoint anywhere in MainForm.cs
2. When breakpoint hits, open **Immediate Window** (Ctrl+Alt+I or Debug ? Windows ? Immediate)
3. Type: `SwitchCameraEngine("EmguCV")`
4. Press Enter
5. Continue execution (F5)
6. Go to **Video Processing** tab - EmguCV functions should now appear!

### Method 2: Call from Code (Permanent for Testing)
Add this line in `Form1_Load()` after cameras are initialized (around line 409):

```csharp
StartCameras();

// TEMP: Switch to EmguCV for testing
SwitchCameraEngine("EmguCV");

UpCamZoomFactor_textBox.Text = Setting.UpCam_Zoomfactor.ToString("0.0", CultureInfo.InvariantCulture);
```

### Method 3: Modify Settings File
1. Close LitePlacer
2. Open `LitePlacer.Appsettings` in a text editor
3. Find: `"CameraEngine": "AForge"`
4. Change to: `"CameraEngine": "EmguCV"`
5. Save and restart LitePlacer
6. **IMPORTANT:** Add this line to `Form1_Load()` after line 439:

```csharp
ConnectToCnc(Setting.CNC_SerialPort);  // This can raise error condition, needing the form up

// Apply saved camera engine setting
if (Setting.CameraEngine != null && Setting.CameraEngine.Contains("EmguCV"))
{
    SwitchCameraEngine("EmguCV");
}

MotorPower_timer.Enabled = true;
```

## Permanent Solution (Add UI Control)

### Option A: Add Button to Video Processing Tab
The easiest permanent solution is to add a simple button:

1. Open MainForm in Designer
2. Go to **Video Processing** tab (Algorithms_tabPage)
3. Add a Button named `btnSwitchEngine`
4. Set properties:
   - Text: "Switch to EmguCV"
   - Location: Near top of tab, visible spot
5. Double-click button to create event handler
6. Add code:

```csharp
private void btnSwitchEngine_Click(object sender, EventArgs e)
{
    if (DownCamera.CurrentEngine.EngineName.Contains("AForge"))
    {
        SwitchCameraEngine("EmguCV");
        btnSwitchEngine.Text = "Switch to AForge";
        btnSwitchEngine.BackColor = Color.LightGreen;
    }
    else
    {
        SwitchCameraEngine("AForge");
        btnSwitchEngine.Text = "Switch to EmguCV";
        btnSwitchEngine.BackColor = SystemColors.Control;
    }
}
```

7. Initialize button state in `Algorithms_tabPage_Begin()`:

```csharp
// Update engine button to reflect current state
if (btnSwitchEngine != null)
{
    if (cam?.CurrentEngine?.EngineName.Contains("EmguCV") == true)
    {
        btnSwitchEngine.Text = "Switch to AForge";
        btnSwitchEngine.BackColor = Color.LightGreen;
    }
    else
    {
        btnSwitchEngine.Text = "Switch to EmguCV";
        btnSwitchEngine.BackColor = SystemColors.Control;
    }
}
```

### Option B: Add ListBox (Original Design Intent)
The original code expected a ListBox control. To complete this:

1. Open MainForm in Designer
2. Navigate to appropriate tab/panel
3. Add ListBox control named `listBoxCameraEngin`
4. The existing event handler (line 15037) will automatically work
5. Initialize in `Form1_Load()` or `Algorithms_tabPage_Begin()`:

```csharp
listBoxCameraEngin.Items.Clear();
listBoxCameraEngin.Items.Add("AForge.NET");
listBoxCameraEngin.Items.Add("EmguCV (OpenCV)");

// Select based on saved setting
if (Setting.CameraEngine?.Contains("EmguCV") == true)
{
    listBoxCameraEngin.SelectedIndex = 1;
}
else
{
    listBoxCameraEngin.SelectedIndex = 0;
}
```

## Verification

After switching to EmguCV, you should see:
- Message in display window: "=== Switching to EmguCV (OpenCV) engine ==="
- In Video Processing tab dropdown, new functions appear:
  - "--- EmguCV Advanced Features ---"
  - Canny edge detection
  - Sobel edge detection
  - Adaptive threshold
  - Bilateral filter
  - CLAHE
  - ... and 11 more advanced functions

## API Reference

### SwitchCameraEngine(string engineName)
**Location:** MainForm.cs, line 15040  
**Access:** Public method

**Parameters:**
- `engineName` - Engine to switch to ("AForge", "EmguCV", "OpenCV")

**Behavior:**
- Switches both DownCamera and UpCamera to specified engine
- Saves selection to Settings.CameraEngine
- Refreshes Video Processing function list
- Displays status messages

**Example Usage:**
```csharp
// Switch to EmguCV
SwitchCameraEngine("EmguCV");

// Switch back to AForge
SwitchCameraEngine("AForge");
```

## Current Function Availability

### AForge.NET Functions (15 total):
- Threshold, Invert, Meas. zoom, Histogram
- Grayscale, Edge detect, Noise reduction, Erosion
- Kill color, Keep color, Blur, Gaussian blur
- Hough circles, Filter Features by Size, Jog before measurement

### EmguCV Additional Functions (16 total):
- **Canny edge detection** ? (implemented)
- Sobel edge detection
- Laplacian edge detection
- Adaptive threshold
- Bilateral filter
- CLAHE
- Morphological gradient
- Morphological top hat
- Morphological black hat
- Hough circles (sub-pixel)
- Harris corners
- Shi-Tomasi corners
- FAST feature detection
- Template matching
- Contour detection
- Watershed segmentation

**Note:** EmguCV also includes all AForge-compatible functions (Grayscale ?, Threshold, Blur, etc.)

## Troubleshooting

### "EmguCV library not available"
- Check that Emgu.CV NuGet packages are installed
- Verify native DLLs are in bin/Debug:
  - `cvextern.dll` (~42 MB, x86 version)
  - `opencv_videoio_ffmpeg453.dll` (~19 MB, x86 version)
- See `EMGUCV_SETUP.md` for setup instructions

### EmguCV functions not appearing after switch
- Ensure you're on the **Video Processing** tab
- Check that `RefreshFunctionList()` was called
- Look for "Loaded X functions from EmguCV" message in display

### Functions appear but crash when used
- Only 2 processors are implemented so far (Grayscale, CannyEdge)
- Other functions need processor classes created
- See `EMGUCV_PARAMETERIZATION_SUMMARY.md` for implementation status

---

*Last Updated: 2025-01-XX*
