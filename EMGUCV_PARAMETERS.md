# EmguCV Function Parameterization Guide

## Overview
This document defines the parameters for all EmguCV advanced functions, following the existing AForge parameterization pattern used in `VideoAlgorithmsUI.cs`.

---

## Parameter Architecture

Each function uses the `AForgeFunctionDefinition` class which provides:
- `parameterInt` - Integer parameter (0-255 typical range)
- `parameterDouble` - Primary floating-point parameter
- `parameterDoubleA` - First additional double parameter
- `parameterDoubleB` - Second additional double parameter
- `parameterDoubleC` - Third additional double parameter
- `R`, `G`, `B` - Color values (0-255)

---

## EmguCV Advanced Functions - Parameter Definitions

### 1. Canny Edge Detection ? IMPLEMENTED
**Current Implementation:** Uses `parameterDoubleA` and `parameterDoubleB`

**Parameters:**
- `parameterDoubleA` = Lower threshold (default: 100, range: 0-255)
- `parameterDoubleB` = Upper threshold (default: 200, range: 0-255)
- `parameterInt` = Aperture size (default: 3, range: 3, 5, 7)

**UI Setup:**
```csharp
case "Canny edge detection":
    EnableDoubleA("Lower threshold:");
    EnableDoubleB("Upper threshold:");
    EnableInt(3, 7, "Aperture size:");
    FunctionExplanation_textBox.Text = 
        "Detects edges using Canny algorithm with hysteresis.\r\n" +
        "Lower threshold: Weak edges below this are discarded.\r\n" +
        "Upper threshold: Strong edges above this are kept.\r\n" +
        "Aperture: Sobel kernel size (3, 5, or 7)";
    break;
```

---

### 2. Sobel Edge Detection
**Parameters:**
- `parameterInt` = Direction (default: 0=Both, range: 0-3)
  - 0 = Both X and Y
  - 1 = X direction only
  - 2 = Y direction only  
  - 3 = Magnitude
- `parameterDouble` = Scale factor (default: 1.0, range: 0.1-10.0)
- `parameterDoubleA` = Delta (default: 0, range: 0-255)

**Default Values:**
```csharp
case "Sobel edge detection":
    funct.parameterInt = 0;        // Both directions
    funct.parameterDouble = 1.0;   // Scale
    funct.parameterDoubleA = 0;    // Delta
    break;
```

**UI Setup:**
```csharp
case "Sobel edge detection":
    EnableInt(0, 3, "Direction:");
    EnableDouble("Scale:");
    EnableDoubleA("Delta:");
    FunctionExplanation_textBox.Text = 
        "Detects edges using Sobel operator.\r\n" +
        "Direction: 0=Both XY, 1=X only, 2=Y only, 3=Magnitude\r\n" +
        "Scale: Multiplier for gradient values\r\n" +
        "Delta: Value added to results";
    break;
```

---

### 3. Laplacian Edge Detection
**Parameters:**
- `parameterInt` = Aperture size (default: 3, range: 1, 3, 5, 7)
- `parameterDouble` = Scale factor (default: 1.0, range: 0.1-10.0)
- `parameterDoubleA` = Delta (default: 0, range: 0-255)

**Default Values:**
```csharp
case "Laplacian edge detection":
    funct.parameterInt = 3;        // Aperture
    funct.parameterDouble = 1.0;   // Scale
    funct.parameterDoubleA = 0;    // Delta
    break;
```

**UI Setup:**
```csharp
case "Laplacian edge detection":
    EnableInt(1, 7, "Aperture:");
    EnableDouble("Scale:");
    EnableDoubleA("Delta:");
    FunctionExplanation_textBox.Text = 
        "Detects edges using second derivative (Laplacian).\r\n" +
        "Good for finding zero-crossings and fine details.\r\n" +
        "Aperture: Kernel size (1, 3, 5, or 7)";
    break;
```

---

### 4. Adaptive Threshold
**Parameters:**
- `parameterInt` = Method (default: 0=Mean, range: 0-1)
  - 0 = Mean
  - 1 = Gaussian
- `parameterDouble` = Block size (default: 11, must be odd, range: 3-99)
- `parameterDoubleA` = C constant (default: 2, range: -50 to 50)
- `parameterDoubleB` = Max value (default: 255, range: 0-255)

