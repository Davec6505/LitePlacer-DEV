# LitePlacer Development Status

## Current Focus
**EmguCV Camera Engine Integration** — branch `feature/nozzle-pull-tape-indexing`

The EmguCV engine is now fully wired end-to-end for display, measurement, and startup restore.
Runtime testing of measurement accuracy is the next step.

---

## What Was Fixed (This Session)

### Fix 1: ShiTomasiCorners — wrong API for EmguCV 4.5
**File:** `LitePlacer/CameraEngines/EmguCVFunctions.cs`  
**Problem:** Used `CvInvoke.CornerMinEigenVal` (does not exist in EmguCV 4.5) and `GoodFeaturesToTrack`
(not available as a `CvInvoke` static). Also `MinMaxLoc` signature was wrong.  
**Fix:** Replaced with `CvInvoke.CornerHarris` + `Normalize` + pixel scan. Produces equivalent
corner visualisation (Harris response = Shi-Tomasi criterion for feature display purposes).

---

### Fix 2: Engine not applied at startup (always defaulted to AForge)
**File:** `LitePlacer/VideoAlgorithmsUI.cs`, `InitVideoAlgorithmsUI()`  
**Problem:** `listBoxCameraEngin.SelectedIndex = 1` fires `listBoxCameraEngin_SelectedIndexChanged`
but that handler (MainForm.cs ~line 15124) returns immediately because `StartingUp == true`.
`StartingUp` is only cleared ~104 lines later in `FormMain_Shown`. So the saved engine setting
was visually restored but never actually applied — AForge was always used until the user
manually toggled the dropdown.  
**Fix:** After setting `SelectedIndex`, call `SwitchCameraEngine(listBoxCameraEngin.SelectedItem.ToString())`
directly. `SwitchCameraEngine` has no `StartingUp` guard so it applies the engine immediately.

---

### Fix 3: NullReferenceException in UpdateVideoProcessing at startup
**File:** `LitePlacer/VideoAlgorithmsUI.cs`, `UpdateVideoProcessing()`  
**Problem:** Fix 2 caused `SwitchCameraEngine` ? `ChangeCamera` ? `AlgorithmsTab_RestoreBehaviour`
? `UpdateVideoProcessing` to run before `VideoAlgorithms = new VideoAlgorithmsCollection()` was
assigned (which happens two lines after `SwitchCameraEngine` in `InitVideoAlgorithmsUI`).
`UpdateVideoProcessing` dereferenced `VideoAlgorithms.CurrentAlgorithm` ? NullReferenceException.  
**Fix:** Added `VideoAlgorithms == null` to the existing null guard:
```csharp
if (VideoAlgorithms == null || VideoAlgorithms.CurrentAlgorithm == null)
```

---

### Fix 4: EmguCV processing not shown despite "Show processing" being selected
**File:** `LitePlacer/Camera.cs`, `Video_NewFrame()`  
**Problem:** `ShowUnprocessed` flag was only cleared when `DisplayFunctions.Count != 0`.
When EmguCV is active, `DisplayFunctions` (the AForge list) is always empty — the EmguCV
pipeline lives in `_displayEnginePipeline`. So `ShowUnprocessed` stayed `true` and the raw
frame was always shown even with functions configured and "Show processing" selected.  
**Fix:** Added a second check — if still `ShowUnprocessed` and the engine is not AForge,
check `_displayEnginePipeline.Count != 0` and clear `ShowUnprocessed` if so:
```csharp
if (ShowUnprocessed && _currentEngine != null && _currentEngine.EngineName != "AForge.NET")
{
    lock (_displayEnginePipelineLock)
    {
        if (_displayEnginePipeline != null && _displayEnginePipeline.Count != 0)
            ShowUnprocessed = false;
    }
}
```

---

### Fix 5: Processing still showing after "No video processing" selected
**File:** `LitePlacer/Camera.cs`, `ClearDisplayFunctionsList()`  
**Problem:** When "No video processing" is selected, `StopVideoProcessing()` calls
`ClearDisplayFunctionsList()`. This cleared `DisplayFunctions` (AForge list) but never touched
`_displayEnginePipeline`. Fix 4 then saw `_displayEnginePipeline.Count != 0`, cleared
`ShowUnprocessed`, and continued to show processed output.  
**Fix:** `ClearDisplayFunctionsList` now clears both lists. Also removed the early-return guard
`if (DisplayFunctions.Count == 0) return` which was preventing the engine pipeline from clearing:
```csharp
lock (DisplayFunctionsLock)    { DisplayFunctions.Clear(); }
lock (_displayEnginePipelineLock) { _displayEnginePipeline.Clear(); }
```

---

## Architecture: Camera Engine Pipelines

### Two parallel pipeline pairs in `Camera`

