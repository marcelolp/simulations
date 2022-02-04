using System;

namespace SimMath
{

    /// <summary>
    /// Float based math utility functions
    /// </summary>
    public static class Util
    {

        public static float TriangleWave(float t, float amp, float lambda2)
        {
            return (4.0f * amp / lambda2) * Math.Abs(((t - lambda2 / 4.0f) % lambda2) - lambda2/2.0f) - amp;
        }

        public static float Dist(float x, float y, float cx, float cy)
        {
            return (float)Math.Sqrt((x - cx)*(x - cx) + (y - cy)*(y - cy));
        }

        /// <summary>
        /// Gaussian function of the form f(x, y) = a * exp(-((x - x0)^2/(2*sigmax) + ((y - y0)^2)/(2*sigmay)))
        /// </summary>
        /// <param name="x">x-Coordinate of f(x, y)</param>
        /// <param name="y">y-Coordinate of f(x, y)</param>
        /// <param name="x0">offset of the peak in direction of the y-axis</param>
        /// <param name="y0">offset of the peak in direction of the y-axis</param>
        /// <param name="sigmax">standard deviation ("width" of the function) in the x-axis</param>
        /// <param name="sigmay">standard deviation ("width" of the function) in the y-axis</param>
        /// <param name="amp">amplitude of the function at the peak f(x0, y0)</param>
        /// <returns></returns>
        public static float Gaussian(float x, float y, float x0, float y0, float sigmax, float sigmay, float amp)
        {
            float xd = ((x - x0) * (x - x0))/(2.0f * sigmax);
            float yd = ((y - y0) * (y - y0))/(2.0f * sigmay);

            float exponent = -(xd + yd);
            return (float)(amp * Math.Exp(exponent));
        }

        /// <summary>
        /// Linear interpolation between v1 and v2 with t in [0, 1]
        /// Return v1 if t = 0, v2 if t = 1
        /// </summary>
        public static float Lerp(float v1, float v2, float t)
        {
            return (v2 * t) + (v1 * (1.0f - t));
        }

        /// <summary>
        /// Uses a cosine to interpolate instead of a linear function
        /// </summary>
        public static float CosInterpolation(float v1, float v2, float t)
        {
            float f = (1 - (float)System.Math.Cos(t * System.Math.PI)) * 0.5f;
            return v1 * (1 - f) + v2 * f;
        }

        /// <summary>
        /// Interpolates v1,...,v4 with a cubic polynomial
        /// Derivation from https://www.paulinternet.nl/?page=bicubic
        /// </summary>
        public static float CubicInterpolation(float v1, float v2, float v3, float v4, float t)
        {
            return v2 + 0.5f * t * (v3 - v1 + t * (4.0f * v3 - 5.0f * v2 - v4 + 2.0f * v1 + t * (3.0f * (v2 - v3) - v1 + v4)));
        }

        /// <summary>
        /// Returns max if v > max, min if v < min, v otherwise
        /// </summary>
        public static float Clamp(float min, float max, float v)
        {
            return v > max ? max : v < min ? min : v;
        }

        /// <summary>
        /// Jet color mapping from a given range of values
        /// see https://stackoverflow.com/questions/7706339 (eyeballed the jet color mapping, blue is a bit to dar at the beginning)
        /// </summary>
        public static (float r, float b, float g) ToRgbJet(float minv, float maxv, float val)
        {
            val = Clamp(minv, maxv, val);
            float dif = maxv - minv;

            float r, g, b;
            // 0.125 + 0.25 + 0.25 + 0.25 + 0.125 are the breaks in the color gradients
            if (val < (minv + 0.125 * dif))
            {
                r = 0.0f;
                g = 0.0f;
                b = 4.0f * ((val - minv) / dif) + 0.5f;
            }
            else if (val < (minv + 0.375f * dif))
            {
                r = 0.0f;
                g = 4.0f * (val - minv - 0.125f * dif) / dif;
                b = 1.0f;
            }
            else if (val < (minv + 0.625f * dif))
            {
                r = 4.0f * (val - minv - 0.375f * dif) / dif;
                g = 1.0f;
                b = 1.0f + 4.0f * (minv + 0.375f * dif - val) / dif;
            }
            else if (val < (minv + 0.875f * dif))
            {
                r = 1.0f;
                g = 1.0f + 4.0f * (minv + 0.625f * dif - val) / dif;
                b = 0.0f;
            }
            else
            {
                r = 1.0f + 4.0f * (minv + 0.875f * dif - val) / dif;
                g = 0.0f;
                b = 0.0f;
            }

            return (r, g, b);
        }

        public static (float r, float b, float g) ToRgb(float minv, float maxv, float val)
        {
            val = Clamp(minv, maxv, val);
            float dif = maxv - minv;

            float r, g, b;

            // red, green blue all have equal functions, shifted by 85 and 170 respectively
            if (val < (minv + 0.25f * dif))
            {
                r = 0.0f;
                g = 4.0f * (val - minv) / dif;
                b = 1.0f;
            }
            else if (val < (minv + 0.5f * dif))
            {
                r = 0.0f;
                g = 1.0f;
                b = 1.0f + 4.0f * (minv + 0.25f * dif - val) / dif;
            }
            else if (val < (minv + 0.7f * dif))
            {
                r = 4.0f * (val - minv - 0.5f * dif) / dif;
                g = 1.0f;
                b = 0.0f;
            }
            else
            {
                r = 1.0f;
                g = 1.0f + 4.0f * (minv + 0.75f * dif - val) / dif;
                b = 0.0f;
            }
            return (r, g, b);
        }

        public static (float r, float b, float g) ToBv(float minv, float maxv, float val)
        {
            val = Clamp(minv, maxv, val);
            float dif = maxv - minv;
            val = (val - minv) * (1.0f / dif);
            return (val, val, val);
        }

    }
}
