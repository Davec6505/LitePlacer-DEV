using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace LitePlacer.CameraEngines
{
    /// <summary>
    /// EmguCV implementation of grayscale conversion
    /// </summary>
    internal class EmguCV_Grayscale : IProcessingFunction
    {
        private AForgeFunctionDefinition _def;

        public string Name => "Grayscale";

        public EmguCV_Grayscale(AForgeFunctionDefinition def)
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
                    // Convert to grayscale using OpenCV
                    using (Image<Gray, byte> gray = imgColor.Convert<Gray, byte>())
                    {
                        return gray.ToBitmap();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Grayscale conversion failed: {ex.Message}", ex);
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
