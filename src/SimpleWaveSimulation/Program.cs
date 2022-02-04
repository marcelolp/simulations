using OpenTK;
using OpenTK.Graphics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace SimpleWaveSimulation
{
    class Program
    {

        static void Main(string[] args)
        {
            NativeWindowSettings nativeWindowSettings = new()
            {
                Size = new Vector2i(800, 600),
                Title = "Simulation",
                Flags = ContextFlags.ForwardCompatible,
                WindowState = WindowState.Maximized,
            };

            using (Window window = new(GameWindowSettings.Default, nativeWindowSettings))
            {
                window.Run();
            }
        }
    }
}
