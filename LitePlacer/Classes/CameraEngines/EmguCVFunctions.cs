using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace LitePlacer.CameraEngines
{
    /// <summary>
    /// Delegate for EmguCV processing functions (matches AForge pattern)
    /// </summary>
    public delegate void EmguCV_op(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
        double par_dA, double par_dB, double par_dC);

    /// <summary>
    /// EmguCV image processing functions - all in one place (like AForge)
    /// Each function processes a Bitmap using OpenCV operations
    /// </summary>
    public static class EmguCVFunctions
    {
        /// <summary>
        /// Convert to grayscale
        /// </summary>
        public static void Grayscale(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat gray = new Mat();
                    if (src.NumberOfChannels > 1)
                    {
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    }
                    else
                    {
                        gray = src.Clone();
                    }
                    
                    frame.Dispose();
                    frame = gray.ToBitmap();
                    gray.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV Grayscale error: {ex.Message}");
            }
        }

        /// <summary>
        /// Binary threshold
        /// par_int = threshold value (0-255)
        /// </summary>
        public static void Threshold(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat gray = new Mat();
                    if (src.NumberOfChannels > 1)
                    {
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    }
                    else
                    {
                        gray = src.Clone();
                    }
                    
                    Mat thresholded = new Mat();
                    CvInvoke.Threshold(gray, thresholded, par_int, 255, ThresholdType.Binary);
                    gray.Dispose();
                    
                    frame.Dispose();
                    frame = thresholded.ToBitmap();
                    thresholded.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV Threshold error: {ex.Message}");
            }
        }

        /// <summary>
        /// Invert image (bitwise NOT)
        /// </summary>
        public static void Invert(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat inverted = new Mat();
                    CvInvoke.BitwiseNot(src, inverted);
                    
                    frame.Dispose();
                    frame = inverted.ToBitmap();
                    inverted.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV Invert error: {ex.Message}");
            }
        }

        /// <summary>
        /// Edge detection using Sobel operator
        /// par_int = kernel size (1, 3, 5, or 7)
        /// </summary>
        public static void EdgeDetect(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat gray = new Mat();
                    if (src.NumberOfChannels > 1)
                    {
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    }
                    else
                    {
                        gray = src.Clone();
                    }
                    
                    int ksize = par_int < 1 ? 3 : par_int;
                    if (ksize > 7) ksize = 7;
                    if (ksize % 2 == 0) ksize++;
                    
                    Mat gradX = new Mat();
                    Mat gradY = new Mat();
                    Mat absGradX = new Mat();
                    Mat absGradY = new Mat();
                    Mat grad = new Mat();
                    
                    CvInvoke.Sobel(gray, gradX, DepthType.Cv16S, 1, 0, ksize);
                    CvInvoke.ConvertScaleAbs(gradX, absGradX, 1, 0);
                    
                    CvInvoke.Sobel(gray, gradY, DepthType.Cv16S, 0, 1, ksize);
                    CvInvoke.ConvertScaleAbs(gradY, absGradY, 1, 0);
                    
                    CvInvoke.AddWeighted(absGradX, 0.5, absGradY, 0.5, 0, grad);
                    
                    gray.Dispose();
                    gradX.Dispose();
                    gradY.Dispose();
                    absGradX.Dispose();
                    absGradY.Dispose();
                    
                    frame.Dispose();
                    frame = grad.ToBitmap();
                    grad.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV EdgeDetect error: {ex.Message}");
            }
        }

        /// <summary>
        /// Measurement zoom: centre-crop then resize back to original dimensions.
        /// Zooms in so small features fill more of the frame for detection.
        /// par_d = zoom factor (e.g. 2.0 = 2x zoom). GetMeasurementZoom() reads this
        /// value so mm/pixel is corrected automatically during measurement.
        /// </summary>
        public static void MeasZoom(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            double factor = par_d < 0.1 ? 1.0 : par_d;
            if (Math.Abs(factor - 1.0) < 0.01)
                return;

            int origW = frame.Width;
            int origH = frame.Height;
            int centerX = origW / 2;
            int centerY = origH / 2;
            int cropW = (int)(origW / factor);
            int cropH = (int)(origH / factor);
            int fromX = centerX - cropW / 2;
            int fromY = centerY - cropH / 2;

            // Clamp to frame bounds
            fromX = Math.Max(0, fromX);
            fromY = Math.Max(0, fromY);
            cropW = Math.Min(cropW, origW - fromX);
            cropH = Math.Min(cropH, origH - fromY);

            try
            {
                Bitmap cropped = new Bitmap(cropW, cropH);
                using (Graphics g = Graphics.FromImage(cropped))
                {
                    g.DrawImage(frame, new Rectangle(0, 0, cropW, cropH),
                        new Rectangle(fromX, fromY, cropW, cropH), GraphicsUnit.Pixel);
                }
                Bitmap resized = new Bitmap(origW, origH);
                using (Graphics g = Graphics.FromImage(resized))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                    g.DrawImage(cropped, 0, 0, origW, origH);
                }
                cropped.Dispose();
                frame.Dispose();
                frame = resized;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV MeasZoom error: {ex.Message}");
            }
        }

        /// <summary>
        /// Simple box filter blur
        /// par_int = kernel size
        /// </summary>
        public static void Blur(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    int ksize = par_int < 1 ? 3 : par_int;
                    if (ksize % 2 == 0) ksize++;
                    
                    Mat blurred = new Mat();
                    CvInvoke.Blur(src, blurred, new Size(ksize, ksize), new Point(-1, -1));
                    
                    frame.Dispose();
                    frame = blurred.ToBitmap();
                    blurred.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV Blur error: {ex.Message}");
            }
        }

        /// <summary>
        /// Gaussian blur
        /// par_int = kernel size, par_d = sigma
        /// </summary>
        public static void GaussianBlur(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    int ksize = par_int < 1 ? 5 : par_int;
                    if (ksize % 2 == 0) ksize++;
                    double sigma = par_d > 0 ? par_d : 0;
                    
                    Mat blurred = new Mat();
                    CvInvoke.GaussianBlur(src, blurred, new Size(ksize, ksize), sigma, sigma);
                    
                    frame.Dispose();
                    frame = blurred.ToBitmap();
                    blurred.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV GaussianBlur error: {ex.Message}");
            }
        }

        /// <summary>
        /// Morphological erosion
        /// par_int = kernel size
        /// </summary>
        public static void Erosion(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    int ksize = par_int < 1 ? 3 : par_int;
                    Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, 
                        new Size(ksize, ksize), new Point(-1, -1));
                    
                    Mat eroded = new Mat();
                    CvInvoke.Erode(src, eroded, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                    kernel.Dispose();
                    
                    frame.Dispose();
                    frame = eroded.ToBitmap();
                    eroded.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV Erosion error: {ex.Message}");
            }
        }

        /// <summary>
        /// Morphological dilation
        /// par_int = kernel size
        /// </summary>
        public static void Dilation(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    int ksize = par_int < 1 ? 3 : par_int;
                    Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, 
                        new Size(ksize, ksize), new Point(-1, -1));
                    
                    Mat dilated = new Mat();
                    CvInvoke.Dilate(src, dilated, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                    kernel.Dispose();
                    
                    frame.Dispose();
                    frame = dilated.ToBitmap();
                    dilated.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV Dilation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Noise reduction using median filter
        /// par_int = kernel size (must be odd)
        /// </summary>
        public static void NoiseReduction(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    int ksize = par_int < 1 ? 5 : par_int;
                    if (ksize % 2 == 0) ksize++;
                    
                    Mat filtered = new Mat();
                    CvInvoke.MedianBlur(src, filtered, ksize);
                    
                    frame.Dispose();
                    frame = filtered.ToBitmap();
                    filtered.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV NoiseReduction error: {ex.Message}");
            }
        }

        /// <summary>
        /// Canny edge detection
        /// par_dA = lower threshold, par_dB = upper threshold, par_int = aperture size (3/5/7)
        /// </summary>
        public static void CannyEdge(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat gray = new Mat();
                    if (src.NumberOfChannels > 1)
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    else
                        gray = src.Clone();

                    double t1 = par_dA > 0 ? par_dA : 100;
                    double t2 = par_dB > 0 ? par_dB : 200;
                    int apt = (par_int == 3 || par_int == 5 || par_int == 7) ? par_int : 3;

                    Mat edges = new Mat();
                    CvInvoke.Canny(gray, edges, t1, t2, apt);
                    gray.Dispose();

                    // Convert back to colour so downstream functions work
                    Mat colour = new Mat();
                    CvInvoke.CvtColor(edges, colour, ColorConversion.Gray2Bgr);
                    edges.Dispose();

                    frame.Dispose();
                    frame = colour.ToBitmap();
                    colour.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV CannyEdge error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sobel edge detection
        /// par_int = direction (0=both, 1=X, 2=Y), par_d = scale, par_dA = delta
        /// </summary>
        public static void SobelEdge(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat gray = new Mat();
                    if (src.NumberOfChannels > 1)
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    else
                        gray = src.Clone();

                    double scale = par_d > 0 ? par_d : 1.0;
                    double delta = par_dA;
                    Mat result = new Mat();

                    if (par_int == 1)
                    {
                        CvInvoke.Sobel(gray, result, DepthType.Cv8U, 1, 0, 3, scale, delta, BorderType.Default);
                    }
                    else if (par_int == 2)
                    {
                        CvInvoke.Sobel(gray, result, DepthType.Cv8U, 0, 1, 3, scale, delta, BorderType.Default);
                    }
                    else
                    {
                        Mat gx = new Mat();
                        Mat gy = new Mat();
                        CvInvoke.Sobel(gray, gx, DepthType.Cv8U, 1, 0, 3, scale, delta, BorderType.Default);
                        CvInvoke.Sobel(gray, gy, DepthType.Cv8U, 0, 1, 3, scale, delta, BorderType.Default);
                        CvInvoke.AddWeighted(gx, 0.5, gy, 0.5, 0, result);
                        gx.Dispose();
                        gy.Dispose();
                    }
                    gray.Dispose();

                    Mat colour = new Mat();
                    CvInvoke.CvtColor(result, colour, ColorConversion.Gray2Bgr);
                    result.Dispose();

                    frame.Dispose();
                    frame = colour.ToBitmap();
                    colour.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV SobelEdge error: {ex.Message}");
            }
        }

        /// <summary>
        /// Laplacian edge detection
        /// par_int = aperture size, par_d = scale, par_dA = delta
        /// </summary>
        public static void LaplacianEdge(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat gray = new Mat();
                    if (src.NumberOfChannels > 1)
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    else
                        gray = src.Clone();

                    int ksize = (par_int >= 1) ? par_int : 3;
                    double scale = par_d > 0 ? par_d : 1.0;
                    double delta = par_dA;

                    Mat lap = new Mat();
                    CvInvoke.Laplacian(gray, lap, DepthType.Cv8U, ksize, scale, delta, BorderType.Default);
                    gray.Dispose();

                    Mat colour = new Mat();
                    CvInvoke.CvtColor(lap, colour, ColorConversion.Gray2Bgr);
                    lap.Dispose();

                    frame.Dispose();
                    frame = colour.ToBitmap();
                    colour.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV LaplacianEdge error: {ex.Message}");
            }
        }

        /// <summary>
        /// Adaptive threshold
        /// par_int = method (0=Mean, 1=Gaussian), par_d = block size (odd), par_dA = C constant, par_dB = max value
        /// </summary>
        public static void AdaptiveThreshold(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat gray = new Mat();
                    if (src.NumberOfChannels > 1)
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    else
                        gray = src.Clone();

                    int blockSize = (par_d >= 3) ? (int)par_d : 11;
                    if (blockSize % 2 == 0) blockSize++;
                    double C = par_dA;
                    double maxVal = par_dB > 0 ? par_dB : 255;
                    AdaptiveThresholdType method = par_int == 1
                        ? AdaptiveThresholdType.GaussianC
                        : AdaptiveThresholdType.MeanC;

                    Mat thresh = new Mat();
                    CvInvoke.AdaptiveThreshold(gray, thresh, maxVal, method, ThresholdType.Binary, blockSize, C);
                    gray.Dispose();

                    Mat colour = new Mat();
                    CvInvoke.CvtColor(thresh, colour, ColorConversion.Gray2Bgr);
                    thresh.Dispose();

                    frame.Dispose();
                    frame = colour.ToBitmap();
                    colour.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV AdaptiveThreshold error: {ex.Message}");
            }
        }

        /// <summary>
        /// Bilateral filter - edge-preserving noise reduction
        /// par_int = diameter, par_d = sigma color, par_dA = sigma space
        /// </summary>
        public static void BilateralFilter(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    int d = par_int > 0 ? par_int : 9;
                    double sigmaColor = par_d > 0 ? par_d : 75;
                    double sigmaSpace = par_dA > 0 ? par_dA : 75;

                    Mat filtered = new Mat();
                    CvInvoke.BilateralFilter(src, filtered, d, sigmaColor, sigmaSpace);

                    frame.Dispose();
                    frame = filtered.ToBitmap();
                    filtered.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV BilateralFilter error: {ex.Message}");
            }
        }

        /// <summary>
        /// CLAHE - Contrast Limited Adaptive Histogram Equalization
        /// par_d = clip limit, par_int = tile grid size
        /// </summary>
        public static void CLAHE(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat gray = new Mat();
                    if (src.NumberOfChannels > 1)
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    else
                        gray = src.Clone();

                    double clipLimit = par_d > 0 ? par_d : 40.0;
                    int tileSize = par_int > 0 ? par_int : 8;

                    Mat result = new Mat();
                    CvInvoke.CLAHE(gray, clipLimit, new System.Drawing.Size(tileSize, tileSize), result);
                    gray.Dispose();

                    Mat colour = new Mat();
                    CvInvoke.CvtColor(result, colour, ColorConversion.Gray2Bgr);
                    result.Dispose();

                    frame.Dispose();
                    frame = colour.ToBitmap();
                    colour.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV CLAHE error: {ex.Message}");
            }
        }

        /// <summary>
        /// Morphological gradient (dilation - erosion) - highlights edges
        /// par_int = kernel size, par_d = iterations
        /// </summary>
        public static void MorphologicalGradient(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    int ksize = par_int > 0 ? par_int : 3;
                    int iterations = par_d > 0 ? (int)par_d : 1;

                    Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle,
                        new System.Drawing.Size(ksize, ksize), new System.Drawing.Point(-1, -1));

                    Mat result = new Mat();
                    CvInvoke.MorphologyEx(src, result, MorphOp.Gradient, kernel,
                        new System.Drawing.Point(-1, -1), iterations, BorderType.Default, new MCvScalar());
                    kernel.Dispose();

                    frame.Dispose();
                    frame = result.ToBitmap();
                    result.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV MorphologicalGradient error: {ex.Message}");
            }
        }

        /// <summary>
        /// Morphological top hat - extracts bright features smaller than structuring element
        /// par_int = kernel size
        /// </summary>
        public static void MorphologicalTopHat(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    int ksize = par_int > 0 ? par_int : 5;
                    Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle,
                        new System.Drawing.Size(ksize, ksize), new System.Drawing.Point(-1, -1));

                    Mat result = new Mat();
                    CvInvoke.MorphologyEx(src, result, MorphOp.Tophat, kernel,
                        new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                    kernel.Dispose();

                    frame.Dispose();
                    frame = result.ToBitmap();
                    result.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV MorphologicalTopHat error: {ex.Message}");
            }
        }

        /// <summary>
        /// Morphological black hat - extracts dark features smaller than structuring element
        /// par_int = kernel size
        /// </summary>
        public static void MorphologicalBlackHat(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    int ksize = par_int > 0 ? par_int : 5;
                    Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle,
                        new System.Drawing.Size(ksize, ksize), new System.Drawing.Point(-1, -1));

                    Mat result = new Mat();
                    CvInvoke.MorphologyEx(src, result, MorphOp.Blackhat, kernel,
                        new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                    kernel.Dispose();

                    frame.Dispose();
                    frame = result.ToBitmap();
                    result.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV MorphologicalBlackHat error: {ex.Message}");
            }
        }

        /// <summary>
        /// Hough circles (sub-pixel) - draws detected circles onto the image for visualisation
        /// par_d = dp, par_dA = min distance, par_dB = param1 (Canny), par_dC = param2 (accumulator)
        /// par_R = min radius, par_G = max radius
        /// </summary>
        public static void HoughCirclesVis(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat gray = new Mat();
                    if (src.NumberOfChannels > 1)
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    else
                        gray = src.Clone();

                    double dp = par_d > 0 ? par_d : 1.0;
                    double minDist = par_dA > 0 ? par_dA : 20;
                    double param1 = par_dB > 0 ? par_dB : 100;
                    double param2 = par_dC > 0 ? par_dC : 30;
                    int minR = par_R > 0 ? par_R : 10;
                    int maxR = par_G > 0 ? par_G : 100;

                    CircleF[] circles = CvInvoke.HoughCircles(gray, HoughModes.Gradient, dp, minDist, param1, param2, minR, maxR);
                    gray.Dispose();

                    // Draw onto colour copy
                    Mat colour = new Mat();
                    if (src.NumberOfChannels == 1)
                        CvInvoke.CvtColor(src, colour, ColorConversion.Gray2Bgr);
                    else
                        colour = src.Clone();

                    foreach (var c in circles)
                        CvInvoke.Circle(colour, new System.Drawing.Point((int)c.Center.X, (int)c.Center.Y),
                            (int)c.Radius, new MCvScalar(0, 255, 0), 2);

                    frame.Dispose();
                    frame = colour.ToBitmap();
                    colour.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV HoughCirclesVis error: {ex.Message}");
            }
        }

        /// <summary>
        /// Harris corner detection - draws corners onto the image
        /// par_int = block size, par_d = aperture, par_dA = k parameter
        /// </summary>
        public static void HarrisCorners(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat gray = new Mat();
                    if (src.NumberOfChannels > 1)
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    else
                        gray = src.Clone();

                    int blockSize = par_int > 0 ? par_int : 2;
                    int aperture = par_d > 0 ? (int)par_d : 3;
                    if (aperture % 2 == 0) aperture++;
                    double k = par_dA > 0 ? par_dA : 0.04;

                    Mat dst = new Mat();
                    CvInvoke.CornerHarris(gray, dst, blockSize, aperture, k);
                    gray.Dispose();

                    // Normalise and draw bright corners as red dots
                    Mat norm = new Mat();
                    CvInvoke.Normalize(dst, norm, 0, 255, NormType.MinMax, DepthType.Cv32F);
                    dst.Dispose();

                    Mat colour = new Mat();
                    if (src.NumberOfChannels == 1)
                        CvInvoke.CvtColor(src, colour, ColorConversion.Gray2Bgr);
                    else
                        colour = src.Clone();

                    float[,,] data = norm.ToImage<Gray, float>().Data;
                    for (int y = 0; y < norm.Rows; y++)
                        for (int x = 0; x < norm.Cols; x++)
                            if (data[y, x, 0] > 200)
                                CvInvoke.Circle(colour, new System.Drawing.Point(x, y), 3, new MCvScalar(0, 0, 255), -1);
                    norm.Dispose();

                    frame.Dispose();
                    frame = colour.ToBitmap();
                    colour.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV HarrisCorners error: {ex.Message}");
            }
        }

        /// <summary>
        /// Shi-Tomasi corner detection - draws corners onto the image
        /// par_int = max corners, par_d = quality level, par_dA = min distance
        /// </summary>
        public static void ShiTomasiCorners(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat gray = new Mat();
                    if (src.NumberOfChannels > 1)
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    else
                        gray = src.Clone();

                    int maxCorners = par_int > 0 ? par_int : 100;
                    double quality = par_d > 0 ? par_d : 0.01;
                    double minDist = par_dA > 0 ? par_dA : 10;

                    // CornerMinEigenVal not in EmguCV 4.5 - use CornerHarris as equivalent
                    // feature visualisation (both detect strong corners).
                    int bsz = (par_int > 0 && par_int < 10) ? par_int : 3;
                    Mat dst = new Mat();
                    CvInvoke.CornerHarris(gray, dst, bsz, 3, 0.04);
                    gray.Dispose();

                    Mat norm = new Mat();
                    CvInvoke.Normalize(dst, norm, 0, 255, NormType.MinMax, DepthType.Cv32F);
                    dst.Dispose();

                    // Use quality level as a fraction of the normalised max (255)
                    double eigThresh = quality * 255.0;

                    Mat colour = new Mat();
                    if (src.NumberOfChannels == 1)
                        CvInvoke.CvtColor(src, colour, ColorConversion.Gray2Bgr);
                    else
                        colour = src.Clone();

                    var normImg = norm.ToImage<Gray, float>();
                    norm.Dispose();
                    int drawn = 0;
                    for (int ey = 0; ey < normImg.Rows && drawn < maxCorners; ey++)
                        for (int ex = 0; ex < normImg.Cols && drawn < maxCorners; ex++)
                            if (normImg.Data[ey, ex, 0] > eigThresh)
                            {
                                CvInvoke.Circle(colour, new System.Drawing.Point(ex, ey), 4, new MCvScalar(0, 255, 255), -1);
                                drawn++;
                            }
                    normImg.Dispose();

                    frame.Dispose();
                    frame = colour.ToBitmap();
                    colour.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV ShiTomasiCorners error: {ex.Message}");
            }
        }

        /// <summary>
        /// FAST feature detection - draws keypoints onto the image
        /// par_int = threshold, par_d = non-max suppression (1=on)
        /// </summary>
        public static void FastFeatureDetection(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat gray = new Mat();
                    if (src.NumberOfChannels > 1)
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    else
                        gray = src.Clone();

                    int threshold = par_int > 0 ? par_int : 40;
                    bool nonmax = par_d >= 1;

                    using (var fast = new Emgu.CV.Features2D.FastFeatureDetector(threshold, nonmax))
                    using (VectorOfKeyPoint keypoints = new VectorOfKeyPoint())
                    {
                        fast.DetectRaw(gray, keypoints);
                        gray.Dispose();

                        Mat colour = new Mat();
                        if (src.NumberOfChannels == 1)
                            CvInvoke.CvtColor(src, colour, ColorConversion.Gray2Bgr);
                        else
                            colour = src.Clone();

                        foreach (var kp in keypoints.ToArray())
                            CvInvoke.Circle(colour, new System.Drawing.Point((int)kp.Point.X, (int)kp.Point.Y),
                                3, new MCvScalar(255, 0, 255), -1);

                        frame.Dispose();
                        frame = colour.ToBitmap();
                        colour.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV FastFeatureDetection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Contour detection - draws contours onto the image
        /// par_int = min contour area (pixels)
        /// </summary>
        public static void ContourDetection(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    Mat gray = new Mat();
                    if (src.NumberOfChannels > 1)
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    else
                        gray = src.Clone();

                    double minArea = par_int > 0 ? par_int : 10;

                    using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                    {
                        CvInvoke.FindContours(gray, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                        gray.Dispose();

                        Mat colour = new Mat();
                        if (src.NumberOfChannels == 1)
                            CvInvoke.CvtColor(src, colour, ColorConversion.Gray2Bgr);
                        else
                            colour = src.Clone();

                        for (int i = 0; i < contours.Size; i++)
                        {
                            using (VectorOfPoint c = contours[i])
                            {
                                if (CvInvoke.ContourArea(c) >= minArea)
                                    CvInvoke.DrawContours(colour, contours, i, new MCvScalar(0, 255, 0), 1);
                            }
                        }

                        frame.Dispose();
                        frame = colour.ToBitmap();
                        colour.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV ContourDetection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Watershed segmentation - segments image regions
        /// par_int = min blob area for seed markers
        /// </summary>
        public static void WatershedSegmentation(ref Bitmap frame, int par_int, double par_d, int par_R, int par_G, int par_B,
            double par_dA, double par_dB, double par_dC)
        {
            try
            {
                using (Mat src = frame.ToMat())
                {
                    // Watershed needs a colour 3-channel image
                    Mat colour = new Mat();
                    if (src.NumberOfChannels == 1)
                        CvInvoke.CvtColor(src, colour, ColorConversion.Gray2Bgr);
                    else
                        colour = src.Clone();

                    // Build simple markers from threshold
                    Mat gray = new Mat();
                    CvInvoke.CvtColor(colour, gray, ColorConversion.Bgr2Gray);
                    Mat binary = new Mat();
                    CvInvoke.Threshold(gray, binary, 0, 255, ThresholdType.Binary | ThresholdType.Otsu);
                    gray.Dispose();

                    Mat markers = new Mat(binary.Rows, binary.Cols, DepthType.Cv32S, 1);
                    markers.SetTo(new MCvScalar(0));

                    using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                    {
                        CvInvoke.FindContours(binary, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                        binary.Dispose();

                        int minArea = par_int > 0 ? par_int : 50;
                        int label = 1;
                        for (int i = 0; i < contours.Size; i++)
                        {
                            using (VectorOfPoint c = contours[i])
                            {
                                if (CvInvoke.ContourArea(c) >= minArea)
                                {
                                    CvInvoke.DrawContours(markers, contours, i, new MCvScalar(label), -1);
                                    label++;
                                }
                            }
                        }
                    }

                    CvInvoke.Watershed(colour, markers);

                    // Render watershed boundaries as red lines
                    var markData = markers.ToImage<Gray, int>();
                    for (int y = 0; y < colour.Rows; y++)
                        for (int x = 0; x < colour.Cols; x++)
                            if (markData.Data[y, x, 0] == -1)
                                CvInvoke.Circle(colour, new System.Drawing.Point(x, y), 1, new MCvScalar(0, 0, 255), -1);

                    markers.Dispose();

                    frame.Dispose();
                    frame = colour.ToBitmap();
                    colour.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV WatershedSegmentation error: {ex.Message}");
            }
        }
    }
}
