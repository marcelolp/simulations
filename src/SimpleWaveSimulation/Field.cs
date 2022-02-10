using SimMath;
using System;
using System.Threading.Tasks;

namespace SimpleWaveSimulation
{
    /// <summary>
    /// Solves a simple wave equation numerically
    /// A couple of examples for interesting source functions and initial conditions can be found in the corresponding sections of the code
    /// </summary>
    internal class Field
    {
        private const bool SHOW_WAVE_ENERGY = false; // overrides SHOW_WAVE_ABS
        private const bool SHOW_WAVE_ABS = true;     // absolute value of the amplitude

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
        private float _max_wave_speed;              // for easier calculation of Courant number
        private float _max_courant = 0.5f;
        private float _dampening_coeff = 1.0f;    // speed of wave energy dissipation

        private bool[] _out_of_bounds;              // determines if a gridpoint is out of bounds
        private float[] _color;                     // contains (r,g,b)-color values for all gridpoints
        private float[] _u;                         // contains the simulation grid at the current timestep
        private float[] _u_last;                    // contains the simulation grid at the last timestep
        private float[] _u_temp;                    // contains the simulation grid for the next timestep
        private float[] _wave_speed;                // contains the wavespeed for a gridpoint

        private readonly Random _rng = new();       

        /// ============================== Source functions ===================================== ///
        float F(int x, int y) => 0.0f; // no source function
        //float F(int x, int y) => 30000.0f * (float)(Math.Sin(_t * 100.0f)) * Util.Gaussian(x, y, NX / 2, NY / 2, 15.0f, 15.0f, 1.0f);             // static oscillator
        //float F(int x, int y) => 30000.0f * (float)(Math.Sin(_t * 100.0f)) * Util.Gaussian(x, y, 3*NX / 5, NY / 4, 15.0f, 15.0f, 1.0f);           // static oscillator
        //float F(int x, int y) => Util.Gaussian(x, y, NX / 2 + _t * 400.0f, NY / 2, 15.0f, 15.0f, 20000.0f * (float)Math.Sin(_t * 100.0f));        // moving oscillator
        //float F(int x, int y) =>  (Util.Gaussian(x, y, 6*NX / 13, NY / 2, 15.0f, 15.0f, 1.0f)
        //                + Util.Gaussian(x, y, 7*NX / 13, NY / 2, 15.0f, 15.0f, 1.0f))
        //                * 30000.0f * (float)(Math.Sin(_t * 100.0f));                                                                                // double static oscillator

        /// ============================== Boundary functions ===================================== ///
        /// Pretty much any 2d signed distance function can be used here to create obstacles for the wave
        //bool B(int x, int y) => x == 0 || x == NX - 1 || y == 0 || y == NY - 1;                                                   // rectangular boundary
        //bool B(int x, int y) => Util.Dist(x, y, NX / 2, NY / 2) > 15 * NY / 32;                                                 // circular boundary
        //bool B(int x, int y) => x <= 20 + Util.TriangleWave(y, 30.0f, NY / 20) || x >= NX - 21 + Util.TriangleWave(y, 30.0f, NY / 20)
        //                     || y <= 20 + Util.TriangleWave(x, 30.0f, NX / 30) || y >= NY - 21 + Util.TriangleWave(x, 30.0f, NX / 30);  // spiked boundary
        //bool B(int x, int y) => Util.Dist(x, y, NX / 2, NY / 2) > 7 * NY / 16.0 
        //                      - Util.TriangleWave((float)(Math.Atan2(y - NY / 2, x - NX / 2) + Math.PI), 10.0f, 0.133f);        // spiked circular boundary
        bool B(int x, int y) => Util.UnionSDF(Util.UnionSDF(Util.CircleSDF(x, y, NX / 3, NY / 4, NX / 15),
                                                            Util.RectSDF(x, y, NX / 4, 2 * NY / 3, NX / 20, NY / 3)),
                                              Util.CircleSDF(x, y, 2 * NX / 3, NY / 2, NX / 8)) < 0
                                || x == 0 || x == NX - 1 || y == 0 || y == NY - 1;                                             // some circles and a rectangle

