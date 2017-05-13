using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Waveguide
{
    public class Histogram
    {
               
        WriteableBitmap m_histogramBitmap;
        int m_histogramHeight;
        int m_maxBucketCount;
        int m_numBuckets;        
        int[] m_histogramBucket;
        int m_bucketWidth;
        MaskContainer m_mask;
        

        public Histogram(int numBuckets = 1024)
        {
            m_numBuckets = numBuckets;
            m_histogramHeight = 256;
            m_histogramBitmap = BitmapFactory.New(m_numBuckets, m_histogramHeight);
            m_histogramBucket = new int[m_numBuckets];
            m_maxBucketCount = 0; // maximum value in a bucket, across all buckets   

            m_bucketWidth = (GlobalVars.MaxPixelValue + 1) / m_numBuckets;

            m_mask = null;
        }



        public WriteableBitmap GetHistogramBitmap()
        {
            return m_histogramBitmap;
        }

        public int GetHistogramNumBuckets()
        {
            return m_numBuckets;
        }

        public int GetHistogramBucketWidth()
        {
            return m_bucketWidth;
        }

        public void SetMask(MaskContainer mask, int hBinning, int vBinning)
        {
            m_mask = mask;
            m_mask.BuildPixelList(GlobalVars.PixelWidth, GlobalVars.PixelHeight, hBinning, vBinning );
        }


        public void BuildImageHistogram(ushort[] grayImage)
        {
            if (grayImage == null) return;

            // put zero in all histogram data buckets
            Array.Clear(m_histogramBucket, 0, m_numBuckets);
         
            m_maxBucketCount = 16;  // minimum height for the histogram

            int index;

            int bigCount = 0;


            if (m_mask == null)
            {
                for (int i = 0; i < grayImage.Length; i++)
                {
                    index = grayImage[i] / m_bucketWidth;

                    if (index >= m_numBuckets) index = m_numBuckets - 1;

                    m_histogramBucket[index]++;

                    if (m_histogramBucket[index] > m_maxBucketCount) m_maxBucketCount = m_histogramBucket[index];
                }
            }
            else
            {
                for (int r = 0; r < m_mask.Rows; r++)
                    for (int c = 0; c < m_mask.Cols; c++)
                    {
                        foreach (int ndx in m_mask.PixelList[r, c])
                        {
                            //if(grayImage[ndx] > 16000)
                            //{
                            //    ushort center = grayImage[ndx];
                            //    ushort left = (ndx>0) ? grayImage[ndx - 1] : (ushort)0;
                            //    ushort right = (ndx<grayImage.Length -1) ? grayImage[ndx + 1] : (ushort)0;
                            //    ushort above = (ndx > GlobalVars.PixelWidth) ? grayImage[ndx - GlobalVars.PixelWidth] : (ushort)0;
                            //    ushort below = (ndx < (grayImage.Length - GlobalVars.PixelWidth)) ? grayImage[ndx + GlobalVars.PixelWidth] : (ushort)0;
                            //    bigCount++;
                            //}

                            index = (int)grayImage[ndx] / m_bucketWidth;
                            if (index >= m_numBuckets) index = m_numBuckets - 1;
                            m_histogramBucket[index]++;
                            if (m_histogramBucket[index] > m_maxBucketCount) m_maxBucketCount = m_histogramBucket[index];
                        }
                    }
            }

        }


        public void GetPercentValue(double percent, ushort[] grayImage, ref int value, ref int histogramIndex)
        {
            // this function returns the pixel value of which <percent> of the total number of pixels is below.  For example,
            // if percent = 80, and this function return 547, it would mean that 80% of the pixels in grayImage
            // have a value of 547 or less.  The function also returns the index of the histogram bucket where value resides.
            // This index can be used to conveniently draw a marker on the histogram.

        
            double currentPercent;

            if(grayImage != null)
            {
                double totalPixels = (double)grayImage.Length;
                int pixelCount = 0;

                for (int i = 0; i < m_numBuckets; i++)
                {
                    pixelCount += m_histogramBucket[i];

                    currentPercent = (double)pixelCount / totalPixels;

                    if(currentPercent >= percent)
                    {
                        value = i * m_bucketWidth;
                        histogramIndex = i;
                        break;
                    }
                }
            }
        }


        public void DrawHistogram()
        {  
            // draw histogram
            m_histogramBitmap.Lock();
            m_histogramBitmap.Clear();
            for (int i = 0; i < m_numBuckets; i++)
            {
                int h = (int)((double)m_histogramBucket[i] / m_maxBucketCount * m_histogramHeight);
                if (m_histogramBucket[i] > 1 && h < 5) h = 5;
                m_histogramBitmap.DrawLine(i, m_histogramHeight, i, m_histogramHeight - h, Colors.Black);
            }
            m_histogramBitmap.Unlock();
        }


    }


    public class HistogramBar : INotifyPropertyChanged
    {
        private int _pixelValue;
        private int _count;

        public int m_pixelValue  // x axis
        {
            get { return _pixelValue; }
            set
            {
                _pixelValue = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("m_pixelValue"));
            }
        }

        public int m_count // y axis
        {
            get { return _count; }
            set
            {
                _count = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("m_count"));
            }
        }

        public HistogramBar(int pixelValue, int count)
        {
            _pixelValue = pixelValue;
            _count = count;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
