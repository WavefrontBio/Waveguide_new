using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATMCD64CS;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace Waveguide
{
  

    public class CameraParams
    {
        private int vssIndex;
        private int hssIndex;
        private int vertClockAmpIndex;
        private int preAmpGainIndex;
        private bool useEMAmp;
        private int emGain;
        private bool useFrameTransfer;

        private bool ready;
        public event EventHandler Updated;

        protected virtual void OnUpdated(CameraParamsChangeEventArgs e)
        {
            if (Updated != null)
                Updated(this, e);
        }

        public CameraParams() 
        {
            vssIndex = 0;
            hssIndex = 0;
            vertClockAmpIndex = 0;
            preAmpGainIndex = 0;
            emGain = 5;
            useEMAmp = true;
            useFrameTransfer = true;
            ready = false;
        }
        public CameraParams(int _vssIndex, int _hssIndex, int _vertClockAmpIndex, int _preAmpGainIndex, int _emGain, bool _useEMAmp, bool _useFrameTransfer)
        {
            vssIndex = _vssIndex;
            hssIndex = _hssIndex;
            vertClockAmpIndex = _vertClockAmpIndex;
            preAmpGainIndex = _preAmpGainIndex;
            emGain = _emGain;
            useEMAmp = _useEMAmp;
            useFrameTransfer = _useFrameTransfer;
            ready = false;
        }

        public int VSSIndex { get { return vssIndex; } set { if (value != vssIndex) { vssIndex = value; ready = false; } } }
        public int HSSIndex { get { return hssIndex; } set { if (value != hssIndex) { hssIndex = value; ready = false; } } }
        public int VertClockAmpIndex { get { return vertClockAmpIndex; } set { if (value != vertClockAmpIndex) { vertClockAmpIndex = value; ready = false; } } }
        public int PreAmpGainIndex { get { return preAmpGainIndex; } set { if (value != preAmpGainIndex) { preAmpGainIndex = value; ready = false; } } }
        public int EMGain { get { return emGain; } set { if (value != emGain) { emGain = value; ready = false; } } }
        public bool UseEMAmp { get { return useEMAmp; } set { if (value != useEMAmp) { useEMAmp = value; ready = false; } } }
        public bool UseFrameTransfer { get { return useFrameTransfer; } set { if (value != useFrameTransfer) { useFrameTransfer = value; ready = false; } } }

        // Ready flag indicates whether the current CameraParams have been set on the camera.
        //  An event is raised whenever the ready flag transitions from false to true. This event can signal
        // external objects that may need to update when camera settings change.
        public bool Ready { get { return ready; } set { if (value != ready) { ready = value; if (ready == true) OnUpdated(new CameraParamsChangeEventArgs("Camera Params Change")); } } }
    }

    public class AcquisitionParams
    {
        private int readMode;
        private int acquisitionMode;
        private int triggerMode;
        private int emGainMode;
        private int adChannel;
        private int hbin;
        private int vbin;
        private int roiX;
        private int roiY;
        private int roiW;
        private int roiH;

        private UInt16 binnedFullImageWidth;
        private UInt16 binnedFullImageHeight;
        private UInt16 binnedRoiX;
        private UInt16 binnedRoiY;
        private UInt16 binnedRoiW;
        private UInt16 binnedRoiH;

        private UInt32 binnedFullImageNumPixels;
        private UInt32 binnedRoiImageNumPixels;

        private bool ready;
        public event EventHandler Updated;

        protected virtual void OnUpdated(CameraParamsChangeEventArgs e)
        {
            if (Updated != null)
                Updated(this, e);
        }


        // readMode - 0=Full Vertical Binning, 1=Multi-Track, 2=Random-Track, 3=Single-Track, 4=Image
        // acquisitionMode: Single Scan(1), Accumulate(2), kinetic(3), fast kinetic(4), run till abort(5)
        // triggerMode: internal trigger(0), software trigger(10)
        // emGainMode: 0 - gain controlled by DAC settings in the range of 0-255
        //             1 - gain controlled by DAC settings in the range of 0-4095
        //             2 - linear mode
        //             3 - real EM gain
        // ADChannel: the AD channel used, which is typically 0
        // hbin,vbin - binning, possible values are 1,2,4, or 8
        // roiX,roiY,roiW,roiH - set a Region-of-Interest

        // typical:
        //          readMode = 4
        //          acquisitionMode = 1
        //          triggerMode = 10
        //          emGainMode = 3
        //          ADChannel = 0
        //          hbin = 1, vbin = 1  (i.e. no binning)
        //          roiX = 0, roiY = 0, roiW = XPixels, roiY = YPixels (i.e. full image)

        public AcquisitionParams()
        {
            readMode = 4;
            acquisitionMode = 5;
            triggerMode = 10;
            emGainMode = 3;
            adChannel = 0;
            hbin = 1;
            vbin = 1;
            roiX = 0;
            roiY = 0;
            roiW = GlobalVars.Instance.PixelWidth;
            roiH = GlobalVars.Instance.PixelHeight;
            ready = false;
            UpdateBinnedSizes();
        }

        public AcquisitionParams(int _readMode, int _acquisitionMode, int _triggerMode, int _emGainMode, int _ADChannel, 
            int _hbin, int _vbin, int _roiX, int _roiY, int _roiW, int _roiH)
        {
            readMode = _readMode;
            acquisitionMode = _acquisitionMode;
            triggerMode = _triggerMode;
            emGainMode = _emGainMode;
            adChannel = _ADChannel;
            hbin = _hbin;
            vbin = _vbin;
            roiX = _roiX;
            roiY = _roiY;
            roiW = _roiW;
            roiH = _roiH;
            ready = false;
            UpdateBinnedSizes();
        }

        public int ReadMode { get { return readMode; } set { if (value != readMode) { readMode = value; ready = false; } } }
        public int AcquisitionMode { get { return acquisitionMode; } set { if (value != acquisitionMode) { acquisitionMode = value; ready = false; } } }
        public int TriggerMode { get { return triggerMode; } set { if (value != triggerMode) { triggerMode = value; ready = false; } } }
        public int EMGainMode { get { return emGainMode; } set { if (value != emGainMode) { emGainMode = value; ready = false; } } }
        public int ADChannel { get { return adChannel; } set { if (value != adChannel) { adChannel = value; ready = false; } } }
        public int HBin { get { return hbin; } set { if (value != hbin) { hbin = value; UpdateBinnedSizes(); ready = false; } } }
        public int VBin { get { return vbin; } set { if (value != vbin) { vbin = value; UpdateBinnedSizes(); ready = false; } } }
        public int RoiX { get { return roiX; } set { if (value != roiX) { roiX = value; UpdateBinnedSizes(); ready = false; } } }
        public int RoiY { get { return roiY; } set { if (value != roiY) { roiY = value; UpdateBinnedSizes(); ready = false; } } }
        public int RoiW { get { return roiW; } set { if (value != roiW) { roiW = value; UpdateBinnedSizes(); ready = false; } } }
        public int RoiH { get { return roiH; } set { if (value != roiH) { roiH = value; UpdateBinnedSizes(); ready = false; } } }

        public UInt16 BinnedFullImageWidth { get {return binnedFullImageWidth;}}
        public UInt16 BinnedFullImageHeight { get {return binnedFullImageHeight;}}
        public UInt16 BinnedRoiX { get {return binnedRoiX;}}
        public UInt16 BinnedRoiY { get { return binnedRoiY; }}
        public UInt16 BinnedRoiW  { get { return binnedRoiW; }}
        public UInt16 BinnedRoiH { get { return binnedRoiH; }}
        public UInt32 BinnedFullImageNumPixels { get { return binnedFullImageNumPixels; }}
        public UInt32 BinnedRoiImageNumPixels { get { return binnedRoiImageNumPixels; } }

        private void UpdateBinnedSizes()
        {
            binnedFullImageWidth = (UInt16)(GlobalVars.Instance.PixelWidth / HBin);
            binnedFullImageHeight = (UInt16)(GlobalVars.Instance.PixelHeight / VBin);
            binnedRoiX = (UInt16)(RoiX / HBin);
            binnedRoiY = (UInt16)(RoiY / VBin);
            binnedRoiW = (UInt16)(RoiW / HBin);
            binnedRoiH = (UInt16)(RoiH / VBin);
            binnedFullImageNumPixels = (UInt32)binnedFullImageWidth * (UInt32)binnedFullImageHeight;
            binnedRoiImageNumPixels = (UInt32)binnedRoiW * (UInt32)binnedRoiH;
        }

        // Ready flag indicates whether the current AcquisitionParams have been set on the camera.
        //  An event is raised whenever the ready flag transitions from false to true. This event can signal
        // external objects that may need to update when camera settings change.
        public bool Ready { get { return ready; } set { if (value != ready) { ready = value; if (ready == true) OnUpdated(new CameraParamsChangeEventArgs("Acquisition Params Change")); } } }
    }

    


    public class Camera
    {
        // //////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////

        public static uint CameraOK = AndorSDK.DRV_SUCCESS;

        // Camera Capabilities

        AndorSDK.AndorCapabilities capabilities = new AndorSDK.AndorCapabilities();

        public struct VSSpeed
        {
            public int index { get; set; }
            public float speed { get; set; }
            public string description { get; set; }
        }
        public ObservableCollection<VSSpeed> VSSpeeds;


        public struct VertClockVoltageAmplitude
        {
            public int index { get; set; }
            public string description { get; set; }
        }
        public ObservableCollection<VertClockVoltageAmplitude> VertClockVoltageAmplitudes;

        public struct HSSpeed
        {
            public int ADChannel { get; set; }
            public int index { get; set; }
            public float speed { get; set; }
            public string description { get; set; }
        }
        public ObservableCollection<HSSpeed> HSSpeeds_EM;
        public ObservableCollection<HSSpeed> HSSpeeds_Conv;

        public int EMGain_LowLimit;
        public int EMGain_HighLimit;

        public struct PreAmpGain
        {
            public int index { get; set; }
            public float gain { get; set; }
            public string description { get; set; }
        }
        public ObservableCollection<PreAmpGain> PreAmpGains;

        public struct FunctionalMode
        {
            public int index { get; set; }
            public string description { get; set; }
            public FunctionalMode(int ndx, string desc) : this()
            {
                this.index = ndx;
                this.description = desc;
            }
        }
        public ObservableCollection<FunctionalMode> ReadModes;
        public ObservableCollection<FunctionalMode> AcquisitionModes;
        public ObservableCollection<FunctionalMode> TriggerModes;
        public ObservableCollection<FunctionalMode> EMGainModes;
        public ObservableCollection<FunctionalMode> ADChannelOptions;
        public ObservableCollection<FunctionalMode> BinningOptions;


        // //////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////

        //  Camera State
        public CameraParams m_cameraParams;
        public AcquisitionParams m_acqParams;

        public int  ImageRotate { get; set; } // 0 = no rotate, 1 = 90 degs CW, 2 = 90 CCW
        public int  TargetTemperature { get; set; }
        public int  CameraTemperature { get; set; }
        public bool SystemInitialized { get; set; }
        public int  BitDepth { get; set; }
        public int  NumADChannels { get; set; }
        public int  XPixels { get; set; }
        public int  YPixels { get; set; }
        public bool IsEMCCD { get; set; }

        
    
        // //////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////


        public const uint NOT_INITIALIZED = 100;
        public const uint ACQUISITION_NOT_PREPARED = 101;

        public AndorSDK MyCamera = new AndorSDK();

        public uint SUCCESS = AndorSDK.DRV_SUCCESS;
        public uint DRV_SUCCESS = AndorSDK.DRV_SUCCESS;
        public uint DRV_ERROR_ACK = AndorSDK.DRV_ERROR_ACK;
        public uint DRV_ACQUIRING = AndorSDK.DRV_ACQUIRING;
        public uint DRV_IDLE = AndorSDK.DRV_IDLE;


        /////////////////////////////////////////////////////////////////////////////////////////////
        // Class Events AND Delegate Handlers

        public delegate void CameraMessageEventHandler(object sender, WaveGuideEvents.StringMessageEventArgs e);
        public delegate void CameraErrorEventHandler(object sender, WaveGuideEvents.ErrorEventArgs e);

        public event CameraMessageEventHandler CameraMessageEvent;
        public event CameraErrorEventHandler CameraErrorEvent;

        protected virtual void OnPostMessage(WaveGuideEvents.StringMessageEventArgs e)
        {
            if (CameraMessageEvent != null) CameraMessageEvent(this, e);
        }

        protected virtual void OnCameraError(WaveGuideEvents.ErrorEventArgs e)
        {
            if (CameraErrorEvent != null) CameraErrorEvent(this, e);
        }

        public void PostMessage(string msg)
        {
            WaveGuideEvents.StringMessageEventArgs e = new WaveGuideEvents.StringMessageEventArgs(msg);
            OnPostMessage(e);
        }

        public void PostError(string errMsg)
        {
            WaveGuideEvents.ErrorEventArgs e = new WaveGuideEvents.ErrorEventArgs(errMsg);
            OnCameraError(e);
        }


        //////////////////////////////////////////////////////////////////////////////////////////


       
        public Camera()  // constructor
        {
            // default number of pixels in CCD; this should be set to real value in the Initialize() method below
            XPixels = GlobalVars.Instance.PixelWidth;
            YPixels = GlobalVars.Instance.PixelHeight;


            VSSpeeds = new ObservableCollection<VSSpeed>();
            HSSpeeds_EM = new ObservableCollection<HSSpeed>();
            HSSpeeds_Conv = new ObservableCollection<HSSpeed>();
            PreAmpGains = new ObservableCollection<PreAmpGain>();
            VertClockVoltageAmplitudes = new ObservableCollection<VertClockVoltageAmplitude>();
            ReadModes = new ObservableCollection<FunctionalMode>();
            AcquisitionModes = new ObservableCollection<FunctionalMode>();
            TriggerModes = new ObservableCollection<FunctionalMode>();
            EMGainModes = new ObservableCollection<FunctionalMode>();
            ADChannelOptions = new ObservableCollection<FunctionalMode>();
            BinningOptions = new ObservableCollection<FunctionalMode>();

            EMGain_LowLimit = 2;
            EMGain_HighLimit = 300;

            SystemInitialized = false;

            m_cameraParams = new CameraParams();
            m_acqParams = new AcquisitionParams();

            Initialize();

            ReadModes.Add(new FunctionalMode(0, "Full Vertical Binning"));
            ReadModes.Add(new FunctionalMode(1, "Multi-Track"));
            ReadModes.Add(new FunctionalMode(2, "Random-Track"));
            ReadModes.Add(new FunctionalMode(3, "Single-Track"));
            ReadModes.Add(new FunctionalMode(4, "Image (Default)"));

            AcquisitionModes.Add(new FunctionalMode(1, "Single Scan (Default)"));
            AcquisitionModes.Add(new FunctionalMode(2, "Accumulate"));
            AcquisitionModes.Add(new FunctionalMode(3, "Kinetic"));
            AcquisitionModes.Add(new FunctionalMode(4, "Fast Kinetic"));
            AcquisitionModes.Add(new FunctionalMode(5, "Run Til Abort"));

            TriggerModes.Add(new FunctionalMode(0, "Internal Trigger")); ;
            TriggerModes.Add(new FunctionalMode(10, "Software Trigger (Default)")); ;

            EMGainModes.Add(new FunctionalMode(0, "DAC controll 0-255"));
            EMGainModes.Add(new FunctionalMode(1, "DAC controll 0-4095"));
            EMGainModes.Add(new FunctionalMode(2, "Linear"));
            EMGainModes.Add(new FunctionalMode(3, "Real EM Gain (Default)"));

            for (int i = 0; i < NumADChannels; i++)
                ADChannelOptions.Add(new FunctionalMode(i, "Channel " + i.ToString() + ((i == 0) ? " (Default)" : "")));

            BinningOptions.Add(new FunctionalMode(1, "1 x 1"));
            BinningOptions.Add(new FunctionalMode(2, "2 x 2"));
            BinningOptions.Add(new FunctionalMode(4, "4 x 4"));
        }

        ~Camera() // destructor
        {
            try
            {
                MyCamera.CoolerOFF();
            }
            catch
            {
            }
        }


        public bool Initialize() // configure the camera to capture a single, full image from the camera
        {
            uint uiErrorCode;
            string errMsg = "";

            SystemInitialized = false;

            // initialize the camera
            try
            {
                uiErrorCode = MyCamera.Initialize("");
            }
            catch (Exception e)
            {
                PostError("Andor SDK did not load: " + e.Message);
                return false;
            }
            bool success = CheckCameraResult(uiErrorCode, ref errMsg);
            if (!success)
            {
                PostError("Camera: " + errMsg);
                return false;
            }

            SystemInitialized = true;
            PostMessage("Camera initialized");


            // get capabilities
            success = GetCameraCapabilities();
            if(!success)
            {
                PostError("Failed to get Camera Capabilities");
                return false;
            }


            // ADDED by BG, 26 Mar 2014, Want camera to start cooling right away.  Also added MyCamera.CoolerOFF() in destructor
            CameraTemperature = GlobalVars.Instance.CameraTargetTemperature;
            SetCoolerTemp(CameraTemperature);
            MyCamera.SetImageRotate(ImageRotate); // 0 = no rotate, 1 = 90 degs CW, 2 = 90 CCW

            return true;
        }

        public void GetCurrentCameraSettings(out CameraParams cameraParams, out AcquisitionParams acqParams)
        {
            cameraParams = m_cameraParams;
            acqParams = m_acqParams;
        }
        

        public bool GetCameraCapabilities()
        {
            string errMsg = "No Error";
            uint ecode = 0;

            if (!SystemInitialized) Initialize();

            // get camera capabilities
            //capabilities.ulSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(capabilities);
            capabilities.ulSize = 256; // had to guess at the size since sizeof(AndorSDK.AndorCapabilities) isn't allowed
            ecode = MyCamera.GetCapabilities(ref capabilities); 

            // Vertical Shift Speeds
            int VSScount = 0;
            VSSpeeds.Clear();
            ecode = MyCamera.GetNumberVSSpeeds(ref VSScount);            
            if (CheckCameraResult(ecode, ref errMsg))
            {
                for (int i = 0; i < VSScount; i++)
                {
                    float speed = 0.0f;
                    ecode = MyCamera.GetVSSpeed(i, ref speed);
                    if (CheckCameraResult(ecode, ref errMsg))
                    {
                        VSSpeed vss = new VSSpeed();
                        vss.index = i;
                        vss.speed = speed;
                        vss.description = string.Format("{0:N1}", speed);
                        VSSpeeds.Add(vss);
                    }
                    else
                    {
                        OnCameraError(new WaveGuideEvents.ErrorEventArgs("GetVSSpeed: " + errMsg));
                        return false;
                    }
                }
            }
            else
            {
                OnCameraError(new WaveGuideEvents.ErrorEventArgs("GetNumberVerticalSpeeds: " + errMsg));
                return false;
            }


            // Horizontal Shift Speeds
            int HSScount_EM = 0;
            int HSScount_Conv = 0;
            int _NumADChannels = 0;
            int AmpType = 0; // 0 = EM, 1 = Conventional
            HSSpeeds_EM.Clear();
            HSSpeeds_Conv.Clear();

            ecode = MyCamera.GetNumberADChannels(ref _NumADChannels);
            if (CheckCameraResult(ecode, ref errMsg))
            {
                NumADChannels = _NumADChannels;
            }
            else
            {
                OnCameraError(new WaveGuideEvents.ErrorEventArgs("GetNumberADChannels: " + errMsg));
                return false;
            }


            for (int ADChannel = 0; ADChannel < NumADChannels; ADChannel++)
            {
                ecode = MyCamera.GetNumberHSSpeeds(ADChannel, AmpType, ref HSScount_EM);
                if (CheckCameraResult(ecode, ref errMsg))
                {
                    for (int i = 0; i < HSScount_EM; i++)
                    {
                        float speed = 0.0f;
                        ecode = MyCamera.GetHSSpeed(ADChannel, AmpType, i, ref speed);
                        if (CheckCameraResult(ecode, ref errMsg))
                        {
                            HSSpeed hss = new HSSpeed();
                            hss.ADChannel = ADChannel;
                            hss.index = i;
                            hss.speed = speed;
                            hss.description = string.Format("{0:N1}", speed);
                            HSSpeeds_EM.Add(hss);
                        }
                        else
                        {
                            OnCameraError(new WaveGuideEvents.ErrorEventArgs("GetHSSpeed: " + errMsg));
                            return false;
                        }
                    }
                }
                else
                {
                    OnCameraError(new WaveGuideEvents.ErrorEventArgs("GetNumberHSSpeeds: " + errMsg));
                    return false;
                }


                AmpType = 1; // 0 = EM, 1 = Conventional
                ecode = MyCamera.GetNumberHSSpeeds(ADChannel, AmpType, ref HSScount_Conv);
                if (CheckCameraResult(ecode, ref errMsg))
                {
                    for (int i = 0; i < HSScount_Conv; i++)
                    {
                        float speed = 0.0f;
                        ecode = MyCamera.GetHSSpeed(ADChannel, AmpType, i, ref speed);
                        if (CheckCameraResult(ecode, ref errMsg))
                        {
                            HSSpeed hss = new HSSpeed();
                            hss.ADChannel = ADChannel;
                            hss.index = i;
                            hss.speed = speed;
                            hss.description = string.Format("{0:N1}", speed);
                            HSSpeeds_Conv.Add(hss);
                        }
                        else
                        {
                            OnCameraError(new WaveGuideEvents.ErrorEventArgs("GetHSSpeed: " + errMsg));
                            return false;
                        }
                    }
                }
                else
                {
                    OnCameraError(new WaveGuideEvents.ErrorEventArgs("GetNumberHSSpeeds: " + errMsg));
                    return false;
                }

            }

            // EM Gain limits            
            ecode = MyCamera.GetEMGainRange(ref EMGain_LowLimit, ref EMGain_HighLimit);
            if (CheckCameraResult(ecode, ref errMsg))
            {

            }
            else
            {
                OnCameraError(new WaveGuideEvents.ErrorEventArgs("GetEMGainRange: " + errMsg));
                return false;
            }


            // PreAmp Gains
            int NumPreAmpGains = 0;
            PreAmpGains.Clear();
            ecode = MyCamera.GetNumberPreAmpGains(ref NumPreAmpGains);

            NumPreAmpGains = 2;  // had to hard code.  function above returns 3, when only 2 appear to be available

            if (CheckCameraResult(ecode, ref errMsg))
            {
                for (int i = 0; i < NumPreAmpGains; i++)
                {
                    float gain = 0.0f;
                    ecode = MyCamera.GetPreAmpGain(i, ref gain);
                    if (CheckCameraResult(ecode, ref errMsg))
                    {
                        PreAmpGain pag = new PreAmpGain();
                        pag.index = i;
                        pag.gain = gain;
                        pag.description = string.Format("{0:N0}", gain);
                        PreAmpGains.Add(pag);
                    }
                    else
                    {
                        OnCameraError(new WaveGuideEvents.ErrorEventArgs("GetPreAmpGain: " + errMsg));
                        return false;
                    }
                }
            }
            else
            {
                OnCameraError(new WaveGuideEvents.ErrorEventArgs("GetNumberPreAmpGains: " + errMsg));
                return false;
            }


            // set VertClockVoltageAmplitudes
            VertClockVoltageAmplitudes.Clear();
            VertClockVoltageAmplitude vcv = new VertClockVoltageAmplitude();
            // 0
            vcv.index = 0;
            vcv.description = "0 - Normal";
            VertClockVoltageAmplitudes.Add(vcv);
            // 1
            vcv = new VertClockVoltageAmplitude();
            vcv.index = 1;
            vcv.description = "+1";
            VertClockVoltageAmplitudes.Add(vcv);
            // 2
            vcv = new VertClockVoltageAmplitude();
            vcv.index = 2;
            vcv.description = "+2";
            VertClockVoltageAmplitudes.Add(vcv);
            // 3
            vcv = new VertClockVoltageAmplitude();
            vcv.index = 3;
            vcv.description = "+3";
            VertClockVoltageAmplitudes.Add(vcv);
            // 4
            vcv = new VertClockVoltageAmplitude();
            vcv.index = 4;
            vcv.description = "+4";
            VertClockVoltageAmplitudes.Add(vcv);



            // Get Detector size in pixels
            int _xPixels = 0;
            int _yPixels = 0;
            ecode = MyCamera.GetDetector(ref _xPixels, ref _yPixels);
            if (CheckCameraResult(ecode, ref errMsg))
            {
                XPixels = _xPixels;
                YPixels = _yPixels;
                GlobalVars.Instance.PixelWidth = XPixels;
                GlobalVars.Instance.PixelHeight = YPixels;
            }
            else
            {
                OnCameraError(new WaveGuideEvents.ErrorEventArgs("GetDetector: " + errMsg));
                return false;
            }


            // Is this an EMCCD?
            //  1 = iXon
            //  3 = EMCCD
            // 21 = iXon Ultra
            if (capabilities.ulCameraType == 21 || capabilities.ulCameraType == 3)
                IsEMCCD = true;
            else
                IsEMCCD = false;


            int bitDepth = 16;
            ecode = MyCamera.GetBitDepth(0, ref bitDepth);
            if (CheckCameraResult(ecode, ref errMsg))
            {
                BitDepth = bitDepth;
            }
            else
            {
                PostError("Camera: " + errMsg);
                return false;
            }


            // Set Camera EM Gain Mode
            if (IsEMCCD)
            {
                ecode = MyCamera.SetEMGainMode(3);
                if (!CheckCameraResult(ecode, ref errMsg))
                {
                    PostError("Camera: " + errMsg);
                    return false;
                }
            }


            return true;
        }


        public void ConfigureCamera(CameraSettingsContainer settings)
        {
            ConfigureCamera(settings.VSSIndex,settings.HSSIndex,settings.VertClockAmpIndex,m_cameraParams.PreAmpGainIndex,settings.UseEMAmp,
                m_cameraParams.EMGain,settings.UseFrameTransfer);
        }

        public bool ConfigureCamera()
        {
            return ConfigureCamera(m_cameraParams);
        }

        public bool ConfigureCamera(CameraParams p)
        {
            return ConfigureCamera(p.VSSIndex, p.HSSIndex, p.VertClockAmpIndex, p.PreAmpGainIndex, p.UseEMAmp, p.EMGain, p.UseFrameTransfer);
        }

        public bool ConfigureCamera(int vssIndex, int hssIndex, int vertClockAmpIndex, int preAmpGainIndex, bool useEMAmp, int emGain, bool useFrameTransfer)
        {
            // these settings are equivalent to the Andor OptAcquire settings

            bool success = true;
            uint ecode = 0;
            string errMsg = "No Error";

            // assign properties
            m_cameraParams.VSSIndex = vssIndex;
            m_cameraParams.HSSIndex = hssIndex;
            m_cameraParams.VertClockAmpIndex = vertClockAmpIndex;
            m_cameraParams.PreAmpGainIndex = preAmpGainIndex;
            m_cameraParams.UseEMAmp = useEMAmp;
            m_cameraParams.EMGain = emGain;
            m_cameraParams.UseFrameTransfer = useFrameTransfer;
            ecode = SUCCESS;

            ecode = MyCamera.SetVSSpeed(m_cameraParams.VSSIndex);            
            if (CheckCameraResult(ecode, ref errMsg))
            {
                int type = 0; // use EM Amplifier
                if (!m_cameraParams.UseEMAmp) type = 1; // use Conventional Amplifier
                ecode = MyCamera.SetHSSpeed(type, m_cameraParams.HSSIndex);
                if (CheckCameraResult(ecode, ref errMsg))
                {
                    ecode = MyCamera.SetVSAmplitude(m_cameraParams.VertClockAmpIndex);
                    if (CheckCameraResult(ecode, ref errMsg))
                    {
                        ecode = MyCamera.SetPreAmpGain(m_cameraParams.PreAmpGainIndex);
                        if (CheckCameraResult(ecode, ref errMsg))
                        {
                            ecode = MyCamera.SetEMCCDGain(m_cameraParams.EMGain);
                            if (CheckCameraResult(ecode, ref errMsg))
                            {
                                int mode = 0;  // frame transfer OFF
                                if (m_cameraParams.UseFrameTransfer) mode = 1;
                                ecode = MyCamera.SetFrameTransferMode(mode);
                                if (!CheckCameraResult(ecode, ref errMsg))
                                {
                                    OnCameraError(new WaveGuideEvents.ErrorEventArgs("SetFrameTransferMode: " + errMsg));
                                    success = false;
                                }
                            }
                            else
                            {
                                OnCameraError(new WaveGuideEvents.ErrorEventArgs("SetEMCCDGain: " + errMsg));
                                success = false;
                            }
                        }
                        else
                        {
                            OnCameraError(new WaveGuideEvents.ErrorEventArgs("SetPreAmpGain: " + errMsg));
                            success = false;
                        }
                    }
                    else
                    {
                        OnCameraError(new WaveGuideEvents.ErrorEventArgs("SetVSAmplitude: " + errMsg));
                        success = false;
                    }
                }
                else
                {
                    OnCameraError(new WaveGuideEvents.ErrorEventArgs("SetHSSpeed: " + errMsg));
                    success = false;
                }
            }
            else
            {
                OnCameraError(new WaveGuideEvents.ErrorEventArgs("SetVSSpeed: " + errMsg));
                success = false;
            }

            m_cameraParams.Ready = true;

            return success;
        }


        


        public bool PrepareAcquisition()
        {
            return PrepareAcquisition(m_acqParams);
        }

        public bool PrepareAcquisition(AcquisitionParams p)
        {
            return PrepareAcquisition(p.ReadMode, p.AcquisitionMode, p.TriggerMode, p.EMGainMode, p.ADChannel, p.HBin, p.VBin, p.RoiX, p.RoiY, p.RoiW, p.RoiH);
        }

        public bool PrepareAcquisition(int readMode, int acquisitionMode, int triggerMode, int emGainMode, int adChannel, 
            int hbin, int vbin, int roiX, int roiY, int roiW, int roiH )
        {
            // readMode - 0=Full Vertical Binning, 1=Multi-Track, 2=Random-Track, 3=Single-Track, 4=Image
            // acquisitionMode: Single Scan(1), Accumulate(2), kinetic(3), fast kinetic(4), run till abort(5)
            // triggerMode: internal trigger(0), software trigger(10)
            // emGainMode: 0 - gain controlled by DAC settings in the range of 0-255
            //             1 - gain controlled by DAC settings in the range of 0-4095
            //             2 - linear mode
            //             3 - real EM gain
            // ADChannel: the AD channel used, which is typically 0
            // hbin,vbin - binning, possible values are 1,2,4, or 8
            // roiX,roiY,roiW,roiH - set a Region-of-Interest

            // typical:
            //          readMode = 4
            //          acquisitionMode = 1
            //          triggerMode = 10
            //          emGainMode = 3
            //          ADChannel = 0
            //          hbin = 1, vbin = 1  (i.e. no binning)
            //          roiX = 0, roiY = 0, roiW = XPixels, roiY = YPixels (i.e. full image)
            
            uint ecode;
            bool success = true;
            string errMsg = "No Error";

            if (!SystemInitialized) success = Initialize();

            if (!success) return false;
   
            ecode = MyCamera.SetShutter(1, 1, 0, 0); // type, mode (1=open, 2=close), opening time, closing time
            if(!CheckCameraResult(ecode, ref errMsg))
            {
                PostError("Camera Error: SetShutter - " + errMsg);
                return false;
            }
                        
            ecode = MyCamera.SetReadMode(readMode); // image mode
            m_acqParams.ReadMode = readMode;
            if (!CheckCameraResult(ecode, ref errMsg))
            {
                PostError("Camera Error: SetReadMode - " + errMsg);
                return false;
            }


            VerifyROISize(ref roiX, ref roiY, ref roiW, ref roiH, hbin, vbin);

            // configure binning and image area: part of the ccd taken as the image
            m_acqParams.HBin = hbin;
            m_acqParams.VBin = vbin;
            m_acqParams.RoiX = roiX;
            m_acqParams.RoiY = roiY;
            m_acqParams.RoiW = roiW;
            m_acqParams.RoiH = roiH;
            int x1 = m_acqParams.RoiX + 1;    // change from zero-based numbering to one-based numbering
            int y1 = m_acqParams.RoiY + 1;
            int x2 = x1 + m_acqParams.RoiW - 1;
            int y2 = y1 + m_acqParams.RoiH - 1;
            if (x1 < 1) x1 = 1;
            if (y1 < 1) y1 = 1;
            if (x2 > XPixels) x2 = XPixels;
            if (y2 > YPixels) y2 = YPixels;


            
            ecode = MyCamera.SetImage(hbin, vbin, x1, x2, y1, y2); // params: hbin,vbin,x1,x2,y1,y2
            if (!CheckCameraResult(ecode, ref errMsg))
            {
                PostError("Camera Error: SetImage - " + errMsg);
                return false;
            }

            ecode = MyCamera.SetADChannel(adChannel); // parameter is in the range of 0 to GetNumberADChannels-1
            m_acqParams.ADChannel = adChannel;
            if (!CheckCameraResult(ecode, ref errMsg))
            {
                PostError("Camera Error: SetADChannel - " + errMsg);
                return false;
            }

            ecode = MyCamera.SetAcquisitionMode(acquisitionMode); // SetAcquisitionMode: Single Scan(1), Accumulate(2), kinetic(3), fast kinetic(4), run till abort(5)
            m_acqParams.AcquisitionMode = acquisitionMode;
            if (!CheckCameraResult(ecode, ref errMsg))
            {
                PostError("Camera Error: SetAcquisitionMode - " + errMsg);
                return false;
            }

            ecode = MyCamera.SetTriggerMode(triggerMode); // internal trigger(0), software trigger(10)
            m_acqParams.TriggerMode = triggerMode;
            if (!CheckCameraResult(ecode, ref errMsg))
            {
                PostError("Camera Error: SetTriggerMode - " + errMsg);
                return false;
            }

            ecode = MyCamera.SetEMGainMode(emGainMode);
            m_acqParams.EMGainMode = emGainMode;
            if (!CheckCameraResult(ecode, ref errMsg))
            {
                PostError("Camera Error: SetEMGainMode - " + errMsg);
                return false;
            }


            // 1st param = mode, must be set to 1
            // 2nd param = value, 0 to disable events, 1 to enable events
            // ecode = MyCamera.SetPCIMode(1, 0);
            // ecode = MyCamera.SetCameraStatusEnable(1);

            m_acqParams.Ready = true;

            return success;
        }
        

        public bool CoolerON(bool turnOn)
        {
            if (!SystemInitialized)
            {
                PostError("ERROR: Attempted to turn on Camera Cooler; Camera Not Initialized");
                return false;
            }

            uint ec;
            bool success;
            string errMsg = "";
            if (turnOn)
            {
                ec = MyCamera.CoolerON();
                success = CheckCameraResult(ec, ref errMsg);
                if (success)
                    PostMessage("Camera Cooler ON");
                else
                {
                    PostError("Camera Cooler: " + errMsg);
                    return false;
                }
            }
            else
            {
                ec = MyCamera.CoolerOFF();
                success = CheckCameraResult(ec, ref errMsg);
                if (success)
                    PostMessage("Camera Cooler OFF");
                else
                {
                    PostError("Camera Cooler: " + errMsg);
                    return false;
                }
            }

            return true;
        }


        public bool SetCoolerTemp(int temp)
        {
            if (!SystemInitialized)
            {
                PostError("ERROR: Attempted to set Camera temperature; Camera Not Initialized");
                return false;
            }

            uint ec;
            bool success;
            string errMsg = "";
            ec = MyCamera.SetTemperature(temp);
            success = CheckCameraResult(ec, ref errMsg);
            if (success)
                PostMessage("Camera Cooler Temperature Set to " + temp.ToString());
            else
            {
                PostError("Camera Cooler: " + errMsg);
                return false;
            }

            return true;
        }


        public bool GetCoolerTemp(ref int temp)
        {
            if (!SystemInitialized)
            {
                PostError("Camera not initialized");
                return false;
            }

            uint ec = MyCamera.GetTemperature(ref temp);
            CameraTemperature = temp;
      
            return true;
        }


        public bool AbortAcquisition()
        {
            if (!SystemInitialized)
            {
                PostError("Camera not initialized");
                return false;
            }

            uint uiErrorCode;

            uiErrorCode = MyCamera.AbortAcquisition();
            PostMessage("Acquisition Aborted");

            return true;
        }
        

        public uint AcquireImage(int exposure, ref ushort[] image)
        { 
            // exp is the exposure time in milliseconds
            float expSeconds = (float)(exposure) / 1000;
            
            if (!SystemInitialized)
            {
                image = null;
                PostError("Camera Error: Not Initialized");
                return NOT_INITIALIZED;
            }

            bool success;
            if (!m_cameraParams.Ready)
            {
                success = ConfigureCamera();
                if (!success) return 30000; // Error Configuring Camera
            }

            m_acqParams.AcquisitionMode = 1;  // set to Single Image Mode
            m_acqParams.TriggerMode = 0; // set to Hardware Trigger Mode

            if (!m_acqParams.Ready)
            {
                success = PrepareAcquisition();
                if (!success) return 30001; // Error Configuring Acquisition
            }

            uint uiErrorCode = 0;
            int status = 0;

            // set exposure time in seconds
            if (MyCamera.SetExposureTime(expSeconds) == AndorSDK.DRV_SUCCESS)
            {
                MyCamera.SetShutter(1, 1, 0, 0); // open shutter
                uiErrorCode = MyCamera.PrepareAcquisition();
                                
                // start the image acquisition
                if (MyCamera.StartAcquisition() == AndorSDK.DRV_SUCCESS)
                {
                    uiErrorCode = MyCamera.WaitForAcquisition();
                }
            }
            MyCamera.SetShutter(1, 0, 0, 0); // close shutter

            if (uiErrorCode == AndorSDK.DRV_SUCCESS)
            {
                // if good acquisition occurred, get image
                uiErrorCode = MyCamera.GetStatus(ref status);
                if (status == AndorSDK.DRV_IDLE)
                {
                    //uiErrorCode = MyCamera.GetAcquiredData16(image, (uint)TotalPixels);
                    uiErrorCode = MyCamera.GetOldestImage16(image, (uint)m_acqParams.BinnedRoiImageNumPixels);
                }
                else
                {
                    image = null;
                }
            }

            return uiErrorCode;
        }




        public uint EnableAdvancedGainMode(bool enable)
        {
            uint uiErrorCode;
            int state = 0;

            if (enable) state = 1;

            uiErrorCode = MyCamera.SetEMAdvanced(state);

            return uiErrorCode;
        }


        public uint SetOutputAmplifier(int amp)
        {
            uint uiErrorCode;

            uiErrorCode = MyCamera.SetOutputAmplifier(amp);

            return uiErrorCode;
        }

        public uint SetCameraPreAmpGain(int index)
        {
            uint uiErrorCode;

            if (index > 1) index = 1;
            if (index < 0) index = 0;

            uiErrorCode = MyCamera.SetPreAmpGain(index);

            return uiErrorCode;
        }


        public uint SetCameraEMGainMode(int mode)
        {   // values for mode:
            // 0 = (default) controlled by DAC 0-255, 1 = controlled by DAC 0-4095, 2 = Linear mode, 3 = Real EM gain

            // if (!SystemInitialized) return NOT_INITIALIZED;

            uint uiErrorCode;
            uiErrorCode = MyCamera.SetEMGainMode(mode);
            if (uiErrorCode != AndorSDK.DRV_SUCCESS)
            {
                //System.Diagnostics.Trace.WriteLine("ERROR: setting EM Gain Mode.");
                return uiErrorCode;
            }

            return AndorSDK.DRV_SUCCESS;
        }


        public uint SetCameraEMGain(int gain)
        {
            if (!SystemInitialized) return NOT_INITIALIZED;

            uint uiErrorCode;

            int lowGain = 0;
            int highGain = 0;
            MyCamera.GetEMGainRange(ref lowGain, ref highGain);
            if (gain > highGain) gain = highGain;
            if (gain < lowGain) gain = lowGain;

            uiErrorCode = MyCamera.SetEMCCDGain(gain);

            return uiErrorCode;
        }


        public uint GetCameraEMGainRange(ref int lowVal, ref int hiVal)
        {
            // valid EM gain range is dependent upon the EM Gain Mode

            if (!SystemInitialized) return NOT_INITIALIZED;

            uint ec = MyCamera.GetEMGainRange(ref lowVal, ref hiVal);

            return ec;
        }


        public uint SetCameraBinning(int horzBinning, int vertBinning)
        {
            if (!SystemInitialized) return NOT_INITIALIZED;

            uint uiErrorCode;

            // Set Readout
            //// set to full resolution for the full image
            //// params: horizontal binning, vertical binning,horz start pixel, horz end pixel, vert start pixel, vert end pixel 
            //uiErrorCode = MyCamera.SetImage(horzBinning, vertBinning, 1, XPixels, 1, YPixels);

            m_acqParams.HBin = horzBinning;
            m_acqParams.VBin = vertBinning;

            PrepareAcquisition();

            uiErrorCode = SUCCESS;
            return uiErrorCode;
        }


        public uint SendSoftwareTrigger()
        {
            uint ui_ret = DRV_ERROR_ACK;
            int count = 0;
            while (ui_ret == DRV_ERROR_ACK && count < 5)
            {
                ui_ret = MyCamera.SendSoftwareTrigger();
                if (ui_ret == DRV_ERROR_ACK)
                {
                    Thread.Sleep(1);
                    count++;
                }
            }
            return ui_ret;
        }


        public bool SafeImageLevels(UInt16[] image, int imageW, int imageH, int percentOfPixelsLimit, UInt16 thresholdPixelValue)
        {
            // this function is meant to check that there are not more pixels above the thresholdPixelValue
            // than are allowed by percentOfPixelsLimit.
            // This is done to safeguard the camera sensor from over exposure

            bool isSafe = true;
            int count = 0;

            for(int ndx = 0; ndx < (imageW*imageH); ndx++)
            {
                if (image[ndx] > thresholdPixelValue) count++;
            }

            float percentFail = (float)count / (float)(imageW * imageH);

            if (percentFail > (float)percentOfPixelsLimit) isSafe = false;          

            return isSafe;
        }



        public void SynthesizeImage(out ushort[] image, int width, int height)
        {
            ushort hiValue = 4095;
            ushort loValue = 2000;

            int NumPixels = width * height * 2;
            image = new ushort[NumPixels];

            bool high = true;

            for (int i = 0; i < NumPixels; i++)
            {
                if (i % 64 == 0) high = !high;
                if (high) { image[i] = hiValue; }
                else { image[i] = loValue; }
            }
        }
        

        public bool CheckCameraResult(uint code, ref string errorMsg)
        {
            bool ok = true;

            errorMsg = "SUCCESS";

            if (code != AndorSDK.DRV_SUCCESS)
            {
                ok = false;
                switch (code)
                {
                    case NOT_INITIALIZED: errorMsg = "Camera Not Initialized"; break;
                    case 20001: errorMsg = "ERROR_CODES"; break;
                    case 20002: errorMsg = "SUCCESS"; break;
                    case 20003: errorMsg = "VXD NOT INSTALLED"; break;
                    case 20004: errorMsg = "ERROR_SCAN"; break;
                    case 20005: errorMsg = "ERROR_CHECK_SUM"; break;
                    case 20006: errorMsg = "ERROR_FILELOAD"; break;
                    case 20007: errorMsg = "UNKNOWN_FUNCTION"; break;
                    case 20008: errorMsg = "ERROR_VXD_INIT"; break;
                    case 20009: errorMsg = "ERROR_ADDRESS"; break;
                    case 20010: errorMsg = "ERROR_PAGELOCK"; break;
                    case 20011: errorMsg = "ERROR_PAGE_UNLOCK"; break;
                    case 20012: errorMsg = "ERROR_BOARDTEST"; break;
                    case 20013: errorMsg = "ERROR_ACK"; break;
                    case 20014: errorMsg = "ERROR_UP_FIFO"; break;
                    case 20015: errorMsg = "ERROR_PATTERN"; break;
                    case 20017: errorMsg = "ACQUISITION_ERRORS"; break;
                    case 20018: errorMsg = "ACQ_BUFFER"; break;
                    case 20019: errorMsg = "ACQ_DOWNFIFO_FULL"; break;
                    case 20020: errorMsg = "PROC_UNKNOWN_INSTRUCTION"; break;
                    case 20021: errorMsg = "ILLEGAL_OP_CODE"; break;
                    case 20022: errorMsg = "KINETIC_TIME_NOT_MET"; break;
                    case 20023: errorMsg = "ACCUM_TIME_NOT_MET"; break;
                    case 20024: errorMsg = "NO_NEW_DATA"; break;
                    case 20026: errorMsg = "SPOOLERROR"; break;
                    case 20033: errorMsg = "TEMPERATURE_CODES"; break;
                    case 20034: errorMsg = "TEMPERATURE_OFF"; break;
                    case 20035: errorMsg = "TEMPERATURE_NOT_STABILIZED"; break;
                    case 20036: errorMsg = "TEMPERATURE_STABILIZED"; break;
                    case 20037: errorMsg = "TEMPERATURE_NOT_REACHED"; break;
                    case 20038: errorMsg = "TEMPERATURE_OUT_RANGE"; break;
                    case 20039: errorMsg = "TEMPERATURE_NOT_SUPPORTED"; break;
                    case 20040: errorMsg = "TEMPERATURE_DRIFT"; break;
                    case 20049: errorMsg = "GENERAL_ERRORS"; break;
                    case 20050: errorMsg = "INVALID_AUX"; break;
                    case 20051: errorMsg = "COF_NOTLOADED"; break;
                    case 20052: errorMsg = "FPGAPROG"; break;
                    case 20053: errorMsg = "FLEXERROR"; break;
                    case 20054: errorMsg = "GPIBERROR"; break;
                    case 20064: errorMsg = "DATATYPE"; break;
                    case 20065: errorMsg = "DRIVER_ERRORS"; break;
                    case 20066: errorMsg = "P1INVALID"; break;
                    case 20067: errorMsg = "P2INVALID"; break;
                    case 20068: errorMsg = "P3INVALID"; break;
                    case 20069: errorMsg = "P4INVALID"; break;
                    case 20070: errorMsg = "INIERROR"; break;
                    case 20071: errorMsg = "COFERROR"; break;
                    case 20072: errorMsg = "ACQUIRING"; break;
                    case 20073: errorMsg = "IDLE"; break;
                    case 20074: errorMsg = "TEMPCYCLE"; break;
                    case 20075: errorMsg = "NOT_INITIALIZED"; break;
                    case 20076: errorMsg = "P5INVALID"; break;
                    case 20077: errorMsg = "P6INVALID"; break;
                    case 20078: errorMsg = "INVALID_MODE"; break;
                    case 20079: errorMsg = "INVALID_FILTER"; break;
                    case 20080: errorMsg = "I2CERRORS"; break;
                    case 20081: errorMsg = "I2CDEVNOTFOUND"; break;
                    case 20082: errorMsg = "I2CTIMEOUT"; break;
                    case 20083: errorMsg = "P7INVALID"; break;
                    case 20089: errorMsg = "USBERROR"; break;
                    case 20090: errorMsg = "IOCERROR"; break;
                    case 20091: errorMsg = "NOT_SUPPORTED"; break;
                    case 20093: errorMsg = "USB_INTERRUPT_ENDPOINT_ERROR"; break;
                    case 20094: errorMsg = "RANDOM_TRACK_ERROR"; break;
                    case 20095: errorMsg = "INVALID_TRIGGER_MODE"; break;
                    case 20096: errorMsg = "LOAD_FIRMWARE_ERROR"; break;
                    case 20097: errorMsg = "DIVIDE_BY_ZERO_ERROR"; break;
                    case 20098: errorMsg = "INVALID_RINGEXPOSURES"; break;
                    case 20099: errorMsg = "BINNING_ERROR"; break;
                    case 20100: errorMsg = "INVALID_AMPLIFIER"; break;
                    case 20115: errorMsg = "ERROR_MAP"; break;
                    case 20116: errorMsg = "ERROR_UNMAP"; break;
                    case 20117: errorMsg = "ERROR_MDL"; break;
                    case 20118: errorMsg = "ERROR_UNMDL"; break;
                    case 20119: errorMsg = "ERROR_BUFFSIZE"; break;
                    case 20121: errorMsg = "ERROR_NOHANDLE"; break;
                    case 20130: errorMsg = "GATING_NOT_AVAILABLE"; break;
                    case 20131: errorMsg = "FPGA_VOLTAGE_ERROR"; break;
                    case 20990: errorMsg = "ERROR_NOCAMERA"; break;
                    case 20991: errorMsg = "NOT_SUPPORTED"; break;
                    case 20992: errorMsg = "NOT_AVAILABLE"; break;

                        // My Custom Error Codes and Messages
                    case 30000: errorMsg = "Error Configuring Camera"; break;
                    case 30001: errorMsg = "Error Configuring Acquisition"; break;
                }
            }

            return ok;
        }


        public void PrepForKineticImaging()
        {
            // Sets up Camera for Run Til Abort mode

            // INPUT:
            // exposure - millisecond exposure time
            // cycleTime - millisecond kinetic cycle time (time between successive images)
            //
            // OUTPUT:
            // actualExposure - the actual exposure time to be used by the camera
            // actualCycleTime - the actual kinetic cycle time to be used by the camera

            // convert parameter types 
      

            // setup Camera, if required:
            if (!m_cameraParams.Ready) ConfigureCamera();


            // setup Acquisition:
            // Read Mode = 4 (Image)
            // Acquisition Mode = 5 (Run Til Abort)
            // Trigger Mode = 10 (Software Trigger)
            // all other settings are as they were previously set
            m_acqParams.ReadMode = 4;
            m_acqParams.AcquisitionMode = 5;
            m_acqParams.TriggerMode = 10;            
            PrepareAcquisition();
           
            
     
            //// Get the Actual Acquisition timing that will be used by the camera
            //float actualExposureFloat = 0;
            //float actualAccumulateFloat = 0;
            //float actualCycleTimeFloat = 0;
            //ecode = MyCamera.GetAcquisitionTimings(ref actualExposureFloat, ref actualAccumulateFloat, ref actualCycleTimeFloat);

            //actualExposure = (int)(actualExposureFloat * 1000);
            //actualCycleTime = (int)(actualCycleTimeFloat * 1000);
        }



        public int GetCycleTime()
        {
            // Get the Actual Acquisition timing that will be used by the camera
            float actualExposureFloat = 0;
            float actualAccumulateFloat = 0;
            float actualCycleTimeFloat = 0;

            uint ecode = MyCamera.GetAcquisitionTimings(ref actualExposureFloat, ref actualAccumulateFloat, ref actualCycleTimeFloat);
            
            int actualCycleTime = (int)(actualCycleTimeFloat * 1000);

            return actualCycleTime;
        }

    


        public void VerifyROISize(ref int roiX, ref int roiY, ref int roiW, ref int roiH, int hBinning, int vBinning)
        {
            // this function makes sure that the width,height of the roi is an integer multiple of hBinning,vBinning respectively
            // if this is not done, the camera SetImage function may fail with an roi incompatible with binning\

            for (int w = 0; w < 8; w++)
            {
                if (((roiW + w) % hBinning) == 0)
                {
                    roiW += w;
                    roiX -= (w / 2);
                    break;
                }
            }

            for (int h = 0; h < 8; h++)
            {
                if (((roiH + h) % vBinning) == 0)
                {
                    roiH += h;
                    roiY -= (h / 2);
                    break;
                }
            }

        }


    }


    public class CameraParamsChangeEventArgs : EventArgs
    {
        private string message;

        public CameraParamsChangeEventArgs(string _message)
        {
            message = _message;
        }

        public string Message
        {
            get { return message; }
            set { message = value; }
        }
    }


}
