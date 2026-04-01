# LitePlacer - Copilot Instructions
See Docs/STATUS.md and Plan/STATUS.md for full context.

## Project
C# 7.3, .NET Framework 4.8, WinForms. Branch: feature/nozzle-pull-tape-indexing.
Remote: https://github.com/Davec6505/LitePlacer-DEV

## Hard Rules
- NO async/await — uses Thread.Sleep and Application.DoEvents()
- NO DI, interface refactoring, .NET Core, or WPF suggestions unless explicitly asked
- DO NOT modify stable working code unless directly related to the task
- ALWAYS read Plan/STATUS.md before making changes

## Conventions
- Methods ending _m: may show MessageBox, must run on UI thread
- DisplayText(text, KnownColor.X): colour-coded log output
- Setting.*: global settings singleton. Cnc.*: global CNC controller
- StartingUp bool: guards in event handlers return early while true
  Set ~line 150 of FormMain_Shown, cleared ~line 439

## Camera Engine Architecture
Two engines, separate pipeline storage:

  AForge.NET -> DisplayFunctions (List<AForgeFunction>), MeasurementFunctions
  EmguCV     -> _displayEnginePipeline (List<IProcessingFunction>), _enginePipeline

CRITICAL: When EmguCV is active, DisplayFunctions and MeasurementFunctions are ALWAYS EMPTY.
Any code checking for active processing must test BOTH lists.

Engine detection pattern used throughout Camera.cs:
  if (_currentEngine != null && _currentEngine.EngineName != "AForge.NET")
      // EmguCV path
  else
      // AForge path

ShowUnprocessed logic in Video_NewFrame:
  1. ShowUnprocessed = true
  2. if DisplayFunctions.Count != 0 -> ShowUnprocessed = false        (AForge)
  3. else if not AForge AND _displayEnginePipeline.Count != 0 -> false (EmguCV)
  Raw frame shown only if ShowUnprocessed is still true after both checks.

Display mode -> pipeline state:
  No video processing -> ClearDisplayFunctionsList() -> both pipelines empty
  Show processing     -> BuildDisplayFunctionsList() -> active pipeline built, ShowProcessing=true
  Show results        -> BuildDisplayFunctionsList() -> active pipeline built, ShowProcessing=false

ClearDisplayFunctionsList() clears BOTH DisplayFunctions AND _displayEnginePipeline.

Startup engine restore (intentional design):
  InitVideoAlgorithmsUI() sets SelectedIndex from Setting.CameraEngine, then calls
  SwitchCameraEngine() DIRECTLY — bypasses StartingUp guard in
  listBoxCameraEngin_SelectedIndexChanged (MainForm.cs ~line 15124).
  UpdateVideoProcessing() guards VideoAlgorithms==null because it runs before
  VideoAlgorithms = new VideoAlgorithmsCollection() two lines later.

## Key Files
  LitePlacer/Camera.cs                         capture, display + measurement pipelines
  LitePlacer/VideoAlgorithmsUI.cs              Video Algorithms tab, engine switching
  LitePlacer/MainForm.cs                       SwitchCameraEngine (~line 15064)
  LitePlacer/CameraEngines/ICameraEngine.cs    engine interface
  LitePlacer/CameraEngines/AForgeEngine.cs     AForge.NET engine
  LitePlacer/CameraEngines/EmguCVEngine.cs     EmguCV: HoughCircles + contour measurement
  LitePlacer/CameraEngines/EmguCVFunctions.cs  25 static EmguCV processing functions

## EmguCV 4.5.3 API Notes
  - GoodFeaturesToTrack NOT available as CvInvoke static — use CornerHarris
  - CvInvoke.CornerMinEigenVal does NOT exist — use CornerHarris
  - CvInvoke.MinMaxLoc needs ref Point minLoc, ref Point maxLoc — cannot omit
  - ShiTomasiCorners uses CornerHarris response (equivalent visualisation)

