# LitePlacer Architecture Analysis

## Purpose
Document understanding of existing codebase architecture during learning phase.

## Class Hierarchy

### Core Classes
```
FormMain (UI)
  ??? CNC (motion control)
  ?   ??? SerialPort (communication)
  ??? Camera (vision system)
  ?   ??? ProtectedPictureBox2 (display)
  ??? Setting (global configuration)
```

## CNC Communication Layer

### Current Implementation
- **File:** `CNC.cs`
- **Pattern:** Direct serial communication with firmware-specific code
- **Protocol:** TinyG JSON or SKR3/Marlin G-code

### Key Methods (To Document)
- [ ] `CNC_XY_m()` - Movement command flow
- [ ] `CNC_Z_m()` - Z-axis movement
- [ ] `CNC_A_m()` - A-axis (rotation) movement
- [ ] `CNC_XYA_m()` - Combined movement
- [ ] `HomeXY_m()` - Homing sequence
- [ ] `serialPort_DataReceived()` - Response parsing
- [ ] Position tracking mechanism
- [ ] Error handling patterns

### Threading Model
- **UI Thread:** Blocks during commands with `Thread.Sleep()` + `Application.DoEvents()`
- **Serial Thread:** Background thread for `DataReceived` events
- **Synchronization:** Manual flags (`_responseReceived`, `_responseOk`)
- **No async/await:** All blocking synchronous calls

## Coordinate Systems

### Transformations
```
CAD Coordinates (from file)
  ? + JigOffset (PCB0)
  ? + JobOffset
Nominal Coordinates
  ? Homographic Transform (via fiducials)
Machine Coordinates
  ? + NozzleOffset (for placement)
Final Placement Position
```

### Key Settings
- `General_JigOffsetX/Y` - PCB origin in machine coordinates (PCB0)
- `Job_Xoffset/Yoffset` - Per-job adjustment
- `DownCam_NozzleOffsetX/Y` - Camera center to nozzle tip distance

## Vision System

### Camera Classes
- **File:** `Camera.cs` - Camera abstraction and control
- **File:** `ImagesForCameras.cs` - Image processing algorithms
- **Status:** Working, proven - do not modify during firmware refactor

### Nozzle Calibration
- Measures nozzle tip offset at 4 rotation angles (0°, 90°, 180°, 270°)
- Uses circle detection on nozzle outer edge
- Size parameters (Xmin/Xmax) must account for zoom factor
- Example: 1mm nozzle at 3x zoom needs Xmin=1.5, Xmax=4.5

### Vision Parameters
- **Zoom Factor:** Magnifies image, affects detected feature size in pixels
- **Size Filters:** Filter detected features by size (mm)
- **Algorithms:** Circle detection, rectangle detection, component outlines

## Settings Management

### Files
- **TinyGSettings.cs** - TinyG-specific board configuration
- Settings persisted to: `LitePlacer.ApplicationSettings_v2` file

### Key Setting Categories
- Motor parameters (speed, current, microstepping)
- Camera calibration (mm/pixel, offsets)
- Machine limits and homing
- Vision algorithm parameters

## Questions to Answer

### Serial Protocol
- [ ] What exact commands does TinyG need for XY move?
- [ ] How does it report position updates?
- [ ] What's the response format for errors?
- [ ] How are motor settings configured?
- [ ] What's the line ending format?

### State Management
- [ ] How does the app know when a move is complete?
- [ ] What happens if serial connection drops?
- [ ] How are position updates synchronized?
- [ ] What are all the timeout values?

### Configuration
- [ ] Where are settings persisted?
- [ ] How are board-specific settings loaded?
- [ ] Can firmware be switched at runtime?

## Discoveries

### 2026-01-03: PCB0 Coordinate Issue
- **Issue:** Components placed 10mm off in X direction
- **Cause:** `JigX_textBox` (PCB0 X) was 26.20, should be 16.20
- **Root Cause:** PCB origin position in machine coordinates was wrong
- **Workaround Used:** Job Offset X = -10mm
- **Proper Fix:** Correct PCB0 X value or remeasure fiducials properly

### 2026-01-03: Zoom Factor Affects Size Detection
- **Issue:** Nozzle detection only worked with Xmin=2, Xmax=5 instead of expected 0.5-1.5
- **Cause:** Zoom factor magnifies image, making features appear larger in pixels
- **Formula:** Apparent_Size = Physical_Size × ZoomFactor
- **Example:** 1mm nozzle at 3x zoom appears as 3mm in size filter
- **Recommendation:** Calibrate nozzles without zoom for consistency

### 2026-01-03: Threading Model Understanding
- **Pattern:** Synchronous blocking, NOT async/await
- **`_m` suffix:** Means "can show MessageBox" and "blocks UI thread"
- **Serial events:** Run on background thread, require Invoke() for UI updates
- **Application.DoEvents():** Used to prevent UI freeze during blocking waits

---
*Updated as understanding grows*
