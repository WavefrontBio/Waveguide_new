using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace Waveguide
{


	public class CameraSetupModel : INotifyPropertyChanged
	{        
        private Imager myImager;
        private Camera myCamera;
        private WaveguideDB myWgDB;

        private CameraParams cameraParams;
        private AcquisitionParams acqParams;

        private bool showConfigPanel;
        private bool enableConfig;
        private bool isManualMode;
        private double sliderLowPosition;
        private double sliderHighPosition;
        private int minCycleTime;
        private int maxCycleTime;
        private int emGain_LowLimit;
        private int emGain_HighLimit;
        private bool isOptimizing;
        private string wellSelectionPBLabel;
   
    
       
        // lists for combo boxes
        private ObservableCollection<CameraSettingsContainer> cameraSettingsList;
        private ObservableCollection<Camera.PreAmpGain> preAmpGains;
        private ObservableCollection<Camera.FunctionalMode> binningOptions;
        private ObservableCollection<FilterContainer> exFilterList;
        private ObservableCollection<FilterContainer> emFilterList;
        private ObservableCollection<Camera.VSSpeed> vsspeeds;
        private ObservableCollection<Camera.HSSpeed> hsspeeds_EM;
        private ObservableCollection<Camera.HSSpeed> hsspeeds_Conv;
        public ObservableCollection<Camera.HSSpeed> hsspeeds;
        private ObservableCollection<Camera.VertClockVoltageAmplitude> vertClockVoltageAmplitudes;

        // current camera values
        private int exposure;
        private int emGain;
        private int preAmpGainIndex;
        private int binning;
        private int cycleTime;
        private FilterContainer exFilter;
        private FilterContainer emFilter;
        private bool applyMask;
        private CameraSettingsContainer currentCameraSettings;

     


        //  Constructor
		public CameraSetupModel(Imager imager, WaveguideDB wgDB, bool AllowCameraConfiguration, bool _isManualMode)
		{
            if (imager == null) return;
            if (imager.m_camera == null) return;
            myImager = imager;
            myCamera = MyImager.m_camera;
            myWgDB = wgDB;

            myCamera.GetCameraCapabilities(); // make sure camera properties are updated

            myWgDB.GetAllCameraSettings();  // make sure camera settings list in wgDB is populated
            cameraSettingsList = myWgDB.m_cameraSettingsList;
            if (CameraSettingsList.Count > 0)
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
            
            cameraParams = MyCamera.m_cameraParams;
            acqParams = MyCamera.m_acqParams;
                        
            exposure = 1;
            isOptimizing = false;

            // set camera capabilities
            vsspeeds = MyCamera.VSSpeeds;
            vertClockVoltageAmplitudes = MyCamera.VertClockVoltageAmplitudes;
            hsspeeds_EM = MyCamera.HSSpeeds_EM;
            hsspeeds_Conv = MyCamera.HSSpeeds_Conv;
            if (cameraParams.UseEMAmp) hsspeeds = hsspeeds_EM;
            else hsspeeds = hsspeeds_Conv;
            emGain_LowLimit = MyCamera.EMGain_LowLimit;
            emGain_HighLimit = MyCamera.EMGain_HighLimit;
            preAmpGains = MyCamera.PreAmpGains;       
            binningOptions = MyCamera.BinningOptions;
              

            // get Camera Parameters
            cameraParams = MyCamera.m_cameraParams;
            

            // get Acquisition Parameters           
            acqParams = MyCamera.m_acqParams;
         

            showConfigPanel = false;  // start with configuration panel hidden
            enableConfig = AllowCameraConfiguration; // if false, hides the configuration tab so that configuration cannot be performed
            isManualMode = _isManualMode;
            applyMask = true;

                        
            myWgDB.GetAllExcitationFilters();
            exFilterList = new ObservableCollection<FilterContainer>();
            foreach(FilterContainer fc in myWgDB.m_filterList)
            {
                if(fc.FilterChanger == 1) exFilterList.Add(fc);
            }            
            exFilter = exFilterList[0];

            myWgDB.GetAllEmissionFilters();
            emFilterList = new ObservableCollection<FilterContainer>();
            foreach (FilterContainer fc in myWgDB.m_filterList)
            {
                if (fc.FilterChanger == 0) emFilterList.Add(fc);
            }   
            emFilter = emFilterList[0];

            sliderLowPosition = 0.0;
            sliderHighPosition = 100.0;

            cycleTime = 1;
            minCycleTime = 1;
            maxCycleTime = 10000;

            WellSelectionPBLabel = "";
		}



        public CameraSettingsContainer AddNew(string description)
        {
            CameraSettingsContainer cs = new CameraSettingsContainer();

            cs.Description = description;
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


            bool success = myWgDB.InsertCameraSettings(ref cs);

            if (success)
            {
                CameraSettingsList.Add(cs);
                CurrentCameraSettings = cs;
            }
            else
            {
                string errMsg = myWgDB.GetLastErrorMsg();
                MessageBox.Show("Failed to Add new Camera Settings record to database: " + errMsg, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                cs = null;
            }

            return cs;
        }

        public CameraSettingsContainer DeleteCurrent()
        {
            if (CameraSettingsList.Count < 1)
            {
                MessageBox.Show("Cannot Delete the last Camera Settings Item", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to delete: " +
                    CurrentCameraSettings.Description + "?", "Delete Camera Setting Item", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // save item to delete
                    bool needToResetDefault = false;
                    CameraSettingsContainer itemToDelete = CurrentCameraSettings;
                    if (itemToDelete.IsDefault) needToResetDefault = true;

                    // find item to be the new current item
                    foreach (var cs in CameraSettingsList)
                    {
                        if (cs.CameraSettingID != itemToDelete.CameraSettingID)
                        {
                            bool success = myWgDB.DeleteCameraSettings(itemToDelete.CameraSettingID);
                            if (success)
                            {
                                CurrentCameraSettings = cs;
                                if (needToResetDefault) CurrentCameraSettings.IsDefault = true;
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
            bool success = myWgDB.UpdateCameraSettings(cs);

            if (!success)
            {
                string errMsg = myWgDB.GetLastErrorMsg();
                MessageBox.Show("Failed to Update Camera Settings: " + errMsg, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return success;
        }




        ///////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////


        public Imager MyImager
        {
            get { return myImager; }
            set
            {
                myImager = value;
                NotifyPropertyChanged("MyImager");
            }
        }

        public Camera MyCamera
        {
            get { return myCamera; }
            set
            {
                myCamera = value;
                NotifyPropertyChanged("MyCamera");
            }
        }


        public WaveguideDB MyWgDB
        {
            get { return myWgDB; }
            set
            {
                myWgDB = value;
                NotifyPropertyChanged("MyWgDB");
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////

        public CameraParams CameraParams
        {
            get { return cameraParams; }
            set
            {
                cameraParams = value;
                NotifyPropertyChanged("CameraParams");
            }
        }

        public AcquisitionParams AcquisitionParams
        {
            get { return acqParams; }
            set
            {
                acqParams = value;
                NotifyPropertyChanged("AcquisitionParams");
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////


        public bool ShowConfigPanel
        {
            get { return showConfigPanel; }
            set
            {
                showConfigPanel = value;
                NotifyPropertyChanged("ShowConfigPanel");
            }
        }

        public bool EnableConfig
        {
            get { return enableConfig; }
            set
            {
                enableConfig = value;
                NotifyPropertyChanged("EnableConfig");
            }
        }


        public bool IsManualMode
        {
            get { return isManualMode; }
            set
            {
                isManualMode = value;
                NotifyPropertyChanged("IsManualMode");
            }
        }

        public double SliderLowPosition
        {
            get { return sliderLowPosition; }
            set
            {
                sliderLowPosition = value;
                NotifyPropertyChanged("SliderLowPosition");
            }
        }

        public double SliderHighPosition
        {
            get { return sliderHighPosition; }
            set
            {
                sliderHighPosition = value;
                NotifyPropertyChanged("SliderHighPosition");
            }
        }



        public int MinCycleTime
        {
            get { return minCycleTime; }
            set
            {
                minCycleTime = value;
                NotifyPropertyChanged("MinCycleTime");
            }
        }

        public int MaxCycleTime
        {
            get { return maxCycleTime; }
            set
            {
                maxCycleTime = value;
                NotifyPropertyChanged("MaxCycleTime");
            }
        }


        public int EMGain_LowLimit
        {
            get { return emGain_LowLimit; }
            set
            {
                emGain_LowLimit = value;
                NotifyPropertyChanged("EMGain_LowLimit");
            }
        }

        public int EMGain_HighLimit
        {
            get { return emGain_HighLimit; }
            set
            {
                emGain_HighLimit = value;
                NotifyPropertyChanged("EMGain_HighLimit");
            }
        }

        public bool IsOptimizing
        {
            get { return isOptimizing; }
            set
            {
                isOptimizing = value;
                NotifyPropertyChanged("IsOptimizing");
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////

        public ObservableCollection<CameraSettingsContainer> CameraSettingsList
        {
            get { return cameraSettingsList; }
            set
            {
                cameraSettingsList = value;
                NotifyPropertyChanged("CameraSettingsList");
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


        public ObservableCollection<Camera.FunctionalMode> BinningOptions
        {
            get { return binningOptions; }
            set
            {
                binningOptions = value;
                NotifyPropertyChanged("BinningOptions");
            }
        }


        public ObservableCollection<FilterContainer> ExFilterList
        {
            get { return exFilterList; }
            set
            {
                exFilterList = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("ExFilterList"));
            }
        }


        public ObservableCollection<FilterContainer> EmFilterList
        {
            get { return emFilterList; }
            set
            {
                emFilterList = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("EmFilterList"));
            }
        }


        public ObservableCollection<Camera.VSSpeed> VSSpeeds
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

     

        ///////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////


        public int Exposure
        {
            get { return exposure; }
            set
            {
                exposure = value;
                NotifyPropertyChanged("Exposure");
            }
        }

        public int EMGain
        {
            get { return cameraParams.EMGain; }
            set
            {
                cameraParams.EMGain = value;
                NotifyPropertyChanged("EMGain");
            }
        }

        public int PreAmpGainIndex
        {
            get { return cameraParams.PreAmpGainIndex; }
            set
            {
                cameraParams.PreAmpGainIndex = value;
                NotifyPropertyChanged("PreAmpGainIndex");
            }
        }

        public int Binning
        {
            get { return acqParams.HBin; }
            set
            {
                acqParams.HBin = value;
                acqParams.VBin = value;
                NotifyPropertyChanged("Binning");
            }
        }

        public int CycleTime
        {
            get { return cycleTime; }
            set
            {
                cycleTime = value;
                NotifyPropertyChanged("CycleTime");
            }
        }

        public FilterContainer ExFilter
        {
            get { return exFilter; }
            set
            {
                exFilter = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("ExFilter"));
            }
        }

        public FilterContainer EmFilter
        {
            get { return emFilter; }
            set
            {
                emFilter = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("EmFilter"));
            }
        }

        public bool ApplyMask
        {
            get { return applyMask; }
            set
            {
                applyMask = value;
                NotifyPropertyChanged("ApplyMask");
            }
        }

        public CameraSettingsContainer CurrentCameraSettings
        {
            get { return currentCameraSettings; }
            set
            {
                currentCameraSettings = value;
                NotifyPropertyChanged("CurrentCameraSettings");
            }
        }



        ///////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////

        public int CameraSettingID
        {
            get { return currentCameraSettings.CameraSettingID; }
            set
            {
                currentCameraSettings.CameraSettingID = value;
                NotifyPropertyChanged("CameraSettingID");
            }
        }

        public string Description
        {
            get { return currentCameraSettings.Description; }
            set
            {
                currentCameraSettings.Description = value;
                NotifyPropertyChanged("Description");
                UpdateDatabase(CurrentCameraSettings);
            }
        }


        public int VSSIndex
        {
            get { return currentCameraSettings.VSSIndex; }
            set
            {
                currentCameraSettings.VSSIndex = value;
                NotifyPropertyChanged("VSSIndex");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int HSSIndex
        {
            get { return currentCameraSettings.HSSIndex; }
            set
            {
                currentCameraSettings.HSSIndex = value;
                NotifyPropertyChanged("HSSIndex");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int VertClockAmpIndex
        {
            get { return currentCameraSettings.VertClockAmpIndex; }
            set
            {
                currentCameraSettings.VertClockAmpIndex = value;
                NotifyPropertyChanged("VertClockAmpIndex");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public bool UseEMAmp
        {
            get { return currentCameraSettings.UseEMAmp; }
            set
            {
                currentCameraSettings.UseEMAmp = value;
                NotifyPropertyChanged("UseEMAmp");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public bool UseFrameTransfer
        {
            get { return currentCameraSettings.UseFrameTransfer; }
            set
            {
                currentCameraSettings.UseFrameTransfer = value;
                NotifyPropertyChanged("UseFrameTransfer");
                UpdateDatabase(CurrentCameraSettings);
            }
        }



        public bool IsDefault
        {
            get { return currentCameraSettings.IsDefault; }
            set
            {
                currentCameraSettings.IsDefault = value;
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
            get { return currentCameraSettings.StartingExposure; }
            set
            {
                currentCameraSettings.StartingExposure = value;
                NotifyPropertyChanged("StartingExposure");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int ExposureLimit
        {
            get { return currentCameraSettings.ExposureLimit; }
            set
            {
                currentCameraSettings.ExposureLimit = value;
                NotifyPropertyChanged("ExposureLimit");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int EMGainLimit
        {
            get { return currentCameraSettings.EMGainLimit; }
            set
            {
                currentCameraSettings.EMGainLimit = value;
                NotifyPropertyChanged("EMGainLimit");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int StartingBinning
        {
            get { return currentCameraSettings.StartingBinning; }
            set
            {
                currentCameraSettings.StartingBinning = value;
                NotifyPropertyChanged("StartingBinning");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int HighPixelThresholdPercent
        {
            get { return currentCameraSettings.HighPixelThresholdPercent; }
            set
            {
                currentCameraSettings.HighPixelThresholdPercent = value;
                NotifyPropertyChanged("HighPixelThresholdPercent");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int LowPixelThresholdPercent
        {
            get { return currentCameraSettings.LowPixelThresholdPercent; }
            set
            {
                currentCameraSettings.LowPixelThresholdPercent = value;
                NotifyPropertyChanged("LowPixelThresholdPercent");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int MinPercentPixelsAboveLowThreshold
        {
            get { return currentCameraSettings.MinPercentPixelsAboveLowThreshold; }
            set
            {
                currentCameraSettings.MinPercentPixelsAboveLowThreshold = value;
                NotifyPropertyChanged("MinPercentPixelsAboveLowThreshold");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public int MaxPercentPixelsAboveHighThreshold
        {
            get { return currentCameraSettings.MaxPercentPixelsAboveHighThreshold; }
            set
            {
                currentCameraSettings.MaxPercentPixelsAboveHighThreshold = value;
                NotifyPropertyChanged("MaxPercentPixelsAboveHighThreshold");
                UpdateDatabase(CurrentCameraSettings);
            }
        }

        public bool IncreasingSignal
        {
            get { return currentCameraSettings.IncreasingSignal; }
            set
            {
                currentCameraSettings.IncreasingSignal = value;
                NotifyPropertyChanged("IncreasingSignal");
                UpdateDatabase(CurrentCameraSettings);

                DecreasingSignal = !value;
            }
        }


        public bool DecreasingSignal
        {
            get { return !currentCameraSettings.IncreasingSignal; }
            set
            {                
                NotifyPropertyChanged("DecreasingSignal");
            }
        }


        public string WellSelectionPBLabel
        {
            get { return wellSelectionPBLabel; }
            set
            {
                wellSelectionPBLabel = value;
                NotifyPropertyChanged("WellSelectionPBLabel");
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////


		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		#endregion
	}
}