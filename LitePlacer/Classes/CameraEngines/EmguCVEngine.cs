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
            
            try
            {
                // Convert Bitmap to Mat (EmguCV native format)
                using (Mat matImage = BitmapToMat(image))
                {
                    // Process image through pipeline (already done in GetMeasurementFrame, but pipeline is empty here)
                    // The processed frame comes in as 'image' parameter
                    
                    // Try all checked search types - matches AForge behaviour which builds
                    // a combined candidates list from all enabled types.
                    if (parameters.SearchRounds)
                    {
                        if (DetectCircles_SubPixel(matImage, parameters, XmmPerPixel, YmmPerPixel, out X, out Y, out A, out XSizeMm, out YSizeMm, DisplayResults))
                            return true;
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
            var result = new List<EngineCircle>();
            if (!IsAvailable || processedFrame == null) return result;

            try
            {
                using (Mat mat = BitmapToMat(processedFrame))
                {
                    Mat gray = new Mat();
                    if (mat.NumberOfChannels > 1)
                        CvInvoke.CvtColor(mat, gray, ColorConversion.Bgr2Gray);
                    else
                        gray = mat.Clone();

                    int centerX = mat.Width / 2;
                    int centerY = mat.Height / 2;

                    // Determine edge vs filled image (same logic as DetectCircles_SubPixel)
                    bool isEdgeImage = true;
                    using (VectorOfVectorOfPoint testContours = new VectorOfVectorOfPoint())
                    {
                        CvInvoke.FindContours(gray.Clone(), testContours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                        double maxArea = 0;
                        for (int t = 0; t < testContours.Size; t++)
                        {
                            using (VectorOfPoint tc = testContours[t])
                            {
                                double a = CvInvoke.ContourArea(tc);
                                if (a > maxArea)
                                {
                                    maxArea = a;
                                    CircleF enc = CvInvoke.MinEnclosingCircle(tc);
                                    double encArea = Math.PI * enc.Radius * enc.Radius;
                                    isEdgeImage = encArea < 1 || (maxArea / encArea) < 0.75;
                                }
                            }
                        }
                    }

                    // For edge images use RetrType.List here (not External) so we see ALL rings —
                    // the display wants to show every contour classified, not just the outermost.
                    using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                    {
                        CvInvoke.FindContours(gray, contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
                        gray.Dispose();

                        double minPerimeterPx = Math.PI * (parameters.Xmin / XmmPerPixel);

                        // Collect every sufficiently circular contour
                        var candidates = new List<EngineCircle>();
                        for (int i = 0; i < contours.Size; i++)
                        {
                            using (VectorOfPoint contour = contours[i])
                            {
                                double perimeter = CvInvoke.ArcLength(contour, true);
                                if (perimeter < minPerimeterPx) continue;

                                double area = CvInvoke.ContourArea(contour);
                                CircleF encCircle = CvInvoke.MinEnclosingCircle(contour);
                                double encRadiusPx = encCircle.Radius;

                                double radiusPx;
                                double circularity;
                                if (isEdgeImage)
                                {
                                    radiusPx = encRadiusPx;
                                    double idealPerimeter = 2.0 * Math.PI * encRadiusPx;
                                    circularity = idealPerimeter > 0 ? Math.Min(1.0, idealPerimeter / perimeter) : 0;
                                }
                                else
                                {
                                    radiusPx = Math.Sqrt(area / Math.PI);
                                    circularity = (4.0 * Math.PI * area) / (perimeter * perimeter);
                                }

                                if (circularity < 0.6) continue;

                                var moments = CvInvoke.Moments(contour);
                                if (moments.M00 < 1) continue;
                                double cx = moments.M10 / moments.M00;
                                double cy = moments.M01 / moments.M00;
                                double diameterMm = radiusPx * 2.0 * XmmPerPixel;

                                bool passesSize = diameterMm >= parameters.Xmin && diameterMm <= parameters.Xmax;
                                double XdistMm = Math.Abs((cx - centerX) * XmmPerPixel);
                                double YdistMm = Math.Abs((cy - centerY) * YmmPerPixel);
                                bool passesDist = XdistMm <= parameters.XUniqueDistance && YdistMm <= parameters.YUniqueDistance;

                                candidates.Add(new EngineCircle
                                {
                                    CenterX = cx,
                                    CenterY = cy,
                                    RadiusPx = radiusPx,
                                    DiameterMm = diameterMm,
                                    PassesSize = passesSize,
                                    PassesDistance = passesDist,
                                    IsSelected = false  // set below
                                });
                            }
                        }

                        // Identify the selected candidate: the one that passes both filters and is
                        // smallest (edge) or closest to centre (filled) — matches DetectCircles_SubPixel.
                        int selectedIdx = -1;
                        double bestDiameterForEdge = double.MaxValue;
                        double bestDist = double.MaxValue;
                        for (int i = 0; i < candidates.Count; i++)
                        {
                            var c = candidates[i];
                            if (!c.PassesSize || !c.PassesDistance) continue;
                            double distPx = Math.Sqrt(
                                (c.CenterX - centerX) * (c.CenterX - centerX) +
                                (c.CenterY - centerY) * (c.CenterY - centerY));
                            bool isBetter = isEdgeImage
                                ? c.DiameterMm < bestDiameterForEdge
                                : distPx < bestDist;
                            if (isBetter)
                            {
                                bestDiameterForEdge = c.DiameterMm;
                                bestDist = distPx;
                                selectedIdx = i;
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
            }
            catch (Exception ex)
            {
                _mainForm.DisplayText($"EmguCV FindCirclesForDisplay error: {ex.Message}", System.Drawing.KnownColor.DarkRed);
            }
            return result;
        }

        /// <summary>
        /// Detect circles using contour analysis - matches AForge FindCirclesFunct() behaviour.
        /// AForge uses BlobCounter + SimpleShapeChecker.IsCircle() on the pipeline-processed image.
        /// We do the same with FindContours + circularity check (4*pi*area/perimeter^2).
        /// </summary>
        private bool DetectCircles_SubPixel(Mat image, MeasurementParametersClass parameters,
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

                // Detect whether the pipeline produced an edge image (Canny/Sobel) or a filled binary image
                // (Threshold+Invert). This drives which radius estimator is correct:
                //   Filled image -> radius = sqrt(area / pi)   [area of the filled disc - most accurate]
                //   Edge image   -> radius = perimeter / (2*pi)[arc length of the ring]
                //
                // The filled-disc area estimator is always preferred when available because it averages
                // out pixel stairstepping across the whole disc boundary rather than amplifying it via
                // perimeter measurement.
                //
                // Detection method: compare the largest single contour's area to its enclosing circle area.
                // A filled disc has contourArea / enclosingCircleArea > 0.85 (nearly fills the circle).
                // A Canny ring has contourArea / enclosingCircleArea << 0.1 (ring pixels vs filled area).
                // This is robust regardless of the dot's size relative to the frame.
                double totalPixels = image.Width * image.Height;
                MCvScalar nonZeroCount = new MCvScalar(CvInvoke.CountNonZero(gray));
                double fillRatio = nonZeroCount.V0 / totalPixels;

                // Find largest contour to test filled vs edge
                bool isEdgeImage = true;
                using (VectorOfVectorOfPoint testContours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(gray.Clone(), testContours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                    double maxArea = 0;
                    for (int t = 0; t < testContours.Size; t++)
                    {
                        using (VectorOfPoint tc = testContours[t])
                        {
                            double a = CvInvoke.ContourArea(tc);
                            if (a > maxArea)
                            {
                                maxArea = a;
                                CircleF enc = CvInvoke.MinEnclosingCircle(tc);
                                double encArea = Math.PI * enc.Radius * enc.Radius;
                                // Filled disc fills > 75% of its enclosing circle area
                                isEdgeImage = encArea < 1 || (maxArea / encArea) < 0.75;
                            }
                        }
                    }
                }

                if (DisplayResults)
                    _mainForm.DisplayText($"EmguCV Circles: image={image.Width}x{image.Height}, fill={fillRatio:P1}, mode={(isEdgeImage ? "edge" : "filled")}, XmmPerPix={XmmPerPixel:F4}",
                        System.Drawing.KnownColor.DarkCyan);

                // ChainApproxNone: keep every boundary pixel for accurate perimeter and centroid.
                // Edge images (Canny): use RetrType.External to suppress the inner concentric ring
                // that Canny produces for the bright halo — only the outermost ring is needed.
                // Filled images: RetrType.List is fine (single blob per feature).
                RetrType retrieval = isEdgeImage ? RetrType.External : RetrType.List;
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(gray, contours, null, retrieval, ChainApproxMethod.ChainApproxNone);
                    gray.Dispose();

                    if (DisplayResults)
                        _mainForm.DisplayText($"  {contours.Size} raw contours found", System.Drawing.KnownColor.DarkCyan);

                    // Minimum perimeter guard based on Xmin: contours shorter than the
                    // circumference of a circle of diameter Xmin are too small to be the target.
                    double minPerimeterPx = Math.PI * (parameters.Xmin / XmmPerPixel); // pi * d

                    // Edge images: pick the SMALLEST valid circle (the hole, not the surrounding halo).
                    // Filled images: pick the CLOSEST valid circle to the frame centre.
                    double bestDist = double.MaxValue;
                    double bestDiameterForEdge = double.MaxValue;
                    double bestCx = 0, bestCy = 0, bestDiameterMm = 0;
                    bool foundValid = false;

                    // Diagnostic: track the most circular candidate regardless of size/distance
                    // filters so we can log what the algorithm actually sees when nothing passes.
                    double diagBestCircularity = 0;
                    double diagBestDiameterMm = 0;
                    double diagBestDist = double.MaxValue;

                    for (int i = 0; i < contours.Size; i++)
                    {
                        using (VectorOfPoint contour = contours[i])
                        {
                            double perimeter = CvInvoke.ArcLength(contour, true);
                            if (perimeter < minPerimeterPx) continue;

                            double area = CvInvoke.ContourArea(contour);
                            CircleF encCircle = CvInvoke.MinEnclosingCircle(contour);
                            double encRadiusPx = encCircle.Radius;

                            double radiusPx;
                            double circularity;

                            if (isEdgeImage)
                            {
                                // Edge image (Canny ring): use MinEnclosingCircle radius as the size estimator.
                                // It is immune to jagged perimeter noise that inflates perimeter/(2?).
                                // Circularity: compare ideal perimeter (2?·r) to actual perimeter.
                                // A perfect circle ? ratio=1.0. Noise/non-circle ? <1.0.
                                // Clamped to [0,1] to prevent >1 from sub-pixel jaggedness.
                                radiusPx = encRadiusPx;
                                double idealPerimeter = 2.0 * Math.PI * encRadiusPx;
                                circularity = idealPerimeter > 0
                                    ? Math.Min(1.0, idealPerimeter / perimeter)
                                    : 0;
                            }
                            else
                            {
                                // Filled image: standard 4?A/P² circularity, area-based radius.
                                radiusPx = Math.Sqrt(area / Math.PI);
                                circularity = (4.0 * Math.PI * area) / (perimeter * perimeter);
                            }

                            double diameterMm = radiusPx * 2.0 * XmmPerPixel;

                            if (circularity < 0.6) continue;

                            if (DisplayResults)
                                _mainForm.DisplayText($"    contour {i}: d={diameterMm:F3}mm circ={circularity:F2}", System.Drawing.KnownColor.DarkGray);

                            // Track best circular candidate for diagnostics (before size/distance filter)
                            var momDiag = CvInvoke.Moments(contour);
                            if (momDiag.M00 >= 1)
                            {
                                double dxDiag = (momDiag.M10 / momDiag.M00) - centerX;
                                double dyDiag = (momDiag.M01 / momDiag.M00) - centerY;
                                double distDiag = Math.Sqrt(dxDiag * dxDiag + dyDiag * dyDiag);
                                if (circularity > diagBestCircularity || (circularity >= diagBestCircularity && distDiag < diagBestDist))
                                {
                                    diagBestCircularity = circularity;
                                    diagBestDiameterMm = diameterMm;
                                    diagBestDist = distDiag;
                                }
                            }

                            // Size filter
                            if (diameterMm < parameters.Xmin || diameterMm > parameters.Xmax)
                                continue;

                            // Sub-pixel centroid from moments
                            var moments = CvInvoke.Moments(contour);
                            if (moments.M00 < 1) continue;
                            double cx = moments.M10 / moments.M00;
                            double cy = moments.M01 / moments.M00;

                            // Distance filter
                            double XdistMm = Math.Abs((cx - centerX) * XmmPerPixel);
                            double YdistMm = Math.Abs((cy - centerY) * YmmPerPixel);
                            if (XdistMm > parameters.XUniqueDistance || YdistMm > parameters.YUniqueDistance)
                                continue;

                            double distPx = Math.Sqrt((cx - centerX) * (cx - centerX) + (cy - centerY) * (cy - centerY));

                            // Edge images: prefer smallest diameter (hole, not halo).
                            // Filled images: prefer closest to centre.
                            bool isBetter = isEdgeImage
                                ? (diameterMm < bestDiameterForEdge)
                                : (distPx < bestDist);

                            if (isBetter)
                            {
                                bestDist = distPx;
                                bestDiameterForEdge = diameterMm;
                                bestCx = cx;
                                bestCy = cy;
                                bestDiameterMm = diameterMm;
                                foundValid = true;
                            }
                        }
                    }

                    if (!foundValid)
                    {
                        if (DisplayResults)
                        {
                            string diagHint = diagBestCircularity > 0
                                ? $" Best candidate: Diameter={diagBestDiameterMm:F3}mm circularity={diagBestCircularity:F2} dist={diagBestDist * XmmPerPixel:F2}mm"
                                : " No circular contours found above circularity threshold.";
                            _mainForm.DisplayText(
                                $"EmguCV: no circles passed filters (Xmin={parameters.Xmin:F2}mm Xmax={parameters.Xmax:F2}mm XDist={parameters.XUniqueDistance:F2}mm YDist={parameters.YUniqueDistance:F2}mm).{diagHint}",
                                System.Drawing.KnownColor.DarkOrange);
                        }
                        return false;
                    }

                    X = (bestCx - centerX) * XmmPerPixel;
                    Y = (centerY - bestCy) * YmmPerPixel;
                    A = 0.0;
                    XSizeMm = bestDiameterMm;
                    YSizeMm = bestDiameterMm;

                    if (DisplayResults)
                        _mainForm.DisplayText($"EmguCV Circle: X={X:F3}mm, Y={Y:F3}mm, Diameter={bestDiameterMm:F3}mm", System.Drawing.KnownColor.DarkGreen);

                    return true;
                }
            }
            catch (Exception ex)
            {
                _mainForm.DisplayText($"EmguCV circle detection error: {ex.Message}", System.Drawing.KnownColor.DarkRed);
                return false;
            }
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
