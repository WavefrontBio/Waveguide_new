using Infragistics.Windows.DataPresenter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// Interaction logic for ExperimentConfigurator.xaml
    /// </summary>
    public partial class ExperimentConfigurator : UserControl
    {
        WaveguideDB wgDB;

        ExperimentConfiguratorViewModel VM;

        Imager m_imager;

        VWorks m_vworks;


        public ExperimentConfigurator()
        {
            InitializeComponent();

            wgDB = new WaveguideDB();

            VM = new ExperimentConfiguratorViewModel();

            m_imager = null;

            m_vworks = null;

            this.DataContext = VM;

            PlateTypeContainer ptc;
            bool success = wgDB.GetDefaultPlateType(out ptc);
            if (success)
                WellSelection.Init(ptc.Rows, ptc.Cols);
            else
                WellSelection.Init(16, 24);

            WellSelection.NewWellSetSelected += WellSelection_NewWellSetSelected;
            

        }

        void WellSelection_NewWellSetSelected(object sender, EventArgs e)
        {
            WellSelectionEventArgs ev = (WellSelectionEventArgs)e;
            VM.ControlSubtractionWellList.Clear();

            foreach (Tuple<int, int> well in ev.WellList)
            {
                VM.ControlSubtractionWellList.Add(well);
            }

            VM.SetExperimentStatus();
        }


        public void SetImager(Imager imager)
        {
            m_imager = imager;
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshProjectList();
            PopulateMaskList();
            PopulatePlateTypeList();
        }


        private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {   
            // disable all groups below and clear combobox lists         
            if (VM.MethodList != null) VM.MethodList.Clear();
            VM.Method = null;
            VM.MethodFilter = 0;
       
            if(VM.CompoundPlateList != null) VM.CompoundPlateList.Clear();     

            if (VM.IndicatorList != null) VM.IndicatorList.Clear();

            
            // get selection
            VM.Project = (ProjectContainer)ProjectComboBox.SelectedItem;

            // if valid selection, enable next group and populate combobox
            if (VM.Project != null)
            {              
                LoadMethods(GlobalVars.UserID, VM.Project.ProjectID, VM.MethodFilter);
            }

            VM.SetExperimentStatus();
            
        }


        private void LoadMethods(int userID, int projectID, int methodFilter)
        {
            bool success;

            switch(methodFilter)
            {
                case 0:  // My Methods only (Current User's Methods only)
                    success = wgDB.GetAllMethodsForUser(userID);
                    if (success)
                    {
                        if (VM.MethodList != null) VM.MethodList.Clear();
                        else VM.MethodList = new ObservableCollection<MethodContainer>();

                        foreach (MethodContainer method in wgDB.m_methodList)
                        {
                            VM.MethodList.Add(method);
                        }
                    }
                    break;
                case 1:  // Public Methods only
                    success = wgDB.GetAllPublicMethods();
                    if (success)
                    {
                        if (VM.MethodList != null) VM.MethodList.Clear();
                        else VM.MethodList = new ObservableCollection<MethodContainer>();

                        foreach (MethodContainer method in wgDB.m_methodList)
                        {
                            if(method.OwnerID != userID) VM.MethodList.Add(method);
                        }
                    }
                    break;
                case 2:  // Current Project's Public Methods only 
                    success = wgDB.GetAllMethodsForProject(projectID);
                    if(success)
                    {
                        if (VM.MethodList != null) VM.MethodList.Clear();
                        else VM.MethodList = new ObservableCollection<MethodContainer>();

                        foreach (MethodContainer method in wgDB.m_methodList)
                        {
                            VM.MethodList.Add(method);
                        }
                    }
                    break;
            }

        }


        private void Method_RadioButton_Checked(object sender, RoutedEventArgs e)
        {      
            if(VM.Project != null)
                LoadMethods(GlobalVars.UserID, VM.Project.ProjectID, VM.MethodFilter);
        }




        private void MethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {          
            if (VM.CompoundPlateList != null) VM.CompoundPlateList.Clear();
                        
            if (VM.IndicatorList != null) VM.IndicatorList.Clear();

 
            // get selection
            VM.Method = (MethodContainer)MethodComboBox.SelectedItem;

            if (VM.Method != null)
            {
            
                // get all the compound plates for the Method
                bool success = wgDB.GetAllCompoundPlatesForMethod(VM.Method.MethodID);

                if (success)
                {
                    if(VM.CompoundPlateList != null) VM.CompoundPlateList.Clear();
                    else VM.CompoundPlateList = new ObservableCollection<ExperimentCompoundPlateContainer>();

                    foreach (CompoundPlateContainer cpdPlate in wgDB.m_compoundPlateList)
                    {
                        ExperimentCompoundPlateContainer expCpdPlate = new ExperimentCompoundPlateContainer();
                        expCpdPlate.Barcode = "";
                        expCpdPlate.Description = cpdPlate.Description;
                        expCpdPlate.ExperimentCompoundPlateID = 0;
                        expCpdPlate.ExperimentID = 0;

                        VM.CompoundPlateList.Add(expCpdPlate);
                    }

                    // get all the indicators for the Method
                    success = wgDB.GetAllIndicatorsForMethod(VM.Method.MethodID);

                    if (success)
                    {
                        if (VM.IndicatorList != null) VM.IndicatorList.Clear();
                        else VM.IndicatorList = new ObservableCollection<ExperimentIndicatorContainer>();

                        foreach (IndicatorContainer indicator in wgDB.m_indicatorList)
                        {
                            ExperimentIndicatorContainer expIndicator = new ExperimentIndicatorContainer();
                            expIndicator.Description = indicator.Description;
                            expIndicator.EmissionFilterPos = indicator.EmissionsFilterPosition;
                            expIndicator.ExcitationFilterPos = indicator.ExcitationFilterPosition;
                            expIndicator.ExperimentID = 0;
                            expIndicator.ExperimentIndicatorID = 0;
                            expIndicator.Exposure = 100; // just something default
                            expIndicator.Gain = 1;  // just something default
                            expIndicator.MaskID = 0;
                            expIndicator.Verified = false;                     
                            expIndicator.FlatFieldRefImageID = 0;
                            expIndicator.DarkFieldRefImageID = 0;
                            

                            FilterContainer filter;
                            success = wgDB.GetExcitationFilterAtPosition(indicator.ExcitationFilterPosition, out filter);
                            if (success)
                            {
                                if (filter != null)
                                {
                                    expIndicator.ExcitationFilterDesc = filter.Description;
                                }
                            }

                            success = wgDB.GetEmissionFilterAtPosition(indicator.EmissionsFilterPosition, out filter);
                            if (success)
                            {
                                if (filter != null)
                                {
                                    expIndicator.EmissionFilterDesc = filter.Description;
                                }
                            }

                            VM.IndicatorList.Add(expIndicator);
                        }
                    }
                }
            }

            VM.SetExperimentStatus();

        }




 

        private void MaskComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // get selection
            VM.Mask = (MaskContainer)MaskComboBox.SelectedItem;

            if (VM.Mask != null)
            {
                // populate ROI pixel coordinates    
                VM.RoiX = 1;
                VM.RoiY = 1;

                if(m_imager != null)
                {                                    
                    VM.RoiW = m_imager.m_camera.XPixels;
                    VM.RoiH = m_imager.m_camera.YPixels;
                }
                else
                {
                    VM.RoiW = 1024;
                    VM.RoiH = 1024;
                }

                // populate ROI mask coordinates
                VM.RoiMaskStartRow = 0;
                VM.RoiMaskEndRow = VM.Mask.Rows - 1;
                VM.RoiMaskStartCol = 0;
                VM.RoiMaskEndCol = VM.Mask.Cols - 1;        
            }

            VM.SetExperimentStatus();


            if (VM.Mask != null)
                WellSelection.Init(VM.Mask.Rows, VM.Mask.Cols);
           

        }


        public void PopulateMaskList()
        {
            bool success = wgDB.GetAllMasks();
            if (success)
            {
                if (VM.MaskList == null) VM.MaskList = new ObservableCollection<MaskContainer>();
                else VM.MaskList.Clear();

                foreach (MaskContainer mask in wgDB.m_maskList)
                {
                    VM.MaskList.Add(mask);
                }

            }
        }



        public void PopulatePlateTypeList()
        {
            bool success = wgDB.GetAllPlateTypes();
            if(success)
            {
                if (VM.PlateTypeList == null) VM.PlateTypeList = new ObservableCollection<PlateTypeContainer>();
                else VM.PlateTypeList.Clear();

                foreach (PlateTypeContainer ptc in wgDB.m_plateTypeList)
                {
                    VM.PlateTypeList.Add(ptc);

                    if (ptc.IsDefault)
                    {
                        VM.PlateType = ptc;  // sets the selected item in the platetype combobox

                        // populate MaskList with masks for given platetype
                        success = wgDB.GetAllMasksForPlateType(ptc.PlateTypeID);

                        if(success)
                        {
                            if (VM.MaskList == null) VM.MaskList = new ObservableCollection<MaskContainer>();
                            else VM.MaskList.Clear();

                            foreach (MaskContainer mc in wgDB.m_maskList)
                            {
                                VM.MaskList.Add(mc);

                                if (mc.IsDefault) VM.Mask = mc;
                            }
                        }
                    }
                }
            }
        }

            

        public void RefreshProjectList()
        {
            ObservableCollection<ProjectContainer> projList;

            bool success = wgDB.GetAllProjectsForUser(GlobalVars.UserID, out projList);

            if (success)
            {
                VM.ProjectList = projList;               
            }
        }







        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
 


        private void StartExperimentPB_Click(object sender, RoutedEventArgs e)
        {
            bool success;

            List<ExperimentIndicatorContainer> expIndicatorList = new List<ExperimentIndicatorContainer>();
            
            // project, should already exist, so VM.Project should hold a valid project record
            ProjectContainer project = VM.Project;

            // user, get from database 
            UserContainer user;
            success = wgDB.GetUser(GlobalVars.UserID, out user);
            if(!success) 
            {
                ShowErrorDialog("Database Error: GetUser", wgDB.GetLastErrorMsg());
                return;
            } 
            else if(user==null)
            {
                ShowErrorDialog("User with database record id: " + GlobalVars.UserID.ToString() + " does not exist", "Data Error");
                return;
            }

            // method, this should already be populated
            MethodContainer method = VM.Method;            
            if(method==null)
            {
                ShowErrorDialog("Method not set", "Data Error");
                return;
            }



// *************************************************************************************
// *************************************************************************************
// *************************************************************************************

            

            TaskScheduler uiTask = TaskScheduler.FromCurrentSynchronizationContext();

            //if (m_vworks == null)
            //{
            //    m_vworks = new VWorks();
            //}

            

            RunExperiment runDlg = new RunExperiment(m_imager, uiTask);


            if (runDlg.m_ReadyToRun)
            {
                runDlg.Configure(VM.Project, VM.Method, VM.PlateType, VM.Mask,
                                 VM.IndicatorList, VM.CompoundPlateList,
                                 VM.ControlSubtractionWellList,
                                 VM.NumFoFrames,
                                 VM.DynamicRatioNumeratorIndicator,
                                 VM.DynamicRatioDenominatorIndicator,
                                 VM.RoiX, VM.RoiY, VM.RoiW, VM.RoiH,
                                 VM.RoiMaskStartRow, VM.RoiMaskEndRow, VM.RoiMaskStartCol, VM.RoiMaskEndCol);



                runDlg.ShowDialog();

                if (!runDlg.m_ReadyToRun) MessageBox.Show("Run Error", "VWorks failed to load!", MessageBoxButton.OK, MessageBoxImage.Error);


                // reset experiment configurator
                ResetExperimentConfigurator();


                // make sure that VWorks is killed
                Kill_VWorks_Process();
            }

        } // END StartExperimentPB_Click()




        private void Kill_VWorks_Process()
        {
            // make sure the VWorks Process is stopped

            try
            {
                foreach (Process proc in Process.GetProcesses()) 
                {
                    if (proc.ProcessName.Contains("VWorks"))
                    {
                        proc.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



        private void ResetExperimentConfigurator()
        {
            VM.CompoundPlateList.Clear();  // clear compound plate list

            VM.IndicatorList.Clear(); // clear indicator list

            MethodComboBox.SelectedIndex = -1;  // clear method combobox selection

            VM.ControlSubtractionWellList.Clear();  // clear control subtraction well list
        }


        private void ShowErrorDialog(string title, string errMsg)
        {
            // this is just a convenience function
            MessageBox.Show(errMsg,title,MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PlateTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cbox = (ComboBox)sender;

            if (cbox.SelectedItem == null)
            {                
                return;
            }

            if(cbox.SelectedItem.GetType() == typeof(PlateTypeContainer))
            {
                PlateTypeContainer ptc = (PlateTypeContainer)cbox.SelectedItem;

                if (wgDB == null) wgDB = new WaveguideDB();

                bool success = wgDB.GetAllMasksForPlateType(ptc.PlateTypeID);

                if(success)
                {
                    WellSelection.Init(ptc.Rows, ptc.Cols);

                    VM.MaskList.Clear();
                    VM.Mask = null;

                    foreach (MaskContainer mc in wgDB.m_maskList)
                    {
                        VM.MaskList.Add(mc);

                        if (mc.IsDefault) VM.Mask = mc;
                    }
                }
            }
        }





        private void DynamicRatioNumeratorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VM.SetExperimentStatus();
        }


        private void DynamicRatioDenominatorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VM.SetExperimentStatus();
        }


     

    }





    public class ExperimentConfiguratorViewModel : INotifyPropertyChanged
    {
        public enum STEP_STATUS { WAITING_FOR_PREDECESSOR, NEEDS_INPUT, READY };

        private bool _plateEnabled;
        private bool _compoundPlateEnabled;
        private bool _methodEnabled;
        private bool _imagerEnabled;
        private bool _runtimeAnalysisEnabled;
        private bool _runEnabled;
        private bool _plateBarcodeValid;
        private bool _dynamicRatioGroupEnabled;

        private ProjectContainer _project;        
        private MethodContainer _method;
        private MaskContainer _mask;
        private PlateTypeContainer _plateType;
        

        private STEP_STATUS _projectStatus;
        private STEP_STATUS _methodStatus;
        private STEP_STATUS _plateConfigStatus;
        private STEP_STATUS _runtimeAnalysisStatus;
        private STEP_STATUS _staticRatioStatus;
        private STEP_STATUS _controlSubtractionStatus;
        private STEP_STATUS _dynamicRatioStatus;


        public bool ProjectImage { get { return _runEnabled; } set { _runEnabled = value; NotifyPropertyChanged("RunEnabled"); } }

        private int _methodFilter;

        private ObservableCollection<ProjectContainer> _projectList;
        private ObservableCollection<MethodContainer> _methodList;
        private ObservableCollection<MaskContainer> _maskList;
        private ObservableCollection<ExperimentIndicatorContainer> _indicatorList;
        private ObservableCollection<ExperimentCompoundPlateContainer> _compoundPlateList;
        private ObservableCollection<PlateTypeContainer> _plateTypeList;

        private int _cycleTime;
        private int _temperature;
        private int _vertBinning;
        private int _horzBinning;
        private int _roiX;  // pixel coordinates
        private int _roiY;
        private int _roiW;
        private int _roiH;
        private int _roiMaskStartRow;
        private int _roiMaskEndRow;
        private int _roiMaskStartCol;
        private int _roiMaskEndCol;

        private int _numFoFrames;
        private ObservableCollection<Tuple<int, int>> _controlSubtractionWellList;
        private ExperimentIndicatorContainer _dynamicRatioNumeratorIndicator;
        private ExperimentIndicatorContainer _dynamicRatioDenominatorIndicator;


        public string RedArrowFileUri;
        public string GreenCheckFileUri;
        public string BlankFileUri;

        public bool PlateEnabled { get { return _plateEnabled; } set { _plateEnabled = value; NotifyPropertyChanged("PlateEnabled"); } }
        public bool CompoundPlateEnabled { get { return _compoundPlateEnabled; } set { _compoundPlateEnabled = value; NotifyPropertyChanged("CompoundPlateEnabled"); } }
        public bool MethodEnabled { get { return _methodEnabled; } set { _methodEnabled = value; NotifyPropertyChanged("MethodEnabled"); } }
        public bool ImagerEnabled { get { return _imagerEnabled; } set { _imagerEnabled = value; NotifyPropertyChanged("ImagerEnabled"); } }
        public bool RuntimeAnalysisEnabled { get { return _runtimeAnalysisEnabled; } set { _runtimeAnalysisEnabled = value; NotifyPropertyChanged("RuntimeAnalysisEnabled"); } }
        public bool RunEnabled { get { return _runEnabled; } set { _runEnabled = value; NotifyPropertyChanged("RunEnabled"); } }
        public bool PlateBarcodeValid { get { return _plateBarcodeValid; } set { _plateBarcodeValid = value; NotifyPropertyChanged("PlateBarcodeValid"); } }
        public bool DynamicRatioGroupEnabled { get { return _dynamicRatioGroupEnabled; } set { _dynamicRatioGroupEnabled = value; NotifyPropertyChanged("DynamicRatioGroupEnabled"); } }


        public ProjectContainer Project { get { return _project; } set { _project = value; NotifyPropertyChanged("Project"); } }        
        public MethodContainer Method { get { return _method; } set { _method = value; NotifyPropertyChanged("Method"); } }
        public MaskContainer Mask { get { return _mask; } set { _mask = value; NotifyPropertyChanged("Mask"); } }
        public PlateTypeContainer PlateType { get { return _plateType; } set { _plateType = value; NotifyPropertyChanged("PlateType"); } }
        

        public STEP_STATUS ProjectStatus { get { return _projectStatus; } set { _projectStatus = value; NotifyPropertyChanged("ProjectStatus"); } }
        public STEP_STATUS MethodStatus { get { return _methodStatus; } set { _methodStatus = value; NotifyPropertyChanged("MethodStatus"); } }
        public STEP_STATUS PlateConfigStatus { get { return _plateConfigStatus; } set { _plateConfigStatus = value; NotifyPropertyChanged("PlateConfigStatus"); } }
        public STEP_STATUS RuntimeAnalysisStatus { get { return _runtimeAnalysisStatus; } set { _runtimeAnalysisStatus = value; NotifyPropertyChanged("RuntimeAnalysisStatus"); } }
        public STEP_STATUS StaticRatioStatus { get { return _staticRatioStatus; } set { _staticRatioStatus = value; NotifyPropertyChanged("StaticRatioStatus"); } }
        public STEP_STATUS ControlSubtractionStatus { get { return _controlSubtractionStatus; } set { _controlSubtractionStatus = value; NotifyPropertyChanged("ControlSubtractionStatus"); } }
        public STEP_STATUS DynamicRatioStatus { get { return _dynamicRatioStatus; } set { _dynamicRatioStatus = value; NotifyPropertyChanged("DynamicRatioStatus"); } }


        public int MethodFilter { get { return _methodFilter; } set { _methodFilter = value; NotifyPropertyChanged("MethodFilter"); } }

        public ObservableCollection<ProjectContainer> ProjectList { get { return _projectList; } set { _projectList = value; NotifyPropertyChanged("ProjectList"); } }
        public ObservableCollection<ExperimentCompoundPlateContainer> CompoundPlateList { get { return _compoundPlateList; } set { _compoundPlateList = value; NotifyPropertyChanged("CompoundPlateList"); } }
        public ObservableCollection<MethodContainer> MethodList { get { return _methodList; } set { _methodList = value; NotifyPropertyChanged("MethodList"); } }
        public ObservableCollection<MaskContainer> MaskList { get { return _maskList; } set { _maskList = value; NotifyPropertyChanged("MaskList"); } }
        public ObservableCollection<ExperimentIndicatorContainer> IndicatorList { get { return _indicatorList; } set { _indicatorList = value; NotifyPropertyChanged("IndicatorList"); } }
        public ObservableCollection<PlateTypeContainer> PlateTypeList { get { return _plateTypeList; } set { _plateTypeList = value; NotifyPropertyChanged("PlateTypeList"); } }


        public int CycleTime { get { return _cycleTime; } set { _cycleTime = value; NotifyPropertyChanged("CycleTime"); } }
        public int Temperature { get { return _temperature; } set { _temperature = value; NotifyPropertyChanged("Temperature"); } }
        public int VertBinning { get { return _vertBinning; } set { _vertBinning = value; NotifyPropertyChanged("VertBinning"); } }
        public int HorzBinning { get { return _horzBinning; } set { _horzBinning = value; NotifyPropertyChanged("HorzBinning"); } }
        public int RoiX { get { return _roiX; } set { _roiX = value; NotifyPropertyChanged("RoiX"); } }
        public int RoiY { get { return _roiY; } set { _roiY = value; NotifyPropertyChanged("RoiY"); } }
        public int RoiW { get { return _roiW; } set { _roiW = value; NotifyPropertyChanged("RoiW"); } }
        public int RoiH { get { return _roiH; } set { _roiH = value; NotifyPropertyChanged("RoiH"); } }
        public int RoiMaskStartRow { get { return _roiMaskStartRow; } set { _roiMaskStartRow = value; NotifyPropertyChanged("RoiMaskStartRow"); } }
        public int RoiMaskEndRow { get { return _roiMaskEndRow; } set { _roiMaskEndRow = value; NotifyPropertyChanged("RoiMaskEndRow"); } }
        public int RoiMaskStartCol { get { return _roiMaskStartCol; } set { _roiMaskStartCol = value; NotifyPropertyChanged("RoiMaskStartCol"); } }
        public int RoiMaskEndCol { get { return _roiMaskEndCol; } set { _roiMaskEndCol = value; NotifyPropertyChanged("RoiMaskEndCol"); } }

        public int NumFoFrames { get { return _numFoFrames; } set { _numFoFrames = value; NotifyPropertyChanged("NumFoFrames"); } }
        public ObservableCollection<Tuple<int, int>> ControlSubtractionWellList { get { return _controlSubtractionWellList; } set { _controlSubtractionWellList = value; NotifyPropertyChanged("ControlSubtractionWellList"); } }
        public ExperimentIndicatorContainer DynamicRatioNumeratorIndicator { get { return _dynamicRatioNumeratorIndicator; } set { _dynamicRatioNumeratorIndicator = value; NotifyPropertyChanged("DynamicRatioNumeratorIndicator"); } }
        public ExperimentIndicatorContainer DynamicRatioDenominatorIndicator { get { return _dynamicRatioDenominatorIndicator; } set { _dynamicRatioDenominatorIndicator = value; NotifyPropertyChanged("DynamicRatioDenominatorIndicator"); } }



        public ExperimentConfiguratorViewModel()
        {
            PlateEnabled = false;
            CompoundPlateEnabled = false;
            MethodEnabled = false;
            ImagerEnabled = false;
            RuntimeAnalysisEnabled = false;
            RunEnabled = false;

            CycleTime = GlobalVars.CameraDefaultCycleTime;
            Temperature = GlobalVars.CameraTargetTemperature;
            VertBinning = 1;
            HorzBinning = 1;
            RoiX = 0;
            RoiY = 0;
            RoiW = 0;
            RoiH = 0;

            RoiMaskStartRow = 0;
            RoiMaskEndRow = 0;
            RoiMaskStartCol = 0;
            RoiMaskEndCol = 0;

            NumFoFrames = 5;

            ControlSubtractionWellList = new ObservableCollection<Tuple<int, int>>();

            RedArrowFileUri = "pack://application:,,,/Waveguide;component/Images/red_arrow_2.png";
            GreenCheckFileUri = "pack://application:,,,/Waveguide;component/Images/green_check_1.png";
            BlankFileUri = "pack://application:,,,/Waveguide;component/Images/blank.png";

            // Status: 0 = not yet enabled, i.e. something before it needs input (blank)
            //         1 = needs input (red arrow) 
            //         2 = properly completed (green check)
            ProjectStatus = STEP_STATUS.NEEDS_INPUT;
            MethodStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;
            PlateConfigStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;
            RuntimeAnalysisStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;
            StaticRatioStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;
            ControlSubtractionStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;
            DynamicRatioStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;

            MethodFilter = 0;
        }


        //public ImagingParameters BuildImagingParameters()
        //{
        //    ImagingParameters iParams = new ImagingParameters();

        //    iParams.maxPixelValue = GlobalVars.MaxPixelValue;
        //    iParams.imageWidth = GlobalVars.PixelWidth / HorzBinning;
        //    iParams.imageHeight = GlobalVars.PixelHeight / VertBinning;
        //    iParams.Image_StartCol = RoiX;
        //    iParams.Image_EndCol = RoiX + RoiW - 1;
        //    iParams.Image_StartRow = RoiY;
        //    iParams.Image_EndRow = RoiY + RoiH - 1;
        //    iParams.BravoMethodFilename = Method.BravoMethodFile;
        //    iParams.CameraTemperature = GlobalVars.CameraTargetTemperature;
        //    iParams.HorzBinning = HorzBinning;
        //    iParams.VertBinning = VertBinning;
        //    iParams.EmissionFilterChangeSpeed = GlobalVars.FilterChangeSpeed;
        //    iParams.ExcitationFilterChangeSpeed = GlobalVars.FilterChangeSpeed;
        //    iParams.LightIntensity = 100;
        //    iParams.NumImages = 1000000; // artificial limit on number of images
        //    iParams.NumIndicators = IndicatorList.Count;
        //    iParams.SyncExcitationFilterWithImaging = true;

        //    iParams.CycleTime = new int[IndicatorList.Count];
        //    iParams.EmissionFilter = new byte[IndicatorList.Count];
        //    iParams.ExcitationFilter = new byte[IndicatorList.Count];
        //    iParams.Exposure = new float[IndicatorList.Count];
        //    iParams.Gain = new int[IndicatorList.Count];
        //    iParams.ExperimentIndicatorID = new int[IndicatorList.Count];
        //    iParams.IndicatorName = new string[IndicatorList.Count];
        //    iParams.LampShutterIsOpen = new bool[IndicatorList.Count];

        //    iParams.PixelMask = new PixelMaskContainer[IndicatorList.Count];

        //    int i = 0;
        //    foreach (ExperimentIndicatorContainer ind in IndicatorList)
        //    {
        //        iParams.CycleTime[i] = CycleTime;
        //        iParams.EmissionFilter[i] = (byte)ind.EmissionFilterPos;
        //        iParams.ExcitationFilter[i] = (byte)ind.ExcitationFilterPos;
        //        iParams.Exposure[i] = (float)ind.Exposure / 1000;
        //        iParams.Gain[i] = ind.Gain;
        //        iParams.ExperimentIndicatorID[i] = 0; // created by the RunExperiment object when the experiment is run                
        //        iParams.IndicatorName[i] = ind.Description;
        //        iParams.LampShutterIsOpen[i] = true;
        //        iParams.ExperimentIndicatorID[i] = ind.ExperimentIndicatorID;
        //        iParams.FlatFieldSelect[i] = ind.FlatFieldCorrection;

        //        iParams.PixelMask[i] = new PixelMaskContainer(iParams.imageWidth, iParams.imageHeight);

        //        if (ind.UsePixelMask)
        //        {
        //            for (int j = 0; j < ind.PixelMask.MaskData.Length; j++)
        //            {
        //                iParams.PixelMask[i].MaskData[j] = ind.PixelMask.MaskData[j];
        //            }
        //        }

        //        i++;
        //    }

        //    return iParams;
        //}



        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }





        public void SetExperimentStatus()
        {
            ProjectStatus = ExperimentConfiguratorViewModel.STEP_STATUS.NEEDS_INPUT;
            MethodStatus = ExperimentConfiguratorViewModel.STEP_STATUS.WAITING_FOR_PREDECESSOR;
            PlateConfigStatus = ExperimentConfiguratorViewModel.STEP_STATUS.WAITING_FOR_PREDECESSOR;
            RuntimeAnalysisStatus = ExperimentConfiguratorViewModel.STEP_STATUS.WAITING_FOR_PREDECESSOR;
            StaticRatioStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;
            ControlSubtractionStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;
            DynamicRatioStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;

            MethodEnabled = false;
            PlateEnabled = false;
            CompoundPlateEnabled = false;
            ImagerEnabled = false;
            RuntimeAnalysisEnabled = false;

            RunEnabled = false;

            // set Method Status
            if (Project != null)
            {
                ProjectStatus = ExperimentConfiguratorViewModel.STEP_STATUS.READY;

                MethodStatus = ExperimentConfiguratorViewModel.STEP_STATUS.NEEDS_INPUT;
                MethodEnabled = true;

                // set Plate Status
                if (Method != null)
                {
                    MethodStatus = ExperimentConfiguratorViewModel.STEP_STATUS.READY;

                    PlateConfigStatus = ExperimentConfiguratorViewModel.STEP_STATUS.NEEDS_INPUT;
                    PlateEnabled = true;

                    if (PlateType != null && Mask != null)
                    {
                        PlateConfigStatus = ExperimentConfiguratorViewModel.STEP_STATUS.READY;



                        RuntimeAnalysisStatus = ExperimentConfiguratorViewModel.STEP_STATUS.NEEDS_INPUT;
                        RuntimeAnalysisEnabled = true;

                        // set Runtime Analysis Status
                        if (true) // set this as ready as soon as imager status is ready, since
                        // nothing is required
                        {
                            RuntimeAnalysisStatus = ExperimentConfiguratorViewModel.STEP_STATUS.READY;

                            if (IndicatorList.Count < 2) DynamicRatioGroupEnabled = false;
                            else DynamicRatioGroupEnabled = true;

                            RunEnabled = true;

                            // set status of StaticRatio
                            if (RuntimeAnalysisStatus == STEP_STATUS.READY)
                                StaticRatioStatus = STEP_STATUS.READY;
                            else
                                StaticRatioStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;


                            // set status of ControlSubtraction                                    
                            if (ControlSubtractionWellList.Count > 0)
                                ControlSubtractionStatus = STEP_STATUS.READY;
                            else
                                ControlSubtractionStatus = STEP_STATUS.NEEDS_INPUT;


                            // set status of DynamicRatio
                            if (IndicatorList.Count < 2)
                            {
                                DynamicRatioStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;
                            }
                            else if (DynamicRatioNumeratorIndicator == null ||
                                DynamicRatioDenominatorIndicator == null ||
                                DynamicRatioNumeratorIndicator == DynamicRatioDenominatorIndicator)
                            {
                                DynamicRatioStatus = STEP_STATUS.NEEDS_INPUT;
                            }
                            else
                                DynamicRatioStatus = STEP_STATUS.READY;
                        } // if true


                    } // if PlateType
                } // if Method

            } // if Project

        } // END SetExperimentStatus


    }



}
