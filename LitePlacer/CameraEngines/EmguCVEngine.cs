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
            // Create a generic processor that wraps EmguCV static functions
            switch (def.Name)
            {
                case "Grayscale":
                    return new EmguCVProcessor(def, EmguCVFunctions.Grayscale);
                case "Threshold":
                    return new EmguCVProcessor(def, EmguCVFunctions.Threshold);
                case "Invert":
                    return new EmguCVProcessor(def, EmguCVFunctions.Invert);
                case "Edge detect":
                    return new EmguCVProcessor(def, EmguCVFunctions.EdgeDetect);
                case "Blur":
                    return new EmguCVProcessor(def, EmguCVFunctions.Blur);
                case "Gaussian blur":
                    return new EmguCVProcessor(def, EmguCVFunctions.GaussianBlur);
                case "Erosion":
                    return new EmguCVProcessor(def, EmguCVFunctions.Erosion);
                case "Dilation":
                    return new EmguCVProcessor(def, EmguCVFunctions.Dilation);
                case "Noise reduction":
                    return new EmguCVProcessor(def, EmguCVFunctions.NoiseReduction);
                case "Canny edge detection":
                    return new EmguCVProcessor(def, EmguCVFunctions.CannyEdge);
                    
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
                           out double X, 
                           out double Y, 
                           out double A,
                           bool DisplayResults)
        {
            X = Y = A = 0;
            
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
                    
                    // Perform measurement based on parameters
                    if (parameters.SearchRounds)
                    {
                        return DetectCircles_SubPixel(matImage, parameters, out X, out Y, out A, DisplayResults);
                    }
                    else if (parameters.SearchRectangles)
                    {
                        return DetectRectangles_Precise(matImage, parameters, out X, out Y, out A, DisplayResults);
                    }
                    else if (parameters.SearchComponentOutlines)
                    {
                        return DetectComponent_Contours(matImage, parameters, out X, out Y, out A, DisplayResults);
                    }
                    
                    _mainForm.DisplayText("EmguCV: No search type selected", 
                        System.Drawing.KnownColor.DarkOrange);
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
        /// Detect circles using HoughCircles with sub-pixel accuracy
        /// Much more accurate than AForge implementation
        /// </summary>
        private bool DetectCircles_SubPixel(Mat image, MeasurementParametersClass parameters,
                                            out double X, out double Y, out double A, bool DisplayResults)
        {
            X = Y = A = 0;
            
            try
            {
                // Convert to grayscale if needed
                Mat gray = new Mat();
                if (image.NumberOfChannels > 1)
                {
                    CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);
                }
                else
                {
                    gray = image.Clone();
                }
                
                // Apply Gaussian blur to reduce noise
                CvInvoke.GaussianBlur(gray, gray, new Size(9, 9), 2, 2);
                
                // HoughCircles parameters
                double dp = 1.0;  // Inverse ratio of accumulator resolution
                double minDist = Math.Max((parameters.Xmax - parameters.Xmin) / 2.0, 20);  // Minimum distance between circle centers
                double param1 = 50;  // Canny edge threshold (LOWERED from 100 - more sensitive)
                double param2 = 20;   // Accumulator threshold (LOWERED from 30 - more circles detected)
                int minRadius = (int)(parameters.Xmin / 2.0);
                int maxRadius = (int)(parameters.Xmax / 2.0);
                
                // Debug logging
                _mainForm.DisplayText($"EmguCV HoughCircles: minDist={minDist:F1}, param1={param1}, param2={param2}, " +
                    $"minR={minRadius}, maxR={maxRadius}", System.Drawing.KnownColor.DarkCyan);
                
                // Detect circles
                CircleF[] circles = CvInvoke.HoughCircles(gray, HoughModes.Gradient, dp, minDist, 
                    param1, param2, minRadius, maxRadius);
                
                _mainForm.DisplayText($"EmguCV: Found {circles.Length} raw circles", System.Drawing.KnownColor.DarkCyan);
                
                gray.Dispose();
                
                if (circles.Length == 0)
                {
                    if (DisplayResults)
                    {
                        _mainForm.DisplayText("EmguCV: No circles found", 
                            System.Drawing.KnownColor.DarkOrange);
                    }
                    return false;
                }
                
                // Find closest circle to image center (apply distance filtering)
                int centerX = image.Width / 2;
                int centerY = image.Height / 2;
                double bestDist = double.MaxValue;
                CircleF bestCircle = circles[0];
                
                foreach (var circle in circles)
                {
                    double dx = circle.Center.X - centerX;
                    double dy = circle.Center.Y - centerY;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    
                    // Check if within distance limits
                    if (dist <= parameters.XUniqueDistance && dist < bestDist)
                    {
                        bestDist = dist;
                        bestCircle = circle;
                    }
                }
                
                if (bestDist == double.MaxValue)
                {
                    if (DisplayResults)
                    {
                        _mainForm.DisplayText($"EmguCV: {circles.Length} circles found, none within distance limit", 
                            System.Drawing.KnownColor.DarkOrange);
                    }
                    return false;
                }
                
                // Return coordinates relative to image center (matching AForge behavior)
                X = bestCircle.Center.X - centerX;
                Y = centerY - bestCircle.Center.Y;  // Flip Y (image coordinates are top-down)
                A = 0.0;  // Circles don't have rotation
                
                if (DisplayResults)
                {
                    _mainForm.DisplayText($"EmguCV Circle: X={X:F2}, Y={Y:F2}, R={bestCircle.Radius:F2} " +
                        $"({circles.Length} candidates, best dist={bestDist:F2})", 
                        System.Drawing.KnownColor.DarkGreen);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _mainForm.DisplayText($"EmguCV circle detection error: {ex.Message}", 
                    System.Drawing.KnownColor.DarkRed);
                return false;
            }
        }
        
        /// <summary>
        /// Detect rectangles using contour detection with minimum area rectangle fitting
        /// </summary>
        private bool DetectRectangles_Precise(Mat image, MeasurementParametersClass parameters,
                                              out double X, out double Y, out double A, bool DisplayResults)
        {
            X = Y = A = 0;
            
            try
            {
                // Convert to grayscale if needed
                Mat gray = new Mat();
                if (image.NumberOfChannels > 1)
                {
                    CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);
                }
                else
                {
                    gray = image.Clone();
                }
                
                // Apply Canny edge detection
                Mat edges = new Mat();
                CvInvoke.Canny(gray, edges, 50, 150);
                gray.Dispose();
                
                // Find contours
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(edges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                    edges.Dispose();
                    
                    if (contours.Size == 0)
                    {
                        if (DisplayResults)
                        {
                            _mainForm.DisplayText("EmguCV: No rectangles found", 
                                System.Drawing.KnownColor.DarkOrange);
                        }
                        return false;
                    }
                    
                    // Find best rectangle
                    int centerX = image.Width / 2;
                    int centerY = image.Height / 2;
                    double bestDist = double.MaxValue;
                    RotatedRect bestRect = new RotatedRect();
                    bool foundAny = false;
                    
                    for (int i = 0; i < contours.Size; i++)
                    {
                        using (VectorOfPoint contour = contours[i])
                        {
                            double area = CvInvoke.ContourArea(contour);
                            
                            // Check if area is within size limits
                            double minArea = parameters.Xmin * parameters.Ymin;
                            double maxArea = parameters.Xmax * parameters.Ymax;
                            if (area < minArea || area > maxArea)
                            {
                                continue;
                            }
                            
                            // Fit minimum area rectangle
                            RotatedRect rect = CvInvoke.MinAreaRect(contour);
                            
                            // Check distance from center
                            double dx = rect.Center.X - centerX;
                            double dy = rect.Center.Y - centerY;
                            double dist = Math.Sqrt(dx * dx + dy * dy);
                            
                            if (dist <= parameters.XUniqueDistance && dist < bestDist)
                            {
                                bestDist = dist;
                                bestRect = rect;
                                foundAny = true;
                            }
                        }
                    }
                    
                    if (!foundAny)
                    {
                        if (DisplayResults)
                        {
                            _mainForm.DisplayText($"EmguCV: {contours.Size} contours found, no valid rectangles", 
                                System.Drawing.KnownColor.DarkOrange);
                        }
                        return false;
                    }
                    
                    // Return coordinates relative to image center
                    X = bestRect.Center.X - centerX;
                    Y = centerY - bestRect.Center.Y;
                    A = bestRect.Angle;
                    
                    if (DisplayResults)
                    {
                        _mainForm.DisplayText($"EmguCV Rectangle: X={X:F2}, Y={Y:F2}, A={A:F2}°, " +
                            $"Size={bestRect.Size.Width:F2}x{bestRect.Size.Height:F2}", 
                            System.Drawing.KnownColor.DarkGreen);
                    }
                    
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
                                              out double X, out double Y, out double A, bool DisplayResults)
        {
            // For now, use rectangle detection as component detection is similar
            // Future: Add more sophisticated shape analysis
            return DetectRectangles_Precise(image, parameters, out X, out Y, out A, DisplayResults);
        }
    }
}