        /// ============================== Initial conditions ===================================== ///
        //float I(int x, int y) => 0.0f;                                                                                           // Empty field
        //float I(int x, int y) => (x == NX/2 - 5) ? 0.5f : 0.0f;                                                                     // Vertical strip
        //float I(int x, int y) => 0.02f * (float)Math.Sin(5.0f * x * (Math.PI * 2.0f / NX)) * (float)Math.Sin(0.5f * y * (Math.PI * 2.0f / NY)); // Standing wave in x-direction
        //float I(int x, int y) => Util.Gaussian(x, y, NX/6, NY/2, 15.0f, 15.0f, 1.0f);                                             // spot on the left side of the field
        //float I(int x, int y) => Util.Gaussian(x, y, NX/2, NY/2, 5.0f, 5.0f, 3.0f);                                               // spot in the middle side of the field
        float I(int x, int y) => Util.Dist(x, y, NX / 2, NY / 2) < NY/4 
                               ? 0.05f * (float)Math.Sin(Util.Dist(x, y, NX/2, NY/2) * 10.0f * (Math.PI * 4.0f / (NX))) : 0.0f;     // Concentric circles around the middle

        /// ============================== wavespeed conditions ===================================== ///
        float W(int x, int y) => 6.0f;                                                      // Equal wavespeed everywhere
        //float W(int x, int y) => (x + y) < NY ? 4.0f : 6.0f;                                // Triangular section in the ll corner has slower wavespeed
        //float W(int x, int y) => x < NX/3 ? 4.0f : 6.0f;                                    // Vertical section at the left has slower wavespeed
        //float W(int x, int y) => x < NX/2 ? 4.0f : 6.0f;                                    // Left half of the field has slower wavespeed
        //float W(int x, int y) => (x < NX / 2 && x > 2 * NX / 5) ? 4.0f : 6.0f;                // Vertical strip in the middle has lower wavespeed

