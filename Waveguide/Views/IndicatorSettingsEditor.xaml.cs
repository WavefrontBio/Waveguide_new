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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for IndicatorSettingsEditor.xaml
    /// </summary>
    public partial class IndicatorSettingsEditor : Window
    {

        public WriteableBitmap m_histogramBitmap;

        public ColorModel m_colorModel;

        int xImageSize;
        int yImageSize;
        
        PlotLimit m_PlotLimits;

        ushort[] m_image;  // raw grayscale image from camera


        WaveguideDB wgDB;

        ExperimentIndicatorContainer m_indicator;

        RangeClass m_range;


        public IndicatorSettingsEditor(ExperimentIndicatorContainer indicator)
        {
            InitializeComponent();

            if (indicator == null) return;

            m_indicator = indicator;

            wgDB = new WaveguideDB();


            m_PlotLimits = new PlotLimit();
            m_PlotLimits.m_xmax = 100;
            m_PlotLimits.m_ymax = 1023;

            m_image = null;
            
            Init();

            this.DataContext = m_indicator;

            Test_Image_Load();

        }




        public void Init()
        {          
            m_range = new RangeClass();

            // setup default color model
            m_colorModel = new ColorModel("Default");
            m_colorModel.InsertColorStop(0, 0, 0, 0);
            m_colorModel.InsertColorStop(1023, 255, 255, 255);
            m_colorModel.m_controlPts[1].m_value = 0;
            m_colorModel.m_controlPts[1].m_colorIndex = 0;
            m_colorModel.m_controlPts[2].m_value = 100;
            m_colorModel.m_controlPts[2].m_colorIndex = 1023;
            m_colorModel.BuildColorGradient();
            m_colorModel.BuildColorMap();

            RangeSlider.DataContext = m_range;
            m_range.RangeMin = 0;
            m_range.RangeMax = 100;


            // create default image
            int width = GlobalVars.Instance.PixelWidth;
            int height = GlobalVars.Instance.PixelHeight;
            
            int numbytes = width * height;
            ushort[] defaultImage = new ushort[numbytes];
            for (int i = 0; i < numbytes; i++) defaultImage[i] = 0;  // black image

            SetImage(defaultImage, width, height);

            SetImageSize(width, height);
           
        }

        public void SetImageSize(int imagePixelWidth, int imagePixelHeight)
        {
            xImageSize = imagePixelWidth;
            yImageSize = imagePixelHeight;
        }



        public void SetImage(ushort[] image, int width, int height)
        {
            m_image = image;

            if (!ImageDisplay.IsReady()) ImageDisplay.Init(width, height, m_colorModel.m_maxPixelValue, m_colorModel.m_colorMap);

            ImageDisplay.DisplayImage(m_image);


        }



        public void SetColorModel(ColorModel model)
        {
            m_colorModel = model;
            m_PlotLimits.m_xmax = 100;
            m_PlotLimits.m_ymax = m_colorModel.m_gradientSize;
        }


  

        private void RangeSlider_TrackFillDragCompleted(object sender, Infragistics.Controls.Editors.TrackFillChangedEventArgs<double> e)
        {
            m_colorModel.m_controlPts[1].m_value = (int)RangeMinThumb.Value;
            m_colorModel.m_controlPts[1].m_colorIndex = 0;
            m_colorModel.m_controlPts[2].m_value = (int)RangeMaxThumb.Value;
            m_colorModel.m_controlPts[2].m_colorIndex = 1023;
            m_colorModel.BuildColorMap();

            WG_Color color = m_colorModel.m_colorMap[500];

            if (ImageDisplay.IsReady() && ImageDisplay.HasImage())
            {
                ImageDisplay.SetColorMap(m_colorModel.m_colorMap);
                ImageDisplay.UpdateImage();
            }
        }

        private void RangeMinThumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            m_colorModel.m_controlPts[1].m_value = (int)RangeMinThumb.Value;
            m_colorModel.m_controlPts[1].m_colorIndex = 0;
            m_colorModel.BuildColorMap();

            if (ImageDisplay.IsReady() && ImageDisplay.HasImage())
            {
                ImageDisplay.SetColorMap(m_colorModel.m_colorMap);
                ImageDisplay.UpdateImage();
            }
        }

        private void RangeMaxThumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            m_colorModel.m_controlPts[2].m_value = (int)RangeMaxThumb.Value;
            m_colorModel.m_controlPts[2].m_colorIndex = 1023;
            m_colorModel.BuildColorMap();

            if (ImageDisplay.IsReady() && ImageDisplay.HasImage())
            {
                ImageDisplay.SetColorMap(m_colorModel.m_colorMap);
                ImageDisplay.UpdateImage();
            }
        }


        private void OkPB_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }



        private void Test_Image_Load()
        {
            ReferenceImageContainer refImage;

            bool success = wgDB.GetReferenceImage(6, out refImage);

            if (success)
            {
                if (m_colorModel.m_maxPixelValue != refImage.MaxPixelValue)
                {
                    m_colorModel.SetMaxPixelValue(refImage.MaxPixelValue);
                    m_colorModel.BuildColorMap();
                }

                SetImage(refImage.ImageData, refImage.Width, refImage.Height);

            }
        }



        public class RangeClass
        {
            public int RangeMin
            {
                get;
                set;
            }

            public int RangeMax
            {
                get;
                set;
            }

        }


    }
}
