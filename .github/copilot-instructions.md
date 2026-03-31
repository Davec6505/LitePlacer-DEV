# GitHub Copilot Instructions for LitePlacer-DEV

## COMPLETED TASKS

### ? Thread-Safety Fix for GotoNextPartByMeasurement_m() - COMPLETE

**Status:** ? IMPLEMENTED  
**Completed:** 2024-01-XX  
**Commit:** [pending]  
**File:** `LitePlacer/tapes.cs`  
**Function:** `GotoNextPartByMeasurement_m(int TapeNumber, out double HoleX, out double HoleY)`  
**Lines:** 773-968

#### Implementation Summary:

**Problem Solved:** The function was directly accessing UI controls (DataGridView) without thread marshaling, causing potential cross-thread exceptions if called from background threads during placement operations.

**Solution Implemented:**
1. Added `InvokeRequired` check at function start (line 790)
2. If on background thread: Marshal ALL Grid reads to UI thread using `MainForm.Invoke()`
3. If on UI thread: Read Grid directly (preserves existing behavior)
4. All Grid data read into local variables at function start
5. Rest of function uses only local variables (no Grid access after line 872)

**Changes Made:**
- **Lines 784-787:** Declared local variables for all UI data (`verifyHoleWithCamera`, `NextX`, `NextY`, `tapeId`)
- **Lines 790-823:** Added Invoke path - marshals Grid reads to UI thread when on background thread
- **Lines 827-872:** UI thread path - reads Grid directly when already on UI thread
- **Lines 898-903:** Removed duplicate Grid reads (now using local variables from function start)

**Thread-Safe Data Read:**
```csharp
bool verifyHoleWithCamera = true;
double NextX = 0;
double NextY = 0;
string tapeId = "";

if (MainForm.InvokeRequired)
{
    MainForm.Invoke(new Action(() =>
    {
        // Read ALL Grid data on UI thread
        if (Grid.Rows[TapeNumber].Cells["VerifyHoleWithCamera_Column"].Value != null)
            bool.TryParse(..., out verifyHoleWithCamera);
        NextX = double.Parse(Grid.Rows[TapeNumber].Cells["Next_X_Column"]...);
        NextY = double.Parse(Grid.Rows[TapeNumber].Cells["Next_Y_Column"]...);
        tapeId = Grid.Rows[TapeNumber].Cells["Id_Column"].Value.ToString();
    }));
}
else
{
    // Direct read when already on UI thread
}

// Rest of function uses ONLY local variables
```

**Testing Status:**
- ? Build succeeds with no compilation errors
- ?? Runtime testing pending - test with placement operations
- ?? Verify no cross-thread exceptions in debug output
- ?? Test with VerifyHoleWithCamera checked and unchecked

**Benefits Achieved:**
- ? Prevents crashes from cross-thread UI access
- ? Consistent with `NozzlePullTapeIndex_m()` pattern (MainForm.cs lines 12230-12264)
- ? Backwards compatible - works from both UI and background threads
- ? Defensive programming - safe even if threading model changes

**Note:** Function `SetCurrentTapeMeasurement_m()` (line 973) also accesses Grid directly and may need similar treatment if called from background threads. Evaluate separately based on usage patterns.

---

## NEXT INVESTIGATION - CAMERA VISION IMPROVEMENTS

### AForge.NET Camera Algorithms - Robustness & Accuracy Enhancement

**Status:** ?? Investigation Phase  
**Priority:** MEDIUM - Improve vision system reliability  
**Scope:** Nozzle calibration and hole detection algorithms  
**Libraries:** AForge.NET, AForge.Imaging, AForge.Vision

#### Investigation Goals:

**Primary Objectives:**
1. **Improve nozzle calibration accuracy** - More precise nozzle position detection across rotation angles
2. **Enhance hole detection robustness** - Better tape hole recognition under varying lighting/conditions
3. **Reduce false positives/negatives** - More reliable feature detection with fewer errors
4. **Optimize algorithm parameters** - Better default values and auto-tuning capabilities

#### Current Implementation Analysis Needed:

**Files to Review:**
1. **`LitePlacer/Camera.cs`** - Core camera measurement and algorithm execution
2. **`LitePlacer/VideoAlgorithms*.cs`** - Algorithm implementations (Circle, Rectangle, Component detection)
3. **`LitePlacer/Shapes.cs`** - Shape detection algorithms
4. **`LitePlacer/ImagesForCameras.cs`** - Image processing pipeline

