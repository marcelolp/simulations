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

        private bool _first = true;                 // first time step has to be treated differently as _u_last = undefined

        private float _dx = 0.01f;                  // resolution of 1 step in the x-direction
        private float _dy = 0.01f;                  // resolution of 1 step in the y-direction
        private float _dt;                          // time step size
        private float _t = 0.0f;                    // total time passed

        private float[] _color;                     // contains (r,g,b)-color values for all gridpoints
        private float[] _u;                         // contains the simulation grid at the current timestep
        private float[] _u_last;                    // contains the simulation grid at the last timestep
        private float[] _u_temp;                    // contains the simulation grid for the next timestep

        private readonly Random _rng = new();

        /// ==================================== Initial condition function ======================================= ///
        (float, float) I(int x, int y) => (x, y);
        /// ======================================== Feed function for u ========================================== ///
        float F(int x, int y, float u, float v, float t) => 0.1f;
        /// ======================================== Kill function for v ========================================== ///
        float K(int x, int y, float u, float v, float t) => 0.1f;
        /// ================================= Reaction ratio between u and v ====================================== ///
        float R = 2.0f; // #u + R#v -> (1+R)#v
        /// =================================== Diffusion rate for u and v ======================================== ///
        float Du = 1.0f;
        float Dv = 0.5f;

        /// <summary>
        /// Creates a simulation grid of the given size and timestep (default dt = 0.1)
        /// </summary>
        internal Field(int nX, int nY, float dt = 0.1f)
        {
            NX = nX;
            NY = nY;
            _dt = dt;
            _u = new float[NX * NY];
            _u_last = new float[NX * NY];
            _u_temp = new float[NX * NY];
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

            Parallel.For(0, NY, (y) =>
            {

                for (int x = 0; x < NX; x++)
                {
                    

                }
            });

            // cycle through the grids to avoid creating a new one every iteration
            float[] _tmp;
            _tmp = _u_last;
            _u_last = _u;
            _u = _u_temp;
            _u_temp = _tmp;
            _first = false;
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
                    int idx = (3 * x) + (3 * y) * NX;
                    (_color[0 + idx], _color[1 + idx], _color[2 + idx]) = (0.0f, 0.0f, 0.0f);
                }
            }

        }

    }
}
