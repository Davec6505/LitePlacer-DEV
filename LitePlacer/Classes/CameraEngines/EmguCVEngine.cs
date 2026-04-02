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
                "Noise reduction"
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
                // RetrType.List: retrieves all contours without hierarchy — needed for edge images
                // where Canny produces two concentric rings (inner + outer of the bright halo).
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(gray, contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
                    gray.Dispose();

                    if (DisplayResults)
                        _mainForm.DisplayText($"  {contours.Size} raw contours found", System.Drawing.KnownColor.DarkCyan);

                    // Minimum perimeter guard based on Xmin: contours shorter than the
                    // circumference of a circle of diameter Xmin are too small to be the target.
                    double minPerimeterPx = Math.PI * (parameters.Xmin / XmmPerPixel); // pi * d

                    double bestDist = double.MaxValue;
                    double bestCx = 0, bestCy = 0, bestDiameterMm = 0;
                    bool foundValid = false;

                    for (int i = 0; i < contours.Size; i++)
                    {
                        using (VectorOfPoint contour = contours[i])
                        {
                            double perimeter = CvInvoke.ArcLength(contour, true);
                            if (perimeter < minPerimeterPx) continue;

                            // Radius estimator chosen by image type:
                            //   Edge image: r = perimeter / (2*pi)
                            //     The Canny ring perimeter directly measures the circle circumference.
                            //     This is invariant to halo thickness and threshold level.
                            //   Filled image: r = sqrt(area / pi)
                            //     Area of the filled disc gives true radius without stairstepping bias.
                            double area = CvInvoke.ContourArea(contour);
                            double radiusPx;
                            if (isEdgeImage)
                                radiusPx = perimeter / (2.0 * Math.PI);
                            else
                                radiusPx = Math.Sqrt(area / Math.PI);

                            double diameterMm = radiusPx * 2.0 * XmmPerPixel;

                            // Circularity check on perimeter vs area to reject non-circular contours.
                            // For edge images, use a ring-aware formula: compare enclosing circle area to contour area.
                            double circularity;
                            if (isEdgeImage)
                            {
                                // For edge rings: expected area of a thin ring ~ perimeter * 1px
                                // Use bounding circle area vs perimeter-derived circle area
                                double expectedFilledArea = Math.PI * radiusPx * radiusPx;
                                double enclosingArea = Math.PI * (CvInvoke.MinEnclosingCircle(contour).Radius *
                                                                   CvInvoke.MinEnclosingCircle(contour).Radius);
                                circularity = expectedFilledArea / enclosingArea; // ~1.0 for a circle ring
                            }
                            else
                            {
                                circularity = (4.0 * Math.PI * area) / (perimeter * perimeter);
                            }

                            if (circularity < 0.6) continue;

                            // Size filter
                            if (diameterMm < parameters.Xmin || diameterMm > parameters.Xmax)
                                continue;

                            // Sub-pixel centroid from moments - works correctly for both
                            // edge rings and filled discs.
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
                            if (distPx < bestDist)
                            {
                                bestDist = distPx;
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
                            _mainForm.DisplayText($"EmguCV: no circles passed filters (Xmin={parameters.Xmin:F2}mm Xmax={parameters.Xmax:F2}mm XDist={parameters.XUniqueDistance:F2}mm YDist={parameters.YUniqueDistance:F2}mm)",
                                System.Drawing.KnownColor.DarkOrange);
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
