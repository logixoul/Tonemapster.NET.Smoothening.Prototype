using System;
using System.Collections.Generic;
using System.Text;
using UMapx.Core;
using UMapx.Transform;

namespace Tonemapster.NET.Smoothening.Prototype
{
    /// <summary>
    /// Defines the Laplacian pyramid transform.
    /// </summary>
    /// <remarks>
    /// More information can be found on the website:
    /// http://www.cs.toronto.edu/~jepson/csc320/notes/pyramids.pdf
    /// </remarks>
    [Serializable]
    public class MyLaplacianPyramidTransform
    {
        #region Private data
        int radius;
        int levels;
        #endregion

        #region Pyramid components
        /// <summary>
        /// Initializes the Laplacian pyramid transform.
        /// </summary>
        public MyLaplacianPyramidTransform()
        {
            this.Levels = int.MaxValue;
            this.Radius = 2;
        }
        /// <summary>
        /// Initializes the Laplacian pyramid transform.
        /// </summary>
        /// <param name="levels">Number of levels</param>
        /// <param name="radius">Radius</param>
        public MyLaplacianPyramidTransform(int levels, int radius = 2)
        {
            this.Radius = radius;
            this.Levels = levels;
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
        //            Laplacian Pyramid Transform
        // **************************************************
        // ORIGINALS: Burt, P., and Adelson, E. H.
        // IEEE Transactions on Communication, COM-31:532-540 
        // (1983).
        // Designed by Valery Asiryan (c), 2015-2020
        // **************************************************

        /// <summary>
        /// Forward Laplacian pyramid transform.
        /// </summary>
        /// <param name="data">Matrix</param>
        /// <returns>Pyramid</returns>
        public float[][,] Forward(float[,] data)
        {
            int r = data.GetLength(0), c = data.GetLength(1);
            int nlev = (int)Math.Min((Math.Log(Math.Min(r, c)) / Math.Log(2)), levels);
            float[][,] lapl = new float[nlev][,];
            float[,] I, J = data;

            for (int i = 0; i < nlev - 1; i++)
            {
                I = MyGaussianPyramidTransform.Downsample(J, this.radius);
                lapl[i] = MyGaussianPyramidTransform.Sub(J, MyGaussianPyramidTransform.Upsample(I, this.radius));
                J = I;
            }

            lapl[nlev - 1] = J;
            return lapl;
        }
        
        /// <summary>
        /// Backward Laplacian pyramid transform.
        /// </summary>
        /// <param name="pyramid">Pyramid</param>
        /// <returns>Matrix</returns>
        public float[,] Backward(float[][,] pyramid)
        {
            int nlev = pyramid.Length - 1;
            float[,] I = pyramid[nlev];

            for (int i = nlev - 1; i >= 0; i--)
            {
                I = MyGaussianPyramidTransform.Add(pyramid[i], MyGaussianPyramidTransform.Upsample(I, this.radius));
            }

            return I;
        }
        #endregion

        #region Gaussian pyramid to Laplacian pyramid
        /// <summary>
        /// Forward Laplacian pyramid transform.
        /// </summary>
        /// <param name="data">Gaussian pyramid</param>
        /// <returns>Pyramid</returns>
        public float[][,] Forward(float[][,] data)
        {
            int nlev = data.Length;
            float[][,] lapl = new float[nlev][,];

            for (int i = 1; i < nlev; i++)
            {
                lapl[i - 1] = MyGaussianPyramidTransform.Sub(data[i - 1], MyGaussianPyramidTransform.Upsample(data[i], this.radius));
            }

            lapl[nlev - 1] = data[nlev - 1];
            return lapl;
        }
        
        #endregion
    }
}
