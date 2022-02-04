using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimMath;

namespace Template
{
    /// <summary>
    /// Overwrite this class with the simulation
    /// The template simulates the heat equation as an example for how it can be used
    /// </summary>
    internal class Field
    {
        public float[] Color
        {
            get { return _color; }
            private set { _color = value; }
        }
        internal int NX { get; private set; }
        internal int NY { get; private set; }

        private float _dt = 0.01f;
        private float _t = 0.0f;

        private float[] _color;
        private float[] _field;

        private readonly Random _rng = new();

        /// <summary>
        /// Creates a simulation grid of the given size and timestep (default dt = 0.1)
        /// </summary>
        internal Field(int nX, int nY, float dt = 0.1f)
        {
            NX = nX;
            NY = nY;
            _dt = dt;
            _field = new float[NX * NY];
            _color = new float[NX * NY * 3];

            InitField();
        }

        /// <summary>
        /// Advances the simulation by a predefined timestep
        /// Variable adt contains the actually used timestep
        /// </summary>
        internal void Iterate(out float adt)
        {
            Iterate(_dt, out adt);
        }

        /// <summary>
        /// Advances the simulation by the given timestep
        /// Timestep may be overwritten if it is to large for a stable simulation
        /// Variable adt contains the actually used timestep
        /// </summary>
        internal void Iterate(float dt, out float adt)
        {
            _t += dt;
            int NX = this.NX;
            int NY = this.NY;

            // thermal diffusivity of water
            float alpha = 10.018f;

            // numerical method breaks if timesteps get to large
            dt = (dt > 1.0f / (8.0f * alpha)) ? (1.0f / (8.0f * alpha)) : dt;
            adt = dt;

            float[] tmpField = new float[NX * NY];


            Parallel.For(0, NY, (y) =>
            {
                for (int x = 0; x < NX; x++)
                {
                    float fx0y;
                    float fx1y;
                    float fxy0;
                    float fxy1;
                    float fxy = _field[x + y * NX];

                    if (x == 0)
                        fx0y = _field[x + y * NX];
                    else
                        fx0y = _field[x - 1 + y * NX];


                    if (y == 0)
                        fxy0 = _field[x + (y) * NX];
                    else
                        fxy0 = _field[x + (y - 1) * NX];


                    if (x == NX - 1)
                        fx1y = _field[x + y * NX];
                    else
                        fx1y = _field[x + 1 + y * NX];


                    if (y == NY - 1)
                        fxy1 = _field[x + (y) * NX];
                    else
                        fxy1 = _field[x + (y + 1) * NX];


                    float val = fxy + (fx0y + fx1y + fxy0 + fxy1 - 4.0f * fxy) * dt * alpha;
                    tmpField[x + y * NX] = Util.Clamp(0.0f, 1.0f, val);
                    int idx = (3 * x) + (3 * y) * NX;
                    (_color[0 + idx], _color[1 + idx], _color[2 + idx]) = Util.ToRgbJet(0.0f, 1.0f, val);
                }
            });
            _field = tmpField;
            
        }

        /// <summary>
        /// Function to initialize the field with some values
        /// </summary>
        private void InitField()
        {
            for (int y = 0; y < NY; y++)
            {
                for (int x = 0; x < NX; x++)
                {
                    if (x % 40 < 20 || y % 40 < 20)
                    {
                        _field[x + y * NX] = 1.0f;
                    }
                    if (x % 80 < 20 || y % 80 < 20)
                    {
                        _field[x + y * NX] = 0.3f;
                    }
                    int idx = (3 * x) + (3 * y) * NX;
                    (_color[0 + idx], _color[1 + idx], _color[2 + idx]) = Util.ToRgbJet(0.0f, 1.0f, 1.0f);

                }        
            }            
        }
                
    }
}
