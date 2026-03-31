using System.Drawing;

namespace LitePlacer.CameraEngines
{
    /// <summary>
    /// Interface for a single image processing function in the pipeline
    /// Each function takes an input image and returns a processed output image
    /// Examples: Grayscale conversion, edge detection, thresholding, etc.
    /// </summary>
    public interface IProcessingFunction
    {
        /// <summary>
        /// Name of the processing function (e.g., "Grayscale", "Canny edge detection")
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Processes an input image and returns the result
        /// This is where the actual image processing algorithm executes
        /// </summary>
        /// <param name="input">Input bitmap to process</param>
        /// <returns>Processed output bitmap</returns>
        Bitmap Process(Bitmap input);

        // =============================================================================
        // Parameters - These match AForgeFunctionDefinition structure for compatibility
        // Different functions use different parameters
        // =============================================================================

        /// <summary>
        /// Integer parameter (used by functions that need an int value)
        /// Example: Block size for adaptive threshold, kernel size for erosion, etc.
        /// </summary>
        int ParameterInt { get; set; }

        /// <summary>
        /// Double parameter (used by functions that need a floating-point value)
        /// Example: Threshold value, blur sigma, etc.
        /// </summary>
        double ParameterDouble { get; set; }

        /// <summary>
        /// Additional double parameter A (for functions needing multiple float values)
        /// Example: Canny threshold 1, Sobel X weight, etc.
        /// </summary>
        double ParameterDoubleA { get; set; }

        /// <summary>
        /// Additional double parameter B (for functions needing multiple float values)
        /// Example: Canny threshold 2, Sobel Y weight, etc.
        /// </summary>
        double ParameterDoubleB { get; set; }

        /// <summary>
        /// Additional double parameter C (for functions needing even more float values)
        /// </summary>
        double ParameterDoubleC { get; set; }

        /// <summary>
        /// Red component (for color-based operations)
        /// Example: Color filtering, color replacement, etc.
        /// </summary>
        int R { get; set; }

        /// <summary>
        /// Green component (for color-based operations)
        /// </summary>
        int G { get; set; }

        /// <summary>
        /// Blue component (for color-based operations)
        /// </summary>
        int B { get; set; }
    }
}
