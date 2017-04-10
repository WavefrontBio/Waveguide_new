using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows;
using System.Collections.ObjectModel;

namespace Waveguide
{


	public class CameraSetupModel : INotifyPropertyChanged
	{
        private int exposure;

        private bool showConfigPanel;
        private bool enableConfig;
        private bool autosizeROI;
        private bool applyMask;
        private bool isManualMode;
        private bool isIncreasingSignal;
        private bool isOptimizing;
        private double sliderLowPosition;
        private double sliderHighPosition;

        private CameraParams cameraParams;
        private AcquisitionParams acqParams;

        private int xPixels;
        private int yPixels;
        

        private Imager myImager;
        private Camera myCamera;

        // Camera Capabilities       
        private ObservableCollection<Camera.VSSpeed> vsspeeds;
        private ObservableCollection<Camera.VertClockVoltageAmplitude> vertClockVoltageAmplitudes;
        private ObservableCollection<Camera.HSSpeed> hsspeeds_EM;
        private ObservableCollection<Camera.HSSpeed> hsspeeds_Conv;
        private int emGain_LowLimit;
        private int emGain_HighLimit;
        private ObservableCollection<Camera.PreAmpGain> preAmpGains;
        private ObservableCollection<Camera.FunctionalMode> readModes;
        private ObservableCollection<Camera.FunctionalMode> acquisitionModes;
        private ObservableCollection<Camera.FunctionalMode> triggerModes;
        private ObservableCollection<Camera.FunctionalMode> eMGainModes;
        private ObservableCollection<Camera.FunctionalMode> adChannelOptions;
        private ObservableCollection<Camera.FunctionalMode> binningOptions;

        public ObservableCollection<Camera.HSSpeed> hsspeeds;

        private ObservableCollection<FilterContainer> exFilterList;
        private FilterContainer exFilter;
        private ObservableCollection<FilterContainer> emFilterList;
        private FilterContainer emFilter;

        //  Constructor
		public CameraSetupModel(Imager _imager, bool AllowCameraConfiguration, bool _isManualMode)
		{
            if (_imager == null) return;
            if (_imager.m_camera == null) return;
            MyImager = _imager;
            MyCamera = MyImager.m_camera;
            cameraParams = MyCamera.m_cameraParams;
            acqParams = MyCamera.m_acqParams;

            // default number of pixels in CCD; this should be set to real value in the Initialize() method below
            xPixels = MyCamera.XPixels;
            yPixels = MyCamera.YPixels;

            exposure = 20;
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
            readModes = MyCamera.ReadModes;
            acquisitionModes = MyCamera.AcquisitionModes;
            triggerModes = MyCamera.TriggerModes;
            eMGainModes = MyCamera.EMGainModes;
            adChannelOptions = MyCamera.ADChannelOptions;
            binningOptions = MyCamera.BinningOptions;
              

            // get Camera Parameters
            cameraParams = MyCamera.m_cameraParams;
            

            // get Acquisition Parameters           
            acqParams = MyCamera.m_acqParams;
         

            showConfigPanel = false;  // start with configuration panel hidden
            enableConfig = AllowCameraConfiguration; // if false, hides the configuration tab so that configuration cannot be performed
            isManualMode = _isManualMode;
            autosizeROI = AutosizeROI;
            applyMask = true;

            isIncreasingSignal = true;

            exFilterList = new ObservableCollection<FilterContainer>();
            exFilterList.Add(new FilterContainer(1, 0, 1, "Excitation Filter 1", "", ""));
            exFilterList.Add(new FilterContainer(2, 0, 2, "Excitation Filter 2", "", ""));
            exFilter = exFilterList[0];

            emFilterList = new ObservableCollection<FilterContainer>();
            emFilterList.Add(new FilterContainer(3, 1, 1, "Emission Filter 1", "", ""));
            emFilterList.Add(new FilterContainer(4, 1, 2, "Emission Filter 2", "", ""));
            emFilter = emFilterList[0];

            sliderLowPosition = 0.0;
            sliderHighPosition = 100.0;
		}
    
        ///////////////////////////////////////////////////////////////////////////////////

        //public int EMGain
        //{
        //    get { return cameraParams.emGain; }
        //    set
        //    {
        //        cameraParams.emGain = value;
        //        NotifyPropertyChanged("EMGain");
        //    }
        //}
       
        //public int HSSIndex
        //{
        //    get { return cameraParams.hssIndex; }
        //    set
        //    {
        //        cameraParams.hssIndex = value;
        //        NotifyPropertyChanged("HSSIndex");
        //    }
        //}
        