**Key Functions to Analyze:**
- `Camera.Measure()` - Main measurement function
- Circle detection algorithms (nozzle calibration)
- Hole detection algorithms (tape sprocket holes)
- Component outline detection
- Edge detection and filtering

#### Areas for Investigation:

**1. Nozzle Calibration (Circle Detection):**
- Current AForge circle detection parameters and thresholds
- Lighting compensation techniques
- Multi-scale detection approaches
- Circle fitting accuracy improvements
- Rotation angle correlation accuracy

**2. Hole Detection (Tape Sprocket Holes):**
- Current hole finding algorithm robustness
- Edge detection sensitivity
- Size filtering effectiveness
- Position accuracy under different tape conditions
- Handling of partial/damaged holes

**3. Algorithm Enhancements to Research:**
- **Adaptive thresholding** - Better handling of varying lighting conditions
- **Multi-pass detection** - Coarse-to-fine approach for better accuracy
- **Template matching** - Pre-trained patterns for common features
- **Machine learning integration** - Neural network-based detection (future consideration)
- **Noise filtering** - Better pre-processing to reduce false detections
- **Sub-pixel accuracy** - Interpolation for finer position resolution

**4. Performance Considerations:**
- Algorithm execution time vs accuracy tradeoff
- Real-time processing requirements
- Memory usage optimization
- GPU acceleration possibilities (AForge.NET limitations)

#### Investigation Tasks:

**Phase 1: Current State Analysis**
1. ? Document current AForge.NET algorithms in use
2. ? Identify current pain points (false detections, missed features, inaccurate measurements)
3. ? Benchmark current accuracy and performance metrics
4. ? Review user-reported vision issues in GitHub issues/discussions

**Phase 2: Algorithm Research**
1. ? Research AForge.NET best practices and advanced features
2. ? Investigate alternative algorithms available in AForge.NET
3. ? Review academic papers on industrial vision systems for pick-and-place
4. ? Analyze similar open-source projects (OpenPnP, etc.) for inspiration

**Phase 3: Prototyping**
1. ? Implement test harness for comparing algorithm variations
2. ? Prototype improved circle detection for nozzle calibration
3. ? Prototype enhanced hole detection for tape indexing
4. ? Collect test images from various machine setups for validation

**Phase 4: Integration**
1. ? Integrate best-performing algorithms into codebase
2. ? Add user-configurable parameters for fine-tuning
3. ? Update UI with new algorithm options
4. ? Document changes and create migration guide

#### Resources:

**AForge.NET Documentation:**
- Main Site: http://www.aforgenet.com/
- Framework Docs: http://www.aforgenet.com/framework/docs/
- Vision Library: http://www.aforgenet.com/framework/docs/html/d087503e-77da-dc47-0e33-788275035a90.htm
- Imaging Library: http://www.aforgenet.com/framework/docs/html/d7196718-6d1f-a0e8-d26a-7ab13e4d8c85.htm

**Related Projects:**
- OpenPnP: https://github.com/openpnp/openpnp (Java-based, but good algorithm ideas)
- OpenCV: https://opencv.org/ (C++/Python, reference for advanced techniques)

**Academic Resources:**
- Circle Detection: Hough Transform variants, RANSAC circle fitting
- Edge Detection: Canny, Sobel, Laplacian of Gaussian
- Sub-pixel accuracy: Moment-based, interpolation methods

#### Expected Outcomes:

**Success Metrics:**
- ? 20% reduction in nozzle calibration errors
- ? 30% reduction in hole detection failures
- ? Faster convergence in iterative detection (fewer retries)
- ? Consistent performance across different lighting conditions
- ? Improved user confidence in vision system

**Deliverables:**
- ?? Technical report on current vs improved algorithms
- ?? Updated vision processing code with enhanced algorithms
- ?? User documentation for new parameters and settings
- ?? Test suite with reference images for validation
- ?? Performance benchmarks (before/after comparison)

#### Notes:

