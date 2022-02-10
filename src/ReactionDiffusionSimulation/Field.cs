using SimMath;
using System;
using System.Threading.Tasks;

namespace ReactionDiffusionSimulation
{
    /// <summary>
    /// Solves a simple wave equation numerically
    /// A couple of examples for interesting source functions and initial conditions can be found in the corresponding sections of the code
    /// </summary>
    internal class Field
    {
        internal float[] Color
        {
            get { return _color; }
            private set { _color = value; }
        }
        internal int NX { get; private set; }       // #gridpoints in x-direction
        internal int NY { get; private set; }       // #gridpoints in y-direction

        private float _dx = 0.01f;                  // resolution of 1 step in the x-direction
        private float _dy = 0.01f;                  // resolution of 1 step in the y-direction
        private float _dt;                          // time step size
        private float _t = 0.0f;                    // total time passed
        private float _umin;
        private float _vmax;

        private float[] _color;                     // contains (r,g,b)-color values for all gridpoints
        private float[] _u;                         // contains the simulation grid at the current timestep
        private float[] _v;                         // contains the simulation grid at the current timestep
        private float[] _u_temp;                    // contains the simulation grid for the next timestep
        private float[] _v_temp;                    // contains the simulation grid for the next timestep

        private readonly Random _rng = new();

        /// ==================================== Initial condition function ======================================= ///
        (float, float) I(int x, int y) => (Util.Dist(x, y, NX/2, NY/2) < NY/10 + _rng.Next(NY/10)) ? (0.5f, 0.25f) : (1.0f, 0.0f);
        /// ======================================== Feed function for u ========================================== ///
        //float F(int x, int y, float t) => 0.02f + 0.03f * y/NY;
        //float F(int x, int y, float t) => 0.006f + 0.005f * y/NY;
        float F(int x, int y, float t) => 0.0f + 0.01f * y/NY;
        /// ======================================== Kill function for v ========================================== ///
        //float K(int x, int y, float t) => 0.05f + 0.015f * x/NX;
        //float K(int x, int y, float t) => 0.045f + 0.003f * x/NX;
        float K(int x, int y, float t) => 0.03f + 0.02f * x/NX;
        /// ================================ Reaction function between u and v ==================================== ///
        float R(int x, int y, float u, float v, float t) => u * v * v;
        /// =================================== Diffusion rate for u and v ======================================== ///
        private float _Du = 0.20f;
        private float _Dv = 0.1f;

        /// <summary>
        /// Creates a simulation grid of the given size and timestep (default dt = 0.1)
        /// </summary>
        internal Field(int nX, int nY, float dt = 0.1f)
        {
            NX = nX;
            NY = nY;
            _dt = dt;
            _u = new float[NX * NY];
            _v = new float[NX * NY];
            _u_temp = new float[NX * NY];
            _v_temp = new float[NX * NY];
            _color = new float[NX * NY * 3];

            Init();
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

            // avoid repeated calls to Properties NX, NY
            _t += dt;
            int NX = this.NX;
            int NY = this.NY;

            // incase dt has changed, return the simulated timestep
            adt = dt;

            // the only sensible boundary is to continue the surface like on a torus
            Parallel.For(0, NY, (y) =>
            {
                int yminus = y - 1 < 0 ? NY - 1 : y - 1;
                int yplus  = y + 1 > NY - 1 ? 0 : y + 1;
                
                for (int x = 0; x < NX; x++)
                {
                        
                    int xminus = x - 1 < 0 ? NX - 1 : x - 1;
                    int xplus  = x + 1 > NX - 1 ? 0 : x + 1;

                    if (x == NX/3 && y == NY/3)
                    {
                        x++;
                        x--;
                    }

                    float u_xy = _u[x + y * NX];
                    float v_xy = _v[x + y * NX];

                    _umin = u_xy < _umin ? u_xy : _umin;
                    _vmax = v_xy > _vmax ? v_xy : _vmax;

                    // Laplacian in x, y-direction
                    float nabla_u = -(4.0f * u_xy) + _u[xminus + y * NX] + _u[xplus + y * NX] + _u[x + yminus * NX] + _u[x + yplus * NX];
                    float nabla_v = -(4.0f * v_xy) + _v[xminus + y * NX] + _v[xplus + y * NX] + _v[x + yminus * NX] + _v[x + yplus * NX];

                    float r_val = R(x, y, u_xy, v_xy, _t);
                    float f_val = F(x, y, _t);
                    float k_val = K(x, y, _t);

                    float u_val = u_xy + dt * (_Du * nabla_u - r_val + f_val * (1.0f - u_xy));
                    float v_val = v_xy + dt * (_Dv * nabla_v + r_val - (k_val + f_val) * v_xy);

                    _u_temp[x + y * NX] = Util.Clamp(0.0f, 1.0f, u_val);
                    _v_temp[x + y * NX] = Util.Clamp(0.0f, 1.0f, v_val);

                    int idx = 3*(x + y * NX);
                    (_color[0 + idx], _color[1 + idx], _color[2 + idx]) = Util.ToRgbJet(0.0f, _vmax, v_val);
                }
            });

            // cycle through the grids to avoid creating a new one every iteration
            float[] _tmp;
            _tmp = _u;
            _u = _u_temp;
            _u_temp = _tmp;
            _tmp = _v;
            _v = _v_temp;
            _v_temp = _tmp;
        }


        /// <summary>
        /// Function to initialize the field
        /// </summary>
        private void Init()
        {
            for (int y = 0; y < NY; y++)
            {
                for (int x = 0; x < NX; x++)
                {
                    int idx = x + y * NX;
                    (_u[x + y * NX], _v[x + y * NX]) = I(x, y);
                    idx *= 3;
                    (_color[0 + idx], _color[1 + idx], _color[2 + idx]) = (0.0f, 0.0f, 0.0f);
                }
            }

        }

    }
}
