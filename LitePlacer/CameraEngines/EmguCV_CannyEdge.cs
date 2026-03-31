using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace LitePlacer.CameraEngines
{
    /// <summary>
    /// EmguCV implementation of Canny edge detection
    /// Provides superior edge detection compared to AForge with dual thresholding and hysteresis
    /// </summary>
    internal class EmguCV_CannyEdge : IProcessingFunction
    {
        private AForgeFunctionDefinition _def;

        public string Name => "Canny edge detection";

        public EmguCV_CannyEdge(AForgeFunctionDefinition def)
        {
            _def = def ?? throw new ArgumentNullException(nameof(def));
        }

        public Bitmap Process(Bitmap input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            try
            {
                // Convert Bitmap to EmguCV Image
                using (Image<Bgr, byte> imgColor = input.ToImage<Bgr, byte>())
                {
                    // Convert to grayscale
                    using (Image<Gray, byte> gray = imgColor.Convert<Gray, byte>())
                    {
                        // Get thresholds from parameters
                        // ParameterDoubleA = Lower threshold (default 100)
                        // ParameterDoubleB = Upper threshold (default 200)
                        double threshold1 = _def.parameterDoubleA > 0 ? _def.parameterDoubleA : 100;
                        double threshold2 = _def.parameterDoubleB > 0 ? _def.parameterDoubleB : 200;

                        // Apply Canny edge detection
                        // This is MUCH better than AForge edge detection:
                        // - Uses dual thresholding
                        // - Includes hysteresis for connected edges
                        // - Sub-pixel accuracy available
                        using (Image<Gray, byte> edges = gray.Canny(threshold1, threshold2))
                        {
                            return edges.ToBitmap();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Canny edge detection failed: {ex.Message}", ex);
            }
        }

        // Parameter properties (from AForgeFunctionDefinition)
        public int ParameterInt
        {
            get => _def.parameterInt;
            set => _def.parameterInt = value;
        }

        public double ParameterDouble
        {
            get => _def.parameterDouble;
            set => _def.parameterDouble = value;
        }

        public double ParameterDoubleA
        {
            get => _def.parameterDoubleA;
            set => _def.parameterDoubleA = value;
        }

        public double ParameterDoubleB
        {
            get => _def.parameterDoubleB;
            set => _def.parameterDoubleB = value;
        }

        public double ParameterDoubleC
        {
            get => _def.parameterDoubleC;
            set => _def.parameterDoubleC = value;
        }

        public int R
        {
            get => _def.R;
            set => _def.R = value;
        }

        public int G
        {
            get => _def.G;
            set => _def.G = value;
        }

        public int B
        {
            get => _def.B;
            set => _def.B = value;
        }
    }
}
