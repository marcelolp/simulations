using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace Template
{
    internal class Window : GameWindow
    {
        private const bool SAVE_TO_FILE = false;
        private const bool USE_REAL_TIME = true;
        private const int SIM_WIDTH = 500;
        private const int SIM_HEIGHT = 300;
        private const float FIXED_DELTA = 0.01f; 
        private const float MAX_FRAMERATE = 60.0f;
        private const float MIN_DELTA = 1000.0f / MAX_FRAMERATE;

        private int _vertexBufferObject;
        private int _vertexColorBufferObject;
        private int _vertexArrayObject;
        private int _elementBufferObject;

        private Shader _shader;
        private Field _field;

        private readonly Stopwatch sim_delta;
        private readonly Stopwatch sim_time;
        private readonly float[] _vertices;
        private readonly  uint[] _indices;
        private readonly float[] _colors;


        internal Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings) 
        {
            VSync = VSyncMode.Adaptive;
            RenderFrequency = MAX_FRAMERATE;
            UpdateFrequency = USE_REAL_TIME ? MAX_FRAMERATE : 0.0; // Update as fast as possible if realtime visualization is not important

            _field = USE_REAL_TIME ? new(nX : SIM_WIDTH, nY : SIM_HEIGHT) : new(nX: SIM_WIDTH, nY: SIM_HEIGHT, dt: FIXED_DELTA);
            _colors = _field.Color;
            _vertices = new float[_field.NX * _field.NY * 3];
            _indices = new uint[(_field.NX - 1) * (_field.NY - 1) * 6];


            float deltaX = 2.0f / (_field.NX - 1.0f);
            float deltaY = 2.0f / (_field.NY - 1.0f);

            for (int y = 0; y < _field.NY; y++)  
            {
                for (int x = 0; x < _field.NX; x++)
                {
                    _vertices[0 + (3 * x) + _field.NX * (3 * y)] = x * deltaX - 1.0f;   // x-coord
                    _vertices[1 + (3 * x) + _field.NX * (3 * y)] = y * deltaY - 1.0f;   // y-coord
                    _vertices[2 + (3 * x) + _field.NX * (3 * y)] = 0.0f;                // z-coord
                }
            }

            for (int y = 0; y < _field.NY - 1; y++)
            {
                for (int x = 0; x < _field.NX - 1; x++)
                {
                    int index = (6 * x) + (_field.NX - 1) * (6 * y);

                    _indices[0 + index] = (uint)(x       + _field.NX *       y); //ul-vertex, 1st tri
                    _indices[1 + index] = (uint)((x + 1) + _field.NX *       y); //ur-vertex, 1st tri
                    _indices[2 + index] = (uint)(x       + _field.NX * (y + 1)); //ll-vertex, 1st tri
                    _indices[3 + index] = (uint)((x + 1) + _field.NX * (y + 1)); //lr-vertex, 2nd tri
                    _indices[4 + index] = (uint)((x + 1) + _field.NX *       y); //ur-vertex, 2nd tri
                    _indices[5 + index] = (uint)(x       + _field.NX * (y + 1)); //ll-vertex, 2nd tri
                }
            }

            sim_delta = Stopwatch.StartNew();
            sim_time = Stopwatch.StartNew();
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);

            /// =============================================== VBOs ===================================================

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            _vertexColorBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexColorBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _colors.Length * sizeof(float), _colors, BufferUsageHint.StreamDraw);

            /// ============================================ Attributes ================================================

            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);  // vertices, three elements X, Y, Z

            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexColorBufferObject);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);  // colors, three elements R, G, B

            /// =============================================== EBO ====================================================
            
            _elementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

            _shader = new Shader("../../../Shaders/shader.vert", "../../../Shaders/default.frag");
            _shader.Use();

        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);


            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _shader.Use();

            float time = (float)sim_time.Elapsed.TotalSeconds;
            GL.Uniform1(GL.GetUniformLocation(_shader.Handle, "time"), time);
            
            GL.BindVertexArray(_vertexArrayObject);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();

            
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            long delta = sim_delta.ElapsedMilliseconds;
            sim_delta.Restart();

            Title = $"{1.0f / (delta / 1000.0f):0.00} fps";

            if (USE_REAL_TIME)
                _field.Iterate(delta / 1000.0f, out float adt);
            else
                _field.Iterate(out float adt);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexColorBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _colors.Length * sizeof(float), _colors, BufferUsageHint.StreamDraw);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
        }

    }
}