        //public int PreAmpGainIndex
        //{
        //    get { return cameraParams.preAmpGainIndex; }
        //    set
        //    {
        //        cameraParams.preAmpGainIndex = value;
        //        NotifyPropertyChanged("PreAmpGainIndex");
        //    }
        //}
        
        //public bool UseEMAmp
        //{
        //    get { return cameraParams.useEMAmp; }
        //    set
        //    {
        //        cameraParams.useEMAmp = value;
        //        NotifyPropertyChanged("UseEMAmp");
        //    }
        //}
        
        //public bool UseFrameTransfer
        //{
        //    get { return cameraParams.useFrameTransfer; }
        //    set
        //    {
        //        cameraParams.useFrameTransfer = value;
        //        NotifyPropertyChanged("UseFrameTransfer");
        //    }
        //}
        
        //public int VertClockAmpIndex
        //{
        //    get { return cameraParams.vertClockAmpIndex; }
        //    set
        //    {
        //        cameraParams.vertClockAmpIndex = value;
        //        NotifyPropertyChanged("VertClockAmpIndex");
        //    }
        //}
        
        //public int VSSIndex
        //{
        //    get { return cameraParams.vssIndex; }
        //    set
        //    {
        //        cameraParams.vssIndex = value;
        //        NotifyPropertyChanged("VSSIndex");
        //    }
        //}

      
        //public int ReadMode
        //{
        //    get { return acqParams.readMode; }
        //    set
        //    {
        //        acqParams.readMode = value;
        //        NotifyPropertyChanged("ReadMode");
        //    }
        //}
        
        //public int AcquisitionMode
        //{
        //    get { return acqParams.acquisitionMode; }
        //    set
        //    {
        //        acqParams.acquisitionMode = value;
        //        NotifyPropertyChanged("AcquisitionMode");
        //    }
        //}
        
        //public int TriggerMode
        //{
        //    get { return acqParams.triggerMode; }
        //    set
        //    {
        //        acqParams.triggerMode = value;
        //        NotifyPropertyChanged("TriggerMode");
        //    }
        //}
        
        //public int EMGainMode
        //{
        //    get { return acqParams.emGainMode; }
        //    set
        //    {
        //        acqParams.emGainMode = value;
        //        NotifyPropertyChanged("EMGainMode");
        //    }
        //}
        
        //public int ADChannel
        //{
        //    get { return acqParams.ADChannel; }
        //    set
        //    {
        //        acqParams.ADChannel = value;
        //        NotifyPropertyChanged("ADChannel");
        //    }
        //}
        
        //public int HBin
        //{
        //    get { return acqParams.hbin; }
        //    set
        //    {
        //        acqParams.hbin = value;
        //        NotifyPropertyChanged("HBin");
        //    }
        //}
        
        //public int VBin
        //{
        //    get { return acqParams.vbin; }
        //    set
        //    {
        //        acqParams.vbin = value;
        //        NotifyPropertyChanged("VBin");
        //    }
        //}
       
        //public int RoiX
        //{
        //    get { return acqParams.roiX; }
        //    set
        //    {
        //        acqParams.roiX = value;
        //        NotifyPropertyChanged("RoiX");
        //    }
        //}
        
        //public int RoiY
        //{
        //    get { return acqParams.roiY; }
        //    set
        //    {
        //        acqParams.roiY = value;
        //        NotifyPropertyChanged("RoiY");
        //    }
        //}
        
        //public int RoiW
        //{
        //    get { return acqParams.roiW; }
        //    set
        //    {
        //        acqParams.roiW = value;
        //        NotifyPropertyChanged("RoiW");
        //    }
        //}
        
        //public int RoiH
        //{
        //    get { return acqParams.roiH; }
        //    set
        //    {
        //        acqParams.roiH = value;
        //        NotifyPropertyChanged("RoiH");
        //    }
        //}


        /// ///////////////////////////////////////////////////////////////////////////////////////////

