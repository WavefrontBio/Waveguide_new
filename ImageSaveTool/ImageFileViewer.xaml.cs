#undef SIMULATE

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using ImageSaveTool;
using Waveguide;
using CudaTools;

namespace WPFTools
{

    public partial class ImageFileViewer : System.Windows.Controls.UserControl
    {
        ImageFileViewer_ViewModel m_vm;
        string m_lastErrorMsg;
      
        ushort m_rangeLower, m_rangeUpper;
        ushort m_rangeLower1, m_rangeUpper1;
        ushort m_rangeLower2, m_rangeUpper2;
        ushort m_maxPixelValue;

        ushort[] m_imageData;
        byte[] m_colorImageData;

        ushort[] m_imageData1;
        byte[] m_colorImageData1;

        ushort[] m_imageData2;
        byte[] m_colorImageData2;

        WaveguideDB m_db;

        public UInt16 m_histogramImageWidth = 1024;
        public UInt16 m_histogramImageHeight = 256;

        ImageTool m_imageTool;


        public ImageFileViewer()
        {
            m_maxPixelValue = 65535;

            InitializeComponent();
            m_vm = new ImageFileViewer_ViewModel();
            DataContext = m_vm;
            m_lastErrorMsg = "";

            m_db = new WaveguideDB();

            m_imageTool = new ImageTool();
            bool success = m_imageTool.Init();

            m_rangeLower = 0;
            m_rangeUpper = m_maxPixelValue;
            m_rangeLower1 = 0;
            m_rangeUpper1 = m_maxPixelValue;
            m_rangeLower2 = 0;
            m_rangeUpper2 = m_maxPixelValue;

            byte[] red, green, blue;
            m_vm.colorModel.BuildColorMapForGPU(out red, out green, out blue, m_maxPixelValue);

            m_imageTool.Set_ColorMap(red, green, blue, m_maxPixelValue);



            // get the list of reference images from the database
            m_vm.refImages.Clear();
            m_db.m_refImageList.Clear();            
            if (m_db.GetAllReferenceImages())
            {
                foreach (ReferenceImageContainer ric in m_db.m_refImageList)
                {
                    m_vm.refImages.Add(ric);
                }
            }
            




#if (SIMULATE)
            GetUserList();
#else
            try
            {
                if (!GetUserList())
                    System.Windows.MessageBox.Show("Failed to Get User List from Database!", "Database Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch(Exception ex)
            {
                MainTabControl.SelectedIndex = 1;
                System.Windows.MessageBox.Show("Failed to Get User List from Database!\n" + ex.Message, 
                    "Database Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }


        public bool GetFileList(string directory, string searchPattern)
        {
            bool success = true;
            m_vm.files.Clear();

            try
            {
                string[] allFiles = Directory.GetFiles(directory, searchPattern);
                foreach (string filename in allFiles)
                {
                    ImageFileViewer_Struct item = new ImageFileViewer_Struct(filename, System.IO.Path.GetFileNameWithoutExtension(filename));
                    m_vm.files.Add(item);
                }
            }
            catch(Exception ex)
            {
                success = false;
                m_lastErrorMsg = ex.Message;
            }

            return success;
        }


        public string GetLastError()
        {
            return m_lastErrorMsg;
        }

        private void BrowsePB_Click(object sender, RoutedEventArgs e)
        {

            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.RootFolder = Environment.SpecialFolder.MyComputer;
            dlg.SelectedPath = Directory.GetCurrentDirectory();
            dlg.Description = "Select Directory for Image Files";

            DialogResult result = dlg.ShowDialog();
            if(result == DialogResult.OK)
            {
                m_vm.directory = dlg.SelectedPath;

                GetFileList(m_vm.directory, "*.zip");
            }
        }

        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(FileListBox.SelectedItem.GetType() == typeof(ImageFileViewer_Struct))
            {
                ImageFileViewer_Struct item = (ImageFileViewer_Struct)FileListBox.SelectedItem;
                string filename = item.fullPath;
                string display = item.displayName;

                m_imageData = Zip.Decompress_File(filename);

                if(m_imageData != null)
                {
                    ushort width = (ushort)Math.Sqrt(m_imageData.Length);
                    ushort height = width;

                    if(width != m_vm.width || height != m_vm.height)
                    {
                        m_vm.width = width;
                        m_vm.height = height;
                        m_vm.bitmap = BitmapFactory.New(m_vm.width, m_vm.height);
                    }

                    m_imageTool.PostFullGrayscaleImage(m_imageData, width, height);

                    m_imageTool.Convert_GrayscaleToColor(m_rangeLower, m_rangeUpper);

                    m_imageTool.Download_ColorImage(out m_colorImageData, m_vm.width, m_vm.height);

                    // display the image
                    Int32Rect displayRect = new Int32Rect(0, 0, m_vm.width, m_vm.height);
                    m_vm.bitmap.Lock();
                    m_vm.bitmap.WritePixels(displayRect, m_colorImageData, m_vm.width * 4, 0);
                    m_vm.bitmap.Unlock();
                }
            }
        }

        private void ColorModelRangeSlider_RangeChanged(object sender, RangeSliderEventArgs e)
        {
            m_rangeUpper = (ushort)((float)e.Maximum / 100.0f * (float)m_maxPixelValue);
            m_rangeLower = (ushort)((float)e.Minimum / 100.0f * (float)m_maxPixelValue);

            if (m_vm.bitmap != null && m_imageTool != null)
            {
                m_imageTool.Convert_GrayscaleToColor(m_rangeLower, m_rangeUpper);

                m_imageTool.Download_ColorImage(out m_colorImageData, m_vm.width, m_vm.height);

                // display the image
                Int32Rect displayRect = new Int32Rect(0, 0, m_vm.width, m_vm.height);
                m_vm.bitmap.Lock();
                m_vm.bitmap.WritePixels(displayRect, m_colorImageData, m_vm.width * 4, 0);
                m_vm.bitmap.Unlock();
            }
        }

        private void ColorModelRangeSlider1_RangeChanged(object sender, RangeSliderEventArgs e)
        {
            m_rangeUpper1 = (ushort)((float)e.Maximum / 100.0f * (float)m_maxPixelValue);
            m_rangeLower1 = (ushort)((float)e.Minimum / 100.0f * (float)m_maxPixelValue);

            if (m_vm.bitmap1 != null && m_imageTool != null)
            {
                m_imageTool.Convert_GrayscaleToColor(m_rangeLower1, m_rangeUpper1);

                m_imageTool.Download_ColorImage(out m_colorImageData1, m_vm.width1, m_vm.height1);

                // display the image
                Int32Rect displayRect = new Int32Rect(0, 0, m_vm.width1, m_vm.height1);
                m_vm.bitmap1.Lock();
                m_vm.bitmap1.WritePixels(displayRect, m_colorImageData1, m_vm.width1 * 4, 0);
                m_vm.bitmap1.Unlock();
            }
        }


        public bool GetUserList()
        {
#if (SIMULATE)
            m_vm.users.Clear();
            UserContainer user = new UserContainer();
            user.Firstname = "Bryan";
            user.Lastname = "Greenway";
            user.Password = "password";
            user.Role = GlobalVars.USER_ROLE_ENUM.ADMIN;
            user.UserID = 1;
            user.Username = "bgreenway";
            m_vm.users.Add(user);

            user = new UserContainer();
            user.Firstname = "Dave";
            user.Lastname = "Weaver";
            user.Password = "poon";
            user.Role = GlobalVars.USER_ROLE_ENUM.ADMIN;
            user.UserID = 2;
            user.Username = "dweaver";
            m_vm.users.Add(user);

            return true;
#else
            bool success = true;
            if (m_db.GetAllUsers())
                m_vm.users = m_db.m_userList;
            else
                success = false;


            return success;
#endif
        }

        public bool GetProjectList(int userID)
        {
#if (SIMULATE)
            m_vm.projects.Clear();

            ProjectContainer project = new ProjectContainer();
            project.Archived = false;
            project.Description = "project 1";
            project.ProjectID = 1;
            project.TimeStamp = DateTime.Now;
            m_vm.projects.Add(project);

            project = new ProjectContainer();
            project.Archived = false;
            project.Description = "project 2";
            project.ProjectID = 2;
            project.TimeStamp = DateTime.Now;
            m_vm.projects.Add(project);

            return true;
#else
            bool success = true;
            ObservableCollection<ProjectContainer> projects;
            if (m_db.GetAllProjectsForUser(userID, out projects))
                m_vm.projects = projects;
            else
                success = false;
            return success;
#endif
        }


        public bool GetPlateList(int projectID)
        {
#if (SIMULATE)
            m_vm.plates.Clear();
            PlateContainer plate = new PlateContainer();
            plate.Barcode = "123456";
            plate.BarcodeValid = true;
            plate.Description = "plate 1";
            plate.IsPublic = true;
            plate.OwnerID = 1;
            plate.PlateID = 1;
            plate.PlateIDResetBehavior = PLATE_ID_RESET_BEHAVIOR.CONSTANT;
            plate.PlateTypeID = 1;
            plate.ProjectID = 1;
            m_vm.plates.Add(plate);

            plate = new PlateContainer();
            plate.Barcode = "456123";
            plate.BarcodeValid = true;
            plate.Description = "plate 2";
            plate.IsPublic = true;
            plate.OwnerID = 2;
            plate.PlateID = 2;
            plate.PlateIDResetBehavior = PLATE_ID_RESET_BEHAVIOR.CONSTANT;
            plate.PlateTypeID = 2;
            plate.ProjectID = 2;
            m_vm.plates.Add(plate);

            return true;
#else
            bool success = true;
            if (m_db.GetAllPlatesForProject(projectID))
                m_vm.plates = m_db.m_plateList;
            else
                success = false;
            return success;
#endif
        }

        public bool GetExperimentList(int plateID)
        {
#if (SIMULATE)
            m_vm.experiments.Clear();
            ExperimentContainer experiment = new ExperimentContainer();
            experiment.Description = "experiment 1";
            experiment.ExperimentID = 1;
            experiment.HorzBinning = 4;
            experiment.MethodID = 1;
            experiment.PlateID = 1;
            experiment.ROI_Height = 256;
            experiment.ROI_Origin_X = 0;
            experiment.ROI_Origin_Y = 0;
            experiment.ROI_Width = 256;
            experiment.TimeStamp = DateTime.Now;
            experiment.VertBinning = 4;
            m_vm.experiments.Add(experiment);

            experiment = new ExperimentContainer();
            experiment.Description = "experiment 2";
            experiment.ExperimentID = 2;
            experiment.HorzBinning = 4;
            experiment.MethodID = 2;
            experiment.PlateID = 2;
            experiment.ROI_Height = 256;
            experiment.ROI_Origin_X = 0;
            experiment.ROI_Origin_Y = 0;
            experiment.ROI_Width = 256;
            experiment.TimeStamp = DateTime.Now;
            experiment.VertBinning = 4;
            m_vm.experiments.Add(experiment);

            return true;
#else
            bool success = true;
            ObservableCollection<ExperimentContainer> experimentList;
            if (m_db.GetAllExperimentsForPlate(plateID, out experimentList))
                m_vm.experiments = experimentList;
            else
                success = false;
            return success;
#endif
        }


        public bool GetIndicatorList(int experimentID)
        {
#if (SIMULATE)
            m_vm.indicators.Clear();
            ExperimentIndicatorContainer indicator = new ExperimentIndicatorContainer();
            indicator.CycleTime = 100;
            indicator.DarkFieldRefImageID = 1;
            indicator.Description = "indicator 1";
            indicator.EmissionFilterDesc = "filter 1";
            indicator.EmissionFilterPos = 1;
            indicator.ExcitationFilterDesc = "filter 2";
            indicator.ExcitationFilterPos = 2;
            indicator.ExperimentID = 1;
            indicator.ExperimentIndicatorID = 1;
            indicator.Exposure = 50;
            indicator.FlatFieldCorrection = FLATFIELD_SELECT.NONE;
            indicator.FlatFieldRefImageID = 1;
            indicator.Gain = 10;
            indicator.MaskID = 1;
            indicator.OptimizeWellList = null;
            indicator.PreAmpGain = 1;
            indicator.SignalType = SIGNAL_TYPE.UP;
            indicator.Verified = true;
            m_vm.indicators.Add(indicator);

            indicator = new ExperimentIndicatorContainer();
            indicator.CycleTime = 100;
            indicator.DarkFieldRefImageID = 1;
            indicator.Description = "indicator 2";
            indicator.EmissionFilterDesc = "filter 1";
            indicator.EmissionFilterPos = 1;
            indicator.ExcitationFilterDesc = "filter 2";
            indicator.ExcitationFilterPos = 2;
            indicator.ExperimentID = 2;
            indicator.ExperimentIndicatorID = 2;
            indicator.Exposure = 50;
            indicator.FlatFieldCorrection = FLATFIELD_SELECT.NONE;
            indicator.FlatFieldRefImageID = 1;
            indicator.Gain = 10;
            indicator.MaskID = 1;
            indicator.OptimizeWellList = null;
            indicator.PreAmpGain = 1;
            indicator.SignalType = SIGNAL_TYPE.UP;
            indicator.Verified = true;
            m_vm.indicators.Add(indicator);

            return true;
#else
            bool success = true;
            ObservableCollection<ExperimentIndicatorContainer> indicators;
            if (m_db.GetAllExperimentIndicatorsForExperiment(experimentID, out indicators))
            {
                m_vm.indicators = indicators;
            }
            else
                success = false;

            return success;
#endif
        }

        public bool GetImages(int experimentIndicatorID)
        {
#if (SIMULATE)
            m_vm.images.Clear();
            ExperimentImageContainer image = new ExperimentImageContainer();
            image.CompressionAlgorithm = COMPRESSION_ALGORITHM.GZIP;
            image.ExperimentImageID = 1;
            image.ExperimentIndicatorID = 1;
            image.FilePath = "C:\\Users\\bryan\\Documents\\Visual Studio 2015\\Projects\\Test\\Test\\bin\\x64\\Debug\\00007087_wgi.zip";
            image.ImageData = null;
            image.MaxPixelValue = 65535;
            image.MSecs = 7087;
            image.TimeStamp = DateTime.Now;
            m_vm.images.Add(image);

            image = new ExperimentImageContainer();
            image.CompressionAlgorithm = COMPRESSION_ALGORITHM.GZIP;
            image.ExperimentImageID = 2;
            image.ExperimentIndicatorID = 2;
            image.FilePath = "C:\\Users\\bryan\\Documents\\Visual Studio 2015\\Projects\\Test\\Test\\bin\\x64\\Debug\\00008085_wgi.zip";
            image.ImageData = null;
            image.MaxPixelValue = 65535;
            image.MSecs = 8085;
            image.TimeStamp = DateTime.Now;
            m_vm.images.Add(image);

            image = new ExperimentImageContainer();
            image.CompressionAlgorithm = COMPRESSION_ALGORITHM.GZIP;
            image.ExperimentImageID = 3;
            image.ExperimentIndicatorID = 3;
            image.FilePath = "C:\\Users\\bryan\\Documents\\Visual Studio 2015\\Projects\\Test\\Test\\bin\\x64\\Debug\\00009084_wgi.zip";
            image.ImageData = null;
            image.MaxPixelValue = 65535;
            image.MSecs = 9084;
            image.TimeStamp = DateTime.Now;
            m_vm.images.Add(image);

            return true;
#else
            bool success = true;
            if (m_db.GetAllExperimentImagesForExperimentIndicator(experimentIndicatorID))
                m_vm.images = m_db.m_expImageList;
            else
                success = false;

            return success;
#endif
        }




        private void UserListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(m_vm.user != null)
                GetProjectList(m_vm.user.UserID);
            m_vm.plates.Clear();
            m_vm.experiments.Clear();
            m_vm.indicators.Clear();
            m_vm.images.Clear();
            ClearImage();
        }

        private void ProjectListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(m_vm.project != null)
                GetPlateList(m_vm.project.ProjectID);
            m_vm.experiments.Clear();
            m_vm.indicators.Clear();
            m_vm.images.Clear();
            ClearImage();
        }

        private void PlateListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(m_vm.plate != null)
                GetExperimentList(m_vm.plate.PlateID);
            m_vm.indicators.Clear();
            m_vm.images.Clear();
            ClearImage();
        }

        private void ExperimentListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(m_vm.experiment != null)
                GetIndicatorList(m_vm.experiment.ExperimentID);
            m_vm.images.Clear();
            ClearImage();
        }

