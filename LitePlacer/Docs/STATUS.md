# LitePlacer Session Status Log

---

## Session: 2025-07-14 — EmguCV Camera Engine Integration

### Summary
EmguCV engine fully wired end-to-end. All 5 bugs fixed and confirmed building.

### Fix 1: ShiTomasiCorners — wrong API for EmguCV 4.5
File: LitePlacer/CameraEngines/EmguCVFunctions.cs
Problem: Used CvInvoke.CornerMinEigenVal (does not exist in EmguCV 4.5) and
  GoodFeaturesToTrack (not available as CvInvoke static). MinMaxLoc signature wrong.
Fix: Replaced with CvInvoke.CornerHarris + Normalize + pixel scan.

### Fix 2: Engine not applied at startup (always defaulted to AForge)
File: LitePlacer/VideoAlgorithmsUI.cs, InitVideoAlgorithmsUI()
Problem: listBoxCameraEngin_SelectedIndexChanged killed by StartingUp guard.
Fix: Call SwitchCameraEngine() directly after setting SelectedIndex.

### Fix 3: NullReferenceException in UpdateVideoProcessing at startup
File: LitePlacer/VideoAlgorithmsUI.cs, UpdateVideoProcessing()
Problem: SwitchCameraEngine runs before VideoAlgorithms is assigned.
Fix: Guard VideoAlgorithms == null at top of UpdateVideoProcessing.

### Fix 4: EmguCV processing not displayed
File: LitePlacer/Camera.cs, Video_NewFrame()
Problem: ShowUnprocessed only checked DisplayFunctions (always empty for EmguCV).
Fix: Also check _displayEnginePipeline.Count to clear ShowUnprocessed.

### Fix 5: Processing shown after No video processing selected
File: LitePlacer/Camera.cs, ClearDisplayFunctionsList()
Problem: ClearDisplayFunctionsList only cleared AForge list.
Fix: Now clears both DisplayFunctions and _displayEnginePipeline.

### Architecture note: startup sequence
  InitVideoAlgorithmsUI() sets SelectedIndex -> fires event (killed by StartingUp)
  -> calls SwitchCameraEngine() directly -> ChangeCamera -> AlgorithmsTab_RestoreBehaviour
  -> UpdateVideoProcessing (VideoAlgorithms null-guarded here)
  -> VideoAlgorithms = new VideoAlgorithmsCollection() (two lines after SwitchCameraEngine)

### Files changed
  LitePlacer/CameraEngines/EmguCVFunctions.cs
  LitePlacer/VideoAlgorithmsUI.cs
  LitePlacer/Camera.cs

---

## Session: 2025-07-14 — Nozzle Pull Tape Indexing Bug Fixes

### Summary
Two bugs found and fixed in the camera + pull path.

### Bug 1: Machine moved to wrong position after pull (camera path)
File: LitePlacer/MainForm.cs, PickUpPartWithHoleMeasurement_m()
Problem: NozzlePullTapeIndex_m pulled the tape physically. Next_X/Y still pointed
  at the pre-pull hole. GotoNextPartByMeasurement_m went there, found no hole
  (tape had physically moved), then calculated part offset from the wrong position,
  sending the nozzle in the opposite direction from the part.
Fix: After successful pull, reset Next_X/Y to FirstX/Y. Hole #1 is always in the
  same place after a physical pull. GotoNextPartByMeasurement_m reliably finds it.

### Bug 2: IncrementTape had no awareness of pull mode
File: LitePlacer/tapes.cs, IncrementTape()
Problem: No-increment guard was an if(!useNozzlePull) wrapper in MainForm.cs only.
  Any other caller of IncrementTape would silently increment even with pull enabled.
Fix: IncrementTape() reads UseNozzlePull_Column itself at the top via
  InvokeRequired/Invoke pattern (thread-safe, called from background thread).
  Returns early with no changes if pull is enabled.
  Redundant wrapper removed from MainForm.cs.

### Pull mode tape behaviour (rule)
  - Tape advances physically via NozzlePullTapeIndex_m
  - After pull: Next_X/Y reset to FirstX/Y (hole #1 always same place)
  - IncrementTape: skips counter and Next_X/Y update when pull enabled
  - Camera verifies hole #1, calculates part offset, nozzle moves to part
  - No software counter ever increments

### Files changed
  LitePlacer/MainForm.cs   (PickUpPartWithHoleMeasurement_m — reset Next_X/Y after pull)
  LitePlacer/tapes.cs      (IncrementTape — owns no-increment rule with InvokeRequired)

---

## Session: 2025-07-17 — EmguCV Nozzle Calibration Circle Selection

### Summary
Replaced the previous "pick best" circle selection logic in the EmguCV engine with a
robust smallest-circle + 5% deviation guard approach. Added cross-rotation-step
consistency checking to Calibrate() so a wrong-ring grab is caught immediately.

### Problem
The previous EmguCV DetectCircles_SubPixel used a "pick best" heuristic (smallest
diameter for edge images, closest to centre for filled images). This silently selected
the wrong circle ring when the nozzle outer body was also visible, causing calibration
errors that were invisible to the operator.

### Fix 1: DetectCircles_SubPixel — smallest-circle with 5% ambiguity guard
File: LitePlacer/Classes/CameraEngines/EmguCVEngine.cs
- All circles passing size (Xmin/Xmax) AND distance filters are collected
- List sorted by diameter ascending
- If only 1 passes: use it
- If >1 pass and two smallest within 5% of each other: abort as ambiguous
- If >1 pass and smallest is >5% smaller than next: select smallest, log the rest
  (inner hole is always smaller than the outer body ring)

### Fix 2: FindCirclesForDisplay — overlay mirrors engine selection exactly
File: LitePlacer/Classes/CameraEngines/EmguCVEngine.cs
- Green  = selected smallest circle (what Measure() returns)
- Yellow = passes size+distance but ambiguous with winner (within 5%) or runner-up
- Orange = passes size only, outside distance window
- Red    = fails size filter

### Fix 3: Camera.LastMeasuredSizeMm — diameter exposed to callers
File: LitePlacer/Classes/Camera.cs
- New public field LastMeasuredSizeMm written after each successful engine measurement
- Lets Calibrate() read diameter without changing the Measure() signature

### Fix 4: Calibrate() — cross-step diameter deviation guard
File: LitePlacer/Classes/Nozzle.cs
- Step 0 (angle 0°) sets referenceDiameterMm from Cam.LastMeasuredSizeMm
- Every subsequent step checks: |measured - reference| / reference > 0.05
- If exceeded: abort with message naming the angle and both diameters
- Ambiguous result from engine also aborts with angle-specific message

### Calibration contract (post-fix)
  - Operator sets Xmin/Xmax to bracket the inner nozzle hole diameter
  - Engine selects the smallest circle passing those bounds
  - If two circles indistinguishable in size (within 5%): abort, tighten bounds
  - If selected circle diameter changes >5% between rotation steps: abort
  - Overlay always shows exactly what Measure() will use

### Files changed
  LitePlacer/Classes/Camera.cs                     (LastMeasuredSizeMm field + write)
  LitePlacer/Classes/CameraEngines/EmguCVEngine.cs (DetectCircles_SubPixel + FindCirclesForDisplay)
  LitePlacer/Classes/Nozzle.cs                     (Calibrate() deviation guard)

