using Infragistics.Controls.Charts;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for ColorModelEditor.xaml
    /// </summary>
    public partial class ColorModelEditor : UserControl
    {

        public WriteableBitmap m_gradientBitmap;
        public WriteableBitmap m_colorMapBitmap;
        public WriteableBitmap m_histogramBitmap;

        public ColorModel m_colorModel;

        bool m_isDragging;
        int  m_draggingIndex;

        PlotLimit m_PlotLimits;

        ushort[] m_image;  // raw grayscale image from camera


        WaveguideDB wgDB;


        public ColorModelEditor()
        {
            InitializeComponent();

            wgDB = new WaveguideDB();

            m_isDragging = false;                       

            m_PlotLimits = new PlotLimit();
            m_PlotLimits.m_xmax = 100;
            m_PlotLimits.m_ymax = 1023;

            m_image = null;

            ControlChart.MouseLeftButtonDown += ControlChart_MouseLeftButtonDown;
            ControlChart.MouseLeftButtonUp += ControlChart_MouseLeftButtonUp;
            ControlChart.MouseMove += ControlChart_MouseMove;
            ControlChart.MouseLeave += ControlChart_MouseLeave;

            ControlChart_Xaxis.DataContext = m_PlotLimits;
            ControlChart_Yaxis.DataContext = m_PlotLimits;

            InitDefaultColorModel();
        }

        


        public void InitDefaultColorModel()
        {
            bool success = wgDB.GetAllColorModels();

            if (success)
                if (wgDB.m_colorModelList.Count() > 0)
                {                    
                    ColorModelContainer model = wgDB.m_colorModelList[0];
                    ColorModel m_colorModel = new ColorModel(model.Description, model.MaxPixelValue, model.GradientSize);
            

                    for(int i = 0; i< model.Stops.Count(); i++)
                    {
                        m_colorModel.InsertColorStop(model.Stops[i].ColorIndex,
                                                     model.Stops[i].Red,
                                                     model.Stops[i].Green,
                                                     model.Stops[i].Blue);                        
                    }

                    m_colorModel.BuildColorGradient();
                    m_colorModel.BuildColorMap();

                    SetColorModel(m_colorModel);

                    DrawColorGradient();
                    DrawColorMap();
                }
        }


     

        void ControlChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_isDragging)
            {
                var series = this.ControlChart.Series.FirstOrDefault();
                if (series == null) return;

                var position = e.GetPosition(series);

                // Get the XY value of the mouse in the series.
                ScalerParams sparams = new ScalerParams(ControlChart.ActualWindowRect, ControlChart.ViewportRect, ControlChart_LineSeries.YAxis.IsInverted);
                double xValue = ControlChart_LineSeries.XAxis.GetUnscaledValue(e.GetPosition(ControlChart_LineSeries.RootCanvas).X, sparams);
                double yValue = ControlChart_LineSeries.YAxis.GetUnscaledValue(e.GetPosition(ControlChart_LineSeries.RootCanvas).Y, sparams);

                if (xValue < m_colorModel.m_controlPts[m_draggingIndex - 1].m_value) xValue = m_colorModel.m_controlPts[m_draggingIndex - 1].m_value;
                else if (xValue > m_colorModel.m_controlPts[m_draggingIndex + 1].m_value) xValue = m_colorModel.m_controlPts[m_draggingIndex + 1].m_value;

                if (yValue < 0) yValue = 0;
                else if (yValue > m_colorModel.m_gradientSize) yValue = m_colorModel.m_gradientSize;

                m_colorModel.m_controlPts[m_draggingIndex].m_value = (int)xValue;
                m_colorModel.m_controlPts[m_draggingIndex].m_colorIndex = (int)yValue;

                m_colorModel.BuildColorMap();
                DrawColorMap();

                WG_Color color = m_colorModel.m_colorMap[500];
                
            }
        }



        void ControlChart_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_isDragging = false;

            if (ImageDisplay.HasImage())
            {
                ImageDisplay.SetColorMap(m_colorModel.m_colorMap);
                ImageDisplay.UpdateImage();
            }
        }



        void ControlChart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var series = this.ControlChart.Series.FirstOrDefault();
            if (series == null) return;

            var position = e.GetPosition(series);

            // Get the XY value of the mouse in the series.
            ScalerParams sparams = new ScalerParams(ControlChart.ActualWindowRect, ControlChart.ViewportRect, ControlChart_LineSeries.YAxis.IsInverted);
            double xValue = ControlChart_LineSeries.XAxis.GetUnscaledValue(e.GetPosition(ControlChart_LineSeries.RootCanvas).X, sparams);
            double yValue = ControlChart_LineSeries.YAxis.GetUnscaledValue(e.GetPosition(ControlChart_LineSeries.RootCanvas).Y, sparams);

            // check to see if near a control point
            for (int i = 1; i < m_colorModel.m_controlPts.Count()-1; i++)
            {
                if (Math.Abs(xValue - m_colorModel.m_controlPts[i].m_value) < 2 && Math.Abs(yValue - m_colorModel.m_controlPts[i].m_colorIndex) < 20)
                {
                    m_isDragging = true;
                    m_draggingIndex = i;
                    break;
                }
            }
           
        }



        void ControlChart_MouseLeave(object sender, MouseEventArgs e)
        {
            m_isDragging = false;
        }



        public void SetColorModel(ColorModel model)
        {
            m_colorModel = model;

            ControlChart_LineSeries.DataContext = m_colorModel.m_controlPts;

            m_PlotLimits.m_xmax = 100;
            m_PlotLimits.m_ymax = m_colorModel.m_gradientSize;  
        }



        public void SetImage(ushort[] image, int width, int height)
        {
            m_image = image;

            if (!ImageDisplay.IsReady()) ImageDisplay.Init(width, height, m_colorModel.m_maxPixelValue, m_colorModel.m_colorMap);

            ImageDisplay.DisplayImage(m_image);
              
        }





        public void DrawColorGradient()
        {
            if (m_colorModel == null) return;

            int gradWidth = 40;

            if(m_gradientBitmap==null)
            {
                m_gradientBitmap = BitmapFactory.New(gradWidth, m_colorModel.m_gradientSize);
                GradientImage.Source = m_gradientBitmap;
            }

            for (int i = 0; i < m_colorModel.m_gradientSize; i++)
                {
                    Color color = new Color();
                    color.A = 255;
                    color.R = m_colorModel.m_gradient[i].m_red;
                    color.G = m_colorModel.m_gradient[i].m_green;
                    color.B = m_colorModel.m_gradient[i].m_blue;

                    m_gradientBitmap.DrawLine(0, m_colorModel.m_gradientSize - 1 - i, gradWidth, m_colorModel.m_gradientSize - 1 - i, color);
                }                            
        }



        public void DrawColorMap()
        {
            if (m_colorModel == null) return;

            int colorMapHeight = 40;

            if (m_colorMapBitmap == null)
            {
                m_colorMapBitmap = BitmapFactory.New(m_colorModel.m_maxPixelValue, colorMapHeight);
                ColorMapImage.Source = m_colorMapBitmap;
            }

            for (int i = 0; i < m_colorModel.m_maxPixelValue; i++)
            {
                Color color = new Color();
                color.A = 255;
                color.R = m_colorModel.m_colorMap[i].m_red;
                color.G = m_colorModel.m_colorMap[i].m_green;
                color.B = m_colorModel.m_colorMap[i].m_blue;

                m_colorMapBitmap.DrawLine(i,0,i,colorMapHeight, color);
            }
        }






        private void ColorModel_Load_Click(object sender, EventArgs e)
        {
            WaveguideDB wgDB = new WaveguideDB();

            bool success = wgDB.GetAllColorModels();

            if (success)
            {
                ColorModelSelectDialog diag = new ColorModelSelectDialog(wgDB.m_colorModelList);
                diag.ShowDialog();

                int colorModelID = diag.dbID;

                for (int i = 0; i < wgDB.m_colorModelList.Count(); i++)
                {
                    if (wgDB.m_colorModelList[i].ColorModelID == colorModelID)
                    {
                        ColorModel model = new ColorModel(wgDB.m_colorModelList[i].Description,wgDB.m_colorModelList[i].MaxPixelValue, wgDB.m_colorModelList[i].GradientSize);
                        for (int j = 0; j < wgDB.m_colorModelList[i].Stops.Count(); j++)
                        {
                            model.InsertColorStop(wgDB.m_colorModelList[i].Stops[j].ColorIndex,
                                                  wgDB.m_colorModelList[i].Stops[j].Red,
                                                  wgDB.m_colorModelList[i].Stops[j].Green,
                                                  wgDB.m_colorModelList[i].Stops[j].Blue);
                        }

                        model.BuildColorGradient();
                        model.BuildColorMap();                       

                        SetColorModel(model);

                        DrawColorGradient();
                        DrawColorMap();

                        if (ImageDisplay.IsReady() && ImageDisplay.HasImage())
                        {
                            ImageDisplay.SetColorMap(m_colorModel.m_colorMap);
                            ImageDisplay.UpdateImage();
                        }
                    }
                }
            }
        }

        private void ColorModel_Save_Click(object sender, EventArgs e)
        {

        }

        private void ColorModel_SaveAs_Click(object sender, EventArgs e)
        {

        }

        private void ColorModel_Delete_Click(object sender, EventArgs e)
        {

        }

        private void Image_Load_Click(object sender, EventArgs e)
        {
            //ImageFromFileToDatabase();

            wgDB.GetAllReferenceImages();

            ImageSelectDialog diag = new ImageSelectDialog();


            for (int i = 0; i < wgDB.m_refImageList.Count(); i++)
            {
                diag.AddImage(wgDB.m_refImageList[i].ImageData, wgDB.m_refImageList[i].Width, wgDB.m_refImageList[i].Height, "test " + i.ToString(), wgDB.m_refImageList[i].ReferenceImageID);
            }

            diag.ShowDialog();

            if (diag.result)
            {
                ReferenceImageContainer refImage;

                bool success = wgDB.GetReferenceImage(diag.databaseID, out refImage);

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

        }

        private void Image_UseCurrent_Click(object sender, EventArgs e)
        {

        }

        private void Image_Clear_Click(object sender, EventArgs e)
        {

        }

        private void ImageDisplayExpander_Expanded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Height += 400;

            this.Height += 400;
        }

        private void ImageDisplayExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Height -= 400;
            this.Height -= 400;
        }




        public void ImageFromFileToDatabase()
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".wgi"; // Default file extension
            dlg.Filter = "WaveGuide Image (.wgi)|*.wgi"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;

                if (File.Exists(filename))
                {
                    byte[] image;
                    ReadImageFromWgiFile(out image, filename);

                    int numbytes = image.Length;

                    ReferenceImageContainer cont = new ReferenceImageContainer();
                    cont.Width = 1024;
                    cont.Height = 1024;
                    cont.Depth = 2;
                    Buffer.BlockCopy(image, 0, cont.ImageData, 0, numbytes);
                    //cont.ImageData = image;
                    cont.TimeStamp = DateTime.Now;
                    cont.NumBytes = numbytes;

                    bool success = wgDB.InsertReferenceImage(ref cont);

                    if (success)
                    {
                        success = true;
                    }
                }


            }
            
        }


        public void ReadImageFromWgiFile(out byte[] imageArray, string filename)
        {
            // this function reads the old WGI format which is just a 1D array of bytes, where each pixel is two bytes long
            int offset = 21;


            imageArray = null;

            byte[] byteArray = new byte[offset];

            try
            {

                using (FileStream wgiFile = File.Open(filename, FileMode.Open, FileAccess.Read))
                {
                    int numBytesToRead = (int)wgiFile.Length;                    
                    int numBytesRead = 0;

                    wgiFile.Read(byteArray, 0, offset);

                    //m_pixelsX = BitConverter.ToUInt16(byteArray, 0);
                    //m_pixelsY = BitConverter.ToUInt16(byteArray, 2);

                    numBytesToRead -= offset;
                    imageArray = new byte[numBytesToRead];

                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead. 
                        int n = wgiFile.Read(imageArray, numBytesRead, numBytesToRead);

                        // Break when the end of the file is reached. 
                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }

                }
            }
            catch (FileNotFoundException ioEx)
            {
                string msg = ioEx.Message;
                msg = msg + " ";
            }

            offset += 0;

        }

        private void ColorModel_New_Click(object sender, EventArgs e)
        {

        }



        
    }  // END ColorModelEditor class

    // /////////////////////////////////////////////////////////////////////////////////////////////
    // /////////////////////////////////////////////////////////////////////////////////////////////
    // /////////////////////////////////////////////////////////////////////////////////////////////
    // /////////////////////////////////////////////////////////////////////////////////////////////

    public class PlotLimit : INotifyPropertyChanged
    {
        private int _xmax;
        private int _ymax;

        public int m_xmax
        {
            get { return _xmax; }
            set
            {
                _xmax = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("m_xmax"));
            }
        }

        public int m_ymax
        {
            get { return _ymax; }
            set
            {
                _ymax = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("m_ymax"));
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }


  


}