        public ObservableCollection<FilterContainer> ExFilterList
        {
            get { return exFilterList; }
            set
            {
                exFilterList = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("ExFilterList"));
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

        public ObservableCollection<FilterContainer> EmFilterList
        {
            get { return emFilterList; }
            set
            {
                emFilterList = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("EmFilterList"));
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





        public int Exposure
        {
            get { return exposure; }
            set
            {
                exposure = value;
                NotifyPropertyChanged("Exposure");
            }
        }



        public int XPixels
        {
            get { return xPixels; }
            set
            {
                xPixels = value;
                NotifyPropertyChanged("XPixels");
            }
        }

        public int YPixels
        {
            get { return yPixels; }
            set
            {
                yPixels = value;
                NotifyPropertyChanged("YPixels");
            }
        }

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

        public int EMGain
        {
            get { return cameraParams.EMGain; }
            set
            {
                cameraParams.EMGain = value;
                NotifyPropertyChanged("EMGain");
            }
        }

        public int HSSIndex
        {
            get { return cameraParams.HSSIndex; }
            set
            {
                cameraParams.HSSIndex = value;
                NotifyPropertyChanged("HSSIndex");
            }
        }

        public int VSSIndex
        {
            get { return cameraParams.VSSIndex; }
            set
            {
                cameraParams.VSSIndex = value;
                NotifyPropertyChanged("VSSIndex");
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

        public int VertClockAmpIndex
        {
            get { return cameraParams.VertClockAmpIndex; }
            set
            {
                cameraParams.VertClockAmpIndex = value;
                NotifyPropertyChanged("VertClockAmpIndex");
            }
        }

        public bool UseEMAmp
        {
            get { return cameraParams.UseEMAmp; }
            set
            {
                cameraParams.UseEMAmp = value;
                NotifyPropertyChanged("UseEMAmp");
            }
        }

        public bool UseFrameTransfer
        {
            get { return cameraParams.UseFrameTransfer; }
            set
            {
                cameraParams.UseFrameTransfer = value;
                NotifyPropertyChanged("UseFrameTransfer");
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


        public bool IsOptimizing
        {
            get { return isOptimizing; }
            set
            {
                isOptimizing = value;
                NotifyPropertyChanged("IsOptimizing");
            }
        }


        public int ReadMode
        {
            get { return acqParams.ReadMode; }
            set
            {
                acqParams.ReadMode = value;
                NotifyPropertyChanged("ReadMode");
            }
        }

        public int AcquisitionMode
        {
            get { return acqParams.AcquisitionMode; }
            set
            {
                acqParams.AcquisitionMode = value;
                NotifyPropertyChanged("AcquisitionMode");
            }
        }

        public int TriggerMode
        {
            get { return acqParams.TriggerMode; }
            set
            {
                acqParams.TriggerMode = value;
                NotifyPropertyChanged("TriggerMode");
            }
        }

        public int EMGainMode
        {
            get { return acqParams.EMGainMode; }
            set
            {
                acqParams.EMGainMode = value;
                NotifyPropertyChanged("EMGainMode");
            }
        }

        public int ADChannel
        {
            get { return acqParams.ADChannel; }
            set
            {
                acqParams.ADChannel = value;
                NotifyPropertyChanged("ADChannel");
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
            

        public int RoiX
        {
            get { return acqParams.RoiX; }
            set
            {
                acqParams.RoiX = value;
                NotifyPropertyChanged("RoiX");
            }
        }

        public int RoiY
        {
            get { return acqParams.RoiY; }
            set
            {
                acqParams.RoiY = value;
                NotifyPropertyChanged("RoiY");
            }
        }

        public int RoiW
        {
            get { return acqParams.RoiW; }
            set
            {
                acqParams.RoiW = value;
                NotifyPropertyChanged("RoiW");
            }
        }

        public int RoiH
        {
            get { return acqParams.RoiH; }
            set
            {
                acqParams.RoiH = value;
                NotifyPropertyChanged("RoiH");
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

        public Imager MyImager
        {
            get { return myImager; }
            set
            {
                myImager = value;
                NotifyPropertyChanged("MyImager");
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
        
        public ObservableCollection<Camera.VertClockVoltageAmplitude> VertClockVoltageAmplitudes
        {
            get { return vertClockVoltageAmplitudes; }
            set
            {
                vertClockVoltageAmplitudes = value;
                NotifyPropertyChanged("VertClockVoltageAmplitudes");
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
        
        public ObservableCollection<Camera.FunctionalMode> BinningOptions
        {
            get { return binningOptions; }
            set
            {
                binningOptions = value;
                NotifyPropertyChanged("BinningOptions");
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


        public bool AutosizeROI
        {
            get { return autosizeROI; }
            set
            {
                autosizeROI = value;
                NotifyPropertyChanged("AutosizeROI");
            }
        }


        public bool IsIncreasingSignal
        {
            get { return isIncreasingSignal; }
            set
            {
                isIncreasingSignal = value;
                NotifyPropertyChanged("IsIncreasingSignal");
            }
        }

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