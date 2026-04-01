using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

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
        /// par_int = threshold1, par_d = threshold2
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
                    {
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    }
                    else
                    {
                        gray = src.Clone();
                    }
                    
                    double threshold1 = par_int > 0 ? par_int : 50;
                    double threshold2 = par_d > 0 ? par_d : 150;
                    
                    Mat edges = new Mat();
                    CvInvoke.Canny(gray, edges, threshold1, threshold2);
                    gray.Dispose();
                    
                    frame.Dispose();
                    frame = edges.ToBitmap();
                    edges.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmguCV CannyEdge error: {ex.Message}");
            }
        }
    }
}
