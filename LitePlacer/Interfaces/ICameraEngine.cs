using System.Collections.Generic;
using System.Drawing;

namespace LitePlacer.CameraEngines
{
    /// <summary>
    /// A circle candidate produced by engine-side circle detection for display overlay.
    /// CenterX/Y are in measurement-frame pixel coordinates (same coordinate space as CameraResolution).
    /// RadiusPx is in measurement-frame pixels.
    /// IsSelected = true means this is the circle the measurement engine would actually use.
    /// PassesFilters = true means it passed size+distance but is not the selected one.
    /// </summary>
    public struct EngineCircle
    {
        public double CenterX;
        public double CenterY;
        public double RadiusPx;
        public double DiameterMm;
        public bool PassesSize;
        public bool PassesDistance;
        public bool IsSelected;
    }

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
        /// Find all circle candidates in the already-processed frame for display overlay.
        /// Returns every contour that is circular, with classification flags set.
        /// Only the selected circle (IsSelected=true) is what Measure() would return.
        /// Returns null / empty list when engine does not support this (AForge path falls back to blobs).
        /// </summary>
        List<EngineCircle> FindCirclesForDisplay(Bitmap processedFrame,
                                                  MeasurementParametersClass parameters,
                                                  double XmmPerPixel,
                                                  double YmmPerPixel);

        List<EngineCircle> FindCirclesForDisplay(Bitmap processedFrame,
                                                  MeasurementParametersClass parameters,
                                                  double XmmPerPixel,
                                                  double YmmPerPixel,
                                                  List<IProcessingFunction> pipeline);

        /// <summary>
        /// Executes measurement on an image using the specified processing pipeline
        /// This is the main entry point for vision-based measurements
        /// </summary>
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