**Default Values:**
```csharp
case "Adaptive threshold":
    funct.parameterInt = 0;          // Mean method
    funct.parameterDouble = 11;      // Block size
    funct.parameterDoubleA = 2;      // C constant
    funct.parameterDoubleB = 255;    // Max value
    break;
```

**UI Setup:**
```csharp
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
    break;
```

---

### 5. Bilateral Filter
**Parameters:**
- `parameterInt` = Diameter (default: 9, range: 1-50)
- `parameterDouble` = Sigma color (default: 75, range: 1-200)
- `parameterDoubleA` = Sigma space (default: 75, range: 1-200)

**Default Values:**
```csharp
case "Bilateral filter":
    funct.parameterInt = 9;        // Diameter
    funct.parameterDouble = 75;    // Sigma color
    funct.parameterDoubleA = 75;   // Sigma space
    break;
```

**UI Setup:**
```csharp
case "Bilateral filter":
    EnableInt(1, 50, "Diameter:");
    EnableDouble("Sigma color:");
    EnableDoubleA("Sigma space:");
    FunctionExplanation_textBox.Text = 
        "Edge-preserving noise reduction.\r\n" +
        "Diameter: Filter size (larger = slower but smoother)\r\n" +
        "Sigma color: Color difference sensitivity\r\n" +
        "Sigma space: Spatial distance sensitivity";
    break;
```

---

### 6. CLAHE (Contrast Limited Adaptive Histogram Equalization)
**Parameters:**
- `parameterDouble` = Clip limit (default: 40.0, range: 1-100)
- `parameterInt` = Tile grid size (default: 8, range: 2-32)

**Default Values:**
```csharp
case "CLAHE":
    funct.parameterDouble = 40.0;  // Clip limit
    funct.parameterInt = 8;        // Tile size
    break;
```

**UI Setup:**
```csharp
case "CLAHE":
    EnableDouble("Clip limit:");
    EnableInt(2, 32, "Tile size:");
    FunctionExplanation_textBox.Text = 
        "Contrast enhancement with clipping to prevent over-amplification.\r\n" +
        "Clip limit: Controls contrast amplification (higher = more contrast)\r\n" +
        "Tile size: Grid size for local histogram equalization";
    break;
```

---

### 7. Morphological Gradient
**Parameters:**
- `parameterInt` = Kernel size (default: 3, range: 1-21)
- `parameterDouble` = Iterations (default: 1, range: 1-10)

**Default Values:**
```csharp
case "Morphological gradient":
    funct.parameterInt = 3;        // Kernel size
    funct.parameterDouble = 1;     // Iterations
    break;
```

**UI Setup:**
```csharp
case "Morphological gradient":
    EnableInt(1, 21, "Kernel size:");
    EnableDouble("Iterations:");
    FunctionExplanation_textBox.Text = 
        "Edge detection via morphology (dilation - erosion).\r\n" +
        "Highlights object boundaries.\r\n" +
        "Kernel size: Structuring element size (odd numbers)";
    break;
```

---

### 8. Morphological Top Hat
**Parameters:**
- `parameterInt` = Kernel size (default: 5, range: 1-21)

**Default Values:**
```csharp
case "Morphological top hat":
    funct.parameterInt = 5;
    break;
```

**UI Setup:**
```csharp
case "Morphological top hat":
    EnableInt(1, 21, "Kernel size:");
    FunctionExplanation_textBox.Text = 
        "Extracts bright features smaller than structuring element.\r\n" +
        "Useful for finding small bright objects on dark background.";
    break;
```

---

### 9. Morphological Black Hat
**Parameters:**
- `parameterInt` = Kernel size (default: 5, range: 1-21)

**Default Values:**
```csharp
case "Morphological black hat":
    funct.parameterInt = 5;
    break;
```

**UI Setup:**
```csharp
case "Morphological black hat":
    EnableInt(1, 21, "Kernel size:");
    FunctionExplanation_textBox.Text = 
        "Extracts dark features smaller than structuring element.\r\n" +
        "Useful for finding small dark objects on bright background.";
    break;
```

---

### 10. Hough Circles (Sub-Pixel)
**Parameters:**
- `parameterInt` = Method (default: 0=Gradient, range: 0-1)
- `parameterDouble` = DP (default: 1.0, range: 0.5-3.0)
- `parameterDoubleA` = Min distance (default: 20, range: 1-500)
- `parameterDoubleB` = Param1 (default: 100, range: 1-300)
- `parameterDoubleC` = Param2 (default: 30, range: 1-100)
- `R` = Min radius (default: 10, range: 0-255)
- `G` = Max radius (default: 100, range: 0-255)

