using System;
using System.Windows;
using System.Windows.Controls;
using Waveguide;
using WPFTools;
using System.Collections.ObjectModel;
using CudaTools;
using System.Windows.Media.Imaging;

namespace WaveExplorer
{
   
    public partial class ExperimentExplorer : UserControl
    {

        WaveguideDB m_db;
        ExperimentExplorer_ViewModel m_vm;

        ushort m_rangeLower, m_rangeUpper;
       
        ushort m_maxPixelValue;

        ushort[] m_imageData;
        byte[] m_colorImageData;

           

        ImageTool m_imageTool;

        public ExperimentExplorer()
        {
            m_maxPixelValue = 65535;

            InitializeComponent();
            m_vm = new ExperimentExplorer_ViewModel();
            DataContext = m_vm;

            m_db = new WaveguideDB();


            m_imageTool = new ImageTool();
            bool success = m_imageTool.Init();

            m_rangeLower = 0;
            m_rangeUpper = m_maxPixelValue;
           
            byte[] red, green, blue;
            m_vm.colorModel.BuildColorMapForGPU(out red, out green, out blue, m_maxPixelValue);

            m_imageTool.Set_ColorMap(red, green, blue, m_maxPixelValue);



            try
            {
                if (!GetUserList())
                    System.Windows.MessageBox.Show("Failed to Get User List from Database!", "Database Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                else
                {
                    if(GlobalVars.Instance.UserRole == GlobalVars.USER_ROLE_ENUM.ADMIN)
                    {
                        UserPB.IsEnabled = true;
                    }
                    else
                    {
                        UserPB.IsEnabled = false;
                    }

                    UserContainer user;
                    if(m_db.GetUser(GlobalVars.Instance.UserID, out user))
                    {
                        m_vm.user = user;

                        if (m_vm.user != null)
                            GetProjectList(m_vm.user.UserID);
                        m_vm.plates.Clear();
                        m_vm.experiments.Clear();
                        m_vm.indicators.Clear();
                        m_vm.images.Clear();
                        ClearImage();

                        m_vm.userStatus = ExperimentExplorer_ViewModel.STEP_STATUS.READY;
                        m_vm.projectStatus = ExperimentExplorer_ViewModel.STEP_STATUS.NEEDS_INPUT;
                        m_vm.plateStatus = ExperimentExplorer_ViewModel.STEP_STATUS.WAITING_FOR_PREDECESSOR;
                        m_vm.experimentStatus = ExperimentExplorer_ViewModel.STEP_STATUS.WAITING_FOR_PREDECESSOR;
                        SetExperimentDescriptionText();
                    }
                    else
                    {
                        m_vm.userStatus = ExperimentExplorer_ViewModel.STEP_STATUS.NEEDS_INPUT;
                        m_vm.projectStatus = ExperimentExplorer_ViewModel.STEP_STATUS.WAITING_FOR_PREDECESSOR;
                        m_vm.plateStatus = ExperimentExplorer_ViewModel.STEP_STATUS.WAITING_FOR_PREDECESSOR;
                        m_vm.experimentStatus = ExperimentExplorer_ViewModel.STEP_STATUS.WAITING_FOR_PREDECESSOR;
                        SetExperimentDescriptionText();
                    }

                   
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to Get User List from Database!\n" + ex.Message,
                    "Database Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }


        private void SetExperimentDescriptionText()
        {
            if(m_vm.experimentStatus == ExperimentExplorer_ViewModel.STEP_STATUS.READY)
            {
                m_vm.experimentDescription = m_vm.user.Username + " / " + m_vm.project.Description + " / " + m_vm.experiment.Description;
            }
            else if (m_vm.plateStatus == ExperimentExplorer_ViewModel.STEP_STATUS.READY)
            {
                m_vm.experimentDescription = m_vm.user.Username + " / " + m_vm.project.Description + " / " +  m_vm.plate.Description;
            }
            else if (m_vm.projectStatus == ExperimentExplorer_ViewModel.STEP_STATUS.READY)
            {
                m_vm.experimentDescription = m_vm.user.Username + " / " + m_vm.project.Description;
            }
            else if (m_vm.userStatus == ExperimentExplorer_ViewModel.STEP_STATUS.READY)
            {
                m_vm.experimentDescription = m_vm.user.Username;
            }
            else
            {
                m_vm.experimentDescription = "<Select User>";
            }
        }


        private void ClearImage()
        {
            if (m_vm.bitmap != null)
                m_vm.bitmap.Clear();
        }

        private void MenuItemHandler(object sender, RoutedEventArgs args)
        {
            var item = sender as MenuItem;
            if(item != null)
            {
                if(item.DataContext.GetType() == typeof(UserContainer))
                {
                    m_vm.user = (UserContainer)item.DataContext;

                    if (m_vm.user != null)
                        GetProjectList(m_vm.user.UserID);
                    m_vm.plates.Clear();
                    m_vm.experiments.Clear();
                    m_vm.indicators.Clear();
                    m_vm.images.Clear();
                    ClearImage();

                    m_vm.userStatus = ExperimentExplorer_ViewModel.STEP_STATUS.READY;
                    m_vm.projectStatus = ExperimentExplorer_ViewModel.STEP_STATUS.NEEDS_INPUT;
                    m_vm.plateStatus = ExperimentExplorer_ViewModel.STEP_STATUS.WAITING_FOR_PREDECESSOR;
                    m_vm.experimentStatus = ExperimentExplorer_ViewModel.STEP_STATUS.WAITING_FOR_PREDECESSOR;
                    SetExperimentDescriptionText();
                }
                else if (item.DataContext.GetType() == typeof(ProjectContainer))
                {
                    m_vm.project = (ProjectContainer)item.DataContext;

                    if (m_vm.project != null)
                        GetPlateList(m_vm.project.ProjectID);
                    m_vm.experiments.Clear();
                    m_vm.indicators.Clear();
                    m_vm.images.Clear();
                    ClearImage();

                    m_vm.userStatus = ExperimentExplorer_ViewModel.STEP_STATUS.READY;
                    m_vm.projectStatus = ExperimentExplorer_ViewModel.STEP_STATUS.READY;
                    m_vm.plateStatus = ExperimentExplorer_ViewModel.STEP_STATUS.NEEDS_INPUT;
                    m_vm.experimentStatus = ExperimentExplorer_ViewModel.STEP_STATUS.WAITING_FOR_PREDECESSOR;
                    SetExperimentDescriptionText();
                }
                else if (item.DataContext.GetType() == typeof(PlateContainer))
                {
                    m_vm.plate = (PlateContainer)item.DataContext;

                    if (m_vm.plate != null)
                        GetExperimentList(m_vm.plate.PlateID);
                    m_vm.indicators.Clear();
                    m_vm.images.Clear();
                    ClearImage();

                    m_vm.userStatus = ExperimentExplorer_ViewModel.STEP_STATUS.READY;
                    m_vm.projectStatus = ExperimentExplorer_ViewModel.STEP_STATUS.READY;
                    m_vm.plateStatus = ExperimentExplorer_ViewModel.STEP_STATUS.READY;
                    m_vm.experimentStatus = ExperimentExplorer_ViewModel.STEP_STATUS.NEEDS_INPUT;
                    SetExperimentDescriptionText();
                }
                else if (item.DataContext.GetType() == typeof(ExperimentContainer))
                {
                    m_vm.experiment = (ExperimentContainer)item.DataContext;

                    if (m_vm.experiment != null)
                        GetIndicatorList(m_vm.experiment.ExperimentID);
                    m_vm.images.Clear();
                    ClearImage();

                    m_vm.userStatus = ExperimentExplorer_ViewModel.STEP_STATUS.READY;
                    m_vm.projectStatus = ExperimentExplorer_ViewModel.STEP_STATUS.READY;
                    m_vm.plateStatus = ExperimentExplorer_ViewModel.STEP_STATUS.READY;
                    m_vm.experimentStatus = ExperimentExplorer_ViewModel.STEP_STATUS.READY;
                    SetExperimentDescriptionText();
                }

                int userID = Convert.ToInt32(item.Tag);
            }

            args.Handled = true;
        }


        private void IndicatorList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(sender.GetType() == typeof(ListBox))
            {
                ListBox listbox = (ListBox)sender;

                if(listbox.SelectedItem != null || listbox.SelectedIndex != -1)
                {
                    m_vm.indicator = (ExperimentIndicatorContainer)listbox.SelectedItem;

                    m_vm.images.Clear();

                    GetImages(m_vm.indicator.ExperimentIndicatorID);
                }
            }
        }


        private void ImageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_vm.image == null) return;

            string filename = m_vm.image.FilePath;

            m_imageData = Zip.Decompress_File(filename);

            if (m_imageData != null)
            {
                ushort width = (ushort)Math.Sqrt(m_imageData.Length);
                ushort height = width;

                if (width != m_vm.width || height != m_vm.height)
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


        public bool GetUserList()
        {
            bool success = true;
            if (m_db.GetAllUsers())
                m_vm.users = m_db.m_userList;
            else
                success = false;

            return success;
        }

        public bool GetProjectList(int userID)
        {
            bool success = true;
            ObservableCollection<ProjectContainer> projects;
            if (m_db.GetAllProjectsForUser(userID, out projects))
                m_vm.projects = projects;
            else
                success = false;
            return success;
        }


        public bool GetPlateList(int projectID)
        {
            bool success = true;
            if (m_db.GetAllPlatesForProject(projectID))
                m_vm.plates = m_db.m_plateList;
            else
                success = false;
            return success;
        }

        private void ColorModelRangeSlider1_RangeChanged(object sender, RangeSliderEventArgs e)
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

        public bool GetExperimentList(int plateID)
        {
            bool success = true;
            ObservableCollection<ExperimentContainer> experimentList;
            if (m_db.GetAllExperimentsForPlate(plateID, out experimentList))
                m_vm.experiments = experimentList;
            else
                success = false;
            return success;
        }


        public bool GetIndicatorList(int experimentID)
        {
            bool success = true;
            ObservableCollection<ExperimentIndicatorContainer> indicators;
            if (m_db.GetAllExperimentIndicatorsForExperiment(experimentID, out indicators))
            {
                m_vm.indicators = indicators;
            }
            else
                success = false;

            return success;
        }

        public bool GetImages(int experimentIndicatorID)
        {
            bool success = true;
            if (m_db.GetAllExperimentImagesForExperimentIndicator(experimentIndicatorID))
                m_vm.images = m_db.m_expImageList;
            else
                success = false;

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




    public class ExperimentExplorer_ViewModel : ObservableObject
    {

        public enum STEP_STATUS { WAITING_FOR_PREDECESSOR, NEEDS_INPUT, READY };

        private STEP_STATUS _userStatus;
        public STEP_STATUS userStatus
        {
            get { return _userStatus; }
            set { if (value != _userStatus) { _userStatus = value; OnPropertyChanged("userStatus"); } }
        }

        private STEP_STATUS _projectStatus;
        public STEP_STATUS   projectStatus
        {
            get { return _projectStatus; }
            set { if (value != _projectStatus) { _projectStatus = value; OnPropertyChanged("projectStatus"); } }
        }

        private STEP_STATUS _plateStatus;
        public STEP_STATUS plateStatus
        {
            get { return _plateStatus; }
            set { if (value != _plateStatus) { _plateStatus = value; OnPropertyChanged("plateStatus"); } }
        }

        private STEP_STATUS _experimentStatus;
        public STEP_STATUS experimentStatus
        {
            get { return _experimentStatus; }
            set { if (value != _experimentStatus) { _experimentStatus = value; OnPropertyChanged("experimentStatus"); } }
        }


        private string _experimentDescription;
        public string experimentDescription
        {
            get { return _experimentDescription; }
            set { if (value != _experimentDescription) { _experimentDescription = value; OnPropertyChanged("experimentDescription"); } }
        }

        private ObservableCollection<UserContainer> _users;
        public ObservableCollection<UserContainer> users
        {
            get { return _users; }
            set { if (value != _users) { _users = value; OnPropertyChanged("users"); } }
        }

        private UserContainer _user;
        public UserContainer user
        {
            get { return _user;}
            set { if (value != _user) { _user = value; OnPropertyChanged("user"); } }
        }


        private ObservableCollection<ProjectContainer> _projects;
        public  ObservableCollection<ProjectContainer>  projects
        {
            get { return _projects; }
            set { if (value != _projects) { _projects = value; OnPropertyChanged("projects"); } }
        }

        private ProjectContainer _project;
        public  ProjectContainer  project
        {
            get { return _project; }
            set { if (value != _project) { _project = value; OnPropertyChanged("project"); } }
        }



        private ObservableCollection<PlateContainer> _plates;
        public  ObservableCollection<PlateContainer>  plates
        {
            get { return _plates; }
            set { if (value != _plates) { _plates = value; OnPropertyChanged("plates"); } }
        }

        private PlateContainer _plate;
        public  PlateContainer  plate
        {
            get { return _plate; }
            set { if (value != _plate) { _plate = value; OnPropertyChanged("plate"); } }
        }



        private ObservableCollection<ExperimentContainer> _experiments;
        public  ObservableCollection<ExperimentContainer>  experiments
        {
            get { return _experiments; }
            set { if (value != _experiments) { _experiments = value; OnPropertyChanged("experiments"); } }
        }

        private ExperimentContainer _experiment;
        public  ExperimentContainer  experiment
        {
            get { return _experiment; }
            set { if (value != _experiment) { _experiment = value; OnPropertyChanged("experiment"); } }
        }


        private ObservableCollection<ExperimentIndicatorContainer> _indicators;
        public  ObservableCollection<ExperimentIndicatorContainer>  indicators
        {
            get { return _indicators; }
            set { if (value != _indicators) { _indicators = value; OnPropertyChanged("indicators"); } }
        }

        private ExperimentIndicatorContainer _indicator;
        public  ExperimentIndicatorContainer  indicator
        { 
            get { return _indicator; }        
            set { if (value != _indicator) { _indicator = value; OnPropertyChanged("indicator"); } }
        }


        private ObservableCollection<ExperimentImageContainer> _images;
        public ObservableCollection<ExperimentImageContainer> images
        {
            get { return _images; }
            set { if (value != _images) { _images = value; OnPropertyChanged("images"); } }
        }

        private ExperimentImageContainer _image;
        public  ExperimentImageContainer  image
        {
            get { return _image; }
            set { if (value != _image) { _image = value; OnPropertyChanged("image"); } }
        }





        private ColorModel _colorModel;
        public ColorModel colorModel
        {
            get { return _colorModel; }
            set { _colorModel = value; OnPropertyChanged("colorModel"); }
        }

        private WriteableBitmap _bitmap;
        public WriteableBitmap bitmap
        {
            get { return _bitmap; }
            set { _bitmap = value; OnPropertyChanged("bitmap"); }
        }

        private ushort _width;
        public ushort width
        {
            get { return _width; }
            set { _width = value; OnPropertyChanged("width"); }
        }

        private ushort _height;
        public ushort height
        {
            get { return _height; }
            set { _height = value; OnPropertyChanged("height"); }
        }


     



        public ExperimentExplorer_ViewModel()
        {
            colorModel = new ColorModel();
            width = 0;
            height = 0;
          
            _userStatus = STEP_STATUS.NEEDS_INPUT;
            _projectStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;
            _plateStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;
            _experimentStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;


            _experimentDescription = "";
            _users = new ObservableCollection<UserContainer>();
            _projects = new ObservableCollection<ProjectContainer>();
            _plates = new ObservableCollection<PlateContainer>();
            _experiments = new ObservableCollection<ExperimentContainer>();
            _indicators = new ObservableCollection<ExperimentIndicatorContainer>();
            _images = new ObservableCollection<ExperimentImageContainer>();

          
        }
    }

          

}
