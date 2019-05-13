using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace WaveExplorer
{
    public class DataProcessor
    {


        public DataProcessor()
        {
           
        }




        public bool Interpolate(double[] xIn, double[] yIn, out CubicSpline spline, out string errMsg)
        {
            bool success = true;
            errMsg = "";
            spline = null;

            if(xIn.Length == yIn.Length)
            {
                var xvec = new DenseVector(xIn);
                var yvec = new DenseVector(yIn);

                spline = CubicSpline.InterpolateNatural(xvec, yvec);
             
            }
            else
            {
                errMsg = "x and y vectors must be the same length";
                success = false;
            }

            return success;
        }

        

        public bool Derivative(CubicSpline spline, double t, out double derivative, out string errMsg)
        {
            bool success = true;
            errMsg = "";
            derivative = 0.0;

            derivative = spline.Differentiate(t);

            return success;
        }

    }
}
