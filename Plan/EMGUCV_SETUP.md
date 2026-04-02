# EmguCV Native DLL Setup

## Issue
EmguCV requires native DLLs (cvextern.dll) to be in the output directory.
The NuGet package doesn't auto-copy them for .NET Framework projects.

**IMPORTANT:** LitePlacer is compiled for **x86 (32-bit)**, so you MUST use the **win-x86** native DLLs, NOT win-x64!

## Solution
After building, manually copy the native DLLs:

### PowerShell Command (x86 - CORRECT):
```powershell
$source = "C:\Users\davec\GIT\LitePlacer-DEV\packages\Emgu.CV.runtime.windows.4.5.3.4721\runtimes\win-x86\native\*.dll"
$dest = "C:\Users\davec\GIT\LitePlacer-DEV\LitePlacer\bin\Debug"
Copy-Item -Path $source -Destination $dest -Force
```

### Required Files (x86):
- cvextern.dll (42 MB) - Main OpenCV library **32-bit**
- opencv_videoio_ffmpeg453.dll (19 MB) - Video I/O support **32-bit**

### For Release Builds:
Replace `\bin\Debug` with `\bin\Release`

## Verification
Check that these files exist in your output directory:
```
LitePlacer\bin\Debug\cvextern.dll (should be ~42 MB for x86)
LitePlacer\bin\Debug\opencv_videoio_ffmpeg453.dll (should be ~19 MB for x86)
```

## Post-Build Event (RECOMMENDED - x86 version):
In Visual Studio:
1. Right-click LitePlacer project → Properties
2. Build Events → Post-build event command line:
```
xcopy /Y /D "$(SolutionDir)packages\Emgu.CV.runtime.windows.4.5.3.4721\runtimes\win-x86\native\*.dll" "$(TargetDir)"
```

**NOTE:** Use `win-x86` NOT `win-x64` for LitePlacer!

## Common Error:
If you see "Error code 193: %1 is not a valid Win32 application", you're using the wrong architecture (x64 instead of x86).