        /// <summary>
        /// Creates a simulation grid of the given size and timestep (default dt = 0.1)
        /// </summary>
        internal Field(int nX, int nY, float dt = 0.1f)
        {
            NX = nX;
            NY = nY;
            _dt = dt;
            _out_of_bounds = new bool[NX * NY];
            _u = new float[NX * NY];
            _u_last = new float[NX * NY];
            _u_temp = new float[NX * NY];
            _wave_speed = new float[NX * NY];
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
            // Courant number influences simulation quality and speed
            float C_square_incomplete = (dt * dt / (_dx * _dx));
            // if it is to high the simulation might break, therefore reduce it and recalculate the maximum time delta

            // does not really work well, the values i set for _dx, _wave_speed and dt are stable and provide a somewhat decent wavespeed so changing them is not recommended
            if (C_square_incomplete * _max_wave_speed * _max_wave_speed > _max_courant)
            {
                C_square_incomplete = _max_courant / (_max_wave_speed * _max_wave_speed);
                dt = (_max_courant * _dx) / _max_wave_speed;
            }

            // avoid repeated calls to Properties NX, NY
            _t += dt;
            int NX = this.NX;
            int NY = this.NY;

            // incase dt has changed, return the simulated timestep
            adt = dt;


            /// TODO: run this on the gpu instead with AleaGPU 
            Parallel.For(0, NY, (y) =>
            {
                // variables have to be declared in here, otherwise its not thread-save
                float val;
                int y_minus = (y - 1 < 0)       ? 0       : y - 1;
                int y_plus =  (y + 1 > NY - 1)  ? NY - 1  : y + 1;

                for (int x = 0; x < NX; x++)
                {
                    int x_minus = (x - 1 < 0)       ? 0       : x - 1;
                    int x_plus  = (x + 1 > NX - 1)  ? NX - 1  : x + 1;

                    float C_square = _wave_speed[x + y * NX];
                    C_square *= C_square * C_square_incomplete;



                    // if the point was already marked as out of bounds, dont recompute the exdpensive boundary condition
                    // only works for the reflecting boundarys but should improve the speed quite a bit if there are a lot of obstacles
                    bool out_of_bounds = _out_of_bounds[x + y * NX]; 
                    if (out_of_bounds)
                    {
                        val = 0.0f;
                    } else if (B(x, y)) 
                    {
                        // No boundary like on a torus surface
                        //float xym, xpy, xmy, xyp;
                        //if (x == 0)
                        //    xmy = _u[NX-1 + y * NX];
                        //else 
                        //    xmy = _u[x_minus + y * NX];
                        //if (y == 0)
                        //    xym = _u[x + (NY-1) * NX];
                        //else
                        //    xym = _u[x + y_minus * NX];
                        //if (x == NX - 1)
                        //    xpy = _u[0 + y * NX];
                        //else
                        //    xpy = _u[x_plus + y * NX];
                        //if (y == NY - 1)
                        //    xyp = _u[x + 0 * NX];
                        //else
                        //    xyp = _u[x + y_plus * NX];

                        //val = 2.0f * _u[x + y * NX] - _u_last[x + y * NX] + C_square * (xym + xpy + xmy + xyp - 4.0f * _u[x + y * NX]) + dt * (dt * f());

                        //Reflecting boundary conditions(+inversed sign)
                        _out_of_bounds[x + y * NX] = true;
                        val = 0.0f;
                        if (_first)
                        {
                            _out_of_bounds[x + y * NX] = true;
                            val = 0.0f;
                        }

                    }
                    else
                    {
                        if (_first)
                        {
                            val = _u[x + y * NX] + 0.5f * C_square * (_u[x + y_minus * NX] + _u[x_plus + y * NX] + _u[x_minus + y * NX] + _u[x + y_plus * NX]
                                - 4.0f * _u[x + y * NX]) + dt * (dt * F(x, y));
                        }
                        val = 2.0f * _u[x + y * NX] - _u_last[x + y * NX] + C_square * (_u[x + y_minus * NX] + _u[x_plus + y * NX] + _u[x_minus + y * NX] + _u[x + y_plus * NX] 
                            - 4.0f * _u[x + y * NX] ) + dt * (dt * F(x, y));
                    }

                    // 100% not the right way to do this but looks nice. This makes the dampening proportional to the energy of the wave at that point
                    // from what I understand it should rely on the wavelength instead, absorbing only a specific range
                    //val *= 1.0f - _dampening_coeff * dt;         
                    _u_temp[x + y * NX] = val;
                    int idx = (3 * x) + (3 * y) * NX;

                    if (SHOW_WAVE_ENERGY)
                        (_color[0 + idx], _color[1 + idx], _color[2 + idx]) = out_of_bounds ? (0.0f, 0.0f, 0.0f) : Util.ToRgbJet(0.0f, 1.0f, val * val);
                    else if (SHOW_WAVE_ABS)
                        (_color[0 + idx], _color[1 + idx], _color[2 + idx]) = out_of_bounds ? (0.0f, 0.0f, 0.0f) : Util.ToRgbJet(0.0f, 1.0f, Math.Abs(val));
                    else
                        (_color[0 + idx], _color[1 + idx], _color[2 + idx]) = out_of_bounds ? (0.0f, 0.0f, 0.0f) : Util.ToRgbJet(-1.0f, 1.0f, val);

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
        /// Function to initialize the wave field and wavespeed field
        /// </summary>
        private void Init()
        {
            for (int y = 0; y < NY; y++)
            {
                for (int x = 0; x < NX; x++)
                {
                    _u[x + y * NX] = I(x, y);
                    float wavespeed = W(x, y);

                    _max_wave_speed = wavespeed > _max_wave_speed ? wavespeed : _max_wave_speed;
                    _wave_speed[x + y * NX] = wavespeed;

                    int idx = (3 * x) + (3 * y) * NX;
                    (_color[0 + idx], _color[1 + idx], _color[2 + idx]) = (0.0f, 0.0f, 0.0f);
                }
            }

        }

    }
}
