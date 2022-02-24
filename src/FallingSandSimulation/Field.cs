using SimMath;
using System;
using System.Threading.Tasks;

namespace FallingSandSimulation
{
    enum Material 
    { 
        None,
        Bound,
        Sand, 
        Gas, 
        Water,
        Plant,
        Fire, 
        Last
    }

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
        internal float MouseX { get; set; } = 0.0f;
        internal float MouseY { get; set; } = 0.0f;

        private float _dx = 0.01f;                  // resolution of 1 step in the x-direction
        private float _dy = 0.01f;                  // resolution of 1 step in the y-direction
        private float _dt;                          // time step size
        private float _t = 0.0f;                    // total time passed

        private float[] _color;                     // contains (r,g,b)-color values for all gridpoints
        private Material[] _u;                      // contains the simulation grid at the current timestep
        private bool[] _u_update;                          // contains the simulation grid at the current timestep

        private int _step = 0;
        private int _brush_radius = 2;

        private Material _used_mat = Material.Sand;
        private readonly Random _rng = new();

        /// <summary>
        /// Creates a simulation grid of the given size and timestep (default dt = 0.1)
        /// </summary>
        internal Field(int nX, int nY, float dt = 0.1f)
        {
            NX = nX;
            NY = nY;
            _dt = dt;
            _u = new Material[NX * NY];
            _u_update = new bool[NX * NY];
            _color = new float[NX * NY * 3];

            Init();
        }

        internal void NextMaterial() 
        {
            _used_mat += 1;
            _used_mat = (_used_mat == Material.Last) ? Material.None : _used_mat;
        }

