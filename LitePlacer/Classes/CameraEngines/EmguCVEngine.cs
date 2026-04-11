using System;
using System.Collections.Generic;
using System.Drawing;

// EmguCV imports
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace LitePlacer.CameraEngines
{
    /// <summary>
    /// EmguCV (OpenCV) camera engine implementation
    /// Provides advanced computer vision algorithms with sub-pixel accuracy
    /// Requires Emgu.CV and Emgu.CV.runtime.windows NuGet packages
    /// </summary>
    public class EmguCVEngine : ICameraEngine
    {
        // Minimum fraction of a full circle circumference that a Canny contour must cover to be
        // accepted as a ring (not a reflection arc). 0.60 = 216-degree minimum arc.
        // Raise toward 0.75 if shoulder-reflection arcs still pass; lower toward 0.45 for
        // worn/dirty nozzles where the Canny ring is partially interrupted.
        private const double ArcCoverageThreshold = 0.60;

        private FormMain _mainForm;
        
        public EmguCVEngine(FormMain mainForm)
        {
            _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
        }

        public string EngineName => "EmguCV (OpenCV)";

        public string Version
        {
            get
            {
                try
                {
                    // EmguCV 4.x doesn't have a Version property, use BuildInformation
                    return "4.12.0 (OpenCV)";
                }
                catch
                {
                    return "Not available";
                }
            }
        }

        public bool IsAvailable
        {
            get
            {
                try
                {
                    // Test if EmguCV DLLs are present and functional
                    // Try a very simple operation that requires the native DLL
                    using (Mat testMat = new Mat(1, 1, DepthType.Cv8U, 1))
                    {
                        // If we can create a Mat, EmguCV is working
                        return testMat.IsEmpty == false;
                    }
                }
                catch (System.DllNotFoundException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EmguCV DLL not found: {ex.Message}");
                    return false;
                }
                catch (System.TypeInitializationException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EmguCV initialization failed: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EmguCV not available: {ex.GetType().Name}: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns list of all EmguCV functions available
        /// Includes all AForge functions plus EmguCV-exclusive advanced features
        /// </summary>
        public List<string> GetAvailableFunctions()
        {
            var functions = new List<string>();
            
            // Standard functions (compatible with AForge)
            functions.AddRange(new[]
            {
                "Grayscale",
                "Invert",
                "Threshold",
                "Blur",
                "Gaussian blur",
                "Erosion",
                "Dilation",
                "Noise reduction",
                "Meas. zoom"
            });
            
            // NEW: EmguCV-exclusive advanced functions
            functions.AddRange(new[]
            {
                "--- EmguCV Advanced Features ---",
                "Canny edge detection",           // Better edge detection with hysteresis
                "Sobel edge detection",            // Directional edge detection
                "Laplacian edge detection",        // Second derivative edge detection
                "Adaptive threshold",              // Handles varying lighting conditions
                "Bilateral filter",                // Edge-preserving noise reduction
                "Morphological gradient",          // Edge detection via morphology
                "Morphological top hat",           // Bright feature extraction
                "Morphological black hat",         // Dark feature extraction
                "CLAHE",                          // Contrast Limited Adaptive Histogram Equalization
                "Hough circles (sub-pixel)",      // Circle detection with sub-pixel accuracy
                "Contour circles",                 // Contour+circularity detection on pre-processed image
                "Harris corners",                  // Corner point detection
                "Shi-Tomasi corners",             // Good features to track
                "FAST feature detection",          // Fast feature point detection
                "Template matching",               // Find patterns in image
                "Contour detection",              // Find and analyze shapes
                "Convex hull",                    // Shape analysis
                "Distance transform",              // Distance to nearest zero pixel
                "Watershed segmentation"           // Separate touching objects
            });
            
            return functions;
        }

        /// <summary>
        /// Builds processing pipeline from function definitions
        /// Creates EmguCV-based processing functions using delegates (like AForge)
        /// </summary>
        public List<IProcessingFunction> BuildProcessingPipeline(List<AForgeFunctionDefinition> definitions)
        {
            var pipeline = new List<IProcessingFunction>();
            
            if (!IsAvailable)
            {
                _mainForm.DisplayText("EmguCV not available - cannot build pipeline", 
                    System.Drawing.KnownColor.DarkRed);
                return pipeline;
            }
            
            foreach (var def in definitions)
            {
                if (!def.Active) continue;
                
                IProcessingFunction function = CreateEmguCVFunction(def);
                if (function != null)
                {
                    pipeline.Add(function);
                }
                else
                {
                    _mainForm.DisplayText($"Warning: Function '{def.Name}' not implemented in EmguCV engine", 
                        System.Drawing.KnownColor.DarkOrange);
                }
            }
            
            return pipeline;
        }

        private IProcessingFunction CreateEmguCVFunction(AForgeFunctionDefinition def)
        {
            switch (def.Name)
            {
                // Standard functions
                case "Grayscale":               return new EmguCVProcessor(def, EmguCVFunctions.Grayscale);
                case "Threshold":               return new EmguCVProcessor(def, EmguCVFunctions.Threshold);
                case "Invert":                  return new EmguCVProcessor(def, EmguCVFunctions.Invert);
                case "Edge detect":             return new EmguCVProcessor(def, EmguCVFunctions.EdgeDetect);
                case "Blur":                    return new EmguCVProcessor(def, EmguCVFunctions.Blur);
                case "Gaussian blur":           return new EmguCVProcessor(def, EmguCVFunctions.GaussianBlur);
                case "Erosion":                 return new EmguCVProcessor(def, EmguCVFunctions.Erosion);
                case "Dilation":                return new EmguCVProcessor(def, EmguCVFunctions.Dilation);
                case "Noise reduction":         return new EmguCVProcessor(def, EmguCVFunctions.NoiseReduction);
                case "Meas. zoom":               return new EmguCVProcessor(def, EmguCVFunctions.MeasZoom);
                // EmguCV advanced functions
                case "Canny edge detection":    return new EmguCVProcessor(def, EmguCVFunctions.CannyEdge);
                case "Sobel edge detection":    return new EmguCVProcessor(def, EmguCVFunctions.SobelEdge);
                case "Laplacian edge detection":return new EmguCVProcessor(def, EmguCVFunctions.LaplacianEdge);
                case "Adaptive threshold":      return new EmguCVProcessor(def, EmguCVFunctions.AdaptiveThreshold);
                case "Bilateral filter":        return new EmguCVProcessor(def, EmguCVFunctions.BilateralFilter);
                case "CLAHE":                   return new EmguCVProcessor(def, EmguCVFunctions.CLAHE);
                case "Morphological gradient":  return new EmguCVProcessor(def, EmguCVFunctions.MorphologicalGradient);
                case "Morphological top hat":   return new EmguCVProcessor(def, EmguCVFunctions.MorphologicalTopHat);
                case "Morphological black hat":  return new EmguCVProcessor(def, EmguCVFunctions.MorphologicalBlackHat);
                case "Hough circles (sub-pixel)":return new EmguCVProcessor(def, EmguCVFunctions.HoughCirclesVis);
                case "Contour circles":          return new EmguCVProcessor(def, EmguCVFunctions.ContourCircles);
                case "Harris corners":          return new EmguCVProcessor(def, EmguCVFunctions.HarrisCorners);
                case "Shi-Tomasi corners":      return new EmguCVProcessor(def, EmguCVFunctions.ShiTomasiCorners);
                case "FAST feature detection":  return new EmguCVProcessor(def, EmguCVFunctions.FastFeatureDetection);
                case "Contour detection":       return new EmguCVProcessor(def, EmguCVFunctions.ContourDetection);
                case "Watershed segmentation":  return new EmguCVProcessor(def, EmguCVFunctions.WatershedSegmentation);
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// Generic EmguCV processor wrapper - holds reference to function delegate
        /// </summary>
        private class EmguCVProcessor : IProcessingFunction
        {
            private AForgeFunctionDefinition _definition;
            private EmguCV_op _function;
            
            public EmguCVProcessor(AForgeFunctionDefinition definition, EmguCV_op function)
            {
                _definition = definition;
                _function = function;
            }
            
            public string Name => _definition.Name;
            
            public int ParameterInt
            {
                get => _definition.parameterInt;
                set => _definition.parameterInt = value;
            }
            
            public double ParameterDouble
            {
                get => _definition.parameterDouble;
                set => _definition.parameterDouble = value;
            }
            
            public double ParameterDoubleA
            {
                get => _definition.parameterDoubleA;
                set => _definition.parameterDoubleA = value;
            }
            
            public double ParameterDoubleB
            {
                get => _definition.parameterDoubleB;
                set => _definition.parameterDoubleB = value;
            }
            
            public double ParameterDoubleC
            {
                get => _definition.parameterDoubleC;
                set => _definition.parameterDoubleC = value;
            }
            
            public int R
            {
                get => _definition.R;
                set => _definition.R = value;
            }
            
            public int G
            {
                get => _definition.G;
                set => _definition.G = value;
            }
            
            public int B
            {
                get => _definition.B;
                set => _definition.B = value;
            }
            
            public Bitmap Process(Bitmap input)
            {
                // EmguCV functions modify by ref, need to work with same instance
                Bitmap frame = input;
                _function(ref frame, ParameterInt, ParameterDouble, R, G, B, 
                    ParameterDoubleA, ParameterDoubleB, ParameterDoubleC);
                return frame;
            }
        }

        /// <summary>
        /// Executes measurement using EmguCV algorithms
        /// Processes image through pipeline then performs feature detection
        /// </summary>
        public bool Measure(Bitmap image, 
                           List<IProcessingFunction> pipeline,
                           MeasurementParametersClass parameters,
                           double XmmPerPixel,
                           double YmmPerPixel,
                           out double X, 
                           out double Y, 
                           out double A,
                           out double XSizeMm,
                           out double YSizeMm,
                           bool DisplayResults)
        {
            X = Y = A = 0;
            XSizeMm = YSizeMm = 0;
            
            if (!IsAvailable)
            {
                _mainForm.DisplayText("EmguCV not available for measurement", 
                    System.Drawing.KnownColor.DarkRed);
                return false;
            }
            
            // Extract Hough parameters from the pipeline if a Hough step is present.
            // All other pipeline steps have already been applied to the bitmap before Measure() is called.
            HoughParams hough = ExtractHoughParams(pipeline, parameters, XmmPerPixel);
            // Use Contour path unless the user explicitly added "Hough circles (sub-pixel)".
            // Hough without tuned parameters fires false positives on everything.
            bool useContour = !HasHoughCircles(pipeline);

            try
            {
                using (Mat matImage = BitmapToMat(image))
                {
                    if (parameters.SearchRounds)
                    {
                        bool found = useContour
                            ? DetectCircles_Contour(matImage, pipeline, parameters, XmmPerPixel, YmmPerPixel, out X, out Y, out A, out XSizeMm, out YSizeMm, DisplayResults)
                            : DetectCircles_SubPixel(matImage, hough, parameters, XmmPerPixel, YmmPerPixel, out X, out Y, out A, out XSizeMm, out YSizeMm, DisplayResults);
                        if (found) return true;
                    }
                    if (parameters.SearchRectangles)
                    {
                        if (DetectRectangles_Precise(matImage, parameters, XmmPerPixel, YmmPerPixel, out X, out Y, out A, out XSizeMm, out YSizeMm, DisplayResults))
                            return true;
                    }
                    if (parameters.SearchComponentOutlines || parameters.SearchComponentPads)
                    {
                        if (DetectComponent_Contours(matImage, parameters, XmmPerPixel, YmmPerPixel, out X, out Y, out A, out XSizeMm, out YSizeMm, DisplayResults))
                            return true;
                    }

                    if (!parameters.SearchRounds && !parameters.SearchRectangles &&
                        !parameters.SearchComponentOutlines && !parameters.SearchComponentPads)
                    {
                        _mainForm.DisplayText("EmguCV: No search type selected",
                            System.Drawing.KnownColor.DarkOrange);
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                _mainForm.DisplayText($"EmguCV measurement error: {ex.Message}", 
                    System.Drawing.KnownColor.DarkRed);
                System.Diagnostics.Debug.WriteLine($"EmguCV Measure exception: {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// Convert System.Drawing.Bitmap to Emgu.CV.Mat
        /// </summary>
        private Mat BitmapToMat(Bitmap bitmap)
        {
            // Use BitmapExtension method to convert Bitmap to Mat
            return bitmap.ToMat();
        }

        // Carries the user-configured HoughCircles parameters extracted from the pipeline.
        private struct HoughParams
        {
            public HoughModes Method;   // Gradient or GradientAlt
            public double Dp;           // Accumulator resolution ratio
            public double MinDist;      // Minimum distance between centres in pixels
            public double Param1;       // Upper Canny threshold
            public double Param2;       // Accumulator centre threshold
            public int MinRadiusPx;
            public int MaxRadiusPx;
        }

        // Reads HoughCircles tuning values from the active pipeline step named "Hough circles (sub-pixel)".
        // If no such step exists (user is using a plain Threshold+Invert pipeline), sensible defaults
        // are derived purely from MeasurementParameters so detection still works without the step.
        private HoughParams ExtractHoughParams(List<IProcessingFunction> pipeline,
            MeasurementParametersClass parameters, double XmmPerPixel)
        {
            var p = new HoughParams
            {
                Method  = HoughModes.Gradient,
                Dp      = 1.0,
                MinDist = 0,        // 0 = auto (derived below)
                Param1  = 100.0,
                Param2  = 30.0,
                MinRadiusPx = Math.Max(1, (int)Math.Floor((parameters.Xmin / 2.0) / XmmPerPixel)),
                MaxRadiusPx = 0     // filled below
            };
            p.MaxRadiusPx = Math.Max(p.MinRadiusPx + 1,
                (int)Math.Ceiling((parameters.Xmax / 2.0) / XmmPerPixel));

            // Override from pipeline step if present
            if (pipeline != null)
            {
                foreach (IProcessingFunction f in pipeline)
                {
                    if (f == null || f.Name != "Hough circles (sub-pixel)") continue;
                    p.Method  = f.ParameterInt == 1 ? HoughModes.GradientAlt : HoughModes.Gradient;
                    p.Dp      = f.ParameterDouble > 0 ? f.ParameterDouble : 1.0;
                    p.MinDist = f.ParameterDoubleA; // 0 = auto
                    p.Param1  = f.ParameterDoubleB > 0 ? f.ParameterDoubleB : 100.0;
                    p.Param2  = f.ParameterDoubleC > 0 ? f.ParameterDoubleC : 30.0;
                    break;
                }
            }

            // Auto minDist: if user left it at 0, set to minRadius so concentric circles are suppressed.
            if (p.MinDist < 1.0)
                p.MinDist = p.MinRadiusPx;

            return p;
        }

        // Returns true if the pipeline contains a "Contour circles" step.
        private bool HasContourCircles(List<IProcessingFunction> pipeline)
        {
            if (pipeline == null) return false;
            foreach (IProcessingFunction f in pipeline)
                if (f != null && f.Name == "Contour circles") return true;
            return false;
        }

        // Returns true if the pipeline contains a "Hough circles (sub-pixel)" step.
        private bool HasHoughCircles(List<IProcessingFunction> pipeline)
        {
            if (pipeline == null) return false;
            foreach (IProcessingFunction f in pipeline)
                if (f != null && f.Name == "Hough circles (sub-pixel)") return true;
            return false;
        }
        
        /// <summary>
        /// Find all circle candidates in the pipeline-processed frame for display overlay.
        /// Uses the same contour + circularity logic as DetectCircles_SubPixel but returns every
        /// circular contour, classified so Camera can draw green/yellow/red accurately.
        /// Coordinates are in measurement-frame pixels (CameraResolution space).
        /// </summary>
        public List<EngineCircle> FindCirclesForDisplay(Bitmap processedFrame,
                                                         MeasurementParametersClass parameters,
                                                         double XmmPerPixel,
                                                         double YmmPerPixel)
        {
            return FindCirclesForDisplay(processedFrame, parameters, XmmPerPixel, YmmPerPixel, null);
        }

        public List<EngineCircle> FindCirclesForDisplay(Bitmap processedFrame,
                                                         MeasurementParametersClass parameters,
                                                         double XmmPerPixel,
                                                         double YmmPerPixel,
                                                         List<IProcessingFunction> pipeline)
        {
            var result = new List<EngineCircle>();
            if (!IsAvailable || processedFrame == null) return result;

            try
            {
                // If the pipeline contains "Contour circles" use that path.
                // If it contains "Hough circles (sub-pixel)" use Hough.
                // If neither is present (plain Threshold+Invert pipeline), default to
                // the Contour path - Hough without tuned parameters fires on everything.
                bool hasHough = false;
                if (pipeline != null)
                    foreach (IProcessingFunction f in pipeline)
                        if (f != null && f.Name == "Hough circles (sub-pixel)") { hasHough = true; break; }

                if (HasContourCircles(pipeline) || !hasHough)
                    return FindCirclesForDisplay_Contour(processedFrame, pipeline, parameters, XmmPerPixel, YmmPerPixel);

                HoughParams hp = ExtractHoughParams(pipeline, parameters, XmmPerPixel);

                using (Mat mat = BitmapToMat(processedFrame))
                {
                    Mat gray = new Mat();
                    if (mat.NumberOfChannels > 1)
                        CvInvoke.CvtColor(mat, gray, ColorConversion.Bgr2Gray);
                    else
                        gray = mat.Clone();

                    int centerX = mat.Width / 2;
                    int centerY = mat.Height / 2;

                    // The frame is already pipeline-processed (binary after Threshold+Invert).
                    // HoughCircles runs its own internal Canny, which needs gradient edges.
                    // A light 3x3 blur softens the 1-pixel binary boundary just enough.
                    // Use lower Param1/Param2 than the natural-image defaults because the
                    // binary edge gradient is shallow even after blurring.
                    Mat blurred = new Mat();
                    CvInvoke.GaussianBlur(gray, blurred, new System.Drawing.Size(3, 3), 0);
                    gray.Dispose();

                    double houghParam1 = hp.Param1 > 0 ? hp.Param1 : 30.0;   // lower Canny threshold for binary
                    double houghParam2 = hp.Param2 > 0 ? hp.Param2 : 15.0;   // lower accumulator threshold for binary

                    // Display: widen search by 50% each side so near-miss circles show as red
                    int displayMinR = Math.Max(1, (int)Math.Floor(hp.MinRadiusPx * 0.5));
                    int displayMaxR = (int)Math.Ceiling(hp.MaxRadiusPx * 1.5);

                    CircleF[] circles = CvInvoke.HoughCircles(blurred, hp.Method, hp.Dp,
                        hp.MinDist, houghParam1, houghParam2, displayMinR, displayMaxR);
                    blurred.Dispose();

                    int selectedIdx = -1;
                    double bestDist = double.MaxValue;
                    for (int i = 0; i < circles.Length; i++)
                    {
                        double d = circles[i].Radius * 2.0 * XmmPerPixel;
                        if (d < parameters.Xmin || d > parameters.Xmax) continue;
                        double xd = Math.Abs((circles[i].Center.X - centerX) * XmmPerPixel);
                        double yd = Math.Abs((circles[i].Center.Y - centerY) * YmmPerPixel);
                        if (xd > parameters.XUniqueDistance || yd > parameters.YUniqueDistance) continue;
                        double distPx = Math.Sqrt(Math.Pow(circles[i].Center.X - centerX, 2) +
                                                  Math.Pow(circles[i].Center.Y - centerY, 2));
                        if (distPx < bestDist) { bestDist = distPx; selectedIdx = i; }
                    }

                    for (int i = 0; i < circles.Length; i++)
                    {
                        double diameterMm = circles[i].Radius * 2.0 * XmmPerPixel;
                        bool passesSize = diameterMm >= parameters.Xmin && diameterMm <= parameters.Xmax;
                        double XdistMm = Math.Abs((circles[i].Center.X - centerX) * XmmPerPixel);
                        double YdistMm = Math.Abs((circles[i].Center.Y - centerY) * YmmPerPixel);
                        bool passesDist = XdistMm <= parameters.XUniqueDistance && YdistMm <= parameters.YUniqueDistance;
                        result.Add(new EngineCircle
                        {
                            CenterX = circles[i].Center.X,
                            CenterY = circles[i].Center.Y,
                            RadiusPx = circles[i].Radius,
                            DiameterMm = diameterMm,
                            PassesSize = passesSize,
                            PassesDistance = passesDist,
                            IsSelected = (i == selectedIdx)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _mainForm.DisplayText($"EmguCV FindCirclesForDisplay error: {ex.Message}", System.Drawing.KnownColor.DarkRed);
            }
            return result;
        }

        private bool DetectCircles_SubPixel(Mat image, HoughParams hp,
                                            MeasurementParametersClass parameters,
                                            double XmmPerPixel, double YmmPerPixel,
                                            out double X, out double Y, out double A,
                                            out double XSizeMm, out double YSizeMm,
                                            bool DisplayResults)
        {
            X = Y = A = 0;
            XSizeMm = YSizeMm = 0;

            try
            {
                Mat gray = new Mat();
                if (image.NumberOfChannels > 1)
                    CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);
                else
                    gray = image.Clone();

                int centerX = image.Width / 2;
                int centerY = image.Height / 2;

                Mat blurred = new Mat();
                CvInvoke.GaussianBlur(gray, blurred, new System.Drawing.Size(3, 3), 0);
                gray.Dispose();

                // Use lower thresholds when operating on a binary pipeline image.
                double houghParam1 = hp.Param1 > 0 ? hp.Param1 : 30.0;
                double houghParam2 = hp.Param2 > 0 ? hp.Param2 : 15.0;

                if (DisplayResults)
                    _mainForm.DisplayText(
                        $"EmguCV HoughCircles: method={hp.Method} dp={hp.Dp:F1} minDist={hp.MinDist:F0}px p1={houghParam1} p2={houghParam2} minR={hp.MinRadiusPx}px maxR={hp.MaxRadiusPx}px",
                        System.Drawing.KnownColor.DarkCyan);

                CircleF[] circles = CvInvoke.HoughCircles(blurred, hp.Method, hp.Dp,
                    hp.MinDist, houghParam1, houghParam2, hp.MinRadiusPx, hp.MaxRadiusPx);
                blurred.Dispose();

                if (DisplayResults)
                    _mainForm.DisplayText($"  {circles.Length} Hough circles found", System.Drawing.KnownColor.DarkCyan);

                var validCandidates = new List<System.Tuple<double, double, double>>();
                foreach (CircleF c in circles)
                {
                    double diameterMm = c.Radius * 2.0 * XmmPerPixel;
                    if (DisplayResults)
                        _mainForm.DisplayText($"    circle: cx={c.Center.X:F1} cy={c.Center.Y:F1} r={c.Radius:F1}px d={diameterMm:F3}mm",
                            System.Drawing.KnownColor.DarkGray);

                    if (diameterMm < parameters.Xmin || diameterMm > parameters.Xmax)
                        continue;

                    double XdistMm = Math.Abs((c.Center.X - centerX) * XmmPerPixel);
                    double YdistMm = Math.Abs((c.Center.Y - centerY) * YmmPerPixel);
                    if (XdistMm > parameters.XUniqueDistance || YdistMm > parameters.YUniqueDistance)
                        continue;

                    validCandidates.Add(System.Tuple.Create((double)c.Center.X, (double)c.Center.Y, diameterMm));
                }

                if (validCandidates.Count == 0)
                {
                    if (DisplayResults)
                        _mainForm.DisplayText(
                            $"EmguCV: no circles passed filters (Xmin={parameters.Xmin:F2}mm Xmax={parameters.Xmax:F2}mm XDist={parameters.XUniqueDistance:F2}mm YDist={parameters.YUniqueDistance:F2}mm)",
                            System.Drawing.KnownColor.DarkOrange);
                    return false;
                }

                // Pick circle closest to frame centre.
                validCandidates.Sort((a, b) =>
                {
                    double da = Math.Sqrt(Math.Pow(a.Item1 - centerX, 2) + Math.Pow(a.Item2 - centerY, 2));
                    double db = Math.Sqrt(Math.Pow(b.Item1 - centerX, 2) + Math.Pow(b.Item2 - centerY, 2));
                    return da.CompareTo(db);
                });

                if (validCandidates.Count > 1 && DisplayResults)
                    _mainForm.DisplayText(
                        $"EmguCV: {validCandidates.Count} circles passed filters - selecting closest to centre",
                        System.Drawing.KnownColor.DarkCyan);

                double bestCx = validCandidates[0].Item1;
                double bestCy = validCandidates[0].Item2;
                double bestDiameterMm = validCandidates[0].Item3;

                X = (bestCx - centerX) * XmmPerPixel;
                Y = (centerY - bestCy) * YmmPerPixel;
                A = 0.0;
                XSizeMm = bestDiameterMm;
                YSizeMm = bestDiameterMm;

                if (DisplayResults)
                    _mainForm.DisplayText($"EmguCV Circle: X={X:F3}mm Y={Y:F3}mm D={bestDiameterMm:F3}mm",
                        System.Drawing.KnownColor.DarkGreen);

                return true;
            }
            catch (Exception ex)
            {
                _mainForm.DisplayText($"EmguCV circle detection error: {ex.Message}", System.Drawing.KnownColor.DarkRed);
                return false;
            }
        }
        
        /// <summary>
        /// Contour-based circle detection. Works on the already-processed pipeline image
        /// (Threshold+Invert, Canny, etc.). Circularity threshold from the "Contour circles"
        /// step's ParameterDouble (default 0.70).
        /// Edge images (Canny): uses MinEnclosingCircle radius + arc-coverage guard.
        /// Filled images (Threshold+Invert): uses area-based radius + 4*pi*A/P^2 circularity.
        /// Image type is detected by fillRatio of the processed image.
        /// </summary>
        private bool DetectCircles_Contour(Mat image, List<IProcessingFunction> pipeline,
                                           MeasurementParametersClass parameters,
                                           double XmmPerPixel, double YmmPerPixel,
                                           out double X, out double Y, out double A,
                                           out double XSizeMm, out double YSizeMm,
                                           bool DisplayResults)
        {
            X = Y = A = 0;
            XSizeMm = YSizeMm = 0;
            try
            {
                double circThreshold = 0.70;
                if (pipeline != null)
                    foreach (IProcessingFunction f in pipeline)
                        if (f != null && f.Name == "Contour circles" && f.ParameterDouble > 0)
                        { circThreshold = f.ParameterDouble; break; }

                Mat gray = new Mat();
                if (image.NumberOfChannels > 1)
                    CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);
                else
                    gray = image.Clone();

                // If Blur or Noise reduction was applied after Invert, the binary image
                // has softened edges. Re-threshold at 128 to restore clean binary boundaries
                // so FindContours sees exactly one clean ring per blob.
                Mat binary = new Mat();
                CvInvoke.Threshold(gray, binary, 128, 255, ThresholdType.Binary);
                gray.Dispose();

                int centerX = image.Width / 2;
                int centerY = image.Height / 2;
                int frameW = image.Width;
                int frameH = image.Height;

                double totalPixels = frameW * frameH;
                double fillRatio = CvInvoke.CountNonZero(binary) / totalPixels;
                bool isEdgeImage = fillRatio < 0.15;

                if (DisplayResults)
                    _mainForm.DisplayText(
                        $"EmguCV ContourCircles: fill={fillRatio:P1} mode={(isEdgeImage ? "edge" : "filled")} circMin={circThreshold:F2}",
                        System.Drawing.KnownColor.DarkCyan);

                double minPerimeterPx = Math.Max(10.0, Math.PI * (parameters.Xmin / XmmPerPixel));

                // Step 1: collect all circular contours that pass circularity and arc-coverage.
                // Store as (cx, cy, radiusPx, diameterMm, circularity).
                var allCircles = new List<System.Tuple<double, double, double, double, double>>();

                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(binary, contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
                    binary.Dispose();

                    for (int i = 0; i < contours.Size; i++)
                    {
                        using (VectorOfPoint contour = contours[i])
                        {
                            System.Drawing.Rectangle bb = CvInvoke.BoundingRectangle(contour);
                            if (bb.X <= 1 || bb.Y <= 1 || bb.Right >= frameW - 1 || bb.Bottom >= frameH - 1) continue;

                            double perimeter = CvInvoke.ArcLength(contour, true);
                            if (perimeter < minPerimeterPx) continue;

                            double area = CvInvoke.ContourArea(contour);
                            CircleF enc = CvInvoke.MinEnclosingCircle(contour);
                            double encR = enc.Radius;

                            double radiusPx, circularity;
                            if (isEdgeImage)
                            {
                                radiusPx = encR;
                                double ideal = 2.0 * Math.PI * encR;
                                circularity = ideal > 0 ? Math.Min(1.0, ideal / perimeter) : 0;
                            }
                            else
                            {
                                radiusPx = Math.Sqrt(area / Math.PI);
                                circularity = (4.0 * Math.PI * area) / (perimeter * perimeter);
                            }

                            if (circularity < circThreshold) continue;

                            if (isEdgeImage)
                            {
                                double arcCoverage = contour.Size / (2.0 * Math.PI * encR);
                                if (arcCoverage < ArcCoverageThreshold) continue;
                            }

                            var mom = CvInvoke.Moments(contour);
                            if (mom.M00 < 1) continue;
                            double cx = mom.M10 / mom.M00;
                            double cy = mom.M01 / mom.M00;
                            double diameterMm = radiusPx * 2.0 * XmmPerPixel;

                            allCircles.Add(System.Tuple.Create(cx, cy, radiusPx, diameterMm, circularity));
                        }
                    }
                }

                // Log all raw candidates in the same format as AForge.
                if (DisplayResults)
                {
                    _mainForm.DisplayText("Result candidates:");
                    _mainForm.DisplayText("Circles:");
                    if (allCircles.Count == 0)
                    {
                        _mainForm.DisplayText("    No results.");
                    }
                    else
                    {
                        _mainForm.DisplayText("Position, pxls|mm               |Size, pxls   |mm         |Circ");
                        foreach (var c in allCircles)
                        {
                            double dxPx = c.Item1 - centerX;
                            double dyPx = centerY - c.Item2;
                            _mainForm.DisplayText(string.Format(
                                "{0,7:0.0},{1,7:0.0}|{2,8:0.000},{3,8:0.000}|{4,6:0.0},{5,6:0.0}|{6,5:0.00},{7,5:0.00}|{8,5:0.00}",
                                dxPx, dyPx,
                                dxPx * XmmPerPixel, dyPx * YmmPerPixel,
                                c.Item3 * 2, c.Item3 * 2,
                                c.Item4, c.Item4,
                                c.Item5));
                        }
                    }
                }

                // Step 2: filter for size.
                var sizeFiltered = new List<System.Tuple<double, double, double, double, double>>();
                foreach (var c in allCircles)
                    if (c.Item4 >= parameters.Xmin && c.Item4 <= parameters.Xmax)
                        sizeFiltered.Add(c);

                if (DisplayResults)
                {
                    _mainForm.DisplayText("");
                    _mainForm.DisplayText(string.Format(
                        "Filtered for size (Xmin: {0:0.000}, Xmax: {1:0.000}, Ymin: {2:0.000}, Ymax: {3:0.000}), results:",
                        parameters.Xmin, parameters.Xmax, parameters.Ymin, parameters.Ymax));
                    if (sizeFiltered.Count == 0)
                    {
                        _mainForm.DisplayText("    No results.");
                        // Show closest miss to help user tune parameters
                        if (allCircles.Count > 0)
                        {
                            var best = allCircles[0];
                            foreach (var c in allCircles)
                                if (c.Item5 > best.Item5) best = c;
                            _mainForm.DisplayText(string.Format(
                                "    Best candidate: d={0:0.000}mm circ={1:0.00} (Xmin={2:0.000} Xmax={3:0.000})",
                                best.Item4, best.Item5, parameters.Xmin, parameters.Xmax),
                                System.Drawing.KnownColor.DarkOrange);
                        }
                    }
                    else
                    {
                        _mainForm.DisplayText("Position, pxls|mm               |Size, pxls   |mm         |Circ");
                        foreach (var c in sizeFiltered)
                        {
                            double dxPx = c.Item1 - centerX;
                            double dyPx = centerY - c.Item2;
                            _mainForm.DisplayText(string.Format(
                                "{0,7:0.0},{1,7:0.0}|{2,8:0.000},{3,8:0.000}|{4,6:0.0},{5,6:0.0}|{6,5:0.00},{7,5:0.00}|{8,5:0.00}",
                                dxPx, dyPx,
                                dxPx * XmmPerPixel, dyPx * YmmPerPixel,
                                c.Item3 * 2, c.Item3 * 2,
                                c.Item4, c.Item4,
                                c.Item5));
                        }
                    }
                }

                if (sizeFiltered.Count == 0)
                    return false;

                // Step 3: filter for distance.
                var distFiltered = new List<System.Tuple<double, double, double, double, double>>();
                foreach (var c in sizeFiltered)
                {
                    double XdistMm = Math.Abs((c.Item1 - centerX) * XmmPerPixel);
                    double YdistMm = Math.Abs((c.Item2 - centerY) * YmmPerPixel);
                    if (XdistMm <= parameters.XUniqueDistance && YdistMm <= parameters.YUniqueDistance)
                        distFiltered.Add(c);
                }

                if (DisplayResults)
                {
                    _mainForm.DisplayText("");
                    _mainForm.DisplayText(string.Format(
                        "Filtered for distance (Xmax dist.: {0:0.000}, Ymax dist.: {1:0.000}), results:",
                        parameters.XUniqueDistance, parameters.YUniqueDistance));
                    if (distFiltered.Count == 0)
                    {
                        _mainForm.DisplayText("    No results.");
                    }
                    else
                    {
                        _mainForm.DisplayText("Position, pxls|mm               |Size, pxls   |mm         |Circ");
                        foreach (var c in distFiltered)
                        {
                            double dxPx = c.Item1 - centerX;
                            double dyPx = centerY - c.Item2;
                            _mainForm.DisplayText(string.Format(
                                "{0,7:0.0},{1,7:0.0}|{2,8:0.000},{3,8:0.000}|{4,6:0.0},{5,6:0.0}|{6,5:0.00},{7,5:0.00}|{8,5:0.00}",
                                dxPx, dyPx,
                                dxPx * XmmPerPixel, dyPx * YmmPerPixel,
                                c.Item3 * 2, c.Item3 * 2,
                                c.Item4, c.Item4,
                                c.Item5));
                        }
                    }
                }

                if (distFiltered.Count == 0)
                    return false;

                // Select: if multiple pass all filters, pick smallest diameter (most precise feature).
                distFiltered.Sort((a, b) => a.Item4.CompareTo(b.Item4));

                if (distFiltered.Count > 1)
                {
                    double smallest = distFiltered[0].Item4;
                    double second   = distFiltered[1].Item4;
                    if ((second - smallest) / smallest < 0.05)
                    {
                        _mainForm.DisplayText(
                            $"EmguCV ContourCircles: result is not unique ({distFiltered.Count} matches)",
                            System.Drawing.KnownColor.Red);
                        return false;
                    }
                }

                double bestCx  = distFiltered[0].Item1;
                double bestCy  = distFiltered[0].Item2;
                double bestDiam = distFiltered[0].Item4;

                X = (bestCx - centerX) * XmmPerPixel;
                Y = (centerY - bestCy) * YmmPerPixel;
                A = 0.0;
                XSizeMm = bestDiam;
                YSizeMm = bestDiam;

                return true;
            }
            catch (Exception ex)
            {
                _mainForm.DisplayText($"EmguCV ContourCircles error: {ex.Message}", System.Drawing.KnownColor.DarkRed);
                return false;
            }
        }

        // Display overlay variant of contour circle detection.
        private List<EngineCircle> FindCirclesForDisplay_Contour(Bitmap processedFrame,
            List<IProcessingFunction> pipeline, MeasurementParametersClass parameters,
            double XmmPerPixel, double YmmPerPixel)
        {
            var result = new List<EngineCircle>();
            try
            {
                double circThreshold = 0.70;
                if (pipeline != null)
                    foreach (IProcessingFunction f in pipeline)
                        if (f != null && f.Name == "Contour circles" && f.ParameterDouble > 0)
                        { circThreshold = f.ParameterDouble; break; }

                using (Mat mat = BitmapToMat(processedFrame))
                {
                    Mat gray = new Mat();
                    if (mat.NumberOfChannels > 1)
                        CvInvoke.CvtColor(mat, gray, ColorConversion.Bgr2Gray);
                    else
                        gray = mat.Clone();

                    // Re-threshold to restore clean binary edges in case Blur/Noise reduction
                    // was applied after Invert (which softens the boundary FindContours needs).
                    Mat binary = new Mat();
                    CvInvoke.Threshold(gray, binary, 128, 255, ThresholdType.Binary);
                    gray.Dispose();

                    int centerX = mat.Width / 2;
                    int centerY = mat.Height / 2;
                    int frameW = mat.Width;
                    int frameH = mat.Height;

                    double fillRatio = CvInvoke.CountNonZero(binary) / (double)(frameW * frameH);
                    bool isEdgeImage = fillRatio < 0.15;
                    double minPerimeterPx = Math.Max(10.0, Math.PI * (parameters.Xmin / XmmPerPixel));

                    // Find which candidate would be selected (smallest passing diameter)
                    int selectedIdx = -1;
                    double smallestPassingDiam = double.MaxValue;

                    var candidates = new List<EngineCircle>();
                    using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                    {
                        CvInvoke.FindContours(binary, contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
                        binary.Dispose();

                        for (int i = 0; i < contours.Size; i++)
                        {
                            using (VectorOfPoint contour = contours[i])
                            {
                                // Reject frame-border contours (image edge artefacts, not circles)
                                System.Drawing.Rectangle bb = CvInvoke.BoundingRectangle(contour);
                                if (bb.X <= 1 || bb.Y <= 1 || bb.Right >= frameW - 1 || bb.Bottom >= frameH - 1) continue;

                                double perimeter = CvInvoke.ArcLength(contour, true);
                                if (perimeter < minPerimeterPx) continue;

                                double area = CvInvoke.ContourArea(contour);
                                CircleF enc = CvInvoke.MinEnclosingCircle(contour);
                                double encR = enc.Radius;

                                double radiusPx, circularity;
                                if (isEdgeImage)
                                {
                                    radiusPx = encR;
                                    double ideal = 2.0 * Math.PI * encR;
                                    circularity = ideal > 0 ? Math.Min(1.0, ideal / perimeter) : 0;
                                }
                                else
                                {
                                    radiusPx = Math.Sqrt(area / Math.PI);
                                    circularity = (4.0 * Math.PI * area) / (perimeter * perimeter);
                                }

                                if (circularity < circThreshold) continue;

                                if (isEdgeImage && contour.Size / (2.0 * Math.PI * encR) < ArcCoverageThreshold) continue;

                                var mom = CvInvoke.Moments(contour);
                                if (mom.M00 < 1) continue;
                                double cx = mom.M10 / mom.M00;
                                double cy = mom.M01 / mom.M00;
                                double diameterMm = radiusPx * 2.0 * XmmPerPixel;

                                if (diameterMm > parameters.Xmax) continue;

                                bool passesSize = diameterMm >= parameters.Xmin && diameterMm <= parameters.Xmax;
                                bool passesCirc = circularity >= circThreshold;
                                double XdistMm = Math.Abs((cx - centerX) * XmmPerPixel);
                                double YdistMm = Math.Abs((cy - centerY) * YmmPerPixel);
                                bool passesDist = XdistMm <= parameters.XUniqueDistance && YdistMm <= parameters.YUniqueDistance;

                                int idx = candidates.Count;
                                candidates.Add(new EngineCircle
                                {
                                    CenterX = cx, CenterY = cy,
                                    RadiusPx = radiusPx, DiameterMm = diameterMm,
                                    PassesSize = passesSize && passesCirc,
                                    PassesDistance = passesDist,
                                    IsSelected = false
                                });

                                if (passesSize && passesCirc && passesDist && diameterMm < smallestPassingDiam)
                                { smallestPassingDiam = diameterMm; selectedIdx = idx; }
                            }
                        }
                    }

                    for (int i = 0; i < candidates.Count; i++)
                    {
                        var c = candidates[i];
                        c.IsSelected = (i == selectedIdx);
                        result.Add(c);
                    }
                }
            }
            catch (Exception ex)
            {
                _mainForm.DisplayText($"EmguCV ContourCircles display error: {ex.Message}", System.Drawing.KnownColor.DarkRed);
            }
            return result;
        }

        /// <summary>
        /// Detect rectangles using contour detection with minimum area rectangle fitting
        /// </summary>
        private bool DetectRectangles_Precise(Mat image, MeasurementParametersClass parameters,
                                              double XmmPerPixel, double YmmPerPixel,
                                              out double X, out double Y, out double A,
                                              out double XSizeMm, out double YSizeMm,
                                              bool DisplayResults)
        {
            X = Y = A = 0;
            XSizeMm = YSizeMm = 0;
            
            try
            {
                // The image has already been processed by the user's pipeline (Threshold, Invert, Canny, etc.).
                // FindContours needs a binary (single-channel) image.
                // If the pipeline included Canny the image is already an edge image - use it directly.
                // If not (e.g. only Threshold+Invert), it is already binary - also fine for FindContours.
                Mat gray = new Mat();
                if (image.NumberOfChannels > 1)
                {
                    CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);
                }
                else
                {
                    gray = image.Clone();
                }
                
                // Find contours on the pipeline-processed image
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(gray, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                    gray.Dispose();
                    
                    if (contours.Size == 0)
                    {
                        if (DisplayResults)
                        {
                            _mainForm.DisplayText("EmguCV: No contours found for rectangle detection", 
                                System.Drawing.KnownColor.DarkOrange);
                        }
                        return false;
                    }
                    
                    // Find best rectangle - all comparisons in mm to match AForge filter logic
                    int centerX = image.Width / 2;
                    int centerY = image.Height / 2;
                    double bestDist = double.MaxValue;
                    RotatedRect bestRect = new RotatedRect();
                    double bestXSizeMm = 0, bestYSizeMm = 0;
                    bool foundAny = false;
                    
                    for (int i = 0; i < contours.Size; i++)
                    {
                        using (VectorOfPoint contour = contours[i])
                        {
                            // Fit minimum area rectangle to get width/height in pixels
                            RotatedRect rect = CvInvoke.MinAreaRect(contour);
                            
                            // Convert pixel size to mm (matching AForge Measure() filter)
                            double XsizeMm = Math.Max(rect.Size.Width, rect.Size.Height) * XmmPerPixel;
                            double YsizeMm = Math.Min(rect.Size.Width, rect.Size.Height) * YmmPerPixel;
                            
                            // Check size in mm (Bug 4 fix: was comparing mm params to pixel area)
                            if (XsizeMm < parameters.Xmin || XsizeMm > parameters.Xmax ||
                                YsizeMm < parameters.Ymin || YsizeMm > parameters.Ymax)
                            {
                                continue;
                            }
                            
                            // Check distance from center in mm (Bug 5 fix: was comparing mm to pixels)
                            double dx = rect.Center.X - centerX;
                            double dy = rect.Center.Y - centerY;
                            double XdistMm = Math.Abs(dx * XmmPerPixel);
                            double YdistMm = Math.Abs(dy * YmmPerPixel);
                            
                            if (XdistMm > parameters.XUniqueDistance || YdistMm > parameters.YUniqueDistance)
                            {
                                continue;
                            }
                            
                            double distPixels = Math.Sqrt(dx * dx + dy * dy);
                            if (distPixels < bestDist)
                            {
                                bestDist = distPixels;
                                bestRect = rect;
                                bestXSizeMm = XsizeMm;
                                bestYSizeMm = YsizeMm;
                                foundAny = true;
                            }
                        }
                    }
                    
                    if (!foundAny)
                    {
                        if (DisplayResults)
                        {
                            _mainForm.DisplayText($"EmguCV: {contours.Size} contours found, none passed size/distance filters " +
                                $"(Xmin={parameters.Xmin:F2}mm, Xmax={parameters.Xmax:F2}mm, " +
                                $"Ymin={parameters.Ymin:F2}mm, Ymax={parameters.Ymax:F2}mm, " +
                                $"XmaxDist={parameters.XUniqueDistance:F2}mm, YmaxDist={parameters.YUniqueDistance:F2}mm)",
                                System.Drawing.KnownColor.DarkOrange);
                        }
                        return false;
                    }
                    
                    // Bug 6 fix: Return coordinates in mm (matching AForge output), not raw pixels
                    X = (bestRect.Center.X - centerX) * XmmPerPixel;
                    Y = (centerY - bestRect.Center.Y) * YmmPerPixel;  // Flip Y: image coords are top-down
                    A = bestRect.Angle;
                    XSizeMm = bestXSizeMm;
                    YSizeMm = bestYSizeMm;

                    return true;
                }
            }
            catch (Exception ex)
            {
                _mainForm.DisplayText($"EmguCV rectangle detection error: {ex.Message}", 
                    System.Drawing.KnownColor.DarkRed);
                return false;
            }
        }
        
        /// <summary>
        /// Detect component using advanced contour analysis
        /// </summary>
        private bool DetectComponent_Contours(Mat image, MeasurementParametersClass parameters,
                                              double XmmPerPixel, double YmmPerPixel,
                                              out double X, out double Y, out double A,
                                              out double XSizeMm, out double YSizeMm,
                                              bool DisplayResults)
        {
            // For now, use rectangle detection as component detection is similar
            // Future: Add more sophisticated shape analysis
            return DetectRectangles_Precise(image, parameters, XmmPerPixel, YmmPerPixel, out X, out Y, out A, out XSizeMm, out YSizeMm, DisplayResults);
        }
    }
}
