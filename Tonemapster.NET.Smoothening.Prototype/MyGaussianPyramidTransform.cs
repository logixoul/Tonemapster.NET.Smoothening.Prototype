using System;
using System.Collections.Generic;
using System.Text;
using UMapx.Core;
using UMapx.Transform;
using System;
using UMapx.Visualization;


namespace Tonemapster.NET.Smoothening.Prototype
{
    /// <summary>
    /// Defines the Gaussian pyramid transform.
    /// </summary>
    /// <remarks>
    /// More information can be found on the website:
    /// http://www.cs.toronto.edu/~jepson/csc320/notes/pyramids.pdf
    /// </remarks>
    [Serializable]
    public class MyGaussianPyramidTransform
    {
        #region Private data
        int radius;
        int levels;
        #endregion

        #region Pyramid components
        /// <summary>
        /// Initializes the Gaussian pyramid transform.
        /// </summary>
        public MyGaussianPyramidTransform()
        {
            this.Levels = int.MaxValue;
            this.Radius = 2;
        }
        /// <summary>
        /// Initializes the Gaussian pyramid transform.
        /// </summary>
        /// <param name="levels">Number of levels</param>
        /// <param name="radius">Radius</param>
        public MyGaussianPyramidTransform(int levels, int radius = 2)
        {
            this.Levels = levels;
            this.Radius = radius;
        }
        /// <summary>
        /// Gets or sets number of levels.
        /// </summary>
        public int Levels
        {
            get
            {
                return this.levels;
            }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Invalid argument value");

                this.levels = value;
            }
        }
        /// <summary>
        /// Gets or sets radius.
        /// </summary>
        public int Radius
        {
            get
            {
                return this.radius;
            }
            set
            {
                this.radius = value;
            }
        }
        #endregion

        #region Apply voids
        // **************************************************
        //            Gaussian Pyramid Transform
        // **************************************************
        // ORIGINALS: Burt, P., and Adelson, E. H.
        // IEEE Transactions on Communication, COM-31:532-540 
        // (1983).
        // Designed by Valery Asiryan (c), 2015-2020
        // **************************************************

        /// <summary>
        /// Forward Gaussian pyramid transform.
        /// </summary>
        /// <param name="data">Matrix</param>
        /// <returns>Pyramid</returns>
        public float[][,] Forward(float[,] data)
        {
            int r = data.GetLength(0), c = data.GetLength(1);
            int nlev = (int)Math.Min((Math.Log(Math.Min(r, c))
                / Math.Log(2)), levels);

            float[][,] pyr = new float[nlev][,];
            float[,] dummy = (float[,])data.Clone();

            for (int i = 0; i < nlev; i++)
            {
                pyr[i] = dummy;
                dummy = Downsample(dummy, this.radius);
            }

            return pyr;
        }
        
        #endregion

