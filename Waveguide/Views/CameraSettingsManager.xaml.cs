using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for CameraSettingsManager.xaml
    /// </summary>
    public partial class CameraSettingsManager : Window
    {
        CameraSettingsManager_ViewModel vm;
        Imager m_imager;
        WaveguideDB m_wgDB;

        public CameraSettingsManager(Imager _imager, WaveguideDB _wgDB)
        {
            m_imager = _imager;
            m_wgDB = _wgDB;

            

            vm = new CameraSettingsManager_ViewModel(m_imager, m_wgDB);

            this.DataContext = vm;

            InitializeComponent();
        }

        private void dgCameraSettings_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {

        }

        private void dgCameraSettings_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {

        }

        private void dgCameraSettings_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {

        }

        private void dgCameraSettings_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void dgCameraSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // force update of all the controls
            if (CameraSettingIDTextBlock != null)
            {
                CameraSettingIDTextBlock.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
                DescriptionTextBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
                VSSCombo.GetBindingExpression(ComboBox.SelectedValueProperty).UpdateTarget();
                HSSCombo.GetBindingExpression(ComboBox.SelectedValueProperty).UpdateTarget();
                VertClockAmpCombo.GetBindingExpression(ComboBox.SelectedValueProperty).UpdateTarget();
                UseEMGainCkBx.GetBindingExpression(CheckBox.IsCheckedProperty).UpdateTarget();
                UseFrameTransferCkBx.GetBindingExpression(CheckBox.IsCheckedProperty).UpdateTarget();
                IsDefaultCkBx.GetBindingExpression(CheckBox.IsCheckedProperty).UpdateTarget();
                StartingExposureUpDown.GetBindingExpression(Xceed.Wpf.Toolkit.IntegerUpDown.ValueProperty).UpdateTarget();
                ExposureLimitUpDown.GetBindingExpression(Xceed.Wpf.Toolkit.IntegerUpDown.ValueProperty).UpdateTarget();
                HighPixelThresholdPercentUpDown.GetBindingExpression(Xceed.Wpf.Toolkit.IntegerUpDown.ValueProperty).UpdateTarget();
                LowPixelThresholdPercentUpDown.GetBindingExpression(Xceed.Wpf.Toolkit.IntegerUpDown.ValueProperty).UpdateTarget();
                MinPercentPixelsAboveLowThresholdUpDown.GetBindingExpression(Xceed.Wpf.Toolkit.IntegerUpDown.ValueProperty).UpdateTarget();
                MaxPercentPixelsAboveHighThresholdUpDown.GetBindingExpression(Xceed.Wpf.Toolkit.IntegerUpDown.ValueProperty).UpdateTarget();
                IncreasingSignalCkBx.GetBindingExpression(CheckBox.IsCheckedProperty).UpdateTarget();
                StartingBinningCombo.GetBindingExpression(ComboBox.SelectedValueProperty).UpdateTarget();
                EMGainLimitUpDown.GetBindingExpression(Xceed.Wpf.Toolkit.IntegerUpDown.ValueProperty).UpdateTarget();
            }
                      
        }


        private void VSSCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void HSSCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void VertClockAmpCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void PreAmpGainCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void UseEMGainCkBx_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void UseEMGainCkBx_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void UseFrameTransferCkBx_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void UseFrameTransferCkBx_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void IsDefaultCkBx_Checked(object sender, RoutedEventArgs e)
        {
            // make sure only one CameraSetting is set as Default
            foreach(var cs in vm.CameraSettingsList)
            {
                if (cs.CameraSettingID != vm.CurrentCameraSettings.CameraSettingID) cs.IsDefault = false;
            }
        }

        private void IsDefaultCkBx_Unchecked(object sender, RoutedEventArgs e)
        {
           
        }

        private void IncreasingSignalCkBx_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void IncreasingSignalCkBx_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void SaveAsPB_Click(object sender, RoutedEventArgs e)
        {
            CameraSettingsContainer cs = vm.AddNew();
            if(cs != null)
                dgCameraSettings.SelectedValue = cs;
        }

        private void DeletePB_Click(object sender, RoutedEventArgs e)
        {
            CameraSettingsContainer cs = vm.DeleteCurrent();
            dgCameraSettings.SelectedValue = cs;
        }

        private void StartingBinningCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

       
              
    }


    public class CameraSettingsManager_ViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<CameraSettingsContainer> _cameraSettingsList;
        private CameraSettingsContainer _currentCameraSettings;

        private Imager _imager;
        private Camera _camera;
        private WaveguideDB _wgDB;

        private ObservableCollection<Camera.VSSpeed> vsspeeds;
        private ObservableCollection<Camera.VertClockVoltageAmplitude> vertClockVoltageAmplitudes;
        private ObservableCollection<Camera.HSSpeed> hsspeeds_EM;
        private ObservableCollection<Camera.HSSpeed> hsspeeds_Conv;
        private ObservableCollection<Camera.PreAmpGain> preAmpGains;
        private ObservableCollection<Camera.FunctionalMode> readModes;
        private ObservableCollection<Camera.FunctionalMode> acquisitionModes;
        private ObservableCollection<Camera.FunctionalMode> triggerModes;
        private ObservableCollection<Camera.FunctionalMode> eMGainModes;
        private ObservableCollection<Camera.FunctionalMode> adChannelOptions;
        private ObservableCollection<Camera.FunctionalMode> binningOptions;

        public ObservableCollection<Camera.HSSpeed> hsspeeds; // this is set to either hsspeeds_EM or hsspeeds_Conv, depending whether EM Amp is used


        public CameraSettingsManager_ViewModel(Imager imager, WaveguideDB wgDB)
        {            
            _imager = imager;
            _camera = _imager.m_camera;
            _wgDB = wgDB;
            _camera.GetCameraCapabilities();

            _wgDB.GetAllCameraSettings();
            CameraSettingsList = _wgDB.m_cameraSettingsList;
            if(CameraSettingsList.Count>0)
            {
                CurrentCameraSettings = CameraSettingsList.ElementAt(0);
            }
            else
            {
                CurrentCameraSettings = new CameraSettingsContainer();
             
                CurrentCameraSettings.CameraSettingID = 0;
                CurrentCameraSettings.VSSIndex = 0;                
                CurrentCameraSettings.HSSIndex = 0;
                CurrentCameraSettings.VertClockAmpIndex = 0;
                CurrentCameraSettings.UseEMAmp = true;
                CurrentCameraSettings.UseFrameTransfer = true;                
                CurrentCameraSettings.Description = "";
                CurrentCameraSettings.IsDefault = true;

                CurrentCameraSettings.StartingExposure = 2;
                CurrentCameraSettings.ExposureLimit = 1000;
                CurrentCameraSettings.HighPixelThresholdPercent = 80;
                CurrentCameraSettings.LowPixelThresholdPercent = 10; // 60 if !IncreasingSignal (i.e. a decreasing signal)
                CurrentCameraSettings.MinPercentPixelsAboveLowThreshold = 50;
                CurrentCameraSettings.MaxPercentPixelsAboveHighThreshold = 10;
                CurrentCameraSettings.IncreasingSignal = true;
                CurrentCameraSettings.StartingBinning = 1;
                CurrentCameraSettings.EMGainLimit = 300;
                
            }

            // set camera capabilities
            vsspeeds = _camera.VSSpeeds;
            vertClockVoltageAmplitudes = _camera.VertClockVoltageAmplitudes;
            hsspeeds_EM = _camera.HSSpeeds_EM;
            hsspeeds_Conv = _camera.HSSpeeds_Conv;
            if (_currentCameraSettings.UseEMAmp) hsspeeds = hsspeeds_EM;
            else hsspeeds = hsspeeds_Conv;
            preAmpGains = _camera.PreAmpGains;
            readModes = _camera.ReadModes;
            acquisitionModes = _camera.AcquisitionModes;
            triggerModes = _camera.TriggerModes;
            eMGainModes = _camera.EMGainModes;
            adChannelOptions = _camera.ADChannelOptions;
            binningOptions = _camera.BinningOptions;
        }

        public CameraSettingsContainer AddNew()
        {
            CameraSettingsContainer cs = new CameraSettingsContainer();

            cs.Description = "";
            cs.ExposureLimit = CurrentCameraSettings.ExposureLimit;
            cs.HighPixelThresholdPercent = CurrentCameraSettings.HighPixelThresholdPercent;
            cs.HSSIndex = CurrentCameraSettings.HSSIndex;
            cs.IncreasingSignal = CurrentCameraSettings.IncreasingSignal;
            cs.IsDefault = false;
            cs.LowPixelThresholdPercent = CurrentCameraSettings.LowPixelThresholdPercent;
            cs.MaxPercentPixelsAboveHighThreshold = CurrentCameraSettings.MaxPercentPixelsAboveHighThreshold;
            cs.MinPercentPixelsAboveLowThreshold = CurrentCameraSettings.MinPercentPixelsAboveLowThreshold;
            cs.StartingExposure = CurrentCameraSettings.StartingExposure;
            cs.UseEMAmp = CurrentCameraSettings.UseEMAmp;
            cs.UseFrameTransfer = CurrentCameraSettings.UseFrameTransfer;
            cs.VertClockAmpIndex = CurrentCameraSettings.VertClockAmpIndex;
            cs.VSSIndex = CurrentCameraSettings.VSSIndex;
            cs.EMGainLimit = CurrentCameraSettings.EMGainLimit;
            cs.StartingBinning = CurrentCameraSettings.StartingBinning;
           

            bool success = _wgDB.InsertCameraSettings(ref cs);

            if(success)
            {
                CameraSettingsList.Add(cs);
            }
            else
            {
                string errMsg = _wgDB.GetLastErrorMsg();
                MessageBox.Show("Failed to Add new Camera Settings record to database: " + errMsg, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                cs = null;
            }

            return cs;
        }

        public CameraSettingsContainer DeleteCurrent()
        {
            if(CameraSettingsList.Count<1)
            {
                MessageBox.Show("Cannot Delete the last Camera Settings Item", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to delete: " +
                    CurrentCameraSettings.Description + "?", "Delete Camera Setting Item", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if(result == MessageBoxResult.Yes)
                {
                    // save item to delete
                    bool needToResetDefault = false;
                    CameraSettingsContainer itemToDelete = CurrentCameraSettings;
                    if (itemToDelete.IsDefault) needToResetDefault = true;

                    // find item to be the new current item
                    foreach(var cs in CameraSettingsList)
                    {
                        if(cs.CameraSettingID != itemToDelete.CameraSettingID)
                        {
                            bool success = _wgDB.DeleteCameraSettings(itemToDelete.CameraSettingID);
                            if (success)
                            {
                                CurrentCameraSettings = cs;
                                if(needToResetDefault) CurrentCameraSettings.IsDefault = true;
                                CameraSettingsList.Remove(itemToDelete);
                            }

                            break;
                        }
                    }
                }
            }

            return CurrentCameraSettings;
        }


        private bool UpdateDatabase(CameraSettingsContainer cs)
        {
            bool success = _wgDB.UpdateCameraSettings(cs);

            if(!success)
            {
                string errMsg = _wgDB.GetLastErrorMsg();
                MessageBox.Show("Failed to Update Camera Settings: " + errMsg, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return success;
        }


        public  ObservableCollection<Camera.VSSpeed> VSSpeeds 
        {
            get { return vsspeeds; }
            set
            {
                vsspeeds = value;
                NotifyPropertyChanged("VSSpeeds");
            }
        }


        public ObservableCollection<Camera.HSSpeed> HSSpeeds
        {
            get { return hsspeeds; }
            set
            {
                hsspeeds = value;
                NotifyPropertyChanged("HSSpeeds");
            }
        }



        public ObservableCollection<Camera.VertClockVoltageAmplitude> VertClockVoltageAmplitudes
        {
            get { return vertClockVoltageAmplitudes; }
            set
            {
                vertClockVoltageAmplitudes = value;
                NotifyPropertyChanged("VertClockVoltageAmplitudes");
            }
        }



        public ObservableCollection<Camera.PreAmpGain> PreAmpGains
        {
            get { return preAmpGains; }
            set
            {
                preAmpGains = value;
                NotifyPropertyChanged("PreAmpGains");
            }
        }

        public ObservableCollection<Camera.FunctionalMode> ReadModes
        {
            get { return readModes; }
            set
            {
                readModes = value;
                NotifyPropertyChanged("ReadModes");
            }
        }

        public ObservableCollection<Camera.FunctionalMode> AcquisitionModes
        {
            get { return acquisitionModes; }
            set
            {
                acquisitionModes = value;
                NotifyPropertyChanged("AcquisitionModes");
            }
        }

        public ObservableCollection<Camera.FunctionalMode> TriggerModes
        {
            get { return triggerModes; }
            set
            {
                triggerModes = value;
                NotifyPropertyChanged("TriggerModes");
            }
        }

        public ObservableCollection<Camera.FunctionalMode> EMGainModes
        {
            get { return eMGainModes; }
            set
            {
                eMGainModes = value;
                NotifyPropertyChanged("EMGainModes");
            }
        }

        public ObservableCollection<Camera.FunctionalMode> ADChannelOptions
        {
            get { return adChannelOptions; }
            set
            {
                adChannelOptions = value;
                NotifyPropertyChanged("ADChannelOptions");
            }
        }


        public ObservableCollection<CameraSettingsContainer> CameraSettingsList
        {
            get { return _cameraSettingsList; }
            set
            {
                _cameraSettingsList = value;
                NotifyPropertyChanged("CameraSettingsList");
            }
        }


        public ObservableCollection<Camera.FunctionalMode> BinningOptions
        {
            get { return binningOptions; }
            set
            {
                binningOptions = value;
                NotifyPropertyChanged("BinningOptions");
            }
        }



        public CameraSettingsContainer CurrentCameraSettings
        {
            get { return _currentCameraSettings; }
            set
            {
                _currentCameraSettings = value;
                NotifyPropertyChanged("CurrentCameraSettings");
            }
        }

        public int CameraSettingID
        {
            get { return _currentCameraSettings.CameraSettingID; }
            set
            {
                _currentCameraSettings.CameraSettingID = value;
                NotifyPropertyChanged("CameraSettingID");
            }
        }
        
        public int VSSIndex
        {
            get { return _currentCameraSettings.VSSIndex; }
            set
            {
                _currentCameraSettings.VSSIndex = value;
                NotifyPropertyChanged("VSSIndex");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int HSSIndex
        {
            get { return _currentCameraSettings.HSSIndex; }
            set
            {
                _currentCameraSettings.HSSIndex = value;
                NotifyPropertyChanged("HSSIndex");
                UpdateDatabase(CurrentCameraSettings);
            }
        }
        
        public int VertClockAmpIndex
        {
            get { return _currentCameraSettings.VertClockAmpIndex; }
            set
            {
                _currentCameraSettings.VertClockAmpIndex = value;
                NotifyPropertyChanged("VertClockAmpIndex");
                UpdateDatabase(CurrentCameraSettings);
            }
        }
        
        public bool UseEMAmp
        {
            get { return _currentCameraSettings.UseEMAmp; }
            set
            {
                _currentCameraSettings.UseEMAmp = value;
                NotifyPropertyChanged("UseEMAmp");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public bool UseFrameTransfer
        {
            get { return _currentCameraSettings.UseFrameTransfer; }
            set
            {
                _currentCameraSettings.UseFrameTransfer = value;
                NotifyPropertyChanged("UseFrameTransfer");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public string Description
        {
            get { return _currentCameraSettings.Description; }
            set
            {
                _currentCameraSettings.Description = value;
                NotifyPropertyChanged("Description");
                UpdateDatabase(CurrentCameraSettings);
            }
        }
     
        public bool IsDefault
        {
            get { return _currentCameraSettings.IsDefault; }
            set
            {
                _currentCameraSettings.IsDefault = value;
                NotifyPropertyChanged("IsDefault");
                UpdateDatabase(CurrentCameraSettings);

                // make sure only one CameraSetting is set as Default
                foreach (var cs in CameraSettingsList)
                {
                    if (cs.CameraSettingID != CurrentCameraSettings.CameraSettingID)
                    {
                        cs.IsDefault = false;
                        UpdateDatabase(cs);
                    }
                }
            }
        }

        public int StartingExposure
        {
            get { return _currentCameraSettings.StartingExposure; }
            set
            {
                _currentCameraSettings.StartingExposure = value;
                NotifyPropertyChanged("StartingExposure");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int ExposureLimit
        {
            get { return _currentCameraSettings.ExposureLimit; }
            set
            {
                _currentCameraSettings.ExposureLimit = value;
                NotifyPropertyChanged("ExposureLimit");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int EMGainLimit
        {
            get { return _currentCameraSettings.EMGainLimit; }
            set
            {
                _currentCameraSettings.EMGainLimit = value;
                NotifyPropertyChanged("EMGainLimit");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int StartingBinning
        {
            get { return _currentCameraSettings.StartingBinning; }
            set
            {
                _currentCameraSettings.StartingBinning = value;
                NotifyPropertyChanged("StartingBinning");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int HighPixelThresholdPercent
        {
            get { return _currentCameraSettings.HighPixelThresholdPercent; }
            set
            {
                _currentCameraSettings.HighPixelThresholdPercent = value;
                NotifyPropertyChanged("HighPixelThresholdPercent");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int LowPixelThresholdPercent
        {
            get { return _currentCameraSettings.LowPixelThresholdPercent; }
            set
            {
                _currentCameraSettings.LowPixelThresholdPercent = value;
                NotifyPropertyChanged("LowPixelThresholdPercent");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int MinPercentPixelsAboveLowThreshold
        {
            get { return _currentCameraSettings.MinPercentPixelsAboveLowThreshold; }
            set
            {
                _currentCameraSettings.MinPercentPixelsAboveLowThreshold = value;
                NotifyPropertyChanged("MinPercentPixelsAboveLowThreshold");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int MaxPercentPixelsAboveHighThreshold
        {
            get { return _currentCameraSettings.MaxPercentPixelsAboveHighThreshold; }
            set
            {
                _currentCameraSettings.MaxPercentPixelsAboveHighThreshold = value;
                NotifyPropertyChanged("MaxPercentPixelsAboveHighThreshold");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public bool IncreasingSignal
        {
            get { return _currentCameraSettings.IncreasingSignal; }
            set
            {
                _currentCameraSettings.IncreasingSignal = value;
                NotifyPropertyChanged("IncreasingSignal");
                UpdateDatabase(CurrentCameraSettings);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