- AForge.NET is mature but no longer actively developed (last update ~2013)
- Consider eventual migration to OpenCV-based solution (future roadmap)
- Maintain backwards compatibility with existing tape/nozzle calibrations
- Allow users to switch between "classic" and "enhanced" algorithms
- Collect telemetry/feedback from users during beta testing

---

## PENDING TASK - HIGH PRIORITY

### Thread-Safety Fix for GotoNextPartByMeasurement_m()

**Status:** Identified but not yet implemented  
**Priority:** HIGH - Prevents potential cross-thread UI access crashes  
**File:** `LitePlacer/tapes.cs`  
**Function:** `GotoNextPartByMeasurement_m(int TapeNumber, out double HoleX, out double HoleY)`  
**Lines:** 773-904

#### Problem Description:

The `GotoNextPartByMeasurement_m()` function directly accesses UI controls (DataGridView) without thread marshaling:

1. **Line 787-789:** Reads `VerifyHoleWithCamera_Column` checkbox value
2. **Line 816-826:** Reads `Next_X_Column` and `Next_Y_Column` values
3. **Line 819, 829:** Reads `Id_Column` for error messages

**Issue:** If placement operations run on background threads, these direct Grid accesses will cause cross-thread exceptions: `"Control accessed from thread other than the thread it was created on"`

#### Current Implementation Pattern:

The function currently follows this unsafe pattern:
```csharp
// UNSAFE: Direct UI access
bool verifyHoleWithCamera = true;
if (Grid.Rows[TapeNumber].Cells["VerifyHoleWithCamera_Column"].Value != null)
{
    bool.TryParse(Grid.Rows[TapeNumber].Cells["VerifyHoleWithCamera_Column"].Value.ToString(), out verifyHoleWithCamera);
}

// More unsafe Grid accesses at lines 816-826...
double NextX = 0;
if (!double.TryParse(Grid.Rows[TapeNumber].Cells["Next_X_Column"].Value.ToString().Replace(',', '.'), out NextX))
{
    // Error handling with more Grid access
}
```

#### Required Solution (Option 3 - Proper Thread-Safe Implementation):

Follow the pattern established in `MainForm.NozzlePullTapeIndex_m()` (lines 12230-12264):

**Step 1:** Add thread detection check at function start
**Step 2:** If on background thread, use `MainForm.Invoke()` to marshal ALL Grid reads to UI thread
**Step 3:** Read all necessary UI data into local variables at the beginning
**Step 4:** Use local variables for remainder of function (no Grid access after marshaling)

#### Implementation Template:

```csharp
public bool GotoNextPartByMeasurement_m(int TapeNumber, out double HoleX, out double HoleY)
{
    HoleX = 0;
    HoleY = 0;
    double A = 0.0;

    // STEP 1: Declare variables for UI data
    bool verifyHoleWithCamera = true;
    double NextX = 0;
    double NextY = 0;
    string tapeId = "";
    
    // STEP 2: Check if we need thread marshaling
    if (MainForm.InvokeRequired)
    {
        // STEP 3: Marshal to UI thread to read all Grid data
        bool success = false;
        MainForm.Invoke(new Action(() =>
        {
            try
            {
                // Read VerifyHoleWithCamera checkbox
                if (Grid.Rows[TapeNumber].Cells["VerifyHoleWithCamera_Column"].Value != null)
                {
                    bool.TryParse(Grid.Rows[TapeNumber].Cells["VerifyHoleWithCamera_Column"].Value.ToString(), 
                        out verifyHoleWithCamera);
                }
                
                // Read Next_X and Next_Y
                NextX = double.Parse(Grid.Rows[TapeNumber].Cells["Next_X_Column"].Value.ToString().Replace(',', '.'));
                NextY = double.Parse(Grid.Rows[TapeNumber].Cells["Next_Y_Column"].Value.ToString().Replace(',', '.'));
                
                // Read tape ID for error messages
                tapeId = Grid.Rows[TapeNumber].Cells["Id_Column"].Value.ToString();
                
                success = true;
            }
            catch
            {
                success = false;
            }
        }));
        
        if (!success)
        {
            MainForm.DisplayText("*** Failed to read tape data", KnownColor.DarkRed);
            return false;
        }
    }
    else
    {
        // STEP 4: Called from UI thread - read directly (keep existing code)
        if (Grid.Rows[TapeNumber].Cells["VerifyHoleWithCamera_Column"].Value != null)
        {
            bool.TryParse(Grid.Rows[TapeNumber].Cells["VerifyHoleWithCamera_Column"].Value.ToString(), 
                out verifyHoleWithCamera);
        }
        
        if (!double.TryParse(Grid.Rows[TapeNumber].Cells["Next_X_Column"].Value.ToString().Replace(',', '.'), out NextX))
        {
            tapeId = Grid.Rows[TapeNumber].Cells["Id_Column"].Value.ToString();
            MainForm.ShowMessageBox("Bad data at Tape " + tapeId + ", Next X", "Tape data error", MessageBoxButtons.OK);
            return false;
        }
        
        if (!double.TryParse(Grid.Rows[TapeNumber].Cells["Next_Y_Column"].Value.ToString().Replace(',', '.'), out NextY))
        {
            tapeId = Grid.Rows[TapeNumber].Cells["Id_Column"].Value.ToString();
            MainForm.ShowMessageBox("Bad data at Tape " + tapeId + ", Next Y", "Tape data error", MessageBoxButtons.OK);
            return false;
        }
    }
    
    // STEP 5: Rest of function uses LOCAL VARIABLES only (verifyHoleWithCamera, NextX, NextY, tapeId)
    // No more direct Grid access from this point forward
    
    // ... (existing logic continues with local variables) ...
}
```

#### Files to Modify:

1. **`LitePlacer/tapes.cs`** - Add thread marshaling to `GotoNextPartByMeasurement_m()`
2. **`PULL_INDEXING.md`** - Document the thread-safety fix as a new change

#### Testing Required After Implementation:

1. ? Build succeeds with no compilation errors
2. ? Test placement operation from UI thread (should work as before)
3. ? Test placement operation that might use background thread
4. ? Verify no cross-thread exceptions in debug output
5. ? Test with VerifyHoleWithCamera checked and unchecked

#### Benefits of This Fix:

- ? **Prevents crashes** from cross-thread UI access
- ? **Consistent with NozzlePullTapeIndex_m()** pattern already in codebase
- ? **Backwards compatible** - works from both UI and background threads
- ? **Defensive programming** - safe even if threading model changes in future

#### Notes:

- The TapesClass doesn't have direct access to `InvokeRequired`, so we use `MainForm.InvokeRequired`
- Must read ALL Grid data in the Invoke block (no partial reads)
- Error messages should use the stored `tapeId` string, not read from Grid again
- The Dictionary `VerifiedHolePositions` is thread-safe for reads (it's only written from synchronized code paths)

---

## Project Documentation

### Required Reading
Before making any changes to the codebase, **ALWAYS** consult:

1. **STATUS.md** - Development status, change log, and lessons learned
   - Contains comprehensive documentation of all changes made
   - Documents known issues and their root causes
   - Lists attempted solutions and their outcomes
   - **UPDATE THIS FILE** whenever you make code changes

2. **Plan/*.md** - Project planning and architecture documents
   - System architecture overview
   - Feature specifications
   - Design decisions

## Making Changes

### Change Documentation Protocol

When making ANY code change, you **MUST**:

1. **Read STATUS.md first** to understand:
   - What has already been tried
   - Why previous approaches failed
   - Current state of the codebase

2. **Update STATUS.md** immediately after making changes:
   - Add entry to "Changes Made" section
   - Document: File, Line numbers, Function name
   - Explain: What changed and WHY
   - Add entry to "Change Log" table
   - Update "Testing Status" if applicable

3. **Update the "Last Updated" date** at bottom of STATUS.md

### Change Log Entry Format

```markdown
#### Change N: [Brief Description]
**Location:** `FileName.cs`, [MethodName()], lines X-Y
**Commit:** [commit hash or "Uncommitted"]
**Why:** [Reason for change]

**What Changed:**
- Bullet list of specific changes
- Be detailed and specific

**Code Sample:**
```csharp
// Show key code changes
```

**Status:** ? Complete / ? In Progress / ? Failed
```

## Z-Axis Probing Context

**CRITICAL UNDERSTANDING:** 
- Z=0 is HOME (nozzle UP, away from PCB)
- Z=positive is DOWN (toward PCB)  
- Probing DOWN = moving to POSITIVE Z (e.g., G0 Z79)
- Z-max switch is at PCB surface (positive Z position)
- Z-min switch is at home (Z=0 position)

**Always verify coordinate system understanding before modifying Z-axis code!**

## TinyG JSON Commands

**CRITICAL:** TinyG uses **NON-STANDARD JSON** format with **COMMAS** instead of colons!

? **CORRECT for TinyG:** `{\"zsn\",3}`  
? **WRONG for TinyG:** `{\"zsn\":3}` (this is standard JSON but TinyG does NOT accept it)

**Important Notes:**
- This is NOT standard JSON syntax, but TinyG firmware accepts it
- **DO NOT "fix" these to use colons** - it will break TinyG communication
- All existing code uses comma syntax: `{\"param\",value}`
- Keep this format consistent throughout the codebase

**Examples of correct TinyG commands:**
```csharp
Write_m("{\"zsn\",3}", 150);      // Set Z-min switch mode
Write_m("{\"zsx\",2}", 150);      // Set Z-max switch mode
Write_m("{\"zzb\",2.0}", 150);    // Set zero backoff
Write_m("{\"st\",0}", 150);       // Set switch type
```

## MZ_CNC (GRBL) Commands

**CRITICAL:** MZ_CNC uses **STANDARD GRBL v1.1** protocol!

? **CORRECT for GRBL:** `$110=5000.0` (Settings)  
? **CORRECT for GRBL:** `G0 X10 Y20` (Movement)  
? **CORRECT for GRBL:** `G38.2 Z10` (Probing)  
? **CORRECT for GRBL:** `$H` (Homing)  

**Important Notes:**
- GRBL uses `$xxx=value` for settings (not JSON!)
- Settings use `CultureInfo.InvariantCulture` for decimal formatting (always `.` not `,`)
- `$27` = Homing pull-off distance in mm (standard GRBL v1.1)
- `$26` = Homing seek rate in mm/min (applies to all axes)
- `$30` = Max spindle speed (RPM) - **NOT related to homing!**

**Examples of correct GRBL commands:**
```csharp
Write_m("$110=5000.0");           // Set X max rate (mm/min)
Write_m("$120=500.0");            // Set X acceleration (mm/sec^2)
Write_m("$26=1000.0");            // Set homing seek rate (mm/min)
Write_m("$27=2.0");               // Set homing pull-off distance (mm)
Write_m("$30=24000.0");           // Set max spindle speed (RPM)
Write_m("$H");                    // Start homing cycle
Write_m("G38.2 Z10 F100");       // Probe down to Z10 at 100mm/min
Write_m("M7");                    // Vacuum ON (mist coolant)
Write_m("M9");                    // Vacuum/Pump OFF (all coolant)
```

**GRBL Response Parsing:**
- Status reports: `<Idle|MPos:0.000,0.000,79.000,0.000|...>`
- Probe results: `[PRB:10.123,20.456,5.789,0.000:1]`
- Settings echo: `$110=5000.000` (confirms new value)
- Acknowledgment: `ok` (command accepted)

**GRBL v1.1 Homing Settings (Standard):**
- `$25` = Homing seek rate (mm/min)
- `$27` = Homing pull-off distance (mm)
- `$30` = Max spindle speed (RPM) - **NOT related to homing!**

**Note:** There is NO PIC32MZ firmware quirk. Your firmware is standard GRBL v1.1.
Use `$27` for homing pull-off in millimeters.

**MZ_CNC Settings UI:**
- Event handlers use **KeyPress** with **Enter key** to apply changes
- All event handlers send `$xxx=value` commands via `Cnc.MZ_CNC.Write_m()`
- Handlers validate input before sending to controller
- Success/failure logged with color-coded messages

**MZ_CNC Settings Files:**
- **Separate settings files** for each controller type (TinyG, SKR3, MZ_CNC)
- **Board Settings Save/Load** buttons repurposed for all controllers
- MZ_CNC uses `MZ_CNC_Settings.json` file format (like TinyG pattern)
- Settings include GRBL-writable values + AppSettings-only values
- Files stored in application directory (same as TinyG settings)

**Settings File Format:**
```
MZ_CNC  \n\r
{
  "XSpeed": 5000.0,
  "XAccel": 500.0,
  ...
}
```

## Code Style

### Comments
- Add comments ONLY when necessary to explain WHY, not WHAT
- Match existing comment style in the file
- Reference STATUS.md entry numbers in complex changes

### Logging
- Use color-coded logging for different severity:
  - `KnownColor.DarkRed` - Errors
  - `KnownColor.DarkOrange` - Warnings
  - `KnownColor.DarkGreen` - Success
  - `KnownColor.DarkCyan` - Informational

### Error Handling
- Always check return values from TinyG commands
- Provide clear error messages to user
- Log failures with context

## Testing Requirements

Before marking a change as complete:

1. ? Build succeeds with no errors
2. ? Code follows existing patterns in file
3. ? Change is documented in STATUS.md
4. ? If hardware-related, describe test procedure in STATUS.md
5. ? Update "Testing Status" section

## Git Workflow

### Current State
- Project is in detached HEAD state
- Need to create proper feature branch before committing

### Recommended Workflow
```bash
git checkout -b feature/descriptive-name
git add [files]
git commit -m "Descriptive commit message

Refs: STATUS.md changes section X"
```

### Commit Message Format
```
Short summary (50 chars or less)

Detailed explanation of what changed and why.
Reference STATUS.md change entry number.

Refs: STATUS.md Change #N
```

## File Organization

### Core Files
- `LitePlacer/TinyGControl.cs` - TinyG communication and control
- `LitePlacer/CNC.cs` - Generic CNC abstraction
- `LitePlacer/MainForm.cs` - UI and application logic
- `STATUS.md` - **Change documentation (ALWAYS UPDATE!)**

### When Modifying TinyGControl.cs
- Understand that this handles ALL TinyG communication
- Changes here affect machine safety - test carefully
- Document expected TinyG responses
- Always include timeout handling

## Safety Considerations

### Z-Axis Changes
- **ALWAYS** ensure limit switches are enabled after operations
- **NEVER** disable both Z-switches simultaneously during normal operation
- Test switch response before running full operations
- Add safety checks for `zzb` value (must be >0 for homing to work)

### Switch Configuration
Before changing switch settings:
1. Document current state
2. Explain why change is needed
3. Test on actual hardware if possible
4. Provide rollback procedure

## Questions to Ask Before Changing Code

1. **Has this been tried before?** ? Check STATUS.md "Solutions Attempted"
2. **Why did it fail last time?** ? Read failure analysis in STATUS.md
3. **What's the root cause?** ? Check "Root Cause Analysis" section
4. **Will this break existing functionality?** ? Review "Key Learnings"
5. **How will I test this?** ? Add to "Testing Status" section

## Emergency Procedures

If changes cause machine malfunction:

1. **Document in STATUS.md immediately:**
   - What was attempted
   - What went wrong
   - Current machine state
   - Recovery steps taken

2. **Update "Solutions Attempted" with ? Failed status**

3. **Add to "Known Issues" section**

4. **Consider reverting to last known good state:**
   ```bash
   git checkout [last-good-commit]
   ```

## AI Assistant Guidelines

### When Providing Solutions

1. **Read STATUS.md first** - Check what's already been tried
2. **Explain your reasoning** - Reference sections from STATUS.md
3. **Show what's different** - How is this different from failed attempts?
4. **Document in STATUS.md** - Add new entry for your change
5. **Update change log** - Add to table at bottom

### When User Reports Issue

1. **Check STATUS.md** - Is this a known issue?
2. **Review previous attempts** - What's been tried already?
3. **Analyze root cause** - Is it in "Root Cause Analysis"?
4. **Propose solution** - Explain how it differs from previous attempts
5. **Update documentation** - Add to STATUS.md

## Resources

### TinyG Documentation
- Configuration: https://github.com/synthetos/TinyG/wiki/TinyG-Configuration
- Switch Setup: https://github.com/synthetos/TinyG/wiki/TinyG-Configuration#switches
- Homing: https://github.com/synthetos/TinyG/wiki/TinyG-Homing
- Probing: https://github.com/synthetos/TinyG/wiki/TinyG-Probing

### LitePlacer Resources
- Main Repo: https://github.com/Davec6505/LitePlacer-DEV
- STATUS.md: [Root of repository]

---

**Remember:** STATUS.md is the single source of truth for what has been tried, what worked, and what didn't. **ALWAYS update it when making changes!**

---

*Last Updated: 2024-01-XX*