        private void IndicatorListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(m_vm.indicator != null)
                GetImages(m_vm.indicator.ExperimentIndicatorID);
            ClearImage();
        }

        private void ClearImage()
        {
            if (m_vm.bitmap1 != null)
                m_vm.bitmap1.Clear();
        }

        private void RefImageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RefImageListBox.SelectedItem == null) return;

            if(RefImageListBox.SelectedItem.GetType() == typeof(ReferenceImageContainer))
            {
                ReferenceImageContainer ric = (ReferenceImageContainer)RefImageListBox.SelectedItem;

                if(ric.ImageData != null)
                {
                    if (ric.Width != m_vm.width2 || ric.Height != m_vm.height2)
                    {
                        m_vm.width2 = (ushort)ric.Width;
                        m_vm.height2 = (ushort)ric.Height;
                        m_vm.bitmap2 = BitmapFactory.New(m_vm.width2, m_vm.height2);
                    }

                    m_imageTool.PostFullGrayscaleImage(ric.ImageData, m_vm.width2, m_vm.height2);

                    m_imageTool.Convert_GrayscaleToColor(m_rangeLower2, m_rangeUpper2);

                    m_imageTool.Download_ColorImage(out m_colorImageData2, m_vm.width2, m_vm.height2);

                    // display the image
                    Int32Rect displayRect = new Int32Rect(0, 0, m_vm.width2, m_vm.height2);
                    m_vm.bitmap2.Lock();
                    m_vm.bitmap2.WritePixels(displayRect, m_colorImageData2, m_vm.width2 * 4, 0);
                    m_vm.bitmap2.Unlock();



                    // calculate the image histogram
                    UInt32[] histogram;
                    m_imageTool.GetHistogram_512(out histogram, 16);

                    // build the histogram image and download it to the CPU
                    byte[] histImage;
                    m_imageTool.GetHistogramImage_512(out histImage, m_histogramImageWidth, m_histogramImageHeight, 0);

                    // display the histogram image
                    m_vm.histImage2 = BitmapFactory.New(m_histogramImageWidth, m_histogramImageHeight);
                    Int32Rect histRect = new Int32Rect(0, 0, m_histogramImageWidth, m_histogramImageHeight);
                    m_vm.histImage2.Lock();
                    m_vm.histImage2.WritePixels(histRect, histImage, m_histogramImageWidth * 4, 0);
                    m_vm.histImage2.Unlock();
                    HistImage.Source = m_vm.histImage2;
                }
            }
       
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
          
        }

        private void ImageDisplay2_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(ImageDisplay2);

            int x = (int)(p.X/ImageDisplay2.ActualWidth * 1024.0);
            int y = (int)(p.Y/ImageDisplay2.ActualHeight * 1024.0);

            UInt16 val = 0;

            if (RefImageListBox.SelectedItem != null)
            {
                ReferenceImageContainer ric = (ReferenceImageContainer)RefImageListBox.SelectedItem;
                int index = (y * 1024) + x;
                val = ric.ImageData[index];
            }

            XPos.Text = x.ToString();
            YPos.Text = y.ToString();
            PixelValue.Text = val.ToString();
            
        }

        private void ColorModelRangeSlider2_RangeChanged(object sender, RangeSliderEventArgs e)
        {
            m_rangeUpper2 = (ushort)((float)e.Maximum / 100.0f * (float)m_maxPixelValue);
            m_rangeLower2 = (ushort)((float)e.Minimum / 100.0f * (float)m_maxPixelValue);

            if (m_vm.bitmap2 != null && m_imageTool != null)
            {
                m_imageTool.Convert_GrayscaleToColor(m_rangeLower2, m_rangeUpper2);

                m_imageTool.Download_ColorImage(out m_colorImageData2, m_vm.width2, m_vm.height2);

                // display the image
                Int32Rect displayRect = new Int32Rect(0, 0, m_vm.width2, m_vm.height2);
                m_vm.bitmap2.Lock();
                m_vm.bitmap2.WritePixels(displayRect, m_colorImageData2, m_vm.width2 * 4, 0);
                m_vm.bitmap2.Unlock();
            }
        }

        private void ImageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_vm.image == null) return;

            string filename = m_vm.image.FilePath;

                m_imageData1 = Zip.Decompress_File(filename);

                if (m_imageData1 != null)
                {
                    ushort width = (ushort)Math.Sqrt(m_imageData1.Length);
                    ushort height = width;

                    if (width != m_vm.width1 || height != m_vm.height1)
                    {
                        m_vm.width1 = width;
                        m_vm.height1 = height;
                        m_vm.bitmap1 = BitmapFactory.New(m_vm.width1, m_vm.height1);
                    }

                    m_imageTool.PostFullGrayscaleImage(m_imageData1, width, height);

                    m_imageTool.Convert_GrayscaleToColor(m_rangeLower1, m_rangeUpper1);

                    m_imageTool.Download_ColorImage(out m_colorImageData1, m_vm.width1, m_vm.height1);

                    // display the image
                    Int32Rect displayRect = new Int32Rect(0, 0, m_vm.width1, m_vm.height1);
                    m_vm.bitmap1.Lock();
                    m_vm.bitmap1.WritePixels(displayRect, m_colorImageData1, m_vm.width1 * 4, 0);
                    m_vm.bitmap1.Unlock();
                }
            
        }

        

        public bool SaveImage(string filename)
        {
            bool success = true;
            try
            {
                BitmapSource image;

                if(MainTabControl.SelectedIndex == 0)  // database tab
                {
                    image = BitmapSource.Create(
                    m_vm.width1,
                    m_vm.height1,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null,
                    m_colorImageData1,
                    m_vm.width1 * 4);
                }
                else  // directory browse tab
                {
                    image = BitmapSource.Create(
                    m_vm.width,
                    m_vm.height,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null,
                    m_colorImageData,
                    m_vm.width * 4);
                }

              
                FileStream stream = new FileStream(filename, FileMode.Create);
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                TextBlock myTextBlock = new TextBlock();
                myTextBlock.Text = "Codec Author is: " + encoder.CodecInfo.Author.ToString();
                encoder.FlipHorizontal = false;
                encoder.FlipVertical = false;
                encoder.QualityLevel = 80;
                encoder.Rotation = Rotation.Rotate0;
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
            }
            catch(Exception ex)
            {
                m_lastErrorMsg = ex.Message;
                success = false;
            }

            return success;

        }
    }


    public class ImageFileViewer_Struct
    {
        public string fullPath { get; set; }
        public string displayName { get; set; }
        public ImageFileViewer_Struct(string FullPath, string DisplayName)
        {
            fullPath = FullPath;
            displayName = DisplayName;
        }
    }

    public class ImageFileViewer_ViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ImageFileViewer_Struct> _files;
        public ObservableCollection<ImageFileViewer_Struct> files
        {
            get { return _files; }
            set { _files = value; OnPropertyChanged(new PropertyChangedEventArgs("files")); }
        }

        private string _directory;
        public string directory
        {
            get { return _directory; }
            set { _directory = value; OnPropertyChanged(new PropertyChangedEventArgs("directory")); }
        }

        private string _currentFile;
        public string currentFile
        {
            get { return _currentFile; }
            set { _currentFile = value; OnPropertyChanged(new PropertyChangedEventArgs("currentFile")); }
        }

        private ImageSaveTool.ColorModel _colorModel;
        public ImageSaveTool.ColorModel colorModel
        {
            get { return _colorModel; }
            set { _colorModel = value; OnPropertyChanged(new PropertyChangedEventArgs("colorModel")); }
        }

        private WriteableBitmap _bitmap;
        public WriteableBitmap bitmap
        {
            get { return _bitmap; }
            set { _bitmap = value; OnPropertyChanged(new PropertyChangedEventArgs("bitmap")); }
        }

        private ushort _width;
        public ushort width
        {
            get { return _width; }
            set { _width = value; OnPropertyChanged(new PropertyChangedEventArgs("width")); }
        }

        private ushort _height;
        public ushort height
        {
            get { return _height; }
            set { _height = value; OnPropertyChanged(new PropertyChangedEventArgs("height")); }
        }


        private WriteableBitmap _bitmap1;
        public WriteableBitmap bitmap1
        {
            get { return _bitmap1; }
            set { _bitmap1 = value; OnPropertyChanged(new PropertyChangedEventArgs("bitmap1")); }
        }

        private ushort _width1;
        public ushort width1
        {
            get { return _width1; }
            set { _width1 = value; OnPropertyChanged(new PropertyChangedEventArgs("width1")); }
        }

        private ushort _height1;
        public ushort height1
        {
            get { return _height1; }
            set { _height1 = value; OnPropertyChanged(new PropertyChangedEventArgs("height1")); }
        }


        private WriteableBitmap _bitmap2;
        public WriteableBitmap bitmap2
        {
            get { return _bitmap2; }
            set { _bitmap2 = value; OnPropertyChanged(new PropertyChangedEventArgs("bitmap2")); }
        }

        private ushort _width2;
        public ushort width2
        {
            get { return _width2; }
            set { _width2 = value; OnPropertyChanged(new PropertyChangedEventArgs("width2")); }
        }

        private ushort _height2;
        public ushort height2
        {
            get { return _height2; }
            set { _height2 = value; OnPropertyChanged(new PropertyChangedEventArgs("height2")); }
        }


        private WriteableBitmap _histImage2;
        public WriteableBitmap histImage2
        {
            get { return _histImage2; }
            set { _histImage2 = value; OnPropertyChanged(new PropertyChangedEventArgs("histImage2")); }
        }




        private UserContainer _user;
        public UserContainer user
        {
            get { return _user; }
            set { _user = value; OnPropertyChanged(new PropertyChangedEventArgs("user")); }
        }

        private ProjectContainer _project;
        public ProjectContainer project
        {
            get { return _project; }
            set { _project = value; OnPropertyChanged(new PropertyChangedEventArgs("project")); }
        }

        private PlateContainer _plate;
        public PlateContainer plate
        {
            get { return _plate; }
            set { _plate = value; OnPropertyChanged(new PropertyChangedEventArgs("plate")); }
        }

        private ExperimentContainer _experiment;
        public ExperimentContainer experiment
        {
            get { return _experiment; }
            set { _experiment = value; OnPropertyChanged(new PropertyChangedEventArgs("experiment")); }
        }

        private ExperimentIndicatorContainer _indicator;
        public ExperimentIndicatorContainer indicator
        {
            get { return _indicator; }
            set { _indicator = value; OnPropertyChanged(new PropertyChangedEventArgs("indicator")); }
        }

        private ExperimentImageContainer _image;
        public ExperimentImageContainer image
        {
            get { return _image; }
            set { _image = value; OnPropertyChanged(new PropertyChangedEventArgs("image")); }
        }


        private ObservableCollection<UserContainer> _users;
        public ObservableCollection<UserContainer> users
        {
            get { return _users; }
            set { _users = value; OnPropertyChanged(new PropertyChangedEventArgs("users")); }
        }


        private ObservableCollection<ProjectContainer> _projects;
        public ObservableCollection<ProjectContainer> projects
        {
            get { return _projects; }
            set { _projects = value; OnPropertyChanged(new PropertyChangedEventArgs("projects")); }
        }


        private ObservableCollection<PlateContainer> _plates;
        public ObservableCollection<PlateContainer> plates
        {
            get { return _plates; }
            set { _plates = value; OnPropertyChanged(new PropertyChangedEventArgs("plates")); }
        }

        private ObservableCollection<ExperimentContainer> _experiments;
        public ObservableCollection<ExperimentContainer> experiments
        {
            get { return _experiments; }
            set { _experiments = value; OnPropertyChanged(new PropertyChangedEventArgs("experiments")); }
        }


        private ObservableCollection<ExperimentIndicatorContainer> _indicators;
        public ObservableCollection<ExperimentIndicatorContainer> indicators
        {
            get { return _indicators; }
            set { _indicators = value; OnPropertyChanged(new PropertyChangedEventArgs("indicators")); }
        }

        private ObservableCollection<ExperimentImageContainer> _images;
        public ObservableCollection<ExperimentImageContainer> images
        {
            get { return _images; }
            set { _images = value; OnPropertyChanged(new PropertyChangedEventArgs("images")); }
        }



        private ReferenceImageContainer _refImage;
        public ReferenceImageContainer refImage
        {
            get { return _refImage; }
            set { _refImage = value; OnPropertyChanged(new PropertyChangedEventArgs("refImage")); }
        }

        private ObservableCollection<ReferenceImageContainer> _refImages;
        public ObservableCollection<ReferenceImageContainer> refImages
        {
            get { return _refImages; }
            set { _refImages = value; OnPropertyChanged(new PropertyChangedEventArgs("refImages")); }
        }



        public ImageFileViewer_ViewModel()
        {
            files = new ObservableCollection<ImageFileViewer_Struct>();
            colorModel = new ImageSaveTool.ColorModel();
            width = 0;
            height = 0;
            width1 = 0;
            height1 = 0;
            width2 = 0;
            height2 = 0;

            users = new ObservableCollection<UserContainer>();
            projects = new ObservableCollection<ProjectContainer>();
            plates = new ObservableCollection<PlateContainer>();
            experiments = new ObservableCollection<ExperimentContainer>();
            indicators = new ObservableCollection<ExperimentIndicatorContainer>();
            images = new ObservableCollection<ExperimentImageContainer>();
            refImages = new ObservableCollection<ReferenceImageContainer>();

        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }
    }
}