## Coordinate Systems
  PCB0 Jig Offset: Setting.General_JigOffsetX/Y
  Job Offset:      Setting.Job_Xoffset/Y
  Nozzle Offset:   Setting.DownCam_NozzleOffsetX/Y
  Z=0 = HOME nozzle up; Z positive = DOWN toward PCB

## Protocol Notes
  TinyG: non-standard JSON — commas not colons: {"zsn",3} NOT {"zsn":3}. Never change this.
  GRBL:  standard plaintext: $110=5000.0, G0 X10, G38.2 Z10 F100

## Nozzle Pull Tape Indexing Feature
Branch feature/nozzle-pull-tape-indexing. Full detail in Plan/PULL_INDEXING.md.
The feature uses the nozzle to physically pull tape by engaging sprocket holes.
Mode: UseNozzlePull_Column checked, CoordinatesForParts DISABLED (camera hole measurement active).

UI columns added to Tapes_dataGridView:
  UseNozzlePull_Column  (checkbox, enables per-tape nozzle pull)
  PullDistance_Column   (mm, how far to pull per cycle, default 4.0mm)

Pull sequence per component cycle (verified from code 2025-07-14):
  1. PickUpPartWithHoleMeasurement_m() called — CoordinatesForParts is false so stays in this path
  2. useNozzlePull read from UseNozzlePull_Column (on UI thread, direct read — called from UI)
  3. NozzlePullTapeIndex_m() called with pullDistance from PullDistance_Column:
       a. Reads Next_X/Y (current known hole position) and PickupZ from grid (InvokeRequired safe)
       b. CNC_XYA_m: nozzle moves XY to hole position (Z stays high)
       c. Cnc.Execute_Z: nozzle descends to pickupZ + 2.5mm (engage into 1.5mm sprocket hole)
       d. Cnc.Execute_XYA: nozzle pulls tape pullDistance mm in tape orientation direction (300mm/min)
       e. ZGuardOff(), Cnc.Z: nozzle lifts 10mm above engaged position (Z-guard stays off)
       f. Returns true — Z-guard left off for subsequent pickup move
  4. Next_X/Y reset to FirstX/Y (hole always returns to same position after physical pull)
  5. GotoNextPartByMeasurement_m(): camera moves to FirstX/Y, measures exact hole #1 position
  6. Part position calculated from measured hole position + tape offsets
  7. PickUpThis_m(): nozzle moves to part XY, descends to pickupZ, picks up
  8. ZGuardOn() re-enabled after pickup completes
  9. IncrementTape(): detects UseNozzlePull via InvokeRequired, returns early — no counter change

Key method details: NozzlePullTapeIndex_m(int tapeRow, double pullDistance)
  - Engagement depth: pickupZ + 2.5mm (fixed, into 1.5mm EIA-481 sprocket hole)
  - Pull speed: 300mm/min XY
  - Lift clearance: 10mm above engaged Z
  - Z-guard disabled during pull, re-enabled by caller after pickup
  - Thread-safe: InvokeRequired pattern for all grid reads
  - Pull direction from Orientation_Column: +X, -X, +Y, -Y

Known limitations:
  - Fixed 2.5mm engagement depth (no adjustment for tape thickness variation)
  - No hole engagement verification (no sensor confirmation)
  - CP40 nozzles only (0.5-1.5mm tip diameter to fit 1.5mm sprocket hole)
  - Requires PickupZ to be taught before pull will work

Next on pull feature: runtime testing on hardware.

## Status
  Plan/STATUS.md        full project history (TinyG, MZ_CNC, SKR3, concurrency, EmguCV) — 1393+ lines
  Plan/PULL_INDEXING.md nozzle pull feature complete changelog — 1114 lines
  Docs/STATUS.md        session fix summaries (this folder)
  Last updated: 2025-07-14
  Current state: EmguCV fully wired. Pull feature camera-path bugs fixed.
  Next: runtime testing on hardware