**Default Values:**
```csharp
case "Hough circles (sub-pixel)":
    funct.parameterInt = 0;         // Method
    funct.parameterDouble = 1.0;    // DP
    funct.parameterDoubleA = 20;    // Min distance
    funct.parameterDoubleB = 100;   // Param1
    funct.parameterDoubleC = 30;    // Param2
    funct.R = 10;                   // Min radius
    funct.G = 100;                  // Max radius
    break;
```

**UI Setup:**
```csharp
case "Hough circles (sub-pixel)":
    EnableInt(0, 1, "Method:");
    EnableDouble("DP:");
    EnableDoubleA("Min distance:");
    EnableDoubleB("Edge threshold:");
    EnableDoubleC("Center threshold:");
    EnableRGB("Radius (R=min, G=max):");
    FunctionExplanation_textBox.Text = 
        "Circle detection with sub-pixel accuracy.\r\n" +
        "DP: Inverse accumulator resolution\r\n" +
        "Min distance: Minimum distance between circle centers\r\n" +
        "Edge threshold: Canny edge detector threshold\r\n" +
        "Center threshold: Accumulator threshold for centers";
    break;
```

---

### 11. Harris Corners
**Parameters:**
- `parameterInt` = Block size (default: 2, range: 1-10)
- `parameterDouble` = Aperture size (default: 3, range: 3-31)
- `parameterDoubleA` = K parameter (default: 0.04, range: 0.01-0.1)

**Default Values:**
```csharp
case "Harris corners":
    funct.parameterInt = 2;         // Block size
    funct.parameterDouble = 3;      // Aperture
    funct.parameterDoubleA = 0.04;  // K
    break;
```

**UI Setup:**
```csharp
case "Harris corners":
    EnableInt(1, 10, "Block size:");
    EnableDouble("Aperture:");
    EnableDoubleA("K parameter:");
    FunctionExplanation_textBox.Text = 
        "Detects corner points (Harris detector).\r\n" +
        "Block size: Neighborhood size\r\n" +
        "Aperture: Sobel derivative aperture\r\n" +
        "K: Harris detector free parameter (0.04-0.06 typical)";
    break;
```

---

### 12. Shi-Tomasi Corners
**Parameters:**
- `parameterInt` = Max corners (default: 100, range: 1-500)
- `parameterDouble` = Quality level (default: 0.01, range: 0.001-0.1)
- `parameterDoubleA` = Min distance (default: 10, range: 1-100)
- `parameterInt` also used for block size if needed

**Default Values:**
```csharp
case "Shi-Tomasi corners":
    funct.parameterInt = 100;       // Max corners
    funct.parameterDouble = 0.01;   // Quality
    funct.parameterDoubleA = 10;    // Min distance
    break;
```

**UI Setup:**
```csharp
case "Shi-Tomasi corners":
    EnableInt(1, 500, "Max corners:");
    EnableDouble("Quality level:");
    EnableDoubleA("Min distance:");
    FunctionExplanation_textBox.Text = 
        "Detects good features to track (Shi-Tomasi).\r\n" +
        "Max corners: Maximum number of corners to return\r\n" +
        "Quality level: Minimal accepted quality (0.01 typical)\r\n" +
        "Min distance: Minimum distance between corners";
    break;
```

---

### 13. FAST Feature Detection
**Parameters:**
- `parameterInt` = Threshold (default: 40, range: 1-100)
- `parameterDouble` = Non-max suppression (default: 1=true, range: 0-1)

**Default Values:**
```csharp
case "FAST feature detection":
    funct.parameterInt = 40;        // Threshold
    funct.parameterDouble = 1;      // Non-max suppression
    break;
```

**UI Setup:**
```csharp
case "FAST feature detection":
    EnableInt(1, 100, "Threshold:");
    EnableInt(0, 1, "Non-max suppress:");
    FunctionExplanation_textBox.Text = 
        "Fast corner detection (FAST algorithm).\r\n" +
        "Threshold: Detection sensitivity (lower = more corners)\r\n" +
        "Non-max suppress: 1=Remove adjacent weak corners, 0=Keep all";
    break;
```

---