        #region Private voids
        /// <summary>
        /// Upsample the input signal.
        /// </summary>
        /// <param name="u">Matrix</param>
        /// <param name="radius">Radius</param>
        /// <returns>Matrix</returns>
        internal static float[,] Upsample(float[,] u, int radius)
        {
            int r = u.GetLength(0), c = u.GetLength(1);
            int n = r * 2, m = c * 2;
            float[,] v = new float[n, m];

            for (int i = 0, k = 0; i < r; i++, k += 2)
            {
                for (int j = 0, l = 0; j < c; j++, l += 2)
                {
                    v[k, l] = u[i, j];
                }
            }

            float[,] result = Gaussian(v, GaussianKernel(radius));

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    result[i, j] *= 4;
                }
            }

            return result;
        }
        
        /// <summary>
        /// Downsample the input signal.
        /// </summary>
        /// <param name="u">Matrix</param>
        /// <param name="radius">Radius</param>
        /// <returns>Matrix</returns>
        internal static float[,] Downsample(float[,] u, int radius)
        {
            float[,] source = Gaussian(u, GaussianKernel(radius));
            int r = source.GetLength(0);
            int c = source.GetLength(1);
            int n = (r + 1) / 2, m = (c + 1) / 2;
            float[,] v = new float[n, m];

            for (int i = 0, k = 0; i < r; i += 2, k++)
            {
                for (int j = 0, l = 0; j < c; j += 2, l++)
                {
                    v[k, l] = source[i, j];
                }
            }

            return v;
        }
        
        /// <summary>
        /// Add two matrices.
        /// </summary>
        /// <param name="m">Matrix</param>
        /// <param name="n">Matrix</param>
        /// <returns>Matrix</returns>
        internal static float[,] Add(float[,] m, float[,] n)
        {
            int ml = (int)Math.Min(m.GetLength(0), n.GetLength(0));
            int mr = (int)Math.Min(m.GetLength(1), n.GetLength(1));
            float[,] H = new float[ml, mr];
            int i, j;

            for (i = 0; i < ml; i++)
            {
                for (j = 0; j < mr; j++)
                {
                    H[i, j] = m[i, j] + n[i, j];
                }
            }
            return H;
        }
        /// <summary>
        /// Add two arrays.
        /// </summary>
        /// <param name="m">Array</param>
        /// <param name="n">Array</param>
        /// <returns>Array</returns>
        internal static float[] Add(float[] m, float[] n)
        {
            int ml = (int)Math.Min(m.GetLength(0), n.GetLength(0));
            float[] v = new float[ml];
            int i;

            for (i = 0; i < ml; i++)
            {
                v[i] = m[i] + n[i];
            }
            return v;
        }
        /// <summary>
        /// Sub two matrices.
        /// </summary>
        /// <param name="m">Matrix</param>
        /// <param name="n">Matrix</param>
        /// <returns>Matrix</returns>
        internal static float[,] Sub(float[,] m, float[,] n)
        {
            int ml = (int)Math.Min(m.GetLength(0), n.GetLength(0));
            int mr = (int)Math.Min(m.GetLength(1), n.GetLength(1));
            float[,] H = new float[ml, mr];
            int i, j;

            for (i = 0; i < ml; i++)
            {
                for (j = 0; j < mr; j++)
                {
                    H[i, j] = m[i, j] - n[i, j];
                }
            }
            return H;
        }

        
        /// <summary>
        /// Returns Gaussian kernel.
        /// </summary>
        /// <param name="radius">Radius</param>
        /// <returns>Kernel</returns>
        internal static float[] GaussianKernel(int radius)
        {
            int r = Math.Max(0, radius);
            int n = r * 2;
            float[] kernel = new float[n + 1];
            double coeff = 1.0;
            double scale = Math.Pow(2.0, n);

            for (int i = 0; i <= n; i++)
            {
                kernel[i] = (float)(coeff / scale);

                if (i < n)
                {
                    coeff *= (n - i) / (double)(i + 1);
                }
            }

            return kernel;
        }
        /// <summary>
        /// Applies Gaussian filter to the matrix.
        /// </summary>
        /// <param name="u">Matrix</param>
        /// <param name="kernel">Kernel</param>
        /// <returns>Matrix</returns>
        internal static float[,] Gaussian(float[,] u, float[] kernel)
        {
            int r = u.GetLength(0), c = u.GetLength(1);
            int radius = kernel.Length / 2;
            float[] uFlat = new float[r * c];
            Buffer.BlockCopy(u, 0, uFlat, 0, uFlat.Length * sizeof(float));
            float[] v = new float[r * c];
            float[,] w = new float[r, c];

            for (int i = 0; i < r; i++)
            {
                var leftEnd = Math.Min(radius, c);
                var rightStart = Math.Max(leftEnd, c - radius);
                for (int j = 0; j < leftEnd; j++)
                {
                    float sum = 0;

                    for (int k = -radius; k <= radius; k++)
                    {
                        sum += LxFetchElement(uFlat, r, c, i, j + k) * kernel[k + radius];
                    }

                    v[i * c + j] = sum;
                }
                int iByC = i * c;
                for (int j = leftEnd; j < rightStart; j++)
                {
                    float sum = 0;

                    for (int k = -radius; k <= radius; k++)
                    {
                        sum += uFlat[iByC + (j + k)] * kernel[k + radius];
                    }

                    v[iByC + j] = sum;
                }
                for (int j = rightStart; j < c; j++)
                {
                    float sum = 0;

                    for (int k = -radius; k <= radius; k++)
                    {
                        sum += LxFetchElement(uFlat, r, c, i, j + k) * kernel[k + radius];
                    }

                    v[iByC + j] = sum;
                }
            }

            var topEnd = Math.Min(radius, r);
            var bottomStart = Math.Max(topEnd, r - radius);
            for (int i = 0; i < topEnd; i++)
            {
                for (int j = 0; j < c; j++)
                {
                    float sum = 0;

                    for (int k = -radius; k <= radius; k++)
                    {
                        sum += LxFetchElement(v, r, c, i + k, j) * kernel[k + radius];
                    }

                    w[i, j] = sum;
                }
            }
            for (int i = topEnd; i < bottomStart; i++)
            {
                //int scanlineStart = i * c;
                for (int j = 0; j < c; j++)
                {
                    float sum = 0;

                    for (int k = -radius; k <= radius; k++)
                    {
                        sum += v[(i + k) * c + j] * kernel[k + radius];
                    }

                    w[i, j] = sum;
                }
            }
            for (int i = bottomStart; i < r; i++)
            {
                for (int j = 0; j < c; j++)
                {
                    float sum = 0;

                    for (int k = -radius; k <= radius; k++)
                    {
                        sum += LxFetchElement(v, r, c, i + k, j) * kernel[k + radius];
                    }

                    w[i, j] = sum;
                }
            }

            return w;
        }
        
        private static float LxFetchElement(float[,] u, int i, int j)
        {
            int r = u.GetLength(0), c = u.GetLength(1);
            if (i < 0 || i >= r || j < 0 || j >= c)
            {
                return 0;
            }
            return u[i, j];
            //return u[Clamp(i, r - 1), Clamp(j, c - 1)];
        }

        private static float LxFetchElement(float[] u, int r, int c, int i, int j)
        {
            if (i < 0 || i >= r || j < 0 || j >= c)
            {
                return 0;
            }
            return u[i * c + j];
            //return u[Clamp(i, r - 1), Clamp(j, c - 1)];
        }

        #endregion
    }
}
