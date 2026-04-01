# LitePlacer - Copilot Instructions
See STATUS.md and Plan/STATUS.md for full context.

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

Key method: NozzlePullTapeIndex_m(int tapeRow, double pullDistance) in MainForm.cs
  - Thread-safe UI access via InvokeRequired pattern
  - Works only with "Coordinates For Parts" mode (not camera hole detection)
  - Engagement depth: pickup Z + 2.5mm (fixed)
  - Does NOT increment part counter

UI columns added to Tapes_dataGridView:
  UseNozzlePull_Column  (checkbox, enables per-tape)
  PullDistance_Column   (mm, pull distance)

GotoNextPartByMeasurement_m() in tapes.cs (lines 773-968):
  Thread-safety fix applied — InvokeRequired check marshals all Grid reads to UI thread.
  All Grid data read into locals at function start; rest of function uses only locals.

Known limitations:
  - Requires "Coordinates For Parts" mode
  - Single engagement depth (fixed 2.5mm below pickup Z)
  - No hole engagement verification
  - CP40 nozzles only (0.5-1.5mm diameter)

Pull feature bugs fixed (2025-07-14):
  1. Camera + pull path: after NozzlePullTapeIndex_m, Next_X/Y is now reset to
     FirstX/Y so GotoNextPartByMeasurement_m always looks for hole #1 (which is
     always in the same place after the physical pull).
  2. IncrementTape() now owns the no-increment rule itself: reads UseNozzlePull_Column
     via InvokeRequired/Invoke pattern and returns early if pull is enabled.
     Callers no longer need to guard the call.

Next on pull feature: runtime testing on hardware.

## Status
Plan/STATUS.md        full project history (TinyG, MZ_CNC, SKR3, concurrency, EmguCV) — 1393+ lines
Plan/PULL_INDEXING.md nozzle pull feature complete changelog — 1114 lines
LitePlacer/Docs/STATUS.md        session fix log (editable by tools)
LitePlacer/Docs/copilot-instructions.md  mirror of this file (editable by tools)
Last updated: 2025-07-14
Current state: EmguCV fully wired. Pull feature camera-path bugs fixed.
Next: runtime testing on hardware