### 14. Template Matching
**Parameters:**
- `parameterInt` = Method (default: 5=CCOEFF_NORMED, range: 0-5)
  - 0 = SQDIFF
  - 1 = SQDIFF_NORMED
  - 2 = CCORR
  - 3 = CCORR_NORMED
  - 4 = CCOEFF
  - 5 = CCOEFF_NORMED
- `parameterDouble` = Match threshold (default: 0.8, range: 0-1)
- Note: Template image loaded separately

**Default Values:**
```csharp
case "Template matching":
    funct.parameterInt = 5;         // Method
    funct.parameterDouble = 0.8;    // Threshold
    break;
```

**UI Setup:**
```csharp
case "Template matching":
    EnableInt(0, 5, "Method:");
    EnableDouble("Threshold:");
    FunctionExplanation_textBox.Text = 
        "Find template pattern in image.\r\n" +
        "Method: 0-1=SQDIFF, 2-3=CCORR, 4-5=CCOEFF (5 recommended)\r\n" +
        "Threshold: Match quality threshold (0-1, higher = stricter)";
    break;
```

---

### 15. Contour Detection
**Parameters:**
- `parameterInt` = Retrieval mode (default: 1=EXTERNAL, range: 0-3)
  - 0 = EXTERNAL (outer contours only)
  - 1 = LIST (all contours, no hierarchy)
  - 2 = CCOMP (two-level hierarchy)
  - 3 = TREE (full hierarchy)
- `parameterDouble` = Min area (default: 100, range: 0-10000)
- `parameterDoubleA` = Max area (default: 10000, range: 0-100000)

**Default Values:**
```csharp
case "Contour detection":
    funct.parameterInt = 1;         // Mode
    funct.parameterDouble = 100;    // Min area
    funct.parameterDoubleA = 10000; // Max area
    break;
```

**UI Setup:**
```csharp
case "Contour detection":
    EnableInt(0, 3, "Mode:");
    EnableDouble("Min area:");
    EnableDoubleA("Max area:");
    FunctionExplanation_textBox.Text = 
        "Finds and filters contours by area.\r\n" +
        "Mode: 0=External only, 1=All contours, 2=Two-level, 3=Tree\r\n" +
        "Min/Max area: Filter contours by pixel area";
    break;
```

---

### 16. Watershed Segmentation
**Parameters:**
- `parameterInt` = Min distance (default: 10, range: 1-50)
- `parameterDouble` = Threshold factor (default: 0.5, range: 0.1-1.0)

**Default Values:**
```csharp
case "Watershed segmentation":
    funct.parameterInt = 10;        // Min distance
    funct.parameterDouble = 0.5;    // Threshold
    break;
```

**UI Setup:**
```csharp
case "Watershed segmentation":
    EnableInt(1, 50, "Min distance:");
    EnableDouble("Threshold:");
    FunctionExplanation_textBox.Text = 
        "Separates touching objects using watershed algorithm.\r\n" +
        "Min distance: Minimum distance between object seeds\r\n" +
        "Threshold: Controls segmentation sensitivity";
    break;
```

---

## Implementation Checklist

### Phase 1: Core Functions (High Priority)
- [x] ? Canny edge detection (DONE)
- [ ] Adaptive threshold
- [ ] Bilateral filter
- [ ] CLAHE
- [ ] Hough circles (sub-pixel)

### Phase 2: Morphological Operations
- [ ] Morphological gradient
- [ ] Morphological top hat
- [ ] Morphological black hat

### Phase 3: Feature Detection
- [ ] Sobel edge detection
- [ ] Laplacian edge detection
- [ ] Harris corners
- [ ] Shi-Tomasi corners
- [ ] FAST feature detection

### Phase 4: Advanced Analysis
- [ ] Template matching
- [ ] Contour detection
- [ ] Watershed segmentation

---

## Testing Strategy

For each function:
1. Create test case in `SetFunctionDefaultParameters()`
2. Add UI setup in `UpdateParameterTargets()`
3. Create processor class in `CameraEngines/EmguCV_[FunctionName].cs`
4. Register in `EmguCVEngine.CreateEmguCVFunction()`
5. Test with various parameter values
6. Document optimal ranges for typical use cases

---

## Notes

- All parameters use existing `AForgeFunctionDefinition` fields
- UI already supports Int, Double, DoubleA/B/C, and RGB parameters
- Keep parameter ranges sensible for pick-and-place vision tasks
- Default values should work "out of the box" for typical applications
- Consider adding parameter validation in processor implementations

---

*Last Updated: 2025-01-XX*
