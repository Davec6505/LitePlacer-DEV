using System.Drawing;
namespace LitePlacer.CameraEngines { public interface IProcessingFunction { string Name { get; } Bitmap Process(Bitmap input); int ParameterInt { get; set; } double ParameterDouble { get; set; } double ParameterDoubleA { get; set; } double ParameterDoubleB { get; set; } double ParameterDoubleC { get; set; } int R { get; set; } int G { get; set; } int B { get; set; } } }