| | Display | Measurement |
|--|---------|-------------|
| **AForge** | `DisplayFunctions` (List\<AForgeFunction\>) | `MeasurementFunctions` |
| **EmguCV** | `_displayEnginePipeline` (List\<IProcessingFunction\>) | `_enginePipeline` |

### Rule: always check both
Any code that asks "is there anything to process?" must check both the AForge list and the
engine pipeline. The engine in use determines which one is populated.

### Engine detection pattern (used throughout Camera.cs)
```csharp
if (_currentEngine != null && _currentEngine.EngineName != "AForge.NET")
{
    // EmguCV path
}
else
{
    // AForge path
}
```

### Display mode radio buttons ? Camera.ShowProcessing
- **No video processing** ? `ClearDisplayFunctionsList()` ? both pipelines empty ? `ShowUnprocessed = true`
- **Show processing** ? `BuildDisplayFunctionsList()` ? pipeline populated ? `ShowUnprocessed = false`, `ShowProcessing = true`
- **Show results** ? `BuildDisplayFunctionsList()` ? pipeline populated ? `ShowUnprocessed = false`, `ShowProcessing = false`

---

## Architecture: Startup Sequence (FormMain_Shown)

```
FormMain_Shown (~line 289)
  StartingUp = true                          (~line 150)
  ...
  InitVideoAlgorithmsUI()                    (~line 335)
    listBoxCameraEngin.SelectedIndex = N     ? fires event but StartingUp guard kills it
    SwitchCameraEngine(selectedItem)         ? DIRECT call, no StartingUp guard
      ? ChangeCamera()
        ? AlgorithmsTab_RestoreBehaviour()
          ? UpdateVideoProcessing()          ? VideoAlgorithms null guard needed here
    VideoAlgorithms = new ...                ? assigned AFTER SwitchCameraEngine returns
    LoadVideoAlgorithms(...)
  ...
  StartingUp = false                         (~line 439)
```

---

## EmguCV Functions (EmguCVFunctions.cs)

All 25 implemented. EmguCV 4.5.3 specific notes:
- `GoodFeaturesToTrack` is NOT available as `CvInvoke` static in 4.5
- `CvInvoke.CornerMinEigenVal` does NOT exist in 4.5 — use `CornerHarris`
- `CvInvoke.MinMaxLoc` requires `ref Point minLoc, ref Point maxLoc` — cannot omit them
- `ShiTomasiCorners` uses Harris response as an equivalent visualisation

| Function | OpenCV op |
|----------|-----------|
| Grayscale | CvtColor BGR?Gray?BGR |
| Threshold | Threshold binary |
| Invert | BitwiseNot |
| EdgeDetect | Sobel XY combined |
| Blur | Blur (box filter) |
| GaussianBlur | GaussianBlur |
| Erosion | Erode |
| Dilation | Dilate |
| NoiseReduction | MedianBlur |
| CannyEdge | Canny |
| SobelEdge | Sobel (X, Y, or both per par_int) |
| LaplacianEdge | Laplacian |
| AdaptiveThreshold | AdaptiveThreshold |
| BilateralFilter | BilateralFilter |
| CLAHE | CLAHE equalisation |
| MorphologicalGradient | Morph gradient |
| MorphologicalTopHat | Morph top hat |
| MorphologicalBlackHat | Morph black hat |
| HoughCirclesVis | HoughCircles drawn on image |
| HarrisCorners | CornerHarris — red dots |
| ShiTomasiCorners | CornerHarris (4.5 fallback) — cyan dots |
| FastFeatureDetection | FastFeatureDetector keypoints — magenta dots |
| ContourDetection | FindContours — green outlines |
| WatershedSegmentation | Watershed — red boundaries |

---

## Known Remaining Items

| Item | File | Notes |
|------|------|-------|
| Measurement accuracy vs AForge | EmguCVEngine.cs | Runtime test needed with real hardware |
| HoughCircles parameter tuning | EmguCVEngine.cs | param1/param2 may need per-setup adjustment |
| Feature detection (circles/rects) in display | Camera.cs line ~1004 | Still uses AForge BlobCounter — marked TODO |
| GetProcessingZoom() for EmguCV | Camera.cs | Only scans AForge `DisplayFunctions` for zoom — engine zoom not tracked |

---

## Files Modified This Session

| File | What Changed |
|------|-------------|
| `LitePlacer/CameraEngines/EmguCVFunctions.cs` | ShiTomasiCorners reimplemented using CornerHarris |
| `LitePlacer/VideoAlgorithmsUI.cs` | InitVideoAlgorithmsUI calls SwitchCameraEngine directly; UpdateVideoProcessing null-guards VideoAlgorithms |
| `LitePlacer/Camera.cs` | Video_NewFrame checks engine pipeline for ShowUnprocessed; ClearDisplayFunctionsList clears both pipelines |

---

*Last updated: 2025-07-14*
