using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waveguide
{

    class FlatFieldCorrector
    {

        public ushort[] F;  // flat field reference image
        public ushort[] D;  // dark reference image

        public float[] G;  // gain array ->  G[i,j] = m / (F[i,j] - D[i,j])  where i,j are pixel coordinates within image (i = row, j = column)
        //                           m = average of (F - D) for all i,j

        public ushort[] Dc; // dark image corrected for binning
        public float[] Gc;  // gain array corrected for binning
        public int m_HorzBinning;
        public int m_VertBinning;
        public int m_width;
        public int m_height;
        public ushort m_m;  // m = average of (F - D) for all i,j

        public ushort threshold = 200;

        public FlatFieldCorrector(int width, int height, ushort[] flatFieldImage, ushort[] darkImage)
        {
            //  flatFieldImage = flat field image (this is an image with even illumination across the field)
            //  darkFieldImage = dark image (this is an image taken with no lighting.  it bascially gives the dark current noise)

            m_width = width;
            m_height = height;

            int imageSize = width * height;
            F = flatFieldImage;
            D = darkImage;
            G = new float[imageSize];
            Gc = new float[imageSize];
            Dc = new ushort[imageSize];

            m_VertBinning = 1;
            m_HorzBinning = 1;

            // if no flatFieldImage is provided, then set F to full scale in all pixels
            if (flatFieldImage == null)
            {
                F = new ushort[imageSize];
                for (int i = 0; i < imageSize; i++)
                {
                    F[i] = 4095;
                }
            }

            // if no darkFieldImage is provided, then set D to zero in all pixels
            if (darkImage == null)
            {
                D = new ushort[imageSize];
                for (int i = 0; i < imageSize; i++)
                {
                    D[i] = 0;
                }
            }



            // find average of D to set a threshold for whether to include a pixel in the calculation of m
            ulong dSum = 0;
            for (int i = 0; i < imageSize; i++)
            {
                dSum += (ulong)D[i];
            }
            ushort dAvg = (ushort)((double)dSum / (double)imageSize);
            threshold = dAvg;



            // calculate m
            ulong sum = 0;
            ulong pixelCount = 0;
            for (int i = 0; i < imageSize; i++)
            {
                //sum += (ulong)(F[i] - D[i]);
                if (F[i] > threshold)
                {
                    sum += (ulong)(F[i] - D[i]);
                    pixelCount++;
                }
            }
            if (pixelCount < 1) pixelCount = 1;
            //ulong mlong = sum / (ulong)imageSize;
            ulong mlong = sum / pixelCount;
            m_m = (ushort)mlong;
            float maxG = 0;
            float minG = float.MaxValue;

            // calculate G (gain array)
            for (int i = 0; i < imageSize; i++)
            {
                //G[i] = ((float)m) / ((float)(F[i] - D[i]));

                if (F[i] > threshold)
                {
                    G[i] = ((float)m_m) / ((float)(F[i] - D[i]));
                    if (G[i] > maxG) maxG = G[i];
                    if (G[i] < minG) minG = G[i];
                }
                else G[i] = 0;
            }

            // copy to corrected arrays
            for (int i = 0; i < imageSize; i++)
            {
                Gc[i] = G[i];
                Dc[i] = D[i];
            }
        }



        public ushort[] Flatten(ushort[] R, int hBinning, int vBinning)
        {
            // This function performs a Flat Field Correction on the given grayImage.
            //
            // Equation:
            //              Cij = (Rij - Dij) * Gij
            //
            //   where   Gij  =  m / (Fij - Dij)
            //
            //  C = corrected image  (Cij = the pixel at column i and row j)
            //  R = raw image
            //  F = flat field image (this is an image with even illumination across the field)
            //  D = dark image (this is an image taken with no lighting.  it bascially gives the dark current noise)
            //  G = gain
            //  Dc = D corrected to binning size
            //  Gc = G corrected to binning size
            //  m = average of F-D  (this is a scalar, a single value used for all pixels)
            //
            //  for the variables above, capital letters are 2D matrices and lower case letters are scalars (only one is m)
            // 
            //  NOTE:  in this case, our image is handled as a 1D matrix, so the algorithm is adjusted accordingly

            if (hBinning != m_HorzBinning || vBinning != m_VertBinning)
            {
                CorrectForBinning(hBinning, vBinning);
            }

            if (R.Length != Gc.Length) return R;  // incorrect image size

            ushort[] C = new ushort[R.Length];


            for (int i = 0; i < R.Length; i++)
            {
                float val = ((float)R[i] - (float)Dc[i]) * (float)Gc[i];

                if (val > ushort.MaxValue) val = ushort.MaxValue;
                if (val < 0) val = 0;

                C[i] = (ushort)val;
            }

            return C;
        }



        public void CorrectForBinning(int hBinning, int vBinning)
        {
            m_HorzBinning = hBinning;
            m_VertBinning = vBinning;

            int colsRaw = m_width;
            int rowsRaw = m_height;

            int colsCorrected = colsRaw / hBinning;
            int rowsCorrected = rowsRaw / vBinning;

            Dc = new ushort[D.Length / (hBinning * vBinning)];
            Gc = new float[D.Length / (hBinning * vBinning)];

            Array.Clear(Dc, 0, Dc.Length);
            Array.Clear(Gc, 0, Gc.Length);

            int rc = 0, cc = 0; // row,col of corrrected arrays 

            float gMax = float.MinValue, gMin = float.MaxValue;

            // (rb,cb) defines upper left corner of bin
            // (ri,ci) defines pixel within bin (relative to rb,cb)
            for (int rb = 0; rb < rowsRaw; rb += vBinning)  // r,c is the upper left corner of bin
            {
                cc = 0;

                for (int cb = 0; cb < colsRaw; cb += hBinning)
                {
                    float sumG = 0.0f;
                    int sumD = 0;

                    for (int ri = 0; ri < hBinning; ri++) // rb,cb step through the bin
                    {
                        for (int ci = 0; ci < vBinning; ci++)
                        {
                            int idx = ((rb + ri) * colsRaw) + (cb + ci);

                            sumG += G[idx];
                            sumD += D[idx];
                        }
                    }


                    int idxc = (rc * colsCorrected) + cc;
                    Gc[idxc] = sumG / ((float)(vBinning * hBinning));
                    Dc[idxc] = (ushort)(sumD / (vBinning * hBinning));

                    if (Gc[idxc] > gMax) gMax = Gc[idxc];
                    if (Gc[idxc] < gMin) gMin = Gc[idxc];

                    cc++;
                }

                rc++;
            }
        }



        public bool GetCorrectionArrays(int hBinning, int vBinning, out float[] _Gc, out float[] _Dc)
        {
            bool success = true;

            if (hBinning != m_HorzBinning || vBinning != m_VertBinning)
            {
                CorrectForBinning(hBinning, vBinning);
            }

            _Gc = Gc;

            _Dc = new float[Dc.Length];
            for (int i = 0; i < Dc.Length; i++ )
                _Dc[i] = (float)Dc[i];

            return success;
        }

    }
}
