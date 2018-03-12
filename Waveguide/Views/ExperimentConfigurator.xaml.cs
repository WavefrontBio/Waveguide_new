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
        ////////////////////////////////////////////////////////////////////////////
        // Start Experiment Event
        public delegate void StartExperimentEventHandler(object sender, EventArgs e);
        public event StartExperimentEventHandler StartExperimentEvent;

        protected virtual void OnStartExperiment(EventArgs e)        
        {
            if (StartExperimentEvent != null) StartExperimentEvent(this, e);
        }

        public void StartExperiment()
        {
            OnStartExperiment(null);
        }

        ////////////////////////////////////////////////////////////////////////////
        


        WaveguideDB wgDB;
        ExperimentConfiguratorViewModel VM;
        Imager m_imager;


        public ExperimentConfigurator()
        {
            VM = new ExperimentConfiguratorViewModel();

            InitializeComponent();

            wgDB = new WaveguideDB();
            

            m_imager = null;
      
            this.DataContext = VM;

            PlateTypeContainer ptc;
            bool success = wgDB.GetDefaultPlateType(out ptc);
            if (success)
            {
                WellSelection.Init(ptc.Rows, ptc.Cols);
                VM.ExpParams.plateType = ptc;
                PlateTypeComboBox.SelectedItem = ptc;
            }
            else
            {
                WellSelection.Init(16, 24);
                VM.ExpParams.plateType = null;
                VM.ExpParams.mask = null;
            }

            WellSelection.NewWellSetSelected += WellSelection_NewWellSetSelected;
                                 

        }

        void WellSelection_NewWellSetSelected(object sender, EventArgs e)
        {
            WellSelectionEventArgs ev = (WellSelectionEventArgs)e;
            VM.ExpParams.controlSubtractionWellList.Clear();

            foreach (Tuple<int, int> well in ev.WellList)
            {
                VM.ExpParams.controlSubtractionWellList.Add(well);
            }

            VM.SetExperimentStatus();
        }


        public void Init(Imager imager)
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
            VM.ExpParams.method = null;
            VM.MethodFilter = 0;
       
            if(VM.ExpParams.compoundPlateList != null) VM.ExpParams.compoundPlateList.Clear();

            if (VM.ExpParams.indicatorList != null) VM.ExpParams.indicatorList.Clear();

            
            // get selection
            VM.ExpParams.project = (ProjectContainer)ProjectComboBox.SelectedItem;
          
            if (VM.ExpParams.project != null)
            {
                LoadMethods(GlobalVars.UserID, VM.ExpParams.project.ProjectID, VM.MethodFilter);
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
            if(VM.ExpParams.project != null)
                LoadMethods(GlobalVars.UserID, VM.ExpParams.project.ProjectID, VM.MethodFilter);
        }




        private void MethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VM.ExpParams.compoundPlateList != null) VM.ExpParams.compoundPlateList.Clear();

            if (VM.ExpParams.indicatorList != null) VM.ExpParams.indicatorList.Clear();

 
            // get selection
            VM.ExpParams.method = (MethodContainer)MethodComboBox.SelectedItem;

            if (VM.ExpParams.method != null)
            {
            
                // get all the compound plates for the Method
                bool success = wgDB.GetAllCompoundPlatesForMethod(VM.ExpParams.method.MethodID);

                if (success)
                {
                    if (VM.ExpParams.compoundPlateList != null) VM.ExpParams.compoundPlateList.Clear();
                    else VM.ExpParams.compoundPlateList = new ObservableCollection<ExperimentCompoundPlateContainer>();

                    foreach (CompoundPlateContainer cpdPlate in wgDB.m_compoundPlateList)
                    {
                        ExperimentCompoundPlateContainer expCpdPlate = new ExperimentCompoundPlateContainer();
                        expCpdPlate.Barcode = "";
                        expCpdPlate.Description = cpdPlate.Description;
                        expCpdPlate.ExperimentCompoundPlateID = 0;
                        expCpdPlate.ExperimentID = 0;
                        expCpdPlate.PlateIDResetBehavior = cpdPlate.BarcodeReset;

                        VM.ExpParams.compoundPlateList.Add(expCpdPlate);
                    }

                    // get all the indicators for the Method
                    success = wgDB.GetAllIndicatorsForMethod(VM.ExpParams.method.MethodID);

                    if (success)
                    {
                        if (VM.ExpParams.indicatorList != null) VM.ExpParams.indicatorList.Clear();
                        else VM.ExpParams.indicatorList = new ObservableCollection<ExperimentIndicatorContainer>();

                        int i = 0;
                        foreach (IndicatorContainer indicator in wgDB.m_indicatorList)
                        {
                            ExperimentIndicatorContainer expIndicator = new ExperimentIndicatorContainer();
                            expIndicator.Description = indicator.Description;
                            expIndicator.EmissionFilterPos = indicator.EmissionsFilterPosition;
                            expIndicator.ExcitationFilterPos = indicator.ExcitationFilterPosition;                          
                            expIndicator.ExperimentID = 0; // defined when experiment launched
                            expIndicator.ExperimentIndicatorID = i; // defined when experiment launched
                            expIndicator.Exposure = 1; // default
                            expIndicator.Gain = 1;  // default
                            expIndicator.PreAmpGain = 1; // default
                            expIndicator.MaskID = 0; // not defined at this point.  Assigned when Mask is selected.
                            expIndicator.Verified = false;
                            expIndicator.FlatFieldRefImageID = 0;  // defined at Indicator Verify
                            expIndicator.DarkFieldRefImageID = 0;  // defined at Indicator Verify
                            expIndicator.FlatFieldCorrection = FLATFIELD_SELECT.NONE; // default
                            expIndicator.CycleTime = GlobalVars.CameraDefaultCycleTime;
                            expIndicator.SignalType = indicator.SignalType;


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

                            VM.ExpParams.indicatorList.Add(expIndicator);

                            i++; // increment dummy ExperimentIndicatorID
                        }
                    }
                }
            }

            VM.SetExperimentStatus();

            if(m_imager.m_ImagingDictionary == null)
            {
                m_imager.m_ImagingDictionary = new Dictionary<int, ImagingParamsStruct>();
            }
            else
            {
                m_imager.m_ImagingDictionary.Clear();
            }

            foreach(ExperimentIndicatorContainer eic in VM.ExpParams.indicatorList)
            {
                ImagingParamsStruct ips = new ImagingParamsStruct();
                ips.emissionFilterPos = (byte)eic.EmissionFilterPos;
                ips.excitationFilterPos = (byte)eic.ExcitationFilterPos;
                ips.experimentIndicatorID = eic.ExperimentIndicatorID;
                ips.flatfieldType = eic.FlatFieldCorrection;
                ips.indicatorName = eic.Description;
                ips.binning = m_imager.m_camera.m_acqParams.HBin;

                m_imager.m_ImagingDictionary.Add(eic.ExperimentIndicatorID, ips);
            }

        }




 

        private void MaskComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_imager != null)
            {
                // get selection
                VM.ExpParams.mask = (MaskContainer)MaskComboBox.SelectedItem;
                m_imager.m_mask = VM.ExpParams.mask;

                VM.SetExperimentStatus();


                if (VM.ExpParams.mask != null)
                {
                    WellSelection.Init(VM.ExpParams.mask.Rows, VM.ExpParams.mask.Cols);

                    if (VM.ExpParams.indicatorList != null)
                        foreach (ExperimentIndicatorContainer expInd in VM.ExpParams.indicatorList)
                        {
                            expInd.MaskID = VM.ExpParams.mask.MaskID;
                        }
                }
            }          

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
                        VM.ExpParams.plateType = ptc;  // sets the selected item in the platetype combobox
                        PlateTypeComboBox.SelectedItem = ptc;

                        // populate MaskList with masks for given platetype
                        success = wgDB.GetAllMasksForPlateType(ptc.PlateTypeID);

                        if(success)
                        {
                            if (VM.MaskList == null) VM.MaskList = new ObservableCollection<MaskContainer>();
                            else VM.MaskList.Clear();

                            foreach (MaskContainer mc in wgDB.m_maskList)
                            {
                                VM.MaskList.Add(mc);

                                if (mc.IsDefault)
                                {
                                    VM.ExpParams.mask = mc;
                                    MaskComboBox.SelectedItem = mc;
                                }
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
           
            // send signal to MainWindow that we're ready to run an experiment as configured (with configuration stored in ExperimentParams)
            StartExperiment();

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



        public void ResetExperimentConfigurator()
        {
            VM.ExpParams.compoundPlateList.Clear();  // clear compound plate list

            VM.ExpParams.indicatorList.Clear(); // clear indicator list

            MethodComboBox.SelectedIndex = -1;  // clear method combobox selection

            VM.ExpParams.controlSubtractionWellList.Clear();  // clear control subtraction well list

            WellSelection.Reset();  // clears the Well seledtion control (for control subtraction)
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
                    VM.ExpParams.mask = null;
                    MaskComboBox.SelectedItem = null;

                    foreach (MaskContainer mc in wgDB.m_maskList)
                    {
                        VM.MaskList.Add(mc);

                        if (mc.IsDefault)
                        {
                            VM.ExpParams.mask = mc;
                            MaskComboBox.SelectedItem = mc;
                        }
                    }
                }
            }
        }





        private void DynamicRatioNumeratorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cbox = (ComboBox)sender;

            if (cbox.SelectedItem == null)
            {                
                return;
            }

            if (cbox.SelectedItem.GetType() == typeof(ExperimentIndicatorContainer))
            {
                ExperimentIndicatorContainer eic = (ExperimentIndicatorContainer)cbox.SelectedItem;
                VM.ExpParams.dynamicRatioNumerator = eic;
            }
            
            VM.SetExperimentStatus();
        }


        private void DynamicRatioDenominatorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cbox = (ComboBox)sender;

            if (cbox.SelectedItem == null)
            {
                return;
            }

            if (cbox.SelectedItem.GetType() == typeof(ExperimentIndicatorContainer))
            {
                ExperimentIndicatorContainer eic = (ExperimentIndicatorContainer)cbox.SelectedItem;
                VM.ExpParams.dynamicRatioDenominator = eic;
            }
          
            VM.SetExperimentStatus();
        }


     

    }

   
    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////
  

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

     
        private STEP_STATUS _projectStatus;
        private STEP_STATUS _methodStatus;
        private STEP_STATUS _plateConfigStatus;
        private STEP_STATUS _runtimeAnalysisStatus;
        private STEP_STATUS _staticRatioStatus;
        private STEP_STATUS _controlSubtractionStatus;
        private STEP_STATUS _dynamicRatioStatus;

        // make ExperimentParams Singleton part of view model (used to store selections made by user)
        private ExperimentParams _expParams;
        public ExperimentParams ExpParams { get { return _expParams; } }

        public bool ProjectImage { get { return _runEnabled; } set { _runEnabled = value; NotifyPropertyChanged("RunEnabled"); } }

        private int _methodFilter;

        private ObservableCollection<ProjectContainer> _projectList;
        private ObservableCollection<MethodContainer> _methodList;
        private ObservableCollection<MaskContainer> _maskList;
        private ObservableCollection<PlateTypeContainer> _plateTypeList;

 
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


        public STEP_STATUS ProjectStatus { get { return _projectStatus; } set { _projectStatus = value; NotifyPropertyChanged("ProjectStatus"); } }
        public STEP_STATUS MethodStatus { get { return _methodStatus; } set { _methodStatus = value; NotifyPropertyChanged("MethodStatus"); } }
        public STEP_STATUS PlateConfigStatus { get { return _plateConfigStatus; } set { _plateConfigStatus = value; NotifyPropertyChanged("PlateConfigStatus"); } }
        public STEP_STATUS RuntimeAnalysisStatus { get { return _runtimeAnalysisStatus; } set { _runtimeAnalysisStatus = value; NotifyPropertyChanged("RuntimeAnalysisStatus"); } }
        public STEP_STATUS StaticRatioStatus { get { return _staticRatioStatus; } set { _staticRatioStatus = value; NotifyPropertyChanged("StaticRatioStatus"); } }
        public STEP_STATUS ControlSubtractionStatus { get { return _controlSubtractionStatus; } set { _controlSubtractionStatus = value; NotifyPropertyChanged("ControlSubtractionStatus"); } }
        public STEP_STATUS DynamicRatioStatus { get { return _dynamicRatioStatus; } set { _dynamicRatioStatus = value; NotifyPropertyChanged("DynamicRatioStatus"); } }


        public int MethodFilter { get { return _methodFilter; } set { _methodFilter = value; NotifyPropertyChanged("MethodFilter"); } }

        public ObservableCollection<ProjectContainer> ProjectList { get { return _projectList; } set { _projectList = value; NotifyPropertyChanged("ProjectList"); } }
        public ObservableCollection<MethodContainer> MethodList { get { return _methodList; } set { _methodList = value; NotifyPropertyChanged("MethodList"); } }
        public ObservableCollection<MaskContainer> MaskList { get { return _maskList; } set { _maskList = value; NotifyPropertyChanged("MaskList"); } }
        public ObservableCollection<PlateTypeContainer> PlateTypeList { get { return _plateTypeList; } set { _plateTypeList = value; NotifyPropertyChanged("PlateTypeList"); } }



        public ExperimentConfiguratorViewModel()
        {
            _expParams = ExperimentParams.GetExperimentParams;

            PlateEnabled = false;
            CompoundPlateEnabled = false;
            MethodEnabled = false;
            ImagerEnabled = false;
            RuntimeAnalysisEnabled = false;
            RunEnabled = false;


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
            if (ExpParams.project != null)
            {
                ProjectStatus = ExperimentConfiguratorViewModel.STEP_STATUS.READY;

                MethodStatus = ExperimentConfiguratorViewModel.STEP_STATUS.NEEDS_INPUT;
                MethodEnabled = true;

                // set Plate Status
                if (ExpParams.method != null)
                {
                    MethodStatus = ExperimentConfiguratorViewModel.STEP_STATUS.READY;

                    PlateConfigStatus = ExperimentConfiguratorViewModel.STEP_STATUS.NEEDS_INPUT;
                    PlateEnabled = true;

                    if (ExpParams.plateType != null && ExpParams.mask != null)
                    {
                        PlateConfigStatus = ExperimentConfiguratorViewModel.STEP_STATUS.READY;

                        RuntimeAnalysisStatus = ExperimentConfiguratorViewModel.STEP_STATUS.NEEDS_INPUT;
                        RuntimeAnalysisEnabled = true;

                        // set Runtime Analysis Status
                        if (true) // set this as ready as soon as imager status is ready, since
                        // nothing is required
                        {
                            RuntimeAnalysisStatus = ExperimentConfiguratorViewModel.STEP_STATUS.READY;

                            if (ExpParams.indicatorList.Count < 2) DynamicRatioGroupEnabled = false;
                            else DynamicRatioGroupEnabled = true;

                            RunEnabled = true;

                            // set status of StaticRatio
                            if (RuntimeAnalysisStatus == STEP_STATUS.READY)
                                StaticRatioStatus = STEP_STATUS.READY;
                            else
                                StaticRatioStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;


                            // set status of ControlSubtraction                                    
                            if (ExpParams.controlSubtractionWellList.Count > 0)
                                ControlSubtractionStatus = STEP_STATUS.READY;
                            else
                                ControlSubtractionStatus = STEP_STATUS.NEEDS_INPUT;


                            // set status of DynamicRatio
                            if (ExpParams.indicatorList.Count < 2)
                            {
                                DynamicRatioStatus = STEP_STATUS.WAITING_FOR_PREDECESSOR;
                            }
                            else if (ExpParams.dynamicRatioNumerator == null ||
                                ExpParams.dynamicRatioDenominator == null ||
                                ExpParams.dynamicRatioNumerator == ExpParams.dynamicRatioDenominator)
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
