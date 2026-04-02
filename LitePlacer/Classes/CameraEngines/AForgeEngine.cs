using System;
using System.Collections.Generic;
using System.Drawing;

namespace LitePlacer.CameraEngines
{
    /// <summary>
    /// AForge.NET camera engine implementation
    /// Wraps existing AForge.NET functionality to maintain compatibility
    /// This is the default engine and preserves all current behavior
    /// </summary>
    public class AForgeEngine : ICameraEngine
    {
        private Camera _camera;

        /// <summary>
        /// Creates AForge engine wrapper around existing Camera implementation
        /// </summary>
        /// <param name="camera">Camera instance containing existing AForge code</param>
        public AForgeEngine(Camera camera)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        }

        public string EngineName => "AForge.NET";

        public string Version => "2.2.5";

        public bool IsAvailable => true;  // Always available (built into application)

        /// <summary>
        /// Returns list of all AForge functions currently supported
        /// This list matches the functions in Camera.BuildMeasurementFunctionsList()
        /// </summary>
        public List<string> GetAvailableFunctions()
        {
            return new List<string>
            {
                // Core image processing
                "Grayscale",
                "Invert",
                "Contrast scretch",
                
                // Color operations
                "Kill color",
                "Keep color",
                
                // Morphological operations
                "Erosion",
                "Dilation",
                
                // Filtering
                "Noise reduction",
                "Blur",
                "Gaussian blur",
                "Median",
                
                // Edge detection
                "Edge detect",
                
                // Thresholding
                "Threshold",
                "Histogram",
                
                // Feature detection
                "Hough circles",
                
                // Utility
                "Meas. zoom",
                "Jog before measurement"
            };
        }

        /// <summary>
        /// Builds processing pipeline by wrapping existing AForge functions
        /// Delegates to Camera.BuildMeasurementFunctionsList() to maintain compatibility
        /// </summary>
        public List<IProcessingFunction> BuildProcessingPipeline(List<AForgeFunctionDefinition> definitions)
        {
            // For AForge, we don't actually need to build IProcessingFunction list
            // The existing Camera code handles this internally
            // This method exists for interface compliance and future extensibility
            
            // Call existing Camera method to build internal pipeline
            _camera.BuildMeasurementFunctionsList(definitions);
            
            // Return empty list - actual processing happens in Camera.Measure()
            // In future, we could wrap each function for consistency, but not needed for Phase 1
            return new List<IProcessingFunction>();
        }

        /// <summary>
        /// Executes measurement using existing Camera.Measure() implementation
        /// This preserves ALL current AForge functionality without changes
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
            // Size is reported by Camera.Measure's AForge path; return 0 here as this stub
            // delegates entirely to Camera.Measure which handles its own display.
            XSizeMm = 0;
            YSizeMm = 0;
            return _camera.Measure(out X, out Y, out A, DisplayResults);
        }
    }
}
