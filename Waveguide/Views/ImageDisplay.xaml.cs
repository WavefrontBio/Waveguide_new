using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace Waveguide
{
    /// <summary>
    /// Interaction logic for ImageDisplay.xaml
    /// </summary>
    public partial class ImageDisplay : UserControl
    {
        public WriteableBitmap m_imageBitmap;
        
        int m_width;
        int m_height;
        int m_maxPixelValue;
        Int32Rect m_imageRect;
        bool m_ready;
        bool m_hasImage;

        public ushort[] m_grayImage;
        byte[] m_colorImage;
        public WG_Color[] m_colorMap;

        public ImageDisplay()
        {
            m_imageBitmap = null;
            m_ready = false;
            m_hasImage = false;

            InitializeComponent();
        }

        public void SetImageSize(int pixelWidth, int pixelHeight, int maxPixelValue)
        {
            m_width = pixelWidth;
            m_height = pixelHeight;
            m_maxPixelValue = maxPixelValue;

            m_imageRect = new Int32Rect(0, 0, pixelWidth, pixelHeight);


            m_grayImage = new ushort[pixelWidth * pixelHeight];
            m_colorImage = new byte[pixelWidth * pixelHeight * 4];
            
            m_imageBitmap = BitmapFactory.New(pixelWidth, pixelHeight);
            ImageBox.Source = m_imageBitmap;

            m_colorMap = new WG_Color[m_maxPixelValue+1];
        }


        public void SetColorMap(WG_Color[] colorMap)
        {
            m_colorMap = colorMap;
            //int length = colorMap.Length;

            //WG_Color[] tempColorMap = new WG_Color[length];

            //for (int i = 0; i < length; i++ )
            //{
            //    tempColorMap[i] = new WG_Color(colorMap[i].m_red, colorMap[i].m_green, colorMap[i].m_blue);                
            //}

            //m_colorMap = tempColorMap;
        }


        public void Init(int pixelWidth, int pixelHeight, int maxPixelValue, WG_Color[] colorMap)
        {
            SetImageSize(pixelWidth, pixelHeight, maxPixelValue);
            SetColorMap(colorMap);
            m_ready = true;
        }

        public bool IsReady()
        {
            return m_ready;
        }

        public bool HasImage()
        {
            return m_hasImage;
        }

        public void SetHasImage(bool hasImage)
        {
            m_hasImage = hasImage;
        }

        public void ClearImage()        
        {
            if(m_imageBitmap!=null)
                m_imageBitmap.Clear();
        }


        public void DisplayImage(ushort[] grayImage) // pass in data from camera
        {
            Buffer.BlockCopy(grayImage, 0, m_grayImage, 0, m_width * m_height * sizeof(ushort));
            m_hasImage = true;

            UpdateImage();         
        }

        public void UpdateImage()
        {
            LocalConvertToColor(m_grayImage, ref m_colorImage);

            m_imageBitmap.Lock();
            m_imageBitmap.WritePixels(m_imageRect, m_colorImage, m_width * 4, 0);
            m_imageBitmap.Unlock();
        }


        public void DrawXonImage()
        {
            int width = m_imageBitmap.PixelWidth - 1;
            int height = m_imageBitmap.PixelHeight - 1;

            m_imageBitmap.Lock();
                m_imageBitmap.DrawLine(0, 0, width, height, Colors.Red);
                m_imageBitmap.DrawLine(width, 0, 0, height, Colors.Red);
            m_imageBitmap.Unlock();
        }


     

        //public void LocalConvertToColor(ushort[] grayImage, ref byte[] colorImage)
        //{
        //    int maxVal = m_colorMap.Length - 1;

        //    for (int i = 0; i < m_width * m_width; i++)
        //    {
        //        int val = grayImage[i];

        //        if (val > maxVal) val = maxVal;

        //        int pos = i * 4;

        //        colorImage[pos] = m_colorMap[val].m_blue;       // blue
        //        colorImage[pos + 1] = m_colorMap[val].m_green;  // green
        //        colorImage[pos + 2] = m_colorMap[val].m_red;    // red
        //        colorImage[pos + 3] = m_colorMap[val].m_alpha;  // alpha
        //    //}
        //}

        public void LocalConvertToColor(ushort[] grayImage, ref byte[] colorImage)
        {
            var degreeOfParallelism = 8; // Environment.ProcessorCount / 2;

            while (degreeOfParallelism > Environment.ProcessorCount / 2) degreeOfParallelism /= 2;

            if (degreeOfParallelism < 1) degreeOfParallelism = 1;

            var grayImageLength = grayImage.Length;

            int maxVal = m_colorMap.Length - 1;

            var tasks = new Task[degreeOfParallelism];

            byte[] localColorImage = colorImage;

            for (int taskNumber = 0; taskNumber < degreeOfParallelism; taskNumber++)
            {
                // capturing taskNumber in lambda wouldn't work correctly
                int taskNumberCopy = taskNumber;

                tasks[taskNumber] = Task.Factory.StartNew(() =>
                {
                    var max = grayImageLength * (taskNumberCopy + 1) / degreeOfParallelism;

                    for (int i = grayImageLength * taskNumberCopy / degreeOfParallelism; i < max; i++)
                    {
                        int val = grayImage[i];

                        if (val > maxVal) val = maxVal;

                        int pos = i * 4;

                        localColorImage[pos] = m_colorMap[val].m_blue;       // blue
                        localColorImage[pos + 1] = m_colorMap[val].m_green;  // green
                        localColorImage[pos + 2] = m_colorMap[val].m_red;    // red
                        localColorImage[pos + 3] = m_colorMap[val].m_alpha;  // alpha
                    }
                });
            }

            Task.WaitAll(tasks);

            colorImage = localColorImage;
        }


    }
}