        internal void DrawMaterial(float mouseX, float mouseY) 
        {
            if (mouseY - _brush_radius > NY - 1 || mouseX - _brush_radius > NX - 1 || mouseY + _brush_radius < 0 || mouseX + _brush_radius < 0)
            {
                return;
            }

            for (int y = (int)(mouseY - _brush_radius); y <= (int)(mouseY + _brush_radius); y++)
            {
                for (int x = (int)(mouseX - _brush_radius); x <= (int)(mouseX + _brush_radius); x++)
                {
                    if (y >= 0 && y <= NY-1 && x >= 0 && x <= NX-1 && Util.Dist(y, x, mouseY, mouseX) <= _brush_radius)
                    {
                        _u[x + y * NX] = _used_mat;
                    }
                }
            }
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
            _step++;
            // avoid repeated calls to Properties NX, NY
            _t += dt;
            int NX = this.NX;
            int NY = this.NY;

            // incase dt has changed, return the simulated timestep
            adt = dt;

            for (int y = 0; y < NY; y++)
            {
                // variables have to be declared in here, otherwise its not thread-save
                bool y_minus = y - 1 < 0;       // true if out of bounds
                bool y_plus = y + 1 > NY - 1;

                int ym = y_minus ? 0 : y - 1;
                int yp = y_plus ? NY-1 : y + 1;

                for (int x = 0; x < NX; x++)
                {
                    bool x_minus = x - 1 < 0;
                    bool x_plus = x + 1 > NX - 1;

                    int xm = x_minus ? 0 : x - 1;
                    int xp = x_plus ? NX - 1 : x + 1;
                    int dir;
                    if (_u_update[x + y * NX])
                    {
                        _u_update[x + y * NX] = false;
                    } else {
                        Material old_mat = _u[x + y * NX];
                        switch (old_mat)
                        {
                            case Material.None:
                            case Material.Bound:
                                _u[x + y * NX] = old_mat;
                                break;
                            case Material.Sand:
                                dir = _rng.Next(2);
                                if (!y_plus && (_u[x + yp * NX] == Material.None || _u[x + yp * NX] == Material.Water || _u[x + yp * NX] == Material.Gas))
                                {
                                    Switch(x, y, x, yp);
                                }
                                else if (dir == 0 && !y_plus && !x_minus && (_u[xm + yp * NX] == Material.None || _u[xm + yp * NX] == Material.Water || _u[xm + yp * NX] == Material.Gas))
                                {
                                    Switch(x, y, xm, yp);
                                }
                                else if (dir == 1 && !y_plus && !x_plus && (_u[xp + yp * NX] == Material.None || _u[xp + yp * NX] == Material.Water || _u[xp + yp * NX] == Material.Gas))
                                {
                                    Switch(x, y, xp, yp);
                                }
                                break;
                            case Material.Gas:
                                dir = _rng.Next(5);
                                if (dir <= 1 && !y_minus && (_u[x + ym * NX] == Material.None || _u[x + ym * NX] == Material.Water))
                                {
                                    Switch(x, y, x, ym);
                                }
                                else if (dir == 2 && !y_plus && _u[x + yp * NX] == Material.None)
                                {
                                    Switch(x, y, x, yp);
                                }
                                else if (dir == 3 && !x_minus && _u[xm + y * NX] == Material.None)
                                {
                                    Switch(x, y, xm, y);
                                }
                                else if (dir == 4 && !x_plus && _u[xp + y * NX] == Material.None)
                                {
                                    Switch(x, y, xp, y);
                                }
                                break;
                            case Material.Water:
                                dir = _rng.Next(2);
                                if (!y_plus && _u[x + yp * NX] == Material.None)
                                {
                                    Switch(x, y, x, yp);
                                }
                                else if (dir == 0 && !y_plus && !x_minus && _u[xm + yp * NX] == Material.None)
                                {
                                    Switch(x, y, xm, yp);
                                }
                                else if (dir == 1 && !y_plus && !x_plus && _u[xp + yp * NX] == Material.None)
                                {
                                    Switch(x, y, xp, yp);
                                }
                                else if (dir == 0 && !x_plus && _u[xp + y * NX] == Material.None)
                                {
                                    Switch(x, y, xp, y);
                                }
                                else if (dir == 1 && !x_minus && _u[xm + y * NX] == Material.None)
                                {
                                    Switch(x, y, xm, y);
                                }
                                break;
                            case Material.Plant:
                                dir = _rng.Next(500);
                                if (dir < 10 && !y_plus && (_u[x + ym * NX] == Material.None || _u[x + ym * NX] == Material.Gas))
                                {
                                    _u[x + ym * NX] = Material.Plant;
                                    _u_update[x + ym * NX] = true;
                                }
                                else if (dir == 10 && !x_minus && _u[xm + y * NX] == Material.None)
                                {
                                    Switch(x, y, xm, y);
                                }
                                else if (dir == 11 && !x_plus && _u[xp + y * NX] == Material.None)
                                {
                                    Switch(x, y, xp, y);
                                }
                                break;
                            case Material.Fire:
                                dir = _rng.Next(20);
                                if (dir >= 16)
                                {
                                    _u[x + y * NX] = dir == 19 ? Material.Gas : Material.None;
                                } else if (!y_plus && _u[x + yp * NX] == Material.Plant)
                                {
                                    _u[x + yp * NX] = Material.Fire;
                                    _u_update[x + yp * NX] = true;
                                }
                                else if (!x_minus && _u[xm + y * NX] == Material.Plant)
                                {
                                    _u[xm + y * NX] = Material.Fire;
                                    _u_update[xm + y * NX] = true;
                                }
                                else if (!x_plus && _u[xp + y * NX] == Material.Plant)
                                {
                                    _u[xp + y * NX] = Material.Fire;
                                    _u_update[xp + y * NX] = true;
                                }
                                if (!y_minus && _u[x + ym * NX] == Material.Plant)
                                {
                                    _u[x + ym * NX] = Material.Fire;
                                    _u_update[x + ym * NX] = true;
                                }
                                break;
                            default:
                                _u[x + y * NX] = old_mat;
                                break;
                        }
                    }

                    int idx = (3 * x) + (3 * (NY - y - 1)) * NX;
                    if (Util.Dist(x, y, MouseX, MouseY) <= _brush_radius)
                    {
                        (_color[0 + idx], _color[1 + idx], _color[2 + idx]) = Util.Lerp(MapToColor(_u[x + y * NX]), MapToColor(_used_mat), 0.5f);
                    }
                    else 
                    {
                        (_color[0 + idx], _color[1 + idx], _color[2 + idx]) = MapToColor(_u[x + y * NX]);
                    }
                }
            }
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
                    _u[x + y * NX] = 0.0f;

                    int idx = (3 * x) + (3 * y) * NX;
                    (_color[0 + idx], _color[1 + idx], _color[2 + idx]) = (0.0f, 0.0f, 0.0f);
                }
            }
        }

        private void Switch(int x1, int y1, int x2, int y2) 
        {
            Material tmp = _u[x2 + y2 * NX];
            _u[x2 + y2 * NX] = _u[x1 + y1 * NX];
            _u[x1 + y1 * NX] = tmp;
            _u_update[x2 + y2 * NX] = true;
        }

        private (float, float, float) MapToColor(Material mat)
        {
            switch (mat) {
                case Material.None:
                    return (0.0f, 0.0f, 0.0f);
                case Material.Bound:
                    return (0.2f, 0.2f, 0.2f);
                case Material.Sand:
                    return (0.85f, 0.80f, 0.05f);
                case Material.Gas:
                    return (0.4f, 0.7f, 0.8f);
                case Material.Water:
                    return (0.2f, 0.4f, 1.0f);
                case Material.Plant:
                    return (0.35f, 0.85f, 0.2f);
                case Material.Fire:
                    return (0.95f, 0.25f, 0.0f);
                default:
                    return (1.0f, 0.0f, 1.0f);
            }
        }

    }
}
