using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for ImageSelectDialog.xaml
    /// </summary>
    public partial class ImageSelectDialog : Window
    {
        ObservableCollection<binderClass> myList;

        ColorModel colorModel;

        public bool result;
        public int databaseID;

        public ImageSelectDialog()
        {
            InitializeComponent();
            
            InitColorMap();

            myList = new ObservableCollection<binderClass>();

            ImageListBox.ItemsSource = myList;

            result = false;
                        
        }


        public void InitColorMap()
        {
            colorModel = new ColorModel("Default", GlobalVars.MaxPixelValue);  // TODO:  This probably should not be a fixed number!!

            // Black to White colorMap
            colorModel.InsertColorStop(0, 0, 0, 0);
            colorModel.InsertColorStop(colorModel.m_gradientSize-1, 255, 255, 255);

            colorModel.m_controlPts[1].m_colorIndex = 0;
            colorModel.m_controlPts[1].m_value = 0;

            colorModel.m_controlPts[2].m_colorIndex = colorModel.m_gradientSize-1;
            colorModel.m_controlPts[2].m_value = colorModel.m_maxPixelValue;

            colorModel.BuildColorGradient();
            colorModel.BuildColorMap();
           
        }



        public void AdjustColorMap(int threshold, int gain)
        {
            colorModel.m_controlPts[1].m_colorIndex = 0;
            colorModel.m_controlPts[1].m_value = threshold;

            colorModel.m_controlPts[2].m_colorIndex = colorModel.m_gradientSize-1;
            colorModel.m_controlPts[2].m_value = gain;

            colorModel.BuildColorMap();


            for (int i = 0; i < myList.Count(); i++)
            {
                 byte[] colorImage;

                 int width = (int)myList[i].imageBitmap.Width;
                 int height = (int)myList[i].imageBitmap.Height;

                 ConvertToColor(myList[i].imageData, out colorImage, width, height);

                 Int32Rect rect = new Int32Rect(0, 0, width, height);

                 myList[i].imageBitmap.Lock();
                 myList[i].imageBitmap.WritePixels(rect, colorImage, width * 4, 0);
                 myList[i].imageBitmap.Unlock();
            }

        }


        public void ConvertToColor(ushort[] grayImage, out byte[] colorImage, int width, int height)
        {
            colorImage = new byte[width * height * 4];

            int maxVal = colorModel.m_colorMap.Length - 1;

            for (int i = 0; i < width * height; i++)
            {
                int val = grayImage[i];

                if (val > maxVal) val = maxVal;

                int pos = i * 4;

                colorImage[pos] = colorModel.m_colorMap[val].m_blue;       // blue
                colorImage[pos + 1] = colorModel.m_colorMap[val].m_green;  // green
                colorImage[pos + 2] = colorModel.m_colorMap[val].m_red;    // red
                colorImage[pos + 3] = colorModel.m_colorMap[val].m_alpha;  // alpha
            }            
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            result = false;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {

            if ((binderClass)ImageListBox.SelectedItem != null)
            {
                result = true;
                databaseID = ((binderClass)ImageListBox.SelectedItem).databaseID;
            }
            else
                result = false;
            Close();
        }


        public void AddImage(ushort[] imagedata, int width, int height, string name, int dbID)
        {
            byte[] colorImage;

            ConvertToColor(imagedata, out colorImage, width, height);

            WriteableBitmap bmap = BitmapFactory.New(width, height);

            Int32Rect rect = new Int32Rect(0, 0, width, height);

            bmap.Lock();
            bmap.WritePixels(rect, colorImage, width * 4, 0);
            bmap.Unlock();

            binderClass bc = new binderClass();
            bc.imageData = new ushort[imagedata.Length];
            Buffer.BlockCopy(imagedata, 0, bc.imageData, 0, imagedata.Length * 2);
            bc.imageBitmap = bmap;
            bc.displayName = name;
            bc.databaseID = dbID;

            myList.Add(bc);

        }


        public class binderClass
        {
            public ushort[] imageData
            {
                get;
                set;
            }
           
            public WriteableBitmap imageBitmap
            {
                get;
                set;
            }

            public string displayName
            {
                get;
                set;
            }

            public int databaseID
            {
                get;
                set;
            }

        }

    }
}
