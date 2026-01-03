# Learning Journal - Questions & Discoveries

## Format
Each entry follows this template:
```
### [Date] - Topic
**Question:** What I'm trying to understand
**Discovery:** What I learned
**Code Reference:** File and method names
**Notes:** Additional context
```

---

## 2026-01-03: Initial Setup

### PCB Coordinate Offset Issue
**Question:** Why do I need -10mm Job Offset for components to align?

**Discovery:** 
- PCB0 (Jig Offset) X coordinate was wrong: 26.20 instead of 16.20
- This value represents where PCB origin (0,0) is in machine coordinates
- Every component coordinate gets this offset added before placement
- Using Job Offset = -10mm was compensating for misconfigured PCB0

**Code Reference:**
- Setting: `Setting.General_JigOffsetX`
- UI Control: `JigX_textBox` on Setup Cameras tab (label: "PCB Zero")
- Applied in: `BuildMachineCoordinateData_m()` method

**Fix:** Set `JigX_textBox` to correct value (16.20) and remove Job Offset workaround

---

### Threading Model Understanding
**Question:** What does the `_m` suffix mean on methods?

**Discovery:**
- `_m` = "with message" (can show MessageBox dialogs)
- Does NOT mean async/await
- Methods block the UI thread synchronously
- Uses old pattern: `Thread.Sleep()` + `Application.DoEvents()`
- Serial port events run on background thread
- No modern async/await pattern used in codebase

**Code Pattern:**
```csharp
public bool CNC_XY_m(double X, double Y)  // _m suffix
{
    // Can call ShowMessageBox - safe because called from UI thread
    // Blocks UI thread until move completes
    // Uses Application.DoEvents() to prevent freeze
}
```

**Notes:** 
- This is a .NET Framework 4.8 WinForms pattern
- Simple to understand but can cause re-entrancy issues
- Not suitable for modern async applications

---

### Form Layout Issues
**Question:** Why doesn't the form resize properly?

**Discovery:**
- Controls have fixed sizes and positions in designer
- No Anchor or Dock properties set properly
- TabControl should be `Dock.Fill` but isn't
- DataGridViews should be anchored to all sides
- Some manual positioning code in `FormMain_Shown` fights layout

**Code Reference:**
- `MainForm.Designer.cs` - Control initialization
- `MainForm.cs` - Form event handlers

**Status:** Not a priority for firmware refactor - cosmetic issue only

---

### Nozzle Calibration Size Parameters
**Question:** Why does nozzle detection only work with Xmin=2, Xmax=5 instead of expected 0.5-1.5?

**Discovery:**
- **Zoom factor magnifies the image**, making detected features appear larger in pixels
- Size parameters in UI are in **millimeters** (physical size)
- When image is zoomed, a 1mm physical nozzle appears as 3mm in the size filter at 3x zoom
- **Formula:** `Apparent_Size_in_Filter = Physical_Size × ZoomFactor`
- Vision algorithms work in pixel space, then convert to mm using calibration

**Code Reference:**
- Size filtering in: `ImagesForCameras.cs` (vision processing)
- Zoom factor from: `UpCamZoomFactor_textBox` control
- Calibration in: mm/pixel settings

**Solution:**
- Calibrate nozzles **without zoom** (zoom = 1.0) for predictable behavior
- OR: Multiply size parameters by zoom factor manually
- OR: Detect nozzle outer edge (larger, more reliable) instead of bore

**Notes:**
- This explains why outer circle (Xmin=2, Xmax=5) worked but inner bore didn't
- At 3x zoom: 1mm nozzle body ? 3mm apparent, 0.5mm bore ? 1.5mm apparent
- Lighting and focus also critical for consistent detection

---

## Questions Still Open

### CNC Communication
- [ ] Exact TinyG command format for motor configuration
- [ ] How does position polling work vs event-driven updates?
- [ ] What are all the timeout values used and why those values?
- [ ] How does slack compensation work (what's it compensating for)?
- [ ] What's the actual serial protocol flow for a simple XY move?

### Vision System
- [ ] What's the fiducial measurement algorithm exactly?
- [ ] How does the homographic transform get calculated?
- [ ] What vision algorithms are available and when to use each?
- [ ] How are component rotation angles detected?

### Pick and Place Workflow
- [ ] Complete sequence for placing one component?
- [ ] How are tape feeders managed?
- [ ] What's the nozzle change sequence?
- [ ] How is Z-height managed (pickup vs placement)?

### Settings
- [ ] Complete settings file format?
- [ ] What settings are board-specific vs application-specific?
- [ ] How to migrate settings between firmware types?

---

## Code Areas to Study (Priority Order)

### High Priority (Core Functionality)
1. [ ] `CNC.cs` - Main motion control class
   - `CNC_XY_m()`, `CNC_Z_m()`, `CNC_A_m()` methods
   - `serialPort_DataReceived()` event handler
   - Position tracking variables
   - Timeout and error handling

2. [ ] `MainForm.cs` - UI event handlers
   - Button click handlers for movement
   - How settings are read from UI
   - How position is displayed

3. [ ] Coordinate transform methods
   - `BuildMachineCoordinateData_m()`
   - Fiducial measurement
   - Homography calculation

### Medium Priority (Understanding Integration)
4. [ ] `TinyGSettings.cs` - How firmware settings work
5. [ ] `Camera.cs` - Vision system integration
6. [ ] Settings persistence (`Setting` class)

### Lower Priority (Nice to Understand)
7. [ ] Tape feeder management
8. [ ] Job file format
9. [ ] CAD file parsing
10. [ ] Nozzle change automation

---

## Useful Code Snippets (From Exploration)

### Example: How to Add Logging for Study
```csharp
// Add to CNC.cs or MainForm.cs
private void DebugLog(string message)
{
    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    // Also appears in Visual Studio Output window
}
```

---

## Architecture Insights (As Discovered)

### Singleton Pattern Usage
- `Setting` appears to be a static/singleton
- `Cnc` is likely a single global instance
- Cameras are instance-based but referenced globally

### State Management
- Position state tracked in `CNC.cs`
- UI state tracked in `MainForm.cs`
- Settings state in `Setting` class
- No formal state machine pattern visible (yet)

### Error Handling Pattern
- Methods return `bool` for success/failure
- MessageBox used for user-facing errors
- Debug logging for internal errors (possibly)

---

## Next Steps in Learning

### Immediate (This Week)
- [ ] Trace a simple "Home XY" button click through entire code path
- [ ] Document actual TinyG commands sent by capturing serial traffic
- [ ] Map all public methods in CNC.cs that UI uses
- [ ] Understand how settings are loaded at startup

### Short Term (Next 2 Weeks)
- [ ] Complete protocol comparison document
- [ ] Test all basic machine functions with hardware
- [ ] Document quirks and edge cases
- [ ] Create firmware comparison matrix

### Before Refactor
- [ ] Complete understanding of coordinate transforms
- [ ] Test PIC33MZ firmware independently with simple terminal
- [ ] Create protocol test harness
- [ ] Document all custom commands needed

---

*Add new discoveries as you learn*
*Keep this updated - it's your learning journal*
