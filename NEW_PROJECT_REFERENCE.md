# LitePlacer — New Project Reference Document

> **Purpose:** This document is a complete technical analysis of the LitePlacer codebase.  
> It is intended to be copied into the new project repository as the primary architectural reference.  
> It covers what the system does, how it is currently structured, every flaw in the current approach,  
> and the target architecture for the new application.  
> **AForge.NET is explicitly excluded** from the new project. EmguCV (OpenCV) is the only vision library.  
> **ModbusTCP and ModbusRTU** are planned for a later development phase.

---

## Table of Contents

1. [What LitePlacer Is](#1-what-liteplacer-is)
2. [Hardware the Software Controls](#2-hardware-the-software-controls)
3. [Domain Map — The Five Concerns](#3-domain-map--the-five-concerns)
4. [Domain 1 — Motion (CNC)](#4-domain-1--motion-cnc)
5. [Domain 2 — Imagery (Camera & Vision)](#5-domain-2--imagery-camera--vision)
6. [Domain 3 — Streaming (Serial Protocol Layer)](#6-domain-3--streaming-serial-protocol-layer)
7. [Domain 4 — Tape & Part Management](#7-domain-4--tape--part-management)
8. [Domain 5 — Settings & Persistence](#8-domain-5--settings--persistence)
9. [The God Object — MainForm](#9-the-god-object--mainform)
10. [Coordinate System Reference](#10-coordinate-system-reference)
11. [Nozzle Calibration System](#11-nozzle-calibration-system)
12. [Protocol Reference — Current Boards](#12-protocol-reference--current-boards)
13. [Flaws in the Current Architecture](#13-flaws-in-the-current-architecture)
14. [New Project — Target Architecture](#14-new-project--target-architecture)
15. [New Project — Domain Boundaries](#15-new-project--domain-boundaries)
16. [New Project — Technology Choices](#16-new-project--technology-choices)
17. [New Project — What to Carry Forward](#17-new-project--what-to-carry-forward)
18. [New Project — What to Leave Behind](#18-new-project--what-to-leave-behind)
19. [Future Protocol Additions — Modbus](#19-future-protocol-additions--modbus)
20. [Suggested Folder Structure for New Project](#20-suggested-folder-structure-for-new-project)

---

## 1. What LitePlacer Is

LitePlacer is a desktop application that drives a **pick-and-place machine** — a small CNC robot that picks
electronic components from tape reels and places them onto a printed circuit board (PCB) with sub-millimetre
accuracy.

The operator loads a **job file** (a list of component placements derived from PCB design software), sets up
**tape reels** (rows of components in carrier tape with sprocket holes), and the machine works through the job
automatically: locating each component with a downward-facing camera, picking it with a vacuum nozzle,
optionally inspecting the component rotation on an upward-facing camera, and placing it at the target PCB
coordinate.

The application must simultaneously:
- Stream G-code or proprietary JSON commands to a CNC controller over serial.
- Capture and process live video from two USB cameras in real time.
- Maintain accurate knowledge of machine position including axis squareness correction.
- Allow the operator to intervene, jog the machine, and calibrate at any time.

---

## 2. Hardware the Software Controls

### CNC Controllers (three currently supported)

| Board | Protocol | Notes |
|---|---|---|
| **TinyG** | Non-standard JSON over serial 115200 baud | Commas not colons: `{"zsn",3}` NOT `{"zsn":3}`. Sends status reports as JSON. EEPROM-backed settings. |
| **SKR3 EZ** (BigTreeTech) | grblHAL over serial 115200 baud | Standard G-code + grblHAL extensions. Position reports on request. |
| **MZ_CNC** (PIC32MZ custom) | GRBL v1.1 over serial 115200 baud | 200 MHz PIC32MZ2048EFH100. Compatible with standard GRBL plaintext. |

### Cameras (two)

| Camera | Role | Direction |
|---|---|---|
| **Down camera** | Locates tape sprocket holes, fiducial marks on PCB | Faces down at the work area |
| **Up camera** | Inspects component rotation after pickup | Faces up at the nozzle underside |

Both cameras are USB DirectShow devices. Resolution is configurable; display can be scaled independently of
measurement resolution for performance on high-resolution cameras.

### Mechanical Axes

| Axis | Movement | Notes |
|---|---|---|
| X | Left/Right across PCB | Square correction applied |
| Y | Front/Back across PCB | Reference for square correction |
| Z | Up/Down (nozzle) | Z=0 is home (nozzle fully up). Positive Z = DOWN toward PCB |
| A | Rotation of nozzle | Used for component angle correction during placement |

---

## 3. Domain Map — The Five Concerns

```
???????????????????????????????????????????????????????????????
?                    APPLICATION LAYER                        ?
?              (Job execution, operator workflow)             ?
???????????????????????????????????????????????????????????????
?    MOTION     ?    IMAGERY    ?      TAPE MANAGEMENT        ?
?  Coordinates  ?  Camera feed  ?   Component sequencing      ?
?  Homing       ?  Vision algo  ?   Part counting             ?
?  Jog          ?  Measurement  ?   Nozzle pull indexing      ?
???????????????????????????????????????????????????????????????
?                    STREAMING LAYER                          ?
?        Serial protocol abstraction (TinyG/GRBL/MZ)         ?
???????????????????????????????????????????????????????????????
?                  SETTINGS / PERSISTENCE                     ?
?              JSON file, per-board config                    ?
???????????????????????????????????????????????????????????????
```

---

## 4. Domain 1 — Motion (CNC)

### Key class: `CNC` (`Classes/CNC.cs`)

`CNC` is the motion facade. It owns the concept of current position and routes all move commands to the
active board controller (`TinyGclass`, `SKR3class`, or `MZ_CNCControl`).

### Position Model

The machine is mechanically imperfect — the X and Y axes are never perfectly square to each other.
A **square correction** value is measured by the operator using the camera and stored as
`Setting.CNC_SquareCorrection`.

```
TrueX  = the value sent to the controller
CurrentX = TrueX - CurrentY * SquareCorrection     (what the user sees and uses)
CurrentY = as reported
```

All motion code works in `CurrentX/Y`. The correction is applied transparently inside `CNC.SetCurrentX/Y()`.

### Axes

- `CurrentX`, `CurrentY`, `CurrentZ`, `CurrentA` — user-visible corrected coordinates.
- `TrueX` — raw controller coordinate (X only; Y/Z/A need no correction).
- Position is updated via `SetCurrentX/Y/Z/A()` which also trigger UI updates.

### Move Types (all boards implement these)

| Method | Behaviour |
|---|---|
| `GoToXY` | Absolute XY move, waits for completion |
| `GoToZ` | Absolute Z move |
| `GoToA` | Absolute A (rotation) move |
| `Jog` | Continuous jog at specified speed |
| `CancelJog` | Stop jog immediately |
| `HomeXY`, `HomeZ` | Homing sequence |
| `SetPosition` | Set work coordinate offset |
| `ProbZ` | Z probing (grbl `G38.2`) |

### Speed Control

Three independent slow-speed modes: `SlowXY`, `SlowZ`, `SlowA` each with their own speed value.
These are engaged during sensitive operations (pickup, place, probing).

### Slack Compensation

Optional backlash compensation on X/Y and A axes. Distance is configurable.
Applied inside individual board controllers on direction changes.

### Error State

`Cnc.ErrorState` — when true, all write calls are discarded with a log message.
Raised by `RaiseError()`. Cleared on reconnect or explicit user action.
This is a simple boolean flag — no structured error type, no recovery strategy.

---

## 5. Domain 2 — Imagery (Camera & Vision)

### Key classes: `Camera` (`Classes/Camera.cs`), `EmguCVEngine`, `EmguCVFunctions`

### Camera Class Responsibilities

- Owns the `VideoCaptureDevice` (DirectShow frame source).
- Manages two separate processing pipelines (display and measurement).
- Applies visual overlays: crosshair, grid, dashed cross, arrow, sidemark ticks, snapshot ghost.
- Delivers processed frames to a `ProtectedPictureBox2` (thread-safe `PictureBox` subclass).
- Exposes measurement results (`X`, `Y`, `A`) to calling code.

### Camera Engine Architecture

Two engines exist. In the new project only **EmguCV** will be used.

```
ICameraEngine
    ??? AForgeEngine       [DO NOT CARRY FORWARD]
    ??? EmguCVEngine       [ONLY engine in new project]
```

`ICameraEngine` interface:
- `EngineName` — string identifier
- `IsAvailable` — checks native DLL presence
- `GetAvailableFunctions()` — list of processing function names
- `BuildProcessingPipeline(definitions)` — converts UI function list to executable pipeline
- `Measure(image, pipeline, parameters, mmPerPixel, out X, out Y, out A)` — full measurement

### Processing Pipeline

A pipeline is an ordered list of `IProcessingFunction` objects. Each function takes a `Bitmap` in and
returns a processed `Bitmap` out. Parameters carried per function:

```
ParameterInt    — threshold value, blur kernel size, circle radius range, etc.
ParameterDouble — floating point variant
ParameterDoubleA/B/C — three additional doubles (size filters, etc.)
R, G, B         — colour parameters for kill-colour / keep-colour operations
```

### EmguCV Functions (25 static functions in `EmguCVFunctions.cs`)

| Function | Purpose |
|---|---|
| Grayscale | BGR to single channel |
| Threshold | Binary threshold |
| Invert | Bitwise NOT |
| Blur | Box blur |
| Gaussian blur | Gaussian kernel blur |
| Erosion | Morphological erosion |
| Dilation | Morphological dilation |
| Noise reduction | Median filter |
| Edge detect | Canny edge detection |
| Hough circles | HoughCircles — finds circles (sprocket holes, fiducials) |
| Kill color | Remove a specific colour range |
| Keep color | Keep only a specific colour range |
| Meas. zoom | Crop to centre region for measurement |
| Histogram | Histogram equalisation |
| Filter Features by Size | Remove contours outside size range |
| Find component (outline) | Contour-based component centre detection |
| Find component (pads) | Pad-based component centre detection |
| ShiTomasiCorners | Corner detection via CornerHarris |
| DrawCross | Draw crosshair overlay |
| DrawGrid | Draw measurement grid |
| DrawArrow | Draw direction arrow |
| DrawSidemarks | Draw calibration tick marks on edges |
| DrawBox | Draw scale reference box |
| DrawDashedCross | Dashed crosshair (centre visible) |
| Snapshot | Ghost overlay of a reference frame |

### Measurement Parameters (`MeasurementParametersClass`)

Stored per algorithm. Key fields:
- `Xmin`, `Xmax`, `Ymin`, `Ymax` — search window in mm
- `FeatureSize_min`, `FeatureSize_max` — acceptable feature size range
- `Circles`, `Rectangles`, `Components` — what to search for
- `CalibrationMark` — if true, looking for a fixed reference not a part

### Two Cameras, Two Roles

| Camera | `Camera` instance | Used for |
|---|---|---|
| Down camera | `DownCamera` | Tape hole finding, fiducial alignment, nozzle offset calibration |
| Up camera | `UpCamera` | Component rotation inspection after pickup |

### Display vs Measurement Pipeline

The same algorithm definition drives both pipelines but with different intent:
- **Display pipeline** (`_displayEnginePipeline`): runs on every frame, output shown to operator.
  `ShowProcessing=true` shows intermediate result; `ShowProcessing=false` shows overlay on raw frame.
- **Measurement pipeline** (`_enginePipeline`): runs on demand, returns X/Y/A results.

`ClearDisplayFunctionsList()` empties both pipelines simultaneously.

### mm-per-pixel Calibration

Each camera has a calibrated `XmmPerPixel` and `YmmPerPixel` value. This is set by the operator by
moving to a known distance and recording the pixel count. All measurement output is in mm.

---

## 6. Domain 3 — Streaming (Serial Protocol Layer)

### Key classes: `SerialComm`, `TinyGclass`, `SKR3class`, `MZ_CNCControl`

### SerialComm (`Classes/SerialComm.cs`)

A thin wrapper around `System.IO.Ports.SerialPort`.

- Fixed 115200 baud, 8N1, no hardware handshake, DTR+RTS enabled.
- `Write(string)` — appends `Setting.Serial_EndCharacters` (configurable `\n` or `\r\n`) and sends.
- `DataReceived` event handler accumulates characters into `RxString` buffer, fires `Cnc.LineReceived()`
  on each complete line (terminated by `\n`).
- Close is done on a separate thread to avoid hangs with some USB-serial drivers.
- All logging goes directly to `MainForm.DisplayText()` — tight coupling.

### TinyG Protocol (`Classes/TinyGControl.cs`)

TinyG speaks a **non-standard JSON dialect**:
```
Command format:   {"key",value}     (comma, not colon — this is intentional and must never change)
Response format:  {"r":{"key":value},"f":[...]}   (standard JSON responses)
Status reports:   {"sr":{"posx":12.3,"posy":4.5,...}}
```

Key behaviours:
- `Write_m()` — sends a command and blocks on `ManualResetEvent` until `LineReceived()` signals completion.
- `LineReceived()` — parses JSON response, updates `Cnc` position fields, signals `ReadyEvent`.
- `JustConnected()` — on connect, reads all EEPROM settings, force-restores Z-switch settings, enables motors.
- Settings are persisted in TinyG EEPROM — the application reads them back on connect and uses them as ground truth.
- Motor power command: `{"me":""}`.
- Status reports arrive unsolicited and are parsed to update position display.

### SKR3 / grblHAL Protocol (`Classes/SKR3Control.cs`)

Standard G-code + grblHAL extensions over serial.
```
Move:      G0 X10 Y20 F5000
Probe:     G38.2 Z-10 F100
Home:      $H
Response:  ok     (on success)
           error:N  (on failure)
           <Idle|MPos:x,y,z,a>  (status report)
```

Key differences from TinyG:
- Does NOT send position reports automatically — app must poll or infer position.
- `Write_m()` blocks until `ok` received, using `lock(writeLock)` + polling loop with `Thread.Sleep(2)`.
- `GetResponse_m()` — write and capture the response line (used for `$I`, `$G`, `?` etc.).
- Settings sent at connect time from `MySettings` (not read back from board EEPROM).
- `Application.DoEvents()` removed from the polling loop (concurrency fix) — UI update is explicit.

### MZ_CNC Protocol (`Classes/MZ_CNCControl.cs`)

GRBL v1.1 on a PIC32MZ microcontroller. Largely identical to SKR3 protocol path.
```
Settings:  $110=5000.0  (steps per mm, max rate, etc.)
Move:      G0 X10 Y20
Probe:     G38.2 Z-10 F100
Response:  ok / error:N
```

Additional features:
- `LoadGRBLSettings()` — reads `$$` output and parses all `$NNN=value` lines into a typed `GRBLSettings` object.
- Firmware identity check via `$I` on connect.
- `GRBLSettings` class stores ~30 parameters: steps/mm, max rates, acceleration, travel limits, homing config.

### Shared Blocking Write Pattern (All Three Boards)

All three board classes implement the same fundamental pattern:

```
1. Check ErrorState — discard if true
2. Acquire writeLock
3. Send command via Com.Write()
4. Poll / wait for acknowledgement (ManualResetEvent or lock-based flag)
5. Timeout after N milliseconds ? show MessageBox, return false
6. Release lock, return success/failure
```

The timeout is configurable via `CNC.RegularMoveTimeout` (default 10 seconds).

---

## 7. Domain 4 — Tape & Part Management

### Key class: `TapesClass` (`Classes/Tapes.cs`)

### What a Tape Is

A tape is a reel of SMD components in a carrier tape with sprocket holes at 4mm pitch.
The operator sets up one row per tape in a `DataGridView` with columns:

| Column | Purpose |
|---|---|
| `Id_Column` | Unique tape identifier (referenced from job file) |
| `FirstX_Column`, `FirstY_Column` | Machine coordinate of hole #1 in this tape |
| `NextPart_Column` | Current part counter (incremented after each pickup) |
| `Next_X_Column`, `Next_Y_Column` | Current expected hole position |
| `Pitch_Column` | Distance between parts (mm) — typically 4mm or 8mm |
| `PickupZ_Column` | Z depth for vacuum pickup |
| `PlaceZ_Column` | Z depth for component placement |
| `PartOffset_X/Y_Column` | Offset from hole centre to part centre |
| `RotationOffset_Column` | Component rotation offset (degrees) |
| `UseNozzlePull_Column` | Enable nozzle-pull tape indexing for this tape |
| `PullDistance_Column` | Pull distance in mm for nozzle-pull indexing |

**Critical design flaw:** The tape data model IS the DataGridView. There is no separate `TapeModel` class.
All reads and writes go directly to grid cells, requiring `InvokeRequired`/`Invoke` marshalling from
background threads.

### Part Location Modes

**Mode 1 — Coordinates For Parts:**  
Part positions are pre-calculated from `FirstX/Y` + `Pitch` * `PartNumber`.
No camera measurement. Fast. Used with nozzle-pull indexing.

**Mode 2 — Camera Hole Detection:**  
The down camera locates each sprocket hole visually using a HoughCircles algorithm.
More accurate but slower. Not compatible with nozzle-pull indexing.

### Key Methods

`GotoNextPartByMeasurement_m(tapeRow)` — the main dispatch method:
1. Reads all grid data into locals (thread-safety fix applied 2025-07-14).
2. If `UseNozzlePull` is set, calls `NozzlePullTapeIndex_m()` then resets `Next_X/Y` to `FirstX/Y`.
3. Otherwise, moves to expected hole position and optionally verifies with camera.
4. Returns the final part pickup coordinates.

`IncrementTape(tapeRow)` — advances `NextPart_Column` and recalculates `Next_X/Y`.
If `UseNozzlePull_Column` is true, returns early without incrementing (pull owns the advance).

`PrepareForFastPlacement_m()` — pre-measures first and last hole positions for a batch job,
calculates `FastXstep/Ystep` for interpolated positioning without per-part camera measurement.

### Nozzle Pull Tape Indexing

Implemented in `MainForm.NozzlePullTapeIndex_m(tapeRow, pullDistance)`:
1. Move nozzle to tape first-hole XY.
2. Lower nozzle to `PickupZ + 2.5mm` (engagement depth — fixed, not configurable).
3. Move XY by `pullDistance` in tape direction (pulls tape via sprocket hole engagement).
4. Raise nozzle.
5. Does NOT increment part counter. Does NOT verify hole engagement.

After pull, `Next_X/Y` is reset to `FirstX/Y` because the physical pull has moved the tape so that
hole #1 is back at the original position.

**Known limitations:**
- 2.5mm engagement depth is fixed — no calibration, no feedback.
- No verification that the nozzle actually engaged the hole.
- CP40 nozzle geometry only (0.5–1.5mm tip diameter).
- Only works in Coordinates For Parts mode.

### Verified Hole Position Cache

`VerifiedHolePositions` (`Dictionary<int, (double X, double Y)>`) — when camera verification
is disabled, previously confirmed hole positions are cached per tape row to avoid repeated
camera measurements. Cleared on tape reset.

---

## 8. Domain 5 — Settings & Persistence

### Key class: `MySettings` (`Classes/AppSettings.cs`)

### Format

Settings are serialised to JSON using **Newtonsoft.Json** with `Formatting.Indented`.
Default file path: `Application.StartupPath + "LitePlacerSettings.json"`.

The class is a single flat POCO with ~200+ properties. No nesting, no sections, no versioning.

### Coverage

| Category | Example Properties |
|---|---|
| CNC general | `CNC_SerialPort`, `Controlboard`, `CNC_SquareCorrection`, `CNC_RegularMoveTimeout` |
| SKR3 specific | `SKR3_Xspeed`, `SKR3_XHomingSpeed`, `SKR3_XMicroStep`, `SKR3_XCurrent` (×4 axes) |
| MZ_CNC specific | `MZCNC_XCurrent`, `MZCNC_XMicroStep`, `MZCNC_XDeg18` (×4 axes) |
| Camera | `Cam_ShowPixels`, `Cameras_KeepActive`, `CameraEngine` |
| Coordinate offsets | `General_JigOffsetX/Y`, `Job_Xoffset/Y`, `DownCam_NozzleOffsetX/Y` |
| Nozzles | `Nozzles_count`, `Nozzles_current`, `Placement_OmitNozzleCalibration` |
| General | `General_SaveFilesAtClosing`, `Serial_EndCharacters` |

### Separate Settings Files

Some board-specific settings are in separate JSON files:
- `TinyGSettings.json` — TinyG EEPROM mirror (read back on connect, user-editable)
- `MZ_CNC_Settings.json` — MZ_CNC settings mirror
- `SKR3Settings.json` — SKR3 settings

Nozzle calibration data is in a separate file (`NozzleCalibration.json`).

### The Critical Flaw

`MySettings` has a direct reference to `MainForm`:
```csharp
public FormMain MainForm;
```
This means the settings object cannot exist independently of the UI. It cannot be loaded in a
background thread, tested in isolation, or used in any non-UI context.

---

## 9. The God Object — MainForm

### Size

`MainForm.cs` is a **partial class** split across several files:
- `MainForm.cs` — core, startup, placement loop, fiducial alignment, CNC jog
- `MainForm.Designer.cs` — auto-generated WinForms designer code
- `MainForm1.Designer.cs` — overflow designer file
- `VideoAlgorithmsUI.cs` — camera engine UI, algorithm editor, pipeline management

Estimated total: **15,000–20,000+ lines**.

### What MainForm Owns (everything)

- All UI event handlers.
- The placement loop (pick, inspect, place, increment — all here).
- All camera measurement calls and result interpretation.
- Fiducial alignment calculations.
- All jog handlers (keyboard, numpad, mouse wheel).
- Board connection / disconnection logic.
- All settings load/save triggers.
- The `StartingUp` guard flag — events return early during startup data load.
- `DisplayText()` — the application-wide logging method.
- `SwitchCameraEngine()` — at ~line 15064.
- `NozzlePullTapeIndex_m()`.
- `Update_Xposition()`, `Update_Yposition()`, etc. — position display refresh.

### StartingUp Flag

`StartingUp = true` is set at the top of `Form1_Load`. It is cleared (~line 439) after all
startup data has been loaded and all UI controls have been populated.

Many event handlers check `if (StartingUp) return;` to avoid acting on programmatic UI
changes during startup. This is a global mutable boolean with no scope — it is possible to
accidentally clear it too early and cause spurious event firing, or leave it set and miss
legitimate events.

---

## 10. Coordinate System Reference

```
Machine work area viewed from above:

  (0,0) ??????????????????? +X (right)
    ?
    ?       PCB placed here
    ?       with jig offset applied
    ?
    ?
   +Y (forward, toward operator)

Z axis:
  Z = 0     ? Nozzle fully UP (home position)
  Z positive ? Nozzle moving DOWN toward PCB
  Z at PickupZ ? Nozzle touching part on tape
  Z at PlaceZ  ? Nozzle touching target pad on PCB
```

### Offset Chain (applied in order)

```
Raw machine coordinate
  + JigOffset (Setting.General_JigOffsetX/Y)       ? physical jig position on table
  + JobOffset (Setting.Job_Xoffset/Y)               ? PCB-specific origin offset
  + NozzleOffset (Setting.DownCam_NozzleOffsetX/Y)  ? nozzle tip vs camera centre
  = Final placement coordinate
```

### Square Correction

```
TrueX = CurrentX + CurrentY * SquareCorrection
```

Positive correction means the machine drifts in +X for each unit of +Y travel.
The correction is subtracted when converting back to display coordinates.

---

## 11. Nozzle Calibration System

### Key class: `NozzleCalibrationClass` (`Classes/Nozzle.cs`)

Supports multiple nozzles (count set by `Setting.Nozzles_count`).

Each nozzle has a `CalibrationPointsList` — a set of `(Angle, X_offset, Y_offset)` measurements
taken at multiple rotation angles. At any given rotation the nozzle tip is not perfectly centred
on the rotation axis; the offset is measured by placing a known dot under the up camera and
rotating while measuring displacement.

`GetPositionCorrection_m(angle, out X, out Y)` — interpolates the calibration table to return
the X/Y correction to apply at the requested rotation angle.

Calibration data is serialised to `NozzleCalibration.json` and loaded at startup.

---

## 12. Protocol Reference — Current Boards

### TinyG — Complete Command Reference

```
Motor power on:         {"me":""}
Motor power off:        {"md":""}
Status report request:  ?
All settings dump:      $$
Factory reset:          {"defa":1}

Move (absolute):        G0 X{x} Y{y}  or  G1 X{x} Y{y} F{speed}
Home X/Y:               $H  (homes all enabled axes)
Home Z:                 special sequence with backoff

Switch settings:
  {"st",0}   ? switch type: normally open
  {"zzb",2.0}? Z zero backoff 2.0mm
  {"zlb",10} ? Z latch backoff 10mm
  {"zsn",3}  ? Z min switch mode: homing + limit
  {"zsx",2}  ? Z max switch mode: limit only

Read setting:           {"zzb":""}
Set setting:            {"zzb",2.0}

Status report (unsolicited):
  {"sr":{"posx":12.34,"posy":56.78,"posz":0.0,"posa":0.0,"stat":5}}

stat values: 0=init, 1=ready, 2=alarm, 3=stop, 4=end, 5=run, 6=hold, 9=homing
```

**Non-standard JSON rule: TinyG uses commas, not colons, as the key-value separator in commands.
This is a firmware quirk and must never be "corrected".**

### GRBL v1.1 (SKR3 and MZ_CNC) — Complete Command Reference

```
Unlock alarm:     $X
Home:             $H
Status:           ?  ? <Idle|MPos:x,y,z,a|WPos:x,y,z,a>
All settings:     $$  ? $NNN=value lines, terminated by ok
Build info:       $I
G-code parser:    $G

Move absolute:    G90 G0 X{x} Y{y} F{speed}
Move relative:    G91 G0 X{dx} Y{dy}
Set units mm:     G21
Set units inch:   G20
XY plane:         G17
Probe:            G38.2 Z{depth} F{speed}
  Response if hit:   [PRB:x,y,z:1]  ok
  Response if miss:  [PRB:x,y,z:0]  ok  (or ALARM:4)

Set work coord:   G10 L20 P1 X0 Y0 Z0

Setting format:   $NNN=value
  $100=320.0  ? X steps/mm
  $101=320.0  ? Y steps/mm
  $102=800.0  ? Z steps/mm
  $103=...    ? A steps/mm
  $110=5000.0 ? X max rate mm/min
  $111=5000.0 ? Y max rate mm/min
  $112=500.0  ? Z max rate mm/min
  $120=1000.0 ? X acceleration mm/s²
  $130=400.0  ? X max travel mm
  $22=1       ? homing enable
  $23=3       ? homing dir invert
  $5=0        ? limit pins invert
  $21=1       ? soft limits enable
```

---

## 13. Flaws in the Current Architecture

This is the most important section. Every item here is a lesson that must be designed away in the new project.

---

### FLAW 1 — MainForm Is a God Object (Most Critical)

**Problem:** `MainForm` is a 15,000–20,000 line partial class that owns everything.
Business logic (placement algorithm, fiducial math, tape sequencing), UI event handling,
hardware communication orchestration, and application state all live in the same class.

**Consequence:** Nothing can be tested without the full WinForms environment.
No logic can be reused. Every change risks breaking unrelated behaviour.
Adding a feature requires understanding the entire form.

**New approach:** `MainForm` (or the equivalent shell window) must be a thin shell.
It handles window events and delegates immediately to domain services.
No business logic in the UI class.

---

### FLAW 2 — Tape Data Model Is the DataGridView

**Problem:** `TapesClass` holds no data itself. All tape state lives in `DataGridView` cell values.
Reading tape data from a background thread requires `InvokeRequired`/`Invoke` marshalling for
every single field access.

**Consequence:** Thread safety is extremely fragile — any new method that reads tape data must
remember to marshal or it silently reads stale/wrong values. The 2025-07-14 fix that reads all
grid data into locals at the start of `GotoNextPartByMeasurement_m` is a workaround, not a solution.

**New approach:** Define a `TapeModel` record/class. The grid is a view of that model.
The model is the source of truth. Grid updates are one-way: model ? grid.

---

### FLAW 3 — Settings Object Coupled to MainForm

**Problem:** `MySettings` holds a `public FormMain MainForm` reference.
This means settings cannot be instantiated, loaded, or tested outside the UI context.

**Consequence:** No settings unit test is possible. Settings cannot be validated before the form
is shown. Settings cannot be managed by a background service.

**New approach:** Settings is a pure POCO with no UI references. A separate `SettingsService`
owns load/save. Validation is a static method that takes a settings object and returns errors.

---

### FLAW 4 — No Async — Thread.Sleep + Application.DoEvents() Everywhere

**Problem:** The original codebase uses `Thread.Sleep()` loops with `Application.DoEvents()`
to simulate waiting without blocking the UI. `Application.DoEvents()` pumps the Windows message
queue inline, which means any UI event (including another button click) can re-enter the current
call stack mid-operation.

**Consequences:**
- Re-entrancy bugs: the user can click a button while a movement is in progress.
- `Application.DoEvents()` was removed from some loops as a concurrency fix (SKR3) but still
  present in others, creating inconsistency.
- Timeouts are implemented as counted `Thread.Sleep(2)` loops — imprecise and CPU-wasteful.
- Background threads are created ad-hoc with `new Thread(() => ...)` with no lifecycle management.

**New approach:** `async`/`await` throughout. Serial reads/writes are async.
Vision measurement is async. The placement loop is a cancellable `Task`.
`CancellationToken` is passed through the call chain.
UI remains responsive without `Application.DoEvents()`.

---

### FLAW 5 — No Interface Abstraction on Board Controllers

**Problem:** `CNC.cs` is a manual switch statement — every method has:
```csharp
if (Setting.Controlboard == SKR3) SKR3.Move(...)
else if (Setting.Controlboard == TinyG) TinyG.Move(...)
else if (Setting.Controlboard == MZ_CNC) MZ_CNC.Move(...)
else DisplayText("unknown board")
```

**Consequence:** Adding a new board (e.g. Modbus-based) requires editing `CNC.cs` in dozens of places.
Every new method must be manually replicated. It is impossible to unit-test motion logic independently
of the physical board.

**New approach:** Define `IMotionController` interface with the full move contract.
Each board is one implementation. `CNC` (or `MotionService`) holds one `IMotionController` reference.
Adding a new board = adding one class that implements the interface. Zero changes to existing code.

---

### FLAW 6 — SerialComm Is Tightly Coupled to MainForm

**Problem:** `SerialComm.Write()` calls `MainForm.DisplayText()` directly.
`DataReceived` calls `Cnc.LineReceived()` directly.
The serial layer has no abstraction — it cannot be replaced with a simulator, a TCP bridge,
or a test double.

**Consequence:** Hardware-in-the-loop is required for any test of communication logic.
Debugging protocol issues requires a physical board attached.

**New approach:** Define `ISerialTransport` interface. Real implementation wraps `SerialPort`.
Test implementation is an in-memory loopback. Logging is via an injected `ILogger`, not a direct
call to `MainForm.DisplayText()`.

---

### FLAW 7 — Error Handling Is a Single Boolean Flag

**Problem:** `Cnc.ErrorState` is a boolean. When it is true, all commands are silently discarded
with a log message. There is no structured error type, no error code, no recovery strategy,
no way for callers to distinguish "board in alarm" from "serial port disconnected" from
"command timed out".

**Consequence:** Operators see cryptic discarded-command messages in the log with no explanation.
Automatic recovery is impossible because the error reason is unknown.

**New approach:** A structured `MotionError` type with `ErrorCode`, `Message`, `RecoveryHint`.
Result types (`Result<T>` or `OneOf<Success, MotionError>`) propagated through the motion stack.
The UI maps error codes to user-friendly messages with suggested actions.

---

### FLAW 8 — Vision Pipeline Is Straddled Across Two Separate Designs

**Problem:** The AForge pipeline and the EmguCV pipeline co-exist in `Camera.cs` with
parallel lists, parallel build/clear methods, and string-comparison engine detection
(`if EngineName != "AForge.NET"`). The code that checks "is processing active" must
test BOTH lists because one will always be empty depending on which engine is active.

**Consequence:** Every piece of code that touches the pipeline must understand both
engine models. Adding a third engine would require a third set of lists and a third
branch in every check.

**New approach:** Only EmguCV. Single pipeline list. No string comparison for engine detection.
The `ICameraEngine` interface is the only abstraction needed.

---

### FLAW 9 — Shapes.cs Is Coupled to AForge Geometry Types

**Problem:** `Shapes.cs` uses `AForge.Point`, `AForge.Math.Geometry.LineSegment`, and
`AForge.Imaging.Filters.IntPoint` as core data types. The shape model is inseparable
from the AForge library.

**New approach:** Define domain-native point/shape types (`PointD`, `LineSegment2D`,
`DetectedCircle`, `DetectedRectangle`, `DetectedComponent`). No library types in the domain model.

---

### FLAW 10 — Video Capture Is Polled via DirectShow Events

**Problem:** `VideoCaptureDevice` fires `NewFrame` events on AForge's capture thread.
Frame processing happens inside that event handler. If processing takes longer than the frame
interval the queue backs up. There is no frame dropping strategy, no backpressure, no
explicit buffer management.

**Consequence:** On slow machines or with computationally expensive algorithms the display
lags unpredictably. With 4K cameras at full resolution, frame processing was observed at ~8 fps
with 2-second display delay (noted in Camera.cs comments).

**New approach:** A dedicated `CameraCapture` service that produces frames on a background task.
A bounded channel (capacity = 1 or 2 frames) provides backpressure — newest frame always wins.
The processing pipeline consumes from the channel at its own rate.

---

### FLAW 11 — No Cancellation Anywhere

**Problem:** Long-running operations (homing sequences, measurement loops, batch placement)
have no cancellation mechanism. The only way to stop a running operation is to hit an
emergency stop which triggers `ErrorState = true` and discards all subsequent commands —
a blunt instrument.

**New approach:** `CancellationToken` threaded through every async method. An E-stop sets
the token. Individual operations check it at safe points and clean up gracefully.

---

### FLAW 12 — Configuration Has No Versioning or Migration

**Problem:** `MySettings` is deserialised directly from JSON. If a new property is added to
the class, old settings files simply omit it (Newtonsoft default-initialises it). If a property
is removed or renamed, old data is silently dropped. There is no schema version field,
no migration path, no validation.

**Consequence:** Upgrading the application can silently reset user-calibrated values to defaults.
There is no way to detect or warn about this.

**New approach:** Settings file includes a `"schemaVersion": N` field.
A `SettingsMigrator` class handles version-to-version transformations.
Validation on load produces structured warnings for missing/out-of-range values.

---

### FLAW 13 — TinyG EEPROM Blind Writes on Every Connect

**Problem:** `TinyGclass.JustConnected()` force-writes Z-switch settings to EEPROM on every
connection — not because they need changing, but because a previous EEPROM corruption was
discovered and the fix was to always overwrite. This approach:
- Wears EEPROM unnecessarily.
- Takes ~2 seconds of `Thread.Sleep` calls on every connect.
- Is documented with `CRITICAL:` comments explaining the workaround.

**New approach:** Read settings first. Compare to expected values. Write only changed values.
Log the comparison result. Surface a warning if values cannot be verified after write.

---

### FLAW 14 — Logging Is a Direct Method Call to MainForm

**Problem:** Every class (`TinyGclass`, `SKR3class`, `SerialComm`, `TapesClass`, `Nozzle`) calls
`MainForm.DisplayText(message, colour)` directly. This means:
- Nothing can log without a `FormMain` reference.
- Log output format (colour coding) is a UI concern mixed into domain logic.
- Logs cannot be redirected to a file, a test output, or a remote sink.

**New approach:** Standard `Microsoft.Extensions.Logging.ILogger<T>`. Each class receives a logger
via constructor injection. The WinForms UI subscribes to log events via a custom `ILoggerProvider`
that maps severity to colour. Logs also go to a rolling file.

---

### FLAW 15 — Nozzle Pull Engagement Depth Is a Magic Number

**Problem:** The engagement depth for nozzle-pull tape indexing is hardcoded as:
```csharp
double engageZ = pickupZ + 2.5;
```
There is no calibration step, no per-nozzle override, no per-tape override.
If the tape sits lower than expected (thicker PCB support, different tape carrier) the nozzle
misses the sprocket hole entirely with no feedback.

**New approach:** `PullEngagementDepth` as a configurable per-tape parameter with a default
that can be set globally. A calibration assistant that measures the actual hole engagement depth.

---

## 14. New Project — Target Architecture

```
????????????????????????????????????????????????????????????????
?                     PRESENTATION LAYER                       ?
?   WPF or WinForms shell — thin, no business logic           ?
?   MVVM or MVP pattern — ViewModel/Presenter owns UI state   ?
???????????????????????????????????????????????????????????????
? Motion   ?   Imagery     ?  Tape/Job        ?  Diagnostics  ?
? Service  ?   Service     ?  Service         ?  / Logging    ?
???????????????????????????????????????????????????????????????
?                    DOMAIN MODEL LAYER                        ?
?  TapeModel, JobModel, NozzleProfile, CoordinateTransform    ?
?  MachinePosition, MotionError, DetectedFeature              ?
????????????????????????????????????????????????????????????????
?                  INFRASTRUCTURE LAYER                        ?
?  IMotionController implementations (TinyG, GRBL, Modbus)   ?
?  ISerialTransport (real + in-memory test double)            ?
?  ICameraSource (DirectShow capture + file/simulator)        ?
?  ISettingsRepository (JSON file)                            ?
????????????????????????????????????????????????????????????????
```

All cross-layer dependencies point **inward** (Dependency Rule / Clean Architecture).
Domain model has zero dependencies on infrastructure or UI.
Infrastructure depends on domain interfaces, not concrete domain classes.

---

## 15. New Project — Domain Boundaries

### Motion Domain

**Owns:** Machine position, axis movements, homing, jog, probing, square correction, slack compensation,
speed profiles, E-stop.

**Does not own:** Serial port, UI, settings file, camera.

**Key types:**
```
MachinePosition { X, Y, Z, A }
MoveRequest { TargetPosition, Speed, MoveType }
HomeRequest { Axes }
MotionResult { Success, FinalPosition, Error }
MotionError { Code, Message, RecoveryHint }
IMotionController { MoveAsync, HomeAsync, JogAsync, ProbeAsync, GetPositionAsync }
```

### Imagery Domain

**Owns:** Camera capture, frame pipeline, measurement algorithms, mm-per-pixel calibration,
overlay rendering.

**Does not own:** What to search for (that's job/placement domain), UI display (that's presentation).

**Key types:**
```
CameraFrame { Bitmap/Mat, Timestamp, CameraId }
ProcessingPipeline { List<IProcessingStep> }
MeasurementResult { Found, X, Y, Angle, Confidence }
IProcessingStep { Name, Process(Mat) -> Mat }
ICameraSource { Task<CameraFrame> GetNextFrameAsync(CancellationToken) }
ICameraEngine { MeasureAsync(frame, pipeline, parameters) -> MeasurementResult }
```

### Tape & Job Domain

**Owns:** Tape model, part sequencing, hole position calculation, nozzle pull logic,
job file parsing, placement sequence.

**Does not own:** CNC movement (uses Motion domain via interface), camera measurement
(uses Imagery domain via interface).

**Key types:**
```
TapeModel { Id, FirstPosition, Pitch, CurrentPartIndex, PickupZ, PlaceZ, ... }
TapeIndexer { GetNextPartPosition(), IncrementPart(), ResetTape() }
PlacementJob { List<PlacementStep> }
PlacementStep { ComponentId, TapeId, TargetPosition, Rotation }
```

### Streaming Domain (Infrastructure)

**Owns:** Serial port lifecycle, protocol encoding/decoding, board-specific framing.

**Does not own:** What commands to send (Motion domain decides), logging to UI.

**Key types:**
```
ISerialTransport { SendAsync(string), IAsyncEnumerable<string> ReceiveAsync() }
IMotionProtocol { EncodeMove(MoveRequest) -> string, DecodeLine(string) -> ProtocolEvent }
TinyGProtocol : IMotionProtocol
GrblProtocol : IMotionProtocol
```

---

## 16. New Project — Technology Choices

| Concern | Choice | Reason |
|---|---|---|
| Framework | **.NET 8** | LTS, cross-platform capable, full async support |
| UI | **WPF** or **WinForms** | WPF preferred for MVVM; WinForms acceptable if team prefers |
| UI pattern | **MVVM** (WPF) or **MVP** (WinForms) | Separates UI state from business logic |
| Vision | **EmguCV (OpenCV .NET)** | AForge is abandoned; OpenCV is industry standard |
| Serial | `System.IO.Ports.SerialPort` wrapped in `ISerialTransport` async | Abstracted for testing |
| Async | `async`/`await` + `Channel<T>` for camera frames | Replaces Thread.Sleep + DoEvents |
| Logging | `Microsoft.Extensions.Logging` + Serilog | Structured, redirectable, testable |
| Settings | `System.Text.Json` + versioned POCO | Replace Newtonsoft (or keep it, lower priority) |
| Testing | **xUnit** + **Moq** + **FluentAssertions** | Modern, discoverable, expressive |
| DI | `Microsoft.Extensions.DependencyInjection` | Standard, testable, no magic |
| Modbus (later) | `NModbus4` or `FluentModbus` | TBD at Modbus phase |

---

## 17. New Project — What to Carry Forward

These are proven domain concepts and algorithms worth preserving:

| What | Where in old code | Notes |
|---|---|---|
| Square correction math | `CNC.SetCurrentX/Y()` | Correct and well-understood |
| Offset chain (jig + job + nozzle) | `MainForm` placement methods | Logic is correct, just coupled |
| Nozzle calibration model | `NozzleCalibrationClass` | `(angle, dx, dy)` table + interpolation |
| Tape pitch + part offset model | `TapesClass` grid columns | Model is correct, just wrong storage |
| HoughCircles for hole detection | `EmguCVFunctions.cs` | Well-tuned, keep the parameters |
| Contour-based component detection | `EmguCVEngine.Measure()` | Proven to work |
| TinyG non-standard JSON format | `TinyGControl.cs` | Firmware behaviour, must be preserved |
| GRBL settings parse (`$NNN=value`) | `MZ_CNCControl.LoadGRBLSettings()` | Typed `GRBLSettings` is good design |
| mm-per-pixel calibration | `Camera.cs` | Two-point calibration method works |
| Bounded write timeout pattern | All three board controllers | Correct, just needs async wrapper |
| `ProcessingPipeline` + parameter model | `VideoAlgorithmsCollection` | Good data model, bad storage coupling |
| Verified hole position cache | `TapesClass.VerifiedHolePositions` | Smart optimisation, carry forward |

---

## 18. New Project — What to Leave Behind

| What | Why |
|---|---|
| `MainForm` as business logic owner | Replace entirely with services + thin shell |
| AForge.NET (all of it) | Abandoned library, replaced by EmguCV |
| `Shapes.cs` (AForge geometry types) | Replace with domain-native geometry types |
| `DataGridView` as data store | Replace with proper model classes |
| `MySettings.MainForm` reference | Settings must be UI-independent |
| `Thread.Sleep` + `Application.DoEvents()` | Replace with async/await |
| `if (board == TinyG) ... else if (board == SKR3)` switch chains | Replace with `IMotionController` |
| `MainForm.DisplayText()` as global logger | Replace with `ILogger<T>` |
| Single boolean `ErrorState` | Replace with structured `MotionError` result type |
| `StartingUp` global boolean guard | Replace with proper initialisation lifecycle |
| TinyG force-write-all-settings-on-connect | Replace with read-compare-write-if-changed |
| `BlockingWrite_thread` + `ManualResetEvent` polling | Replace with async serial reads |
| `Application.DoEvents()` in close loops | Remove; replaced by async cancellation |
| Hardcoded `2.5mm` pull engagement depth | Make it a configurable per-tape parameter |
| Flat ~200-property `MySettings` | Replace with nested, versioned, validated settings model |

---

## 19. Future Protocol Additions — Modbus

Modbus is planned for a later development phase. Design notes to inform that phase:

### ModbusTCP

Standard IEC 61158 Modbus over Ethernet (port 502).

```
Function codes needed:
  FC01 - Read Coils (digital outputs status)
  FC02 - Read Discrete Inputs (digital inputs status)
  FC03 - Read Holding Registers (16-bit values: position, speed, status)
  FC05 - Write Single Coil (enable/disable output)
  FC06 - Write Single Register (set target position, speed)
  FC15 - Write Multiple Coils
  FC16 - Write Multiple Registers (motion profile)

Recommended library: FluentModbus (MIT, async, .NET 6+)
Connection: ModbusTcpClient (FluentModbus) wraps TCP socket
```

### ModbusRTU

Modbus over RS-485 serial (same `ISerialTransport` abstraction as TinyG/GRBL).

```
Frame format: [Device Address][Function Code][Data][CRC16]
Typical baud: 9600 / 19200 / 38400 / 115200

Recommended library: FluentModbus (also supports RTU over SerialPort)
Connection: ModbusRtuClient wraps SerialPort
```

### Integration Strategy

Both Modbus variants should implement `IMotionController` just like TinyG and GRBL.
The register map (what FC03 register N means for position, status, etc.) is device-specific
and belongs in the concrete implementation class, not in the protocol layer.

A `ModbusRegisterMap` configuration class (loaded from JSON) allows supporting different
Modbus-controlled motion systems without code changes.

```
// Planned interface extension for Modbus-specific capabilities:
interface IModbusMotionController : IMotionController
{
    Task<short[]> ReadHoldingRegistersAsync(int startAddress, int count, CancellationToken ct);
    Task WriteHoldingRegisterAsync(int address, short value, CancellationToken ct);
}
```

---

## 20. Suggested Folder Structure for New Project

```
LitePlacer2/
??? src/
?   ??? LitePlacer.Domain/               ? Pure C#, zero dependencies
?   ?   ??? Motion/
?   ?   ?   ??? MachinePosition.cs
?   ?   ?   ??? MoveRequest.cs
?   ?   ?   ??? MotionError.cs
?   ?   ?   ??? IMotionController.cs
?   ?   ?   ??? CoordinateTransform.cs
?   ?   ??? Imagery/
?   ?   ?   ??? CameraFrame.cs
?   ?   ?   ??? MeasurementResult.cs
?   ?   ?   ??? IProcessingStep.cs
?   ?   ?   ??? ICameraEngine.cs
?   ?   ?   ??? ICameraSource.cs
?   ?   ??? Tapes/
?   ?   ?   ??? TapeModel.cs
?   ?   ?   ??? TapeIndexer.cs
?   ?   ?   ??? VerifiedHoleCache.cs
?   ?   ??? Jobs/
?   ?   ?   ??? PlacementJob.cs
?   ?   ?   ??? PlacementStep.cs
?   ?   ??? Nozzle/
?   ?   ?   ??? NozzleProfile.cs
?   ?   ?   ??? NozzleCalibration.cs
?   ?   ??? Settings/
?   ?       ??? AppSettings.cs            ? Pure POCO, no UI reference
?   ?       ??? SettingsValidator.cs
?   ?
?   ??? LitePlacer.Infrastructure/        ? Hardware, serial, file I/O
?   ?   ??? Serial/
?   ?   ?   ??? ISerialTransport.cs
?   ?   ?   ??? SerialPortTransport.cs
?   ?   ?   ??? InMemoryTransport.cs      ? For tests / simulation
?   ?   ??? Motion/
?   ?   ?   ??? TinyGController.cs
?   ?   ?   ??? GrblController.cs
?   ?   ?   ??? ModbusController.cs       ? Added in Modbus phase
?   ?   ??? Imagery/
?   ?   ?   ??? DirectShowCameraSource.cs
?   ?   ?   ??? EmguCVEngine.cs
?   ?   ?   ??? EmguCVFunctions.cs
?   ?   ??? Settings/
?   ?   ?   ??? JsonSettingsRepository.cs
?   ?   ?   ??? SettingsMigrator.cs
?   ?   ??? Logging/
?   ?       ??? RichTextBoxLoggerProvider.cs
?   ?
?   ??? LitePlacer.Application/           ? Use cases / services, orchestration
?   ?   ??? MotionService.cs
?   ?   ??? ImagingService.cs
?   ?   ??? TapeService.cs
?   ?   ??? PlacementOrchestrator.cs
?   ?   ??? CalibrationService.cs
?   ?
?   ??? LitePlacer.UI/                    ? WPF or WinForms, thin shell only
?       ??? App.xaml / Program.cs
?       ??? MainWindow / MainForm
?       ??? ViewModels/ (WPF) or Presenters/ (WinForms)
?       ??? Views/
?
??? tests/
    ??? LitePlacer.Domain.Tests/
    ?   ??? Motion/
    ?   ?   ??? CoordinateTransformTests.cs
    ?   ?   ??? MotionErrorTests.cs
    ?   ??? Tapes/
    ?   ?   ??? TapeIndexerTests.cs
    ?   ?   ??? PullDistanceTests.cs
    ?   ??? Imagery/
    ?       ??? MeasurementResultTests.cs
    ??? LitePlacer.Infrastructure.Tests/
    ?   ??? Protocol/
    ?   ?   ??? TinyGProtocolTests.cs
    ?   ?   ??? GrblProtocolTests.cs
    ?   ??? Imagery/
    ?       ??? EmguCVFunctionTests.cs
    ??? LitePlacer.Application.Tests/
        ??? PlacementOrchestratorTests.cs
        ??? CalibrationServiceTests.cs
```

---

## Document History

| Date | Change |
|---|---|
| 2025-07-15 | Initial creation from full codebase analysis |
| 2025-07-16 | Session: EmguCV display overlay fixes + algorithm data management improvements |

> **Session 2025-07-16 — Detail**

### Bug: EmguCV display overlay always showed no circles (all red / nothing drawn)

**Root cause:** `FindCirclesFunct()` in `Camera.cs` uses AForge's `BlobCounter.ProcessImage()`.
`BlobCounter` silently fails and finds zero blobs when given an **8-bit grayscale** bitmap.
EmguCV's `Threshold` function outputs a single-channel grayscale bitmap (`thresholded.ToBitmap()`),
not the 24bpp colour bitmap that `BlobCounter` requires.

**Fix (`Camera.cs` — `Video_NewFrame`):**  
Before passing `AnalyzedFrame` to `FindCirclesFunct` / `FindRectanglesFunct` / `FindComponentsFromOutline_Funct` /
`FindComponentsFromPads_Funct`, the pixel format is checked. If it is not 24bpp or 32bpp colour, a temporary
24bpp copy is created via `Graphics.DrawImage`, used for blob detection, then disposed. `AnalyzedFrame` itself
is unchanged (it continues to be used for display). The conversion is a no-op cost on the AForge path.

```csharp
// Ensure blob input is 24bpp - BlobCounter fails silently on 8-bit grayscale (EmguCV Threshold output)
Bitmap blobInputFrame = AnalyzedFrame;
bool blobFrameOwned = false;
if (AnalyzedFrame.PixelFormat != PixelFormat.Format24bppRgb &&
    AnalyzedFrame.PixelFormat != PixelFormat.Format32bppArgb &&
    AnalyzedFrame.PixelFormat != PixelFormat.Format32bppRgb)
{
    blobInputFrame = new Bitmap(AnalyzedFrame.Width, AnalyzedFrame.Height, PixelFormat.Format24bppRgb);
    using (Graphics g = Graphics.FromImage(blobInputFrame))
        g.DrawImage(AnalyzedFrame, 0, 0);
    blobFrameOwned = true;
}
// ... use blobInputFrame for all Find*Funct calls ...
if (blobFrameOwned) blobInputFrame.Dispose();
```

**Key insight for new project:** In the new project only EmguCV will be used.
`DetectCircles_SubPixel` / `DetectRectangles_Precise` / `DetectComponent_Contours` return typed result
objects directly — no `BlobCounter` blob detection on the display path is needed at all.
The overlay drawing should consume `MeasurementResult` objects returned by the EmguCV engine,
not re-detect from the display frame.

---

### Bug: `GetProcessingZoom()` returned 1.0 when EmguCV was active

**Root cause:** `GetProcessingZoom()` only scanned `DisplayFunctions` (the AForge list).
When EmguCV is active, `DisplayFunctions` is always empty; the zoom multiplier lives in
`_displayEnginePipeline` instead.

**Fix (`Camera.cs` — `GetProcessingZoom`):**  
Added the same engine-detection pattern used by `GetMeasurementZoom()`:

```csharp
if (_currentEngine != null && _currentEngine.EngineName != "AForge.NET")
{
    lock (_displayEnginePipelineLock)
    {
        foreach (var f in _displayEnginePipeline)
        {
            if (f != null && f.Name == "Meas. zoom" && f.ParameterDouble >= 0.1)
                zoom *= f.ParameterDouble;
        }
    }
    return zoom;
}
// AForge path unchanged ...
```

**Note for new project:** `GetProcessingZoom` and `GetMeasurementZoom` being separate methods that
each scan a different list is a structural smell. In the new project there is one pipeline; zoom is
a named step in that pipeline read once by the display renderer.

---

### Bug: Empty tape algorithms caused "Nothing to search for" during job execution

**Root cause:** The `"Paper tape"`, `"Black tape"`, and `"Clear tape"` algorithm entries in the
engine-specific save file (`LitePlacer.VideoAlgorithms.EmguCVOpenCV`) were created as empty
placeholders (no functions, `SearchRounds=false`, all size parameters 0.0). The working algorithm
had been set up under the name `"Paper (White)"` instead. When a tape row referenced `"Paper tape"`,
`SetCurrentTapeMeasurement_m` loaded it, built a pipeline of 0 functions, and emitted:
`"Nothing to search for. Check some of the 'Features to search for' boxes."`

**Fix:** Three-part fix:

1. **Data file patched** (`LitePlacer.VideoAlgorithms.EmguCVOpenCV`):  
   `"Paper tape"`, `"Black tape"`, and `"Clear tape"` were populated by direct JSON text replacement
   with the content of the working `"Paper (White)"` algorithm:
   - Functions: `Meas. zoom (1.5x)` ? `Threshold (78)` ? `Invert` ? `Canny edge detection (100/150)`
   - `SearchRounds = true`, `Xmin = 0.5mm`, `Xmax = 2.0mm`, `XDist = 1.0mm`
   - Note: Black and Clear tape start with paper tape settings; threshold will need per-tape tuning.

2. **New `CopyFrom_button_Click` handler** (`VideoAlgorithmsUI.cs`):  
   Copies functions and measurement parameters from any source algorithm into the currently selected
   one. Opens a minimal inline dialog (label + ComboBox + OK/Cancel). Uses the existing `DeepClone<T>`
   JSON round-trip for a true independent copy. Refreshes the function table and measurement values UI.

3. **`CopyFrom_button` added to `MainForm.Designer.cs`**:  
   Placed at `(1120, 211)` to the right of the existing `Rename` button on the Algorithms tab.
   Field declaration, `Controls.Add`, location/size/text/event wiring all added.

**Important:** The `"Paper tape"` name is hardcoded in `SetCurrentTapeMeasurement_m()` (tapes.cs).
In the new project, algorithm names should not be hardcoded anywhere — the tape row carries a
reference to the algorithm by name/ID and the algorithm is resolved from the collection at runtime.

---

### Architecture note: Algorithm save files are engine-specific

The save file name is `LitePlacer.VideoAlgorithms.<EngineSuffix>` where the suffix is derived by
stripping spaces, parentheses, and `.NET` from the engine name:
- AForge.NET ? `.AForge`
- EmguCV (OpenCV) ? `.EmguCVOpenCV`

Algorithms contain an `EngineName` field. On load, old engine-agnostic files are migrated to the
engine-specific file. In the new project, since only EmguCV exists, there is no suffix or migration;
the single file is `VideoAlgorithms.json`.

---

### Summary: EmguCV overlay pipeline (correct display path)

```
Camera frame
  ?? EmguCV _displayEnginePipeline (Meas.zoom / Threshold / Invert / Canny ...)
       ?? AnalyzedFrame (8-bit grayscale after Threshold)
            ?? Convert to 24bpp copy for BlobCounter ? FindCircles/Rectangles/Components
            ?    ?? DrawCirclesFunct / DrawRectanglesFunct (green if in size, red if not)
            ?? ShowProcessing=true  ? DisplayedFrame = FitImageToUI(AnalyzedFrame)
               ShowProcessing=false ? DisplayedFrame = FitImageToUI(rawSourceFrame)
```

The green/red colour in `DrawCirclesFunct` is determined by comparing the detected circle diameter
(in mm, using `XmmPerPixel * Zoom`) against `MeasurementParameters.Xmin` / `Xmax` from the current
algorithm. Yellow is used when the circle centre is too far from the frame centre (`XUniqueDistance`).

> **Source repository:** https://github.com/Davec6505/LitePlacer-DEV  
> **Source branch at time of analysis:** `feature/nozzle-pull-tape-indexing`  
> **Original project:** C# 7.3 / .NET Framework 4.8 / WinForms  
> **Target project:** C# 12 / .NET 8 / WPF or WinForms / async throughout
