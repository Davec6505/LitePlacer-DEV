using System.Collections.Generic;
using System.Drawing;

namespace LitePlacer.CameraEngines
{
    /// <summary>
    /// Interface for camera vision processing engines (AForge.NET, EmguCV, etc.)
    /// Abstracts the underlying vision library to allow switching between different implementations
    /// </summary>
    public interface ICameraEngine
    {
        /// <summary>
        /// Human-readable name of the engine (e.g., "AForge.NET", "EmguCV")
        /// </summary>
        string EngineName { get; }

        /// <summary>
        /// Version of the underlying library
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Checks if the engine's required libraries are available and functional
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Gets the list of image processing functions available in this engine
        /// Different engines may support different functions
        /// </summary>
        /// <returns>List of function names that can be used in video processing pipelines</returns>
        List<string> GetAvailableFunctions();

        /// <summary>
        /// Builds a processing pipeline from UI-defined function definitions
        /// Converts AForgeFunctionDefinition objects into executable IProcessingFunction instances
        /// </summary>
        /// <param name="definitions">List of function definitions from UI (VideoAlgorithmsCollection)</param>
        /// <returns>List of executable processing functions</returns>
        List<IProcessingFunction> BuildProcessingPipeline(List<AForgeFunctionDefinition> definitions);

        /// <summary>
        /// Executes measurement on an image using the specified processing pipeline
        /// This is the main entry point for vision-based measurements
        /// </summary>
        /// <param name="image">Input image from camera</param>
        /// <param name="pipeline">Processing pipeline to apply</param>
        /// <param name="parameters">Measurement parameters (what to search for, size limits, etc.)</param>
        /// <param name="X">Output X coordinate (mm or pixels)</param>
        /// <param name="Y">Output Y coordinate (mm or pixels)</param>
        /// <param name="A">Output angle (degrees)</param>
        /// <param name="DisplayResults">If true, show visual feedback on processed image</param>
        /// <returns>True if measurement succeeded, false otherwise</returns>
        bool Measure(Bitmap image, 
                     List<IProcessingFunction> pipeline,
                     MeasurementParametersClass parameters,
                     double XmmPerPixel,
                     double YmmPerPixel,
                     out double X, 
                     out double Y, 
                     out double A,
                     out double XSizeMm,
                     out double YSizeMm,
                     bool DisplayResults);
    }
}
