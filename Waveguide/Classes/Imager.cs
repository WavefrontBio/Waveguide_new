using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using CudaToolsNet;
using System.Windows.Interop;
using WPFTools;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Collections;
using Brainboxes.IO;

namespace Waveguide
{

    public delegate void CameraEventHandler(object sender, CameraEventArgs e);
    public delegate void CameraTemperatureEventHandler(object sender, TemperatureEventArgs e);
    public delegate void InsideTemperatureEventHandler(object sender, TemperatureEventArgs e);
    public delegate void ImagerEventHandler(object sender, ImagerEventArgs e);
 

    public struct ImagingParamsStruct
    {
        public ImageDisplay     ImageControl;
        //public WriteableBitmap  bmapImage;     
        public WriteableBitmap  histBitmap;

        public float            exposure;
        public int              binning;
        public byte             excitationFilterPos;
        public byte             emissionFilterPos;
        public string           indicatorName;
        public int              cycleTime;
        public int              gain;
        public int              preAmpGainIndex;
        public FLATFIELD_SELECT flatfieldType;
        public int              experimentIndicatorID;
        public ObservableCollection<Tuple<int, int>> optimizeWellList;
        
                                           

        public ImagingParamsStruct(ImageDisplay imageControl, WriteableBitmap _histBitmap,
            float _exposure, int _binning, byte _excitationFilterPos, byte _emissionFilterPos, string _indicatorName, 
            int _cycleTime, int _gain, int _preAmpGainIndex, FLATFIELD_SELECT _flatfieldType, int _expIndicatorID, 
            ObservableCollection<Tuple<int,int>> _optimizeWellList = null)
        {
            ImageControl = imageControl;
            histBitmap = _histBitmap;
            exposure = _exposure;
            binning = _binning;
            excitationFilterPos = _excitationFilterPos;
            emissionFilterPos = _emissionFilterPos;
            indicatorName = _indicatorName;
            cycleTime = _cycleTime;
            gain = _gain;
            preAmpGainIndex = _preAmpGainIndex;
            flatfieldType = _flatfieldType;
            experimentIndicatorID = _expIndicatorID;
            optimizeWellList = _optimizeWellList;            
        }
    }


    public class Imager
    {
        public WaveguideDB m_wgDB;
        public Camera m_camera;
        public MaskContainer m_mask;
        public ColorModel m_colorModel;
        public CudaToolBox m_cudaToolBox;
        TaskScheduler m_uiTask;
        Stopwatch m_imagingSequenceCounter;
        OmegaTempCtrl m_omegaTempController;
        

        public Lambda m_lambda;
        public Thor m_thor;

        public UInt16 m_histogramImageWidth = 1024;
        public UInt16 m_histogramImageHeight = 256;

        public UInt16 m_RangeSliderLowerSliderPosition;
        public UInt16 m_RangeSliderUpperSliderPosition;

        public bool m_UseMask;
        public bool m_ROIAdjustToMask;
        public bool m_kineticImagingON;

        public ObservableCollection<FilterContainer> m_exFilterList;
        public FilterContainer m_exFilter;
        public ObservableCollection<FilterContainer> m_emFilterList;
        public FilterContainer m_emFilter;
        public byte m_filterChangeSpeed;

             
        CancellationTokenSource m_cancelTokenSource;

        CancellationTokenSource m_cameraTemperatureTokenSource;
        CancellationToken m_cameraTemperatureCancelToken;


        bool m_cameraTempMonitorRunning;
        bool m_insideTempMonitorRunning;
        public bool m_insideHeatingON;

        // dictionary of <Experiment Indicator ID, Stucture holding details of the display panel for this Exp. Ind. ID>
        public Dictionary<int, ImagingParamsStruct> m_ImagingDictionary;

    

        public event CameraEventHandler m_cameraEvent;
        protected virtual void OnCameraEvent(CameraEventArgs e)
        {
            if(m_cameraEvent!=null)
                m_cameraEvent(this, e);
        }

        public event ImagerEventHandler m_imagerEvent;
        protected virtual void OnImagerEvent(ImagerEventArgs e)
        {
            if(m_imagerEvent!=null)
                m_imagerEvent(this, e);
        }

        public event CameraTemperatureEventHandler m_cameraTemperatureEvent;
        protected virtual void OnCameraTemperatureEvent(TemperatureEventArgs e)
        {
            m_cameraTemperatureEvent(this, e);
        }

        public event InsideTemperatureEventHandler m_insideTemperatureEvent;
        protected virtual void OnInsideTemperatureEvent(TemperatureEventArgs e)
        {
            m_insideTemperatureEvent(this, e);
        }


        public int  OptimizationResult_Exposure { get; set; }     
        public bool OptimizationResult_Success  { get; set; }

        public bool ImagerReady { get; set; }

        public Imager()
        {
            ImagerReady = false;

            m_wgDB = new WaveguideDB();

            m_wgDB.GetAllEmissionFilters();
            m_emFilterList = new ObservableCollection<FilterContainer>();
            foreach (FilterContainer filter in m_wgDB.m_filterList) m_emFilterList.Add(filter);
            if (m_emFilterList.Count > 0) m_emFilter = m_emFilterList[0];

            m_wgDB.GetAllExcitationFilters();
            m_exFilterList = new ObservableCollection<FilterContainer>();
            foreach (FilterContainer filter in m_wgDB.m_filterList) m_exFilterList.Add(filter);
            if (m_exFilterList.Count > 0) m_exFilter = m_exFilterList[0];            


            m_camera = new Camera();
            m_camera.CameraErrorEvent += m_camera_CameraErrorEvent;
            m_camera.m_acqParams.Updated += m_acqParams_Updated;
                   
            m_cudaToolBox = new CudaToolBox();

            m_lambda = new Lambda();
            m_thor = new Thor();

            m_filterChangeSpeed = (byte)GlobalVars.FilterChangeSpeed;

            m_insideHeatingON = false;
        }

        public void SetInsideHeatingON(bool turnON)
        {
            m_insideHeatingON = turnON;

            if (turnON)
            {
                m_omegaTempController.EnableOutput(1);
            }
            else
            {
                m_omegaTempController.DisableOutput(1);
            }
        }

        public void SetInsideTemperatureTarget(int targetTemp)
        {
            m_omegaTempController.updateSetPoint(1, targetTemp);
        }


        void m_camera_CameraErrorEvent(object sender, WaveGuideEvents.ErrorEventArgs e)
        {            
            OnImagerEvent(new ImagerEventArgs(e.ErrorMessage, ImagerState.Error));
        }



        void m_acqParams_Updated(object sender, EventArgs e)
        {
            // this function is connected to an event from the camera's AcquisitionParams object that is signal whenever the camera's acquisition
            // has been re-configured according to some new parameters.

            // since AcquisitionParms include binning and image size, the mask may need to change, therefore update the mask
            UpdateMask(m_mask);
            
        }

        public void Shutdown()
        {
                    
            // clean up Cuda stuff
            if(m_cudaToolBox != null) m_cudaToolBox.ShutdownCudaTools();

            // stop the temperature monitoring Task
            if(m_cameraTemperatureTokenSource != null) m_cameraTemperatureTokenSource.Cancel();       
        }

        public void Init()
        {
            bool success = m_camera.Initialize();
            if (!success)
            {
                ImagerReady = false;
                OnImagerEvent(new ImagerEventArgs("Camera Failed to Initialize", ImagerState.Error));
                return;
            }
            else
            {
                // Initialize Lambda (filter controller)
                //if (!m_lambda.SystemInitialized)
                //{
                //    success = m_lambda.Initialize();
                //    if (!success)
                //    {
                //        ImagerReady = false;
                //        OnImagerEvent(new ImagerEventArgs("Filter Controller FAILED to Initialize", ImagerState.Error));
                //        return;
                //    }
                //    else
                //    {
                //        OnImagerEvent(new ImagerEventArgs("Filter Controller Initialized Successfully", ImagerState.Idle));
                //    }
                //}
            }

            ImagerReady = true;

            m_camera.CoolerON(true);

            m_uiTask = TaskScheduler.FromCurrentSynchronizationContext();

            m_RangeSliderLowerSliderPosition = 0;
            m_RangeSliderUpperSliderPosition = (UInt16)GlobalVars.MaxPixelValue;

            m_UseMask = true;
            m_ROIAdjustToMask = false;

            m_kineticImagingON = false;
            m_cameraTempMonitorRunning = false;



            m_cudaToolBox.InitCudaTools(IntPtr.Zero); // Make sure this is done before calling any cuda function

            // set up camera
         
            m_ImagingDictionary = new Dictionary<int, ImagingParamsStruct>();

            m_mask = new MaskContainer();
            m_mask.Angle = 0;
            m_mask.Cols = 24;
            m_mask.Description = "test mask";
            m_mask.IsDefault = true;
            m_mask.MaskID = 1;
            m_mask.NumEllipseVertices = 24;
            m_mask.PixelMaskImage = null;
            m_mask.PixelList = null;
            m_mask.PixelMask = null;
            m_mask.PlateTypeID = 0;
            m_mask.ReferenceImageID = 0;
            m_mask.Rows = 16;
            m_mask.Shape = 0;
            m_mask.XOffset = 100;
            m_mask.XSize = 25;
            m_mask.XStep = 35;
            m_mask.YOffset = 200;
            m_mask.YSize = 25;
            m_mask.YStep = 35;
           
            UpdateMask(m_mask);

            ColorModelContainer colorModelContainer = null;
            SetColorModel(colorModelContainer); // pass null here creates the default color model



            // start Temperature Monitoring Task
            if (!m_cameraTempMonitorRunning)
            {
                m_cameraTemperatureTokenSource = new CancellationTokenSource();
                m_cameraTemperatureCancelToken = m_cameraTemperatureTokenSource.Token;

                var progressIndicator = new Progress<TemperatureProgressReport>(ReportCameraTemperature);

                Task.Factory.StartNew(() =>
                {
                    CameraTempMonitorWorker(progressIndicator);
                }, m_cameraTemperatureCancelToken);

                OnCameraEvent(new CameraEventArgs("Camera Temperature Monitoring Started", false));
            }

            // start Omega Temperature Controller Task
            if(!m_insideTempMonitorRunning)
            {
                m_omegaTempController = new OmegaTempCtrl(GlobalVars.TempControllerIP, 2000);

                m_omegaTempController.TempEvent += m_omegaTempController_TempEvent;
                m_omegaTempController.MessageEvent += m_omegaTempController_MessageEvent;

                m_omegaTempController.Connect();
                m_omegaTempController.StartTempUpdate(1.0);    
            }
           
        }

        void m_omegaTempController_MessageEvent(object sender, OmegaTempCtrlMessageEventArgs e)
        {
            OnImagerEvent(new ImagerEventArgs("Temp Controller: " + e.Message,ImagerState.Idle));            
        }

        void m_omegaTempController_TempEvent(object sender, OmegaTempCtrlTempEventArgs e)
        {
            OnInsideTemperatureEvent(new TemperatureEventArgs(true,(int) e.Temperature));
        }

        public void SetupCamera(bool AllowCameraConfiguration, bool isManualMode)
        {
            // AllowConfiguration Flag - controls whether user can adjust advanced camera settings
            // isManualMode Flag - controls whether certain settings can be changed, like filter positions.  Commonly false when Verifying before an Experiment run
            //                     since you would not want to allow filter changes to be made in this mode.


            if (m_camera == null)
            {               
                OnImagerEvent(new ImagerEventArgs("The Imager has not been assigned a Camera",ImagerState.Error));
            }
            else
            {
                ManualControlDialog dlg = new ManualControlDialog(this, -1, AllowCameraConfiguration, isManualMode);

                dlg.ShowDialog();
            }
        }

        public void UpdateMask(MaskContainer mask)
        {
            if (mask != null && m_cudaToolBox != null)
            {
                m_mask = mask;

                m_mask.BuildPixelList(m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight, m_camera.m_acqParams.HBin, m_camera.m_acqParams.VBin);
                m_mask.BuildPixelMaskImage(m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight);

                // load PixelMaskImage to GPU
                m_cudaToolBox.Set_MaskImage(m_mask.PixelMaskImage, m_camera.m_acqParams.BinnedFullImageWidth,
                                            m_camera.m_acqParams.BinnedFullImageHeight, (UInt16)mask.Rows, (UInt16)mask.Cols);

                SetROI(m_ROIAdjustToMask);
            }
        }
        
        public void SetROI(bool setRoiAroundMask)
        {
            m_ROIAdjustToMask = setRoiAroundMask;

            if (m_ROIAdjustToMask)
            {
                int roix = 0, roiy = 0, roiw = 0, roih = 0;
                m_mask.GetMaskROI(ref roix, ref roiy, ref roiw, ref roih,
                    m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight, m_camera.m_acqParams.HBin, m_camera.m_acqParams.VBin, 4);

                m_camera.VerifyROISize(ref roix, ref roiy, ref roiw, ref roih, m_camera.m_acqParams.HBin, m_camera.m_acqParams.VBin);

                m_camera.m_acqParams.RoiX = roix;
                m_camera.m_acqParams.RoiY = roiy;
                m_camera.m_acqParams.RoiW = roiw;
                m_camera.m_acqParams.RoiH = roih;
            }
            else
            {             
                m_camera.m_acqParams.RoiX = 0;
                m_camera.m_acqParams.RoiY = 0;
                m_camera.m_acqParams.RoiW = GlobalVars.PixelWidth;
                m_camera.m_acqParams.RoiH = GlobalVars.PixelHeight;
            }
        }

        public void SetColorModel(ColorModelContainer colorModelContainer)
        {
            ColorModel colorModel;

            if (colorModelContainer == null)
            {
                colorModel = new ColorModel();
            }
            else
            {
                colorModel = new ColorModel(colorModelContainer, GlobalVars.MaxPixelValue);
            }

            SetColorModel(colorModel);
        }


        public void SetColorModel(ColorModel colorModel)
        {
            m_colorModel = colorModel;
            
            int arraySize = GlobalVars.MaxPixelValue;
            byte[] red = new byte[arraySize];
            byte[] green = new byte[arraySize];
            byte[] blue = new byte[arraySize];

            for (int i = 0; i < arraySize; i++)
            {
                red[i] = m_colorModel.m_colorMap[i].m_red;
                green[i] = m_colorModel.m_colorMap[i].m_green;
                blue[i] = m_colorModel.m_colorMap[i].m_blue;
            }

            // copy to GPU
            if (m_cudaToolBox != null)
                m_cudaToolBox.Set_ColorMap(red, green, blue, (UInt16)arraySize);
        }



        private void SetExcitationFilter(int position)
        {
            m_lambda.MoveFilterA((byte)position, m_filterChangeSpeed);
        }

        private void SetEmissionFilter(int position)
        {
            m_lambda.MoveFilterB((byte)position, m_filterChangeSpeed);
        }


        public void SetEmissionFilter(FilterContainer filter)
        {
            foreach (FilterContainer fc in m_emFilterList)
            {
                if (filter.FilterID == fc.FilterID)
                {                   
                    SetEmissionFilter(fc.PositionNumber);
                    break;
                }
            }
        }


        public void SetExcitationFilter(FilterContainer filter)
        {
            foreach (FilterContainer fc in m_exFilterList)
            {
                if (filter.FilterID == fc.FilterID)
                {
                    SetExcitationFilter(fc.PositionNumber);
                    break;
                }
            }
        }




        public Dictionary<int,ImagingParamsStruct> BuildImagingDictionary(Dictionary<int, ExperimentIndicatorContainer> indicatorDictionary, 
                                                                          Dictionary<int,ImageDisplay> imageDictionary,  // dictionary of ImageDisplay controls
                                                                          Dictionary<int,int>   cycleTimeDictionary, // dictionary of cycle times for each experiment indicator                                                                          
                                                                          Dictionary<int,Image> histImageDictionary)  // can be null if no Histogram Images are to be displayed, otherwise
                                                                                                                      // a dictionary of Image controls used to display histograms
        {

            // NOTE:  this function assumes that the ExperimentIndicatorID's have been set!!
            //        Also, these ExperimentIndicatorID's are used as the key for the Dictionaries 
            //        passed in (imageDictionary, cycleTimeDictionary, and histImageDictionary)

            ResetImagingDictionary();
          
            foreach (ExperimentIndicatorContainer ind in indicatorDictionary.Values)
            {                
                ImagingParamsStruct ips = new ImagingParamsStruct();

                ips.cycleTime = cycleTimeDictionary[ind.ExperimentIndicatorID];
                ips.indicatorName = ind.Description;
                ips.emissionFilterPos = (byte)ind.EmissionFilterPos;
                ips.excitationFilterPos = (byte)ind.ExcitationFilterPos;                
                ips.experimentIndicatorID = ind.ExperimentIndicatorID;
                ips.exposure = (float)ind.Exposure / 1000.0f;
                ips.flatfieldType = ind.FlatFieldCorrection;
                ips.gain = ind.Gain;

                if (histImageDictionary != null)
                {                    
                    Image histImage;
                    if(histImageDictionary.ContainsKey(ind.ExperimentIndicatorID))
                    {
                        histImage = histImageDictionary[ind.ExperimentIndicatorID];
                        ips.histBitmap = BitmapFactory.New(m_histogramImageWidth, m_histogramImageHeight);
                        histImage.Source = ips.histBitmap;
                    }
                    else
                        ips.histBitmap = null;
                }
                else
                    ips.histBitmap = null;


                if (imageDictionary != null)
                {                   
                    if (imageDictionary.ContainsKey(ind.ExperimentIndicatorID))
                    {
                        ips.ImageControl = imageDictionary[ind.ExperimentIndicatorID];                        
                    }
                    else
                        ips.ImageControl = null;
                }
                else
                    ips.ImageControl = null;
                
              
                ips.ImageControl.m_imageBitmap = null;           

                m_ImagingDictionary.Add(ind.ExperimentIndicatorID,ips);

                // this is done AFTER adding it to the m_ImagingDictionary
                ConfigImageDisplaySurface(ind.ExperimentIndicatorID, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight, false);
              
            }

            return m_ImagingDictionary;
        }
       

        public void ResetImagingDictionary()
        {
            foreach(int id in m_ImagingDictionary.Keys)
            {
                m_cudaToolBox.Remove_D3dSurface(id);                
            }
            m_ImagingDictionary.Clear();

        }

      

      


        public void RedisplayCurrentImage(int ID, UInt16 lowerScaleOfColorMap, UInt16 upperScaleOfColorMap)
        {
            ImagingParamsStruct dps;

            if (m_ImagingDictionary.ContainsKey(ID))
            {
                dps = m_ImagingDictionary[ID];

                // convert the grayscale image to color using the colormap that is already on the GPU
                IntPtr colorImageOnGpu = m_cudaToolBox.Convert_GrayscaleToColor(lowerScaleOfColorMap, upperScaleOfColorMap);

                if(dps.ImageControl.m_imageBitmap != null)
                {
                    byte[] colorImage;
                    m_cudaToolBox.Download_ColorImage(out colorImage, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight);

                    // display the image
                    Int32Rect displayRect = new Int32Rect(0, 0, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight);
                    dps.ImageControl.m_imageBitmap.Lock();
                    dps.ImageControl.m_imageBitmap.WritePixels(displayRect, colorImage, m_camera.m_acqParams.BinnedFullImageWidth * 4, 0);
                    dps.ImageControl.m_imageBitmap.Unlock();
                }
            }
            else
            {
                OnImagerEvent(new ImagerEventArgs("Attempted to display for an ID that does not exist in the ImagingDictionary", ImagerState.Error));
            }
        }


        public void ProcessAndDisplayImage(UInt16[] grayRoiImage, int ID, bool applyMask, UInt16 lowerScaleOfColorMap, UInt16 upperScaleOfColorMap)
        {
            // assumes that the Flat Field correction has already been set up on the GPU

            ImagingParamsStruct dps;

            if (m_ImagingDictionary.ContainsKey(ID))
            {
                dps = m_ImagingDictionary[ID];

                UInt16[] grayFullImage = new UInt16[m_camera.m_acqParams.BinnedFullImageNumPixels];

                // process image
                  
                    // copy image to GPU, if it's an ROI, it is padded with 0's to make a full image
                    m_cudaToolBox.PostRoiGrayscaleImage(grayRoiImage, 
                                                m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight,
                                                m_camera.m_acqParams.BinnedRoiW, m_camera.m_acqParams.BinnedRoiH, 
                                                m_camera.m_acqParams.BinnedRoiX, m_camera.m_acqParams.BinnedRoiY);

                    // flatten image
                    m_cudaToolBox.FlattenGrayImage((int)dps.flatfieldType);

                    // apply mask if applyMask is true, this will zero all pixels outside of mask apertures
                    // this function also will apply a flat field correction *IF* a correction matrix has been loaded
                    if (applyMask) m_cudaToolBox.ApplyMaskToGrayscaleImage();

                    m_cudaToolBox.Download_GrayscaleImage(out grayFullImage, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight);

                    // calculate mask aperture sums
                    UInt32[] sums;
                    m_cudaToolBox.GetMaskApertureSums(out sums, m_mask.Rows, m_mask.Cols);

                    // convert the grayscale image to color using the colormap that is already on the GPU
                    IntPtr colorImageOnGpu = m_cudaToolBox.Convert_GrayscaleToColor(lowerScaleOfColorMap, upperScaleOfColorMap);

                    if (dps.ImageControl.m_imageBitmap != null)
                    {
                        byte[] colorImage;
                        m_cudaToolBox.Download_ColorImage(out colorImage, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight);

                        // display the image
                        Int32Rect displayRect = new Int32Rect(0, 0, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight);
                        dps.ImageControl.m_imageBitmap.Lock();
                        dps.ImageControl.m_imageBitmap.WritePixels(displayRect, colorImage, m_camera.m_acqParams.BinnedFullImageWidth * 4, 0);
                        dps.ImageControl.m_imageBitmap.Unlock();
                    }
                

                    if(dps.histBitmap != null)
                    {
                        // calculate the image histogram
                        UInt32[] histogram;
                        m_cudaToolBox.GetHistogram_512(out histogram, 16);
                                              
                        // build the histogram image and download it to the CPU
                        byte[] histImage;
                        m_cudaToolBox.GetHistogramImage_512(out histImage, m_histogramImageWidth, m_histogramImageHeight, 0);
                                             
                        // display the histogram image
                        Int32Rect histRect = new Int32Rect(0, 0, m_histogramImageWidth, m_histogramImageHeight);
                        dps.histBitmap.Lock();
                        dps.histBitmap.WritePixels(histRect, histImage, m_histogramImageWidth * 4, 0);
                        dps.histBitmap.Unlock();
                    }
            }
            else
            {
                OnImagerEvent(new ImagerEventArgs("Attempted to display for an ID that does not exist in the ImagingDictionary", ImagerState.Error));
            }
        }



        public bool AcquireImage(int exposure, out UInt16[] grayRoiImage)
        {            
            uint ecode;
            string errMsg = "No Error";
                      
            //grayRoiImage = new ushort[m_camera.m_acqParams.BinnedFullImageNumPixels];
            grayRoiImage = new ushort[m_camera.m_acqParams.BinnedRoiImageNumPixels];
          
            ecode = m_camera.AcquireImage(exposure, ref grayRoiImage);

            if(!m_camera.CheckCameraResult(ecode ,ref errMsg))
            {
                OnCameraEvent(new CameraEventArgs("Camera Error: " + errMsg , false ));
                OnImagerEvent(new ImagerEventArgs("Camera Error: " + errMsg, ImagerState.Error));
                return false;
            }

            return true;
        }




        public bool GetFFReferenceImagesByType(FLATFIELD_SELECT ffSelect, out ushort[] F, out ushort[] D)
        {
            bool success = true;
            WaveguideDB wgDB = new WaveguideDB();
            ReferenceImageContainer refImage;
            F = null;
            D = null;

            switch (ffSelect)
            {
                case FLATFIELD_SELECT.USE_FLUOR:
                    // this is fluorescence indicator, so get fluorescence reference images
                    success = wgDB.GetReferenceImageByType(REFERENCE_IMAGE_TYPE.REF_FLAT_FIELD_FLUORESCENCE, out refImage);
                    if (success)
                    {
                        if (refImage != null)
                        {
                            F = refImage.ImageData;
                        }
                        success = wgDB.GetReferenceImageByType(REFERENCE_IMAGE_TYPE.REF_DARK_FLUORESCENCE, out refImage);
                        if (success)
                        {
                            if (refImage != null)
                            {
                                D = refImage.ImageData;
                            }
                        }
                    }
                    break;

                case FLATFIELD_SELECT.USE_LUMI:
                    // this is a luminescence indicator, so get luminescence reference images
                    success = wgDB.GetReferenceImageByType(REFERENCE_IMAGE_TYPE.REF_FLAT_FIELD_LUMINESCENCE, out refImage);
                    if (success)
                    {
                        if (refImage != null)
                        {
                            F = refImage.ImageData;
                        }
                        success = wgDB.GetReferenceImageByType(REFERENCE_IMAGE_TYPE.REF_DARK_LUMINESCENCE, out refImage);
                        if (success)
                        {
                            if (refImage != null)
                            {
                                D = refImage.ImageData;
                            }
                        }
                    }
                    break;

                default:
                    F = null;
                    D = null;
                    break;
            }

            if (F == null || D == null) success = false;

            return success;
        }


        public void BuildDefaultFFCRefImages(out ushort[] F, out ushort[] D, int width, int height )
        {
            F = new ushort[width * height];
            D = new ushort[width * height];

            for(int i = 0; i< (width*height); i++)
            {
                F[i] = 1000;  // just put same value in for all of F
                D[i] = 0;
            }
        }


        public void SetupFlatFieldCorrection(FLATFIELD_SELECT type, int binning)
        {
            // set up flat field correction arrays on GPU
            ushort[] F;
            ushort[] D;
            FlatFieldCorrector ffc;                      
            int imageSize = GlobalVars.PixelWidth * GlobalVars.PixelHeight;
            bool success;

            // get ref images
            success = GetFFReferenceImagesByType(type, out F, out D);
            if (!success) BuildDefaultFFCRefImages(out F, out D, GlobalVars.PixelWidth, GlobalVars.PixelHeight);

            // build flat field corrector for given correction type
            ffc = new FlatFieldCorrector(imageSize, F, D);
            ffc.CorrectForBinning(binning, binning);

            // load fluor correction arrays to GPU
            m_cudaToolBox.SetFlatFieldCorrection((int)type, ffc.Gc, ffc.Dc);
        }


        public ITargetBlock<Tuple<ushort[], int, int>> CreateImageProcessingPipeline(TaskScheduler uiTask, CancellationToken cancelToken,
                            AcquisitionParams acqParams, Dictionary<int, ImagingParamsStruct> _imagingDictionary, 
                            MaskContainer mask, bool applyMask, bool saveImages, int projectID, int plateID, int experimentID)
        {

            List<int> indicatorIDList = new List<int>();            
            foreach (int key in _imagingDictionary.Keys) indicatorIDList.Add(key);
            ImageFileManager imageFileManager = new ImageFileManager();
            imageFileManager.SetBasePath(GlobalVars.ImageFileSaveLocation, projectID, plateID, experimentID, indicatorIDList);
            Dictionary<int, ImagingParamsStruct> imagingDictionary = _imagingDictionary;
            CudaToolBox cuda = m_cudaToolBox;

             var firstEntry = imagingDictionary.First();
             ImagingParamsStruct firstIps = firstEntry.Value;
             int binning = firstIps.binning;

            // set up flat field correction arrays on GPU
            SetupFlatFieldCorrection(FLATFIELD_SELECT.USE_FLUOR, binning);
            SetupFlatFieldCorrection(FLATFIELD_SELECT.USE_LUMI, binning);
                //ushort[] F;
                //ushort[] D;
                //FlatFieldCorrector ffc;
                //var firstEntry = imagingDictionary.First();
                //ImagingParamsStruct firstIps = firstEntry.Value;
                //int binning = firstIps.binning;
                //int imageSize = GlobalVars.PixelWidth * GlobalVars.PixelHeight;
                //bool success;

                //// FLUORESCENCE
                //// get fluor ref images
                //success = GetFFReferenceImagesByType(FLATFIELD_SELECT.USE_FLUOR, out F, out D);
                //if (!success) BuildDefaultFFCRefImages(out F, out D, GlobalVars.PixelWidth, GlobalVars.PixelHeight);

                //// build flat field corrector for fluor
                //ffc = new FlatFieldCorrector(imageSize, F, D);
                //ffc.CorrectForBinning(binning, binning);

                //// load fluor correction arrays to GPU
                //cuda.SetFlatFieldCorrection((int)FLATFIELD_SELECT.USE_FLUOR, ffc.Gc, ffc.Dc);

                //// LUMINESCENCE
                //// get lumi ref images
                //success = GetFFReferenceImagesByType(FLATFIELD_SELECT.USE_LUMI, out F, out D);
                //if (!success) BuildDefaultFFCRefImages(out F, out D, GlobalVars.PixelWidth, GlobalVars.PixelHeight);

                //// build flat field corrector for lumi
                //ffc = new FlatFieldCorrector(imageSize, F, D);
                //ffc.CorrectForBinning(binning, binning);

                //// load lumi correction arrays to GPU
                //cuda.SetFlatFieldCorrection((int)FLATFIELD_SELECT.USE_LUMI, ffc.Gc, ffc.Dc);



            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // CudaProcessing and Display

            var CudaProcessAndDisplayImage = new TransformBlock<Tuple<ushort[], int, int>, Tuple<ushort[], int, int>>(inputData =>
            {
                // since this call Cuda and GUI functionality, it must be run on the UI Thread

                ushort[] grayRoiImage = inputData.Item1;  // Raw Grayscale ROI image 
                int expIndID = inputData.Item2;  // Experiment Indicator ID
                int sequenceNumber = inputData.Item3; // number of msecs into the experiment that image was taken

                
                Stopwatch sw = new Stopwatch();
                long t1 = 0;
                ushort[] maskedFullGrayImage = null;
                Int32Rect histRect = new Int32Rect(0, 0, m_histogramImageWidth, m_histogramImageHeight);

                try
                {
                    // set the cuda context to this thread
                    //cuda.PushCudaContext();

                    sw.Restart();
                    // get FlatFieldCorrector for this experiment indicator
                    if (imagingDictionary.ContainsKey(expIndID))
                    {
                        // process image
                        ImagingParamsStruct dps = imagingDictionary[expIndID];

                        // copy image to GPU, if it's an ROI, it is padded with 0's to make a full image
                        cuda.PostRoiGrayscaleImage(grayRoiImage, acqParams.BinnedFullImageWidth, acqParams.BinnedFullImageHeight,
                                                   acqParams.BinnedRoiW, acqParams.BinnedRoiH, acqParams.BinnedRoiX, acqParams.BinnedRoiY);

                        // flatten image
                        cuda.FlattenGrayImage((int)dps.flatfieldType);

                        // apply mask if applyMask is true, this will zero all pixels outside of mask apertures
                        // this function also will apply a flat field correction *IF* a correction matrix has been loaded
                        if (applyMask) cuda.ApplyMaskToGrayscaleImage();

                        cuda.Download_GrayscaleImage(out maskedFullGrayImage, acqParams.BinnedFullImageWidth, acqParams.BinnedFullImageHeight);

                        // calculate mask aperture sums
                        UInt32[] sums;
                        cuda.GetMaskApertureSums(out sums, mask.Rows, mask.Cols);

                        // update the GUI with the aperture sums
                       

                        // calculate the image histogram
                        UInt32[] histogram;
                        cuda.GetHistogram_512(out histogram, 16);
                        //histogram[0] = 0; // clear out the pixels that were zeroed, since they were outside the mask


                        if (dps.histBitmap != null)
                        {
                            // build the histogram image and download it to the CPU
                            byte[] histImage;
                            cuda.GetHistogramImage_512(out histImage, m_histogramImageWidth, m_histogramImageHeight, 0);

                            // display the histogram image
                            dps.histBitmap.Lock();
                            dps.histBitmap.WritePixels(histRect, histImage, m_histogramImageWidth * 4, 0);
                            dps.histBitmap.Unlock();
                        }

                        // convert the grayscale image to color using the colormap that is already on the GPU
                        IntPtr colorImageOnGpu = cuda.Convert_GrayscaleToColor(m_RangeSliderLowerSliderPosition, m_RangeSliderUpperSliderPosition);

                        // display color image
                        if (dps.ImageControl.m_imageBitmap != null)
                        {
                            byte[] colorImage;
                            m_cudaToolBox.Download_ColorImage(out colorImage, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight);

                            // display the image
                            Int32Rect displayRect = new Int32Rect(0, 0, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight);
                            dps.ImageControl.m_imageBitmap.Lock();
                            dps.ImageControl.m_imageBitmap.WritePixels(displayRect, colorImage, m_camera.m_acqParams.BinnedFullImageWidth * 4, 0);
                            dps.ImageControl.m_imageBitmap.Unlock();
                        }

                    }
                    t1 = sw.ElapsedMilliseconds;

                    return Tuple.Create<ushort[], int, int>(maskedFullGrayImage, expIndID, sequenceNumber);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
                finally
                {
                    // make sure the cuda context is released
                    //cuda.PopCudaContext();
                }
            },
                // Specify a task scheduler as that which is passed in
                // so that the action runs on the UI thread. 
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancelToken,
                TaskScheduler = uiTask,
                MaxDegreeOfParallelism = 1
            });



            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Write Image Data to Database/Image file

            var StoreData = new ActionBlock<Tuple<ushort[], int, int>>(inputData =>
            {
                // since this call Cuda and GUI functionality, it must be run on the UI Thread

                ushort[] grayImage = inputData.Item1;  // Raw Grayscale ROI image 
                int expIndID = inputData.Item2;  // Experiment Indicator ID
                int sequenceNumber = inputData.Item3; // number of msecs into experiment that image was taken

                Stopwatch sw = new Stopwatch();

                try
                {
                    sw.Restart();
                    // get FlatFieldCorrector for this experiment indicator
                    if (imagingDictionary.ContainsKey(expIndID))
                    {
                        // process image
                        ImagingParamsStruct dps = imagingDictionary[expIndID];

                        imageFileManager.WriteImageFile(grayImage, expIndID, sequenceNumber);

                    }

                    long t1 = sw.ElapsedMilliseconds;

                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            },
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancelToken,
                MaxDegreeOfParallelism = 1
            });


            CudaProcessAndDisplayImage.LinkTo(StoreData);



            return CudaProcessAndDisplayImage;
        }



        public void ConfigImageDisplaySurface(int ID, uint pixelWidth, uint pixelHeight, bool useAlphaChannel)
        {
            // ID = unique id for this display panel in the surface array.  It might be an ID for the camera, or an ID for an experiment indicator
            // pixelWidth, pixelHeight = the size of the display panel in pixels.  This should match the size of the image to be displaye on it.
            // panelHeader = string that is displayed above the panel
            // useAlphaChannel = sets whether to use the alpha channel or not (usually false)

            bool success = true;

            if (m_ImagingDictionary.ContainsKey(ID))
            {
                ImagingParamsStruct dps = m_ImagingDictionary[ID];

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        dps.ImageControl.SetImageSize((int)pixelWidth, (int)pixelHeight,GlobalVars.MaxPixelValue);
                        dps.ImageControl.ImageBox.Source = dps.ImageControl.m_imageBitmap;
                    });

                // rebuild mask
                m_mask.BuildPixelList(m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight, m_camera.m_acqParams.HBin, m_camera.m_acqParams.HBin);

                m_mask.BuildPixelMaskImage(m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight);

                m_cudaToolBox.Set_MaskImage(m_mask.PixelMaskImage, m_camera.m_acqParams.BinnedFullImageWidth,
                                            m_camera.m_acqParams.BinnedFullImageHeight, (UInt16)m_mask.Rows, (UInt16)m_mask.Cols);

                m_ImagingDictionary[ID] = dps;

                if (!success)
                {
                    OnImagerEvent(new ImagerEventArgs("Failed to Resize Image Panel", ImagerState.Error));
                }
            }
            else
            {
                OnImagerEvent(new ImagerEventArgs("Could not reset D3D Surface.  ID provided did not exist in Dicionary.", ImagerState.Error));
            }
        }






        ////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////

        private void CameraTempMonitorWorker(IProgress<TemperatureProgressReport> progress)  // this is run on a separate Task
        {
            m_cameraTempMonitorRunning = true;
            int t = 0;
            while (true)
            {
                if (m_camera.SystemInitialized)
                {
                    var ok = m_camera.GetCoolerTemp(ref t);
                    if (ok)
                    {
                        m_camera.CameraTemperature = t;

                        progress.Report(new TemperatureProgressReport(true, t));

                        if (m_cameraTemperatureCancelToken.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                    else
                    {
                        // camera did not successfully report temperature
                        progress.Report(new TemperatureProgressReport(false, t));
                    }
                }
                else
                {
                    // camera not initialized
                    progress.Report(new TemperatureProgressReport(false, 0));
                }
                Thread.Sleep(1000);
            }
            m_cameraTempMonitorRunning = false;
        }


        private void ReportCameraTemperature(TemperatureProgressReport progress)
        {
            OnCameraTemperatureEvent(new TemperatureEventArgs(progress.GoodReading, progress.Temperature));
        }



        ////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////








        public async void StartKineticImaging(int maxNumImages, bool saveImages = false, int projectID = 0, int plateID = 0, int experimentID = 0)
        {
            Task KineticImagingTask;

            if (m_kineticImagingON)
            {
                // a kinetic imaging task is already running.  Don't start another one.
                MessageBox.Show("A Imaging Task is already running.  Cannot start another imaging task until the active imaging task completes or is aborted.",
                                "Cannot Start Imaging Task", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                OnImagerEvent(new ImagerEventArgs("Kinetic Imaging Stopped", ImagerState.Idle));
            }
            else
            {
                m_camera.PrepForKineticImaging();

                m_kineticImagingON = true;
                m_cancelTokenSource = new CancellationTokenSource();

                OnImagerEvent(new ImagerEventArgs("Kinetic Imaging Started", ImagerState.Busy));

       
                KineticImagingTask = Task.Factory.StartNew(() => KineticImagingProducer(m_cancelTokenSource.Token, m_cancelTokenSource, maxNumImages, 
                                                                                        saveImages, projectID, plateID, experimentID), m_cancelTokenSource.Token);


                try
                {
                   
                    await KineticImagingTask;
                }
                catch (AggregateException aggEx)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Exception(s) occurred: ");
                    foreach (Exception ex in aggEx.InnerExceptions)
                    {
                        sb.Append(ex.Message);
                        sb.Append(", ");
                    }

                    m_kineticImagingON = false;
                    OnCameraEvent(new CameraEventArgs(sb.ToString(), false));
                }
                catch (OperationCanceledException)
                {
                    m_kineticImagingON = false;
                    OnCameraEvent(new CameraEventArgs("Imaging Cancelled", false));
                }
                catch (Exception ex)
                {
                    m_kineticImagingON = false;
                    OnCameraEvent(new CameraEventArgs(ex.Message, false));
                }
                finally
                {
                    KineticImagingTask.Dispose();
                    OnImagerEvent(new ImagerEventArgs("Kinetic Imaging Stopped", ImagerState.Idle));
                }
            }

            
        }

       
        public void StopKineticImaging()
        {
            // stop video threads
            m_camera.MyCamera.AbortAcquisition();
            m_cancelTokenSource.Cancel();
        }



        private void KineticImagingProducer(CancellationToken ct, CancellationTokenSource cts, int maxImagesToProduce, 
                                            bool saveImages, int projectID, int plateID, int experimentID)
        {
            // Kinetic Image Producer                        

            ushort[] newImage = null;
            int imageCount = 0;
            string errMsg = "No Error";
            bool abort = false;
            Stopwatch sw = new Stopwatch();
            Stopwatch acqTimer = new Stopwatch();
            long t1 = 0, t5 = 0, t6 = 0, t7 = 0, t8 = 0;
            int maxWaitDuration = 0;
            int indicatorIndex = 0;
            uint ecode;

            m_imagingSequenceCounter = sw;

            // log file is saved in same directory as the images
            string logFileName = GlobalVars.ImageFileSaveLocation + "\\" + projectID.ToString() + "\\" + plateID.ToString() + "\\" + experimentID.ToString() + "\\logfile.txt";
            Trace.Listeners.Clear();
            if (saveImages)
            {
                Trace.Listeners.Add(new TextWriterTraceListener(logFileName));
                Trace.TraceInformation(DateTime.Now.ToString());
                Trace.TraceInformation("Database Record ID's:");
                Trace.TraceInformation("ProjectID: " + projectID.ToString());
                Trace.TraceInformation("PlateID: " + plateID.ToString());
                Trace.TraceInformation("ExperimentID: " + experimentID.ToString());
                Trace.TraceInformation("");
                Trace.TraceInformation("Indicators:");
                int indNdx = 0;
                foreach (KeyValuePair<int, ImagingParamsStruct> entry in m_ImagingDictionary)
                {
                    indNdx++;
                    // do something with entry.Value or entry.Key
                    Trace.TraceInformation(indNdx.ToString() + " - " + entry.Value.indicatorName);
                    Trace.TraceInformation("Excitation Filter: " + entry.Value.excitationFilterPos.ToString());
                    Trace.TraceInformation("Emission Filter: " + entry.Value.emissionFilterPos.ToString());
                    Trace.TraceInformation("");
                }
            }

   
            ITargetBlock<Tuple<ushort[], int, int>> ImageProcessingPipeline = CreateImageProcessingPipeline(m_uiTask, ct, m_camera.m_acqParams,
                                                                                    m_ImagingDictionary,
                                                                                    m_mask, m_UseMask, saveImages, projectID, plateID, experimentID);


            // set camera into kinetic imaging mode
            m_camera.PrepForKineticImaging();

          
            // Set Ring Exposure Times
            List<float> expF = new List<float>();
            foreach (ImagingParamsStruct ips in m_ImagingDictionary.Values)
            {
                expF.Add(ips.exposure);
            }
            m_camera.MyCamera.SetRingExposureTimes(expF.Count, expF.ToArray());


            // Set up events used to signal from SDK
            // Define an array with two AutoResetEvent WaitHandles.
            WaitHandle[] eventHandle = new WaitHandle[] 
            {
                new AutoResetEvent(false),
                new AutoResetEvent(false)
            };
            m_camera.MyCamera.SetCameraStatusEnable(1);
            m_camera.MyCamera.SetAcqStatusEvent(eventHandle[0].SafeWaitHandle.DangerousGetHandle());
            m_camera.MyCamera.SetDriverEvent(eventHandle[1].SafeWaitHandle.DangerousGetHandle());


            // Move Filter to first position
            // TODO

            // Start the Acquisition
            m_camera.MyCamera.StartAcquisition();
           

            // build indicator list
            List<int> indicatorIDList = new List<int>();
            foreach (int key in m_ImagingDictionary.Keys) indicatorIDList.Add(key);
            

            indicatorIndex = 0;
            int currentIndicatorID = 0;
            int nextIndicatorID = indicatorIDList[indicatorIndex];  // get first indicator ID
            int cycleTime = 0;
            Task FilterChangeTask = null;
            
            // put system in starting state
            ChangeFilterPositions(m_ImagingDictionary[nextIndicatorID].excitationFilterPos, m_ImagingDictionary[nextIndicatorID].emissionFilterPos);

             // start experiment timer
            sw.Restart();

            
            do
            {
                try
                {
                    // set currentIndicatorID 
                    currentIndicatorID = nextIndicatorID;
                    // set nextIndicatorID
                    indicatorIndex++;
                    if (indicatorIndex == indicatorIDList.Count) indicatorIndex = 0;
                    nextIndicatorID = indicatorIDList[indicatorIndex];

                    // get data for current indicator  
                    cycleTime = m_ImagingDictionary[currentIndicatorID].cycleTime;
                    maxWaitDuration = cycleTime+10;  // max wait = cycleTime plus 10 msecs
                    
                    // start acquisition timer
                    acqTimer.Restart();

                    m_camera.MyCamera.SendSoftwareTrigger();

                    // wait for camera to finish the exposure
                    int eventNumber1 = WaitHandle.WaitAny(eventHandle, maxWaitDuration, false);  // eventNumber should be 0, unless timeout occurs (in that case, it's -1)                    
                    t5 = acqTimer.ElapsedMilliseconds;

                   
                    // if there are more than one indicators, then start prepping for next indicator here (i.e. filter changes)
                    // this is done in a background task from the thread pool
                    if(currentIndicatorID != nextIndicatorID)
                    {
                        // TODO:
                        // start Task to move filter to position for next indicator (i.e. move filter to position for nextIndicatorID),
                        // but only if the nextIndicatorID != currentIndicatorID
                        int excitationPosition = m_ImagingDictionary[nextIndicatorID].excitationFilterPos;
                        int emissionPosition = m_ImagingDictionary[nextIndicatorID].emissionFilterPos;
                        FilterChangeTask = Task.Factory.StartNew(() => ChangeFilterPositions(excitationPosition,emissionPosition));
                    }

                    
                    // wait for frame transfer to complete (i.e. data to be moved from image area to storage area)
                    int eventNumber2 = WaitHandle.WaitAny(eventHandle, maxWaitDuration*2, false);
                    t6 = acqTimer.ElapsedMilliseconds;


                    // now ready to read data off camera

                    // get data off camera                    
                    newImage = new ushort[m_camera.m_acqParams.BinnedRoiImageNumPixels];  // TotalPixels = number of pixels in the defined imaging ROI (binnedRoiW * binnedRoiH)                   
                    ecode = m_camera.MyCamera.GetOldestImage16(newImage, (uint)m_camera.m_acqParams.BinnedRoiImageNumPixels);
                    t7 = acqTimer.ElapsedMilliseconds;

                    if (m_camera.CheckCameraResult(ecode, ref errMsg))
                    {
                        // post image data to be processed (convert to full image, mask, flat field correct, calc histogram, build histogram image,
                        //                                  convert to color, display, calc well sums)

                        ImageProcessingPipeline.Post(Tuple.Create<ushort[], int, int>(newImage, currentIndicatorID, (int)sw.ElapsedMilliseconds));

                        imageCount++;
                                              
                        t8 = acqTimer.ElapsedMilliseconds;

                        // wait for cycle time to elapse
                        while (acqTimer.ElapsedMilliseconds < cycleTime)
                        {
                            Thread.Sleep(1);
                        }


                        // if there are more than one indicators, wait here until the Task started earlier completes first before moving on.
                        // Hopefully, it has already completed by this time.
                        if (currentIndicatorID != nextIndicatorID)
                        {
                            // TODO:
                            // wait for filter changing task to complete
                            bool filterChangeSucceeded = FilterChangeTask.Wait(2000, ct);
                            if(!filterChangeSucceeded)
                            {
                                // TODO: handle filter changer error
                                Trace.TraceError("Filter Change Error at " + sw.ElapsedMilliseconds.ToString() + " msecs into experiment");                                
                            }
                        }                       
                    }
                    else
                    {
                        m_camera.MyCamera.AbortAcquisition();
                        cts.Cancel();
                        abort = true;
                        Trace.TraceError("Failed to get data from camera (error code =  " + ecode.ToString() + ") "
                            + sw.ElapsedMilliseconds.ToString() + " msecs into experiment. Experiment Aborted.");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Add Loop Canceled
                    m_camera.MyCamera.AbortAcquisition();
                    abort = true;
                    Trace.TraceInformation("Experiment Cancelled at " + sw.ElapsedMilliseconds.ToString() + " msecs into experiment");
                    break;
                }

                if (ct.IsCancellationRequested)
                {
                    m_camera.MyCamera.AbortAcquisition();
                    abort = true;
                }

            } while (imageCount < maxImagesToProduce && !abort);

            sw.Stop();
     
            t1 = sw.ElapsedMilliseconds;
            m_camera.MyCamera.AbortAcquisition();

            m_imagingSequenceCounter = null;

            //float rate = (float)imageCount / (float)t1 * 1000;
            //MessageBox.Show("Frames: " + imageCount.ToString() + "\nMSecs: " + t1.ToString() + "\nRate(Hz): " + rate.ToString());

            Trace.TraceInformation("Images Recorded: "+ imageCount.ToString());
            Trace.TraceInformation("Complete: " + DateTime.Now.ToString());

            Trace.Listeners.Clear();

            m_kineticImagingON = false;

        }




        
        public async void StartVideo(int indicatorID, int maxNumImages)
        {
            Task VideoTask;

            if (m_kineticImagingON)
            {
                // a kinetic imaging task is already running.  Don't start another one.
                MessageBox.Show("A Imaging Task is already running.  Cannot start another imaging task until the active imaging task completes or is aborted.",
                                "Cannot Start Imaging Task", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                OnImagerEvent(new ImagerEventArgs("Kinetic Imaging Stopped", ImagerState.Idle));
            }
            else
            {


                m_camera.PrepForKineticImaging();

                m_kineticImagingON = true;
                m_cancelTokenSource = new CancellationTokenSource();

                OnImagerEvent(new ImagerEventArgs("Kinetic Imaging Started", ImagerState.Busy));

       
                VideoTask = Task.Factory.StartNew(() => VideoProducer(indicatorID, m_cancelTokenSource.Token, m_cancelTokenSource, maxNumImages), m_cancelTokenSource.Token);


                try
                {
                   
                    await VideoTask;
                }
                catch (AggregateException aggEx)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Exception(s) occurred: ");
                    foreach (Exception ex in aggEx.InnerExceptions)
                    {
                        sb.Append(ex.Message);
                        sb.Append(", ");
                    }

                    m_kineticImagingON = false;
                    OnCameraEvent(new CameraEventArgs(sb.ToString(), false));
                }
                catch (OperationCanceledException)
                {
                    m_kineticImagingON = false;
                    OnCameraEvent(new CameraEventArgs("Video Cancelled", false));
                }
                catch (Exception ex)
                {
                    m_kineticImagingON = false;
                    OnCameraEvent(new CameraEventArgs(ex.Message, false));
                }
                finally
                {
                    VideoTask.Dispose();
                    OnImagerEvent(new ImagerEventArgs("Video Stopped", ImagerState.Idle));
                }
            }

            
        }



        public void StopVideo()
        {
            // stop video threads
            m_camera.MyCamera.AbortAcquisition();
            m_cancelTokenSource.Cancel();
        }



        private void VideoProducer(int experimentIndicatorID, CancellationToken ct, CancellationTokenSource cts, int maxImagesToProduce)
        {
            // Kinetic Image Producer                        

            ushort[] newImage = null;
            int imageCount = 0;
            string errMsg = "No Error";
            bool abort = false;
            Stopwatch sw = new Stopwatch();
            Stopwatch acqTimer = new Stopwatch();
            long t1 = 0, t5 = 0, t6 = 0, t7 = 0, t8 = 0;
            int maxWaitDuration = 0;
            int indicatorIndex = 0;
            uint ecode;

            m_imagingSequenceCounter = sw;


            ITargetBlock<Tuple<ushort[], int, int>> ImageProcessingPipeline = CreateImageProcessingPipeline(m_uiTask, ct, m_camera.m_acqParams,
                                                                                    m_ImagingDictionary,
                                                                                    m_mask, m_UseMask, false, 0, 0, 0);


            // set camera into kinetic imaging mode
            m_camera.PrepForKineticImaging();


            // Set Ring Exposure Times
            ImagingParamsStruct ips;
            if(m_ImagingDictionary.ContainsKey(experimentIndicatorID))
            {
                m_camera.MyCamera.SetExposureTime(m_ImagingDictionary[experimentIndicatorID].exposure);
            }
            else
            {
                m_camera.MyCamera.SetExposureTime(0.020f);
            }


            // Set up events used to signal from SDK
            // Define an array with two AutoResetEvent WaitHandles.
            WaitHandle[] eventHandle = new WaitHandle[] 
            {
                new AutoResetEvent(false),
                new AutoResetEvent(false)
            };
            m_camera.MyCamera.SetCameraStatusEnable(1);
            m_camera.MyCamera.SetAcqStatusEvent(eventHandle[0].SafeWaitHandle.DangerousGetHandle());
            m_camera.MyCamera.SetDriverEvent(eventHandle[1].SafeWaitHandle.DangerousGetHandle());


            // Move Filter to first position
            // TODO

            // Start the Acquisition
            m_camera.MyCamera.StartAcquisition();


            // build indicator list
            List<int> indicatorIDList = new List<int>();
            indicatorIDList.Add(experimentIndicatorID);


            indicatorIndex = 0;
            int currentIndicatorID = 0;
            int nextIndicatorID = indicatorIDList[indicatorIndex];  // get first indicator ID
            int cycleTime = 0;
            Task FilterChangeTask = null;

            // put system in starting state
            ChangeFilterPositions(m_ImagingDictionary[nextIndicatorID].excitationFilterPos, m_ImagingDictionary[nextIndicatorID].emissionFilterPos);

            // start experiment timer
            sw.Restart();


            do
            {
                try
                {
                    // set currentIndicatorID 
                    currentIndicatorID = nextIndicatorID;
                    // set nextIndicatorID
                    indicatorIndex++;
                    if (indicatorIndex == indicatorIDList.Count) indicatorIndex = 0;
                    nextIndicatorID = indicatorIDList[indicatorIndex];

                    // get data for current indicator  
                    cycleTime = m_ImagingDictionary[currentIndicatorID].cycleTime;
                    maxWaitDuration = cycleTime + 10;  // max wait = cycleTime plus 10 msecs

                    // start acquisition timer
                    acqTimer.Restart();

                    m_camera.MyCamera.SendSoftwareTrigger();

                    // wait for camera to finish the exposure
                    int eventNumber1 = WaitHandle.WaitAny(eventHandle, maxWaitDuration, false);  // eventNumber should be 0, unless timeout occurs (in that case, it's -1)                    
                    t5 = acqTimer.ElapsedMilliseconds;


                    // if there are more than one indicators, then start prepping for next indicator here (i.e. filter changes)
                    // this is done in a background task from the thread pool
                    if (currentIndicatorID != nextIndicatorID)
                    {
                        // TODO:
                        // start Task to move filter to position for next indicator (i.e. move filter to position for nextIndicatorID),
                        // but only if the nextIndicatorID != currentIndicatorID
                        int excitationPosition = m_ImagingDictionary[nextIndicatorID].excitationFilterPos;
                        int emissionPosition = m_ImagingDictionary[nextIndicatorID].emissionFilterPos;
                        FilterChangeTask = Task.Factory.StartNew(() => ChangeFilterPositions(excitationPosition, emissionPosition));
                    }


                    // wait for frame transfer to complete (i.e. data to be moved from image area to storage area)
                    int eventNumber2 = WaitHandle.WaitAny(eventHandle, maxWaitDuration * 2, false);
                    t6 = acqTimer.ElapsedMilliseconds;


                    // now ready to read data off camera

                    // get data off camera                    
                    newImage = new ushort[m_camera.m_acqParams.BinnedRoiImageNumPixels];  // TotalPixels = number of pixels in the defined imaging ROI (binnedRoiW * binnedRoiH)                   
                    ecode = m_camera.MyCamera.GetOldestImage16(newImage, (uint)m_camera.m_acqParams.BinnedRoiImageNumPixels);
                    t7 = acqTimer.ElapsedMilliseconds;

                    if (m_camera.CheckCameraResult(ecode, ref errMsg))
                    {
                        // post image data to be processed (convert to full image, mask, flat field correct, calc histogram, build histogram image,
                        //                                  convert to color, display, calc well sums)

                        ImageProcessingPipeline.Post(Tuple.Create<ushort[], int, int>(newImage, currentIndicatorID, (int)sw.ElapsedMilliseconds));

                        imageCount++;

                        t8 = acqTimer.ElapsedMilliseconds;

                        // wait for cycle time to elapse
                        while (acqTimer.ElapsedMilliseconds < cycleTime)
                        {
                            Thread.Sleep(1);
                        }


                        // if there are more than one indicators, wait here until the Task started earlier completes first before moving on.
                        // Hopefully, it has already completed by this time.
                        if (currentIndicatorID != nextIndicatorID)
                        {
                            // TODO:
                            // wait for filter changing task to complete
                            bool filterChangeSucceeded = FilterChangeTask.Wait(2000, ct);
                            if (!filterChangeSucceeded)
                            {
                                // TODO: handle filter changer error
                                Trace.TraceError("Filter Change Error at " + sw.ElapsedMilliseconds.ToString() + " msecs into experiment");
                            }
                        }
                    }
                    else
                    {
                        m_camera.MyCamera.AbortAcquisition();
                        cts.Cancel();
                        abort = true;
                        Trace.TraceError("Failed to get data from camera (error code =  " + ecode.ToString() + ") "
                            + sw.ElapsedMilliseconds.ToString() + " msecs into experiment. Experiment Aborted.");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Add Loop Canceled
                    m_camera.MyCamera.AbortAcquisition();
                    abort = true;
                    Trace.TraceInformation("Experiment Cancelled at " + sw.ElapsedMilliseconds.ToString() + " msecs into experiment");
                    break;
                }

                if (ct.IsCancellationRequested)
                {
                    m_camera.MyCamera.AbortAcquisition();
                    abort = true;
                }

            } while (imageCount < maxImagesToProduce && !abort);

            sw.Stop();

            t1 = sw.ElapsedMilliseconds;
            m_camera.MyCamera.AbortAcquisition();

            m_imagingSequenceCounter = null;

            //float rate = (float)imageCount / (float)t1 * 1000;
            //MessageBox.Show("Frames: " + imageCount.ToString() + "\nMSecs: " + t1.ToString() + "\nRate(Hz): " + rate.ToString());

            Trace.TraceInformation("Images Recorded: " + imageCount.ToString());
            Trace.TraceInformation("Complete: " + DateTime.Now.ToString());

            Trace.Listeners.Clear();

            m_kineticImagingON = false;

        }





        public long GetImagingSequenceTime()
        {
            if (m_imagingSequenceCounter != null)
                return m_imagingSequenceCounter.ElapsedMilliseconds;
            else
                return 0;
        }


        private void ChangeFilterPositions(int excitationFilterPosition, int emissionFilterPosition)
        {
            // this function is used to change the filter positions

            Thread.Sleep(20);
        }



        #region Optimization
        /// ////////////////////////////////////////////////////////////////////////////////////
        ///  Optimization Routines



        public async void StartOptimization(int ID, CameraSettingsContainer cameraSettings) // bool increasingSignal, int startingExposure)
        {
            Tuple<bool, int> returnVal = Tuple.Create<bool,int>(false,0);
           
            if (m_kineticImagingON)
            {
                // a kinetic imaging task is already running.  Don't start another one.
                MessageBox.Show("A Imaging Task is already running.  Cannot start optimization until the active imaging task completes or is aborted.",
                                "Cannot Start Optimization Task", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                OnImagerEvent(new ImagerEventArgs("Optimization Stopped", ImagerState.Idle));
            }
            else
            {
                m_camera.PrepForKineticImaging();

                m_kineticImagingON = true;
                m_cancelTokenSource = new CancellationTokenSource();

                OnImagerEvent(new ImagerEventArgs("Kinetic Imaging Started", ImagerState.Busy));

                try
                {                    
                    returnVal = await Task.Run(() => OptimizeImaging(ID, cameraSettings), m_cancelTokenSource.Token);
                }
                catch (AggregateException aggEx)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Exception(s) occurred: ");
                    foreach (Exception ex in aggEx.InnerExceptions)
                    {
                        sb.Append(ex.Message);
                        sb.Append(", ");
                    }

                    m_kineticImagingON = false;
                    OnCameraEvent(new CameraEventArgs(sb.ToString(), false));
                }
                catch (OperationCanceledException)
                {
                    m_kineticImagingON = false;
                    OnCameraEvent(new CameraEventArgs("Optimization Cancelled", false));
                }
                catch (Exception ex)
                {
                    m_kineticImagingON = false;
                    OnCameraEvent(new CameraEventArgs(ex.Message, false));
                }
                finally
                {                   
                    OnImagerEvent(new ImagerEventArgs("Optimizing Stopped", ImagerState.Idle));
                    OptimizationResult_Success = returnVal.Item1;
                    OptimizationResult_Exposure = returnVal.Item2;
                }
            }           
        }


        public void StopOptimization()
        {
            // stop video threads
            m_camera.MyCamera.AbortAcquisition();
            m_cancelTokenSource.Cancel();     
        }




        public Tuple<bool,int> OptimizeImaging(int ID, CameraSettingsContainer cameraSettings) // bool forIncreasingSignal, int startingExposure)
        {
            bool success = false;

            int startingExposure;
            int exposureLimit;
            UInt16 highPixelValueThreshold;
            int minPercentOfPixelsAboveLowLimit;
            UInt16 lowPixelValueThreshold;
            int maxPercentOfPixelsAboveHighLimit;
            int tempEMGain = 1;  // turn EM Gain all the way down
            int tempPreAmpIndex = 0; // turn PreAmpGain all the way down

            if (cameraSettings == null)
            {
                startingExposure = 1;
                exposureLimit = 1000;
                highPixelValueThreshold = (UInt16)(0.8 * (float)GlobalVars.MaxPixelValue);
                minPercentOfPixelsAboveLowLimit = 50;
                maxPercentOfPixelsAboveHighLimit = 10;
                lowPixelValueThreshold = (UInt16)(0.1 * (float)GlobalVars.MaxPixelValue);               
            }
            else
            {
                startingExposure = cameraSettings.StartingExposure;
                exposureLimit = cameraSettings.ExposureLimit;
                highPixelValueThreshold = (UInt16)( ((float)cameraSettings.HighPixelThresholdPercent)/100.0f * ((float)GlobalVars.MaxPixelValue)  );
                minPercentOfPixelsAboveLowLimit = cameraSettings.MinPercentPixelsAboveLowThreshold;
                lowPixelValueThreshold = (UInt16)(((float)cameraSettings.LowPixelThresholdPercent) / 100.0f * ((float)GlobalVars.MaxPixelValue)); ;
                maxPercentOfPixelsAboveHighLimit = cameraSettings.MaxPercentPixelsAboveHighThreshold;                
            }

            bool Done = false;
            string errMsg = "No Error";
            int count = 0;
            bool tooDim = false;
            bool tooBright = false;


            m_camera.SetCameraBinning(1, 1);
            success = m_camera.PrepareAcquisition();

            // reset DirectX display panel for new size of image
            uint pixelWidth = (uint)(m_camera.XPixels / m_camera.m_acqParams.HBin);
            uint pixelHeight = (uint)(m_camera.YPixels / m_camera.m_acqParams.VBin);

            ConfigImageDisplaySurface((int)ID, pixelWidth, pixelHeight, false);

            ImagingParamsStruct dps = m_ImagingDictionary[(int)ID];

            ushort[] grayRoiImage = new ushort[m_camera.m_acqParams.BinnedRoiImageNumPixels];
            ushort[] grayFullImage = new ushort[m_camera.m_acqParams.BinnedFullImageNumPixels];

            int exposure = startingExposure;

            // Initialize camera to starting condition            
            m_camera.m_cameraParams.EMGain = 1;
            m_camera.m_cameraParams.HSSIndex = cameraSettings.HSSIndex;
            m_camera.m_cameraParams.PreAmpGainIndex = 0;
            m_camera.m_cameraParams.UseEMAmp = cameraSettings.UseEMAmp;
            m_camera.m_cameraParams.UseFrameTransfer = cameraSettings.UseFrameTransfer;
            m_camera.m_cameraParams.VertClockAmpIndex = cameraSettings.VertClockAmpIndex;
            m_camera.m_cameraParams.VSSIndex = cameraSettings.VSSIndex;
            
            success = m_camera.ConfigureCamera();

            // get well list to use for optimization
            ObservableCollection<Tuple<int, int>> wellsToOptimizeOver = null;
            if (m_ImagingDictionary.ContainsKey((int)ID))
            {
                wellsToOptimizeOver = m_ImagingDictionary[(int)ID].optimizeWellList;
            }
            

            OnImagerEvent(new ImagerEventArgs("Optimization in Progress", ImagerState.Busy));

            OptimizationResult_Success = false;

            while (!Done)
            {
                // acquire image
                if (m_camera.CheckCameraResult(m_camera.AcquireImage(exposure, ref grayRoiImage), ref errMsg))
                {
                    // Post to GPU (which will also convert ROI to full image)
                    m_cudaToolBox.PostRoiGrayscaleImage(grayRoiImage, m_camera.m_acqParams.BinnedFullImageWidth,
                                                        m_camera.m_acqParams.BinnedFullImageHeight,
                                                        m_camera.m_acqParams.BinnedRoiW, m_camera.m_acqParams.BinnedRoiH,
                                                        m_camera.m_acqParams.BinnedRoiX, m_camera.m_acqParams.BinnedRoiY);

                    // Get the full grayscale image for brightness evaluation                    
                    m_cudaToolBox.Download_GrayscaleImage(out grayFullImage, m_camera.m_acqParams.BinnedFullImageWidth,
                                                                    m_camera.m_acqParams.BinnedFullImageHeight);


                    // process and display
                    // apply mask
                    m_cudaToolBox.ApplyMaskToGrayscaleImage();

                    // convert to color
                    IntPtr colorImageOnGpu = m_cudaToolBox.Convert_GrayscaleToColor(m_RangeSliderLowerSliderPosition, m_RangeSliderUpperSliderPosition);

           
                    // check brightness levels
                    m_mask.CheckImageLevelsInMask(grayFullImage, wellsToOptimizeOver, minPercentOfPixelsAboveLowLimit, lowPixelValueThreshold,
                           maxPercentOfPixelsAboveHighLimit, highPixelValueThreshold, ref tooDim, ref tooBright);

                    if (tooBright)
                    {
                        int hbin = m_camera.m_acqParams.HBin;
                        int vbin = m_camera.m_acqParams.VBin;
                        if (cameraSettings.UseEMAmp)
                        {
                            if (DecreaseEMGain(ref tempEMGain))
                            {
                                // successfully decreased em gain
                                m_camera.m_cameraParams.EMGain = tempEMGain;
                                success = m_camera.ConfigureCamera();
                                if (!success)
                                {  // failed to camera config
                                    Done = true;
                                }
                            }
                        }
                        else if (DecreaseExposure(ref exposure))
                        {
                            // successfully decreased exposure
                            success = m_camera.PrepareAcquisition();
                            if (!success)
                            {  // failed to prepare acquisition
                                Done = true;
                            }
                        }
                        else if (DecreaseBinning(ref hbin, ref vbin))
                        {
                            m_camera.m_acqParams.HBin = hbin;
                            m_camera.m_acqParams.VBin = vbin;
                            // successfully decreased binning, so make adjustments due to binning change                          
                            success = m_camera.PrepareAcquisition(m_camera.m_acqParams);
                            // reset display panel for new size of image                           
                            ConfigImageDisplaySurface((int)ID, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight, false);

                            if (!success)
                            {  // failed to prepare acquisition
                                Done = true;
                            }
                        }
                        else if (DecreasePreAmpGain(ref tempPreAmpIndex))
                        {
                            m_camera.m_cameraParams.PreAmpGainIndex = tempPreAmpIndex;
                            success = m_camera.ConfigureCamera(m_camera.m_cameraParams);
                            if (!success)
                            {  // failed to camera config
                                Done = true;
                            }
                        }
                        else
                        {
                            success = false;
                            Done = true;
                        }
                    }
                    else if (tooDim)
                    {
                        int hbin = m_camera.m_acqParams.HBin;
                        int vbin = m_camera.m_acqParams.VBin;

                        if(IncreasePreAmpGain(ref tempPreAmpIndex))
                        {
                            m_camera.m_cameraParams.PreAmpGainIndex = tempPreAmpIndex;
                            success = m_camera.ConfigureCamera(m_camera.m_cameraParams);
                            if (!success)
                            {  // failed to camera config
                                Done = true;
                            }
                        }
                        else if (IncreaseBinning(ref hbin, ref vbin))
                        {
                            m_camera.m_acqParams.HBin = hbin;
                            m_camera.m_acqParams.VBin = vbin;
                            // successfully increased binning, so make adjustments due to binning change
                            success = m_camera.PrepareAcquisition(m_camera.m_acqParams);
                            // reset display panel for new size of image
                            ConfigImageDisplaySurface((int)ID, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight, false);

                            if (!success)
                            {  // failed to prepare acquisition
                                Done = true;
                            }
                        }
                        else if (IncreaseExposure(ref exposure, exposureLimit))
                        {
                            // successfully decreased exposure
                            success = m_camera.PrepareAcquisition(m_camera.m_acqParams);
                            if (!success)
                            {  // failed to prepare acquisition
                                Done = true;
                            }
                        }
                        else if (cameraSettings.UseEMAmp)                             
                        {
                            if (IncreaseEMGain(ref tempEMGain, cameraSettings.EMGainLimit))
                            {
                                // successfully increased gain
                                m_camera.m_cameraParams.EMGain = tempEMGain;                                
                                success = m_camera.ConfigureCamera(m_camera.m_cameraParams);
                                if (!success)
                                {  // failed to camera config
                                    Done = true;
                                }
                            }
                        }                        
                        else
                        {
                            success = false;
                            Done = true;
                        }
                    }
                    else // not tooDim AND not tooBright, i.e. inside defined brightness window                         
                    {
                        // SUCCESS!!
                        success = true;
                        Done = true;
                        OptimizationResult_Success = true;
                        OptimizationResult_Exposure = exposure;  
                      
                        // Update Imaging Dictionary
                        dps.exposure = ((float)exposure)/1000;
                        if (dps.cycleTime < (exposure + 10)) dps.cycleTime = exposure + 10;
                        dps.gain = m_camera.m_cameraParams.EMGain;
                        dps.preAmpGainIndex = m_camera.m_cameraParams.PreAmpGainIndex;
                        dps.binning = m_camera.m_acqParams.HBin;

                        //m_ImagingDictionary[(int)ID] = dps;                        
                    }
                }
                else
                {
                    MessageBox.Show("Error Acquiring Image: " + errMsg);
                    success = false;
                    Done = true;
                }


                count++;
             
                if (count > 100)
                {
                    success = false;
                    Done = true;
                }
            }

            OnImagerEvent(new ImagerEventArgs("Optimization Complete", ImagerState.Idle));
        
            return Tuple.Create<bool,int>(success,exposure);
        }


        public bool ChangeROISizeToBeCompatibleWithBinning(int hbin, int vbin)
        {
            int roiX = m_camera.m_acqParams.RoiX;
            int roiY = m_camera.m_acqParams.RoiY;
            int roiW = m_camera.m_acqParams.RoiW;
            int roiH = m_camera.m_acqParams.RoiH;

            m_camera.VerifyROISize(ref roiX, ref roiY, ref roiW, ref roiH, hbin, vbin);

            bool changed = false;

            if (m_camera.m_acqParams.RoiX != roiX || m_camera.m_acqParams.RoiY != roiY || m_camera.m_acqParams.RoiW != roiW || m_camera.m_acqParams.RoiH != roiH)
            {
                changed = true;
                m_camera.m_acqParams.RoiX = roiX;
                m_camera.m_acqParams.RoiY = roiY;
                m_camera.m_acqParams.RoiW = roiW;
                m_camera.m_acqParams.RoiH = roiH;
            }

            return changed;
        }

        public bool DecreaseBinning(ref int hBinning, ref int vBinning)
        {
            bool changed = false;

            switch (hBinning)
            {
                case 8: hBinning = 4;
                    changed = true;
                    break;
                case 4: hBinning = 2;
                    changed = true;
                    break;
                case 2: hBinning = 1;
                    changed = true;
                    break;
                default:
                    hBinning = 1;
                    break;
            }

            switch (vBinning)
            {
                case 8: vBinning = 4;
                    changed = true;
                    break;
                case 4: vBinning = 2;
                    changed = true;
                    break;
                case 2: vBinning = 1;
                    changed = true;
                    break;
                default:
                    vBinning = 1;
                    break;
            }

            if (ChangeROISizeToBeCompatibleWithBinning(hBinning, vBinning))
            {
                // make sure to call Camera.PrepareAcquisition(...) to set new ROI
            }

            return changed;
        }

        public bool IncreaseBinning(ref int hBinning, ref int vBinning)
        {
            bool changed = false;

            switch (hBinning)
            {
                //case 4: hBinning = 8;
                //    changed = true;
                //    break;
                case 2: hBinning = 4;
                    changed = true;
                    break;
                case 1: hBinning = 2;
                    changed = true;
                    break;
                default:
                    hBinning = 4;
                    break;
            }

            switch (vBinning)
            {
                //case 4: vBinning = 8;
                //    changed = true;
                //    break;
                case 2: vBinning = 4;
                    changed = true;
                    break;
                case 1: vBinning = 2;
                    changed = true;
                    break;
                default:
                    vBinning = 4;
                    break;
            }

            if (ChangeROISizeToBeCompatibleWithBinning(hBinning, vBinning))
            {
                // make sure to call Camera.PrepareAcquisition(...) to set new ROI
            }

            return changed;
        }

        public bool IncreaseEMGain(ref int emGain, int emGainLimit)
        {
            bool changed = false;
            
                if (emGain < emGainLimit)
                {  // EM Amp
                    changed = true;
                    float step = ((float)emGain) * 1.5f;  // increase by 50%
                    int stepInt = (int)step;
                    if (stepInt < 1) stepInt = 1;

                    emGain += stepInt;

                    if (emGain > emGainLimit) emGain = emGainLimit;
                }              

            return changed;
        }

        public bool IncreasePreAmpGain(ref int preAmpGainIndex)
        {
            bool changed = false;
            if (preAmpGainIndex < 1)
            {
                preAmpGainIndex = 1;
                changed = true;
            }
            return changed;
        }

        public bool DecreaseEMGain(ref int emGain)
        {
            bool changed = false;

            // try to reduce emGain first           
                if (emGain > 1)
                {
                    changed = true;
                    float step = ((float)emGain) * 1.5f;  // decrease by 50%
                    int stepInt = (int)step;
                    if (stepInt < 1) stepInt = 1;

                    emGain -= stepInt;

                    if (emGain < 1) emGain = 1;
                }           
     
            return changed;
        }

        public bool DecreasePreAmpGain(ref int preAmpGainIndex)
        {
            bool changed = false;
            if (preAmpGainIndex > 0)
            {
                preAmpGainIndex = 0;
                changed = true;
            }
            return changed;
        }


        public bool IncreaseExposure(ref int exposure, int limit)
        {
            bool changed = false;

            if (exposure < limit)
            {
                changed = true;
                float step = ((float)exposure) * 1.5f;  // increase by 50%
                int stepInt = (int)step;
                if (stepInt < 1) stepInt = 1;

                exposure += stepInt;
                if (exposure > limit) exposure = limit;
            }

            return changed;
        }

        public bool DecreaseExposure(ref int exposure)
        {
            bool changed = false;

            if (exposure > 1)
            {
                changed = true;
                float step = ((float)exposure) * 1.5f;  // increase by 50%
                int stepInt = (int)step;
                if (stepInt < 1) stepInt = 1;

                exposure -= stepInt;
                if (exposure < 1) exposure = 1;
            }

            return changed;
        }

        #endregion


        #region Start Imaging

        ////////////////////////////////////////////////////////////////////////////////////////////



        //public async void StartImaging(ImagingParameters iParams,
        //    Tuple<MaskContainer, ObservableCollection<Tuple<int, int>>, int, int, int> analysisParams,
        //    CancellationToken cancelToken, TaskScheduler uiTask,
        //    Progress<int> progress, ImageDisplay imageDisplay, WG_Color[] colorMap,
        //    Histogram histogram, ChartArray chartArray = null)
        //{
        //    // the imageDisplay and chartArray parameters are a little confusing.  You either supply one or the other.  If you have a chartArray that the images
        //    // are displayed to, then supply chartArray and set imageDisplay = null.  If you're not using a chartArray, but simply want to display to 
        //    // and ImageDisplay, then supply an imageDisplay and set chartArray = null;


        //    bool saveImages = false;
        //    int imageCount = 0;


        //    if (iParams.ExperimentIndicatorID != null)
        //        if (iParams.ExperimentIndicatorID.Length > 0)
        //            if (iParams.ExperimentIndicatorID[0] > 0)
        //                saveImages = true;

        //    ITargetBlock<Tuple<ushort[], int, int, int, WG_Color[]>> displayPipeline;

        //    Dictionary<int, FLATFIELD_SELECT> ffcDictionary = new Dictionary<int, FLATFIELD_SELECT>();

        //    if (chartArray == null && imageDisplay != null)// ImageDisplay Supplied
        //    {
        //        // create a hashtable for the image display
        //        Dictionary<int, ImageDisplay> idDictionary = new Dictionary<int, ImageDisplay>();
        //        idDictionary.Add(iParams.ExperimentIndicatorID[0], imageDisplay);

        //        // create a hashtable for the flat field select                
        //        ffcDictionary.Add(iParams.ExperimentIndicatorID[0], iParams.FlatFieldSelect[0]);

        //        displayPipeline = CreateDisplayPipeline(uiTask, idDictionary, iParams.HorzBinning, iParams.VertBinning, ffcDictionary);
        //    }
        //    else if (chartArray != null)  // ChartArray supplied
        //    {
        //        Dictionary<int, ImageDisplay> idDictionary = chartArray.GetImageDisplayDictionary();

        //        // create a hashtable for the flat field select                
        //        Dictionary<int, ExperimentIndicatorContainer> expIndDictionary = chartArray.GetExperimentIndicatorDictionary();
        //        foreach (KeyValuePair<int, ExperimentIndicatorContainer> entry in expIndDictionary)
        //        {
        //            int expIndID = entry.Key;
        //            ExperimentIndicatorContainer expIndCont = entry.Value;
        //            ffcDictionary.Add(expIndID, expIndCont.FlatFieldCorrection);
        //        }

        //        displayPipeline = CreateDisplayPipeline(uiTask, idDictionary, iParams.HorzBinning, iParams.VertBinning, ffcDictionary);

        //    }
        //    else  // neither an ImageDisplay or ChartArray were supplied
        //        return;



        //    ITargetBlock<Tuple<ushort[], int, int, int>> storagePipeline = null;

        //    ITargetBlock<Tuple<ushort[], int>> histogramPipeline = null;

        //    ITargetBlock<Tuple<ushort[], int, int>> analysisPipeline = null;

        //    if (saveImages)
        //    {
        //        storagePipeline = CreateImageStoragePipeline(GlobalVars.CompressionAlgorithm, iParams.imageWidth, iParams.imageHeight);
        //    }

        //    if (chartArray != null && analysisParams != null)
        //    {
        //        MaskContainer mask = analysisParams.Item1;
        //        ObservableCollection<Tuple<int, int>> controlWells = analysisParams.Item2;
        //        int numFoFrames = analysisParams.Item3;
        //        int dynamicRatioNumeratorID = analysisParams.Item4;
        //        int dynamicRatioDenominatorID = analysisParams.Item5;

        //        analysisPipeline = CreateAnalysisPipeline(chartArray, mask, iParams.imageWidth,
        //            iParams.imageHeight, iParams.HorzBinning, iParams.VertBinning,
        //            iParams.ExperimentIndicatorID, controlWells, numFoFrames,
        //            dynamicRatioNumeratorID, dynamicRatioDenominatorID, iParams.PixelMask);
        //    }

        //    if (histogram != null)
        //    {
        //        histogramPipeline = CreateHistogramPipeline(uiTask, histogram, iParams.HorzBinning, iParams.VertBinning, ffcDictionary);
        //    }

        //    Task<int> ImagingTask;

        //    if (iParams.NumIndicators == 1 && iParams.CycleTime[0] < 300)
        //    {
        //        /// Start Fast Imaging Task 
        //        ImagingTask = Task.Factory.StartNew<int>(() => ImageReader_worker(iParams,
        //            progress,
        //            cancelToken,
        //            colorMap,
        //            displayPipeline,
        //            storagePipeline,
        //            histogramPipeline,
        //            analysisPipeline),
        //            cancelToken);
        //    }
        //    else
        //    {

        //        /// Start Normal Imaging Task 
        //        ImagingTask = Task.Factory.StartNew<int>(() => ImageReader_worker(iParams,
        //            progress,
        //            cancelToken,
        //            colorMap,
        //            displayPipeline,
        //            storagePipeline,
        //            histogramPipeline,
        //            analysisPipeline),
        //            cancelToken);
        //    }

        //    try
        //    {
        //        imageCount = await ImagingTask;
        //    }
        //    catch (AggregateException aggEx)
        //    {
        //        StringBuilder sb = new StringBuilder();
        //        sb.Append("Exception(s) occurred: ");
        //        foreach (Exception ex in aggEx.InnerExceptions)
        //        {
        //            sb.Append(ex.Message);
        //            sb.Append(", ");
        //        }

        //        m_imagerRunning = false;
        //        OnCameraEvent(new CameraEventArgs(sb.ToString(), false));
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        m_imagerRunning = false;
        //        OnCameraEvent(new CameraEventArgs("Imaging Cancelled", false));
        //    }
        //    catch (Exception ex)
        //    {
        //        m_imagerRunning = false;
        //        OnCameraEvent(new CameraEventArgs(ex.Message, false));
        //    }
        //    finally
        //    {
        //        ImagingTask.Dispose();
        //    }

        //    ((IProgress<int>)progress).Report(imageCount);
        //}



        #endregion


        #region Image Storage Pipeline


        public ITargetBlock<Tuple<ushort[], int, int, int>> CreateImageStoragePipeline(int projectID, int plateID, int experimentID, COMPRESSION_ALGORITHM compAlgorithm)
        {
            int m_projectID = projectID;
            int m_plateID = plateID;
            int m_experimentID = experimentID;
            COMPRESSION_ALGORITHM m_compAlgorithm = compAlgorithm;
            int m_maxPixelValue = GlobalVars.MaxPixelValue;

            string m_baseFilePath = GlobalVars.ImageFileSaveLocation + "\\" + m_projectID.ToString() + "\\" + m_plateID.ToString() + "\\" +
                                    m_experimentID.ToString() + "\\";

            WaveguideDB m_wgDB = new WaveguideDB();


            // input: grayimage (ushort[]), ExperimentIndicatorID (int), time (int), exposure (int)            
            var storeImage = new ActionBlock<Tuple<ushort[], int, int, int>>(inputData =>
            {
                ushort[] grayImage = inputData.Item1;
                int expIndicatorID = inputData.Item2;
                int time = inputData.Item3;

                try
                {
                    ExperimentImageContainer expImage = new ExperimentImageContainer();

                    expImage.CompressionAlgorithm = m_compAlgorithm;
                    expImage.ExperimentIndicatorID = expIndicatorID;
                    expImage.ImageData = grayImage;
                    expImage.MaxPixelValue = GlobalVars.MaxPixelValue;
                    expImage.MSecs = time;

                    expImage.TimeStamp = DateTime.Now;

                    expImage.FilePath = m_baseFilePath + expIndicatorID.ToString() + "\\" + expImage.MSecs.ToString("D8") + "_wgi.zip";

                    bool success = m_wgDB.InsertExperimentImage(ref expImage);

                    if (success)
                    {
                        // write image file to disk
                        try
                        {
                            Zip.Compress_File(grayImage, expImage.FilePath);
                        }
                        catch (Exception e)
                        {
                            string errMsg = e.Message;
                        }
                    }
                }
                catch (OperationCanceledException)
                {

                }
            },
               new ExecutionDataflowBlockOptions
               {
                   MaxDegreeOfParallelism = 8
               });


            // return head of storage pipeline
            return storeImage;

        }






        #endregion

        



        #region Analysis Pipeline

        public ITargetBlock<Tuple<ushort[], int, int>> CreateAnalysisPipeline(ChartArray chartArray,
                 MaskContainer mask, int pixelWidth, int pixelHeight, int hBinning, int vBinning,
                 int[] indicatorIdList, ObservableCollection<Tuple<int, int>> controlWells,
                 int numFoFrames, int dynamicRatioNumeratorID, int dynamicRatioDenominatorID)
        {
            // perform pre-processing of mask.  This creates a 2D array with an array element for each mask aperture.  The idea
            // is that for each aperture, create an array of the pixels inside that aperture.  This is only done once, when the 
            // AnalysisPipeline is created.  Thus all that is required to find the sum of pixels within an aperture is to go to
            // that aperture's array of pixels and get the value for each one of those pixels, adding to the sum.  
            //
            // int pixelList[mask.rows, mask.cols][numPixels] will contain the pixel list for each aperture.
            //
            // for example, to sum all pixels in aperture [1,1], you would do this:
            //
            //     foreach(int ndx in pixelList[1,1]) sum[1,1] += grayImage[ndx];
            //
            //          NOTE: grayImage is the raw image from the camera and is a 1D array

            // now...preprocess the mask, i.e. create pixelList[mask.rows,mask.cols][numPixels]

            mask.BuildPixelList(pixelWidth, pixelHeight, hBinning, vBinning);

            TaskScheduler m_chartArrayTask = chartArray.GetTaskScheduler();

            Hashtable m_Fo_Hash = new Hashtable();
            Hashtable m_FoCount_Hash = new Hashtable();
            Hashtable m_FoReady_Hash = new Hashtable();
            Hashtable m_analysis_Hash = new Hashtable();
            Hashtable m_pixelMask_Hash = new Hashtable();

            int m_dynamicRatioNumeratorID = dynamicRatioNumeratorID;
            int m_dynamicRatioDenominatorID = dynamicRatioDenominatorID;
            bool m_dynamicRatioNumeratorReady = false;
            bool m_dynamicRatioDenominatorReady = false;
            float[,] m_dynamicRatioNumeratorValues = new float[mask.Rows, mask.Cols];
            float[,] m_dynamicRatioDenominatorValues = new float[mask.Rows, mask.Cols];

            WaveguideDB wgDB = new WaveguideDB();

            bool success;

            ExperimentIndicatorContainer indicator;
            ushort[] Flat;
            ushort[] Dark;
            int flatID = 0;
            int darkID = 0;

            int cnt = 0;

            foreach (int expIndID in indicatorIdList)
            {
                //////////////////////////////////////////////////////////////////////////////
                // Calculate Fo for this experiment indicator
                float[,] Fo = new float[mask.Rows, mask.Cols];
                for (int r = 0; r < mask.Rows; r++)
                    for (int c = 0; c < mask.Cols; c++)
                    {
                        Fo[r, c] = 0.0f;
                    }
                m_Fo_Hash.Add(expIndID, Fo);

                int Fo_count = 0;
                m_FoCount_Hash.Add(expIndID, Fo_count);

                bool Fo_ready = false;
                m_FoReady_Hash.Add(expIndID, Fo_ready);

                AnalysisContainer anal = new AnalysisContainer();
                anal.Description = "PixelSum";
                anal.ExperimentIndicatorID = expIndID;
                anal.TimeStamp = DateTime.Now;
                anal.RuntimeAnalysis = true;
                wgDB.InsertAnalysis(ref anal);
                m_analysis_Hash.Add(expIndID, anal);

                cnt++;
            }


            // calculate the sum of all pixel values for each mask aperture.  These sums are 
            // stored in a array of mask.rows x mask.cols. 
            //      input: grayimage (ushort[]), ExperimentIndicatorID (int), time (int)
            //      output: array of raw pixel sums for each mask aperture (float[,]), ExperimentIndicatorID (int)
            //              and time (int)
            var calculateApertureSums = new TransformBlock<Tuple<ushort[], int, int>, Tuple<float[,], int, int>>(inputData =>
            {
                ushort[] grayImage = inputData.Item1;
                int expIndicatorID = inputData.Item2;
                int time = inputData.Item3;

                try
                {
                    // sum all pixels in pixelList
                    float[,] F = new float[mask.Rows, mask.Cols];

                    for (int r = 0; r < mask.Rows; r++)
                        for (int c = 0; c < mask.Cols; c++)
                        {
                            F[r, c] = 0;

                            int pixelCount = 0;

                            // calculate sum of pixels inside mask aperture[r,c]
                            foreach (int ndx in mask.PixelList[r, c])
                            {                              
                                F[r, c] += grayImage[ndx];
                                pixelCount++;                              
                            }

                            if (pixelCount > 0)
                                F[r, c] = F[r, c] / pixelCount;
                        }

                    return Tuple.Create(F, expIndicatorID, time);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            },
               new ExecutionDataflowBlockOptions
               {
                   MaxDegreeOfParallelism = 1
               });



            // calculate Static Ratio: F/Fo for each mask aperture.  Fo is the average of the 
            // first N frames for each mask aperture, and F is the raw pixel sum from each 
            // mask aperture.  N is set to numFoFrames, which is passed in during the creation
            // of this pipeline.
            //      input: F array (float[,]), ExperimentIndicatorID (int), and time (int)
            //      output: F array (float[,]), F/Fo array (float[,]), ExperimentIndicatorID (int),
            //              and time (int)
            var calculateStaticRatio = new TransformBlock<Tuple<float[,], int, int>, Tuple<float[,], float[,], int, int>>(inputData =>
            {
                float[,] F = inputData.Item1;
                int expIndicatorID = inputData.Item2;
                int time = inputData.Item3;

                // retrieve data relevant to this ExperimentIndicatore
                float[,] Fo = (float[,])m_Fo_Hash[expIndicatorID];
                int FoCount = (int)m_FoCount_Hash[expIndicatorID];
                bool FoReady = (bool)m_FoReady_Hash[expIndicatorID];

                float[,] staticRatio = null;

                try
                {
                    if (FoReady)
                    {
                        staticRatio = new float[mask.Rows, mask.Cols];
                        for (int r = 0; r < mask.Rows; r++)
                            for (int c = 0; c < mask.Cols; c++)
                            {
                                staticRatio[r, c] = F[r, c] / Fo[r, c];
                            }
                    }
                    else
                    {
                        for (int r = 0; r < mask.Rows; r++)
                            for (int c = 0; c < mask.Cols; c++)
                            {
                                Fo[r, c] += F[r, c];
                            }
                        FoCount++;
                        m_FoCount_Hash[expIndicatorID] = FoCount; // update Hashtable

                        if (FoCount >= numFoFrames)
                        {
                            FoReady = true;
                            m_FoReady_Hash[expIndicatorID] = FoReady; // update Hashtable

                            for (int r = 0; r < mask.Rows; r++)
                                for (int c = 0; c < mask.Cols; c++)
                                {
                                    Fo[r, c] /= numFoFrames;
                                }
                        }

                        staticRatio = null;
                    }

                    return Tuple.Create(F, staticRatio, expIndicatorID, time);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            },
                // Specify a task scheduler as that which is passed in
                // so that the action runs on the UI thread. 
               new ExecutionDataflowBlockOptions
               {
                   MaxDegreeOfParallelism = 1
               });







            // calculate Control Subtraction: for a selected group of wells (mask apertures), designated
            // as "control wells", find the average of the static ratio for those wells and subtract 
            // it from the static ratio of each individual mask aperture.
            //      input: F array (float[,]), F/Fo array (float[,]), ExperimentIndicatorID (int),
            //             and time(int)
            //      output: F array (float[,]), F/Fo array aka "static Ratio" (float[,]), 
            //              control subtraction array (float[,]), and ExperimentIndicatorID (int)
            var calculateControlSubtraction = new TransformBlock<Tuple<float[,], float[,], int, int>, Tuple<float[,], float[,], float[,], int, int>>(inputData =>
            {
                float[,] F = inputData.Item1;
                float[,] staticRatio = inputData.Item2;
                int expIndicatorID = inputData.Item3;
                int time = inputData.Item4;

                float avgControl = 0.0f;

                float[,] controlSubtraction = new float[mask.Rows, mask.Cols];

                try
                {
                    // only do this if the control wells are specified and staticRatio available
                    if (controlWells.Count() > 0 && staticRatio != null)
                    {
                        // calculate Avg(F/Fo) for control wells
                        foreach (Tuple<int, int> well in controlWells)
                        {
                            int row = well.Item1;
                            int col = well.Item2;

                            avgControl += staticRatio[row, col];
                        }
                        avgControl /= controlWells.Count();


                        for (int r = 0; r < mask.Rows; r++)
                            for (int c = 0; c < mask.Cols; c++)
                            {
                                controlSubtraction[r, c] = staticRatio[r, c] - avgControl;
                            }
                    }
                    else
                    {
                        controlSubtraction = null;
                    }

                    return Tuple.Create(F, staticRatio, controlSubtraction, expIndicatorID, time);

                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            },
                // Specify a task scheduler as that which is passed in
                // so that the action runs on the UI thread. 
               new ExecutionDataflowBlockOptions
               {
                   MaxDegreeOfParallelism = 1
               });







            // calculate Dynamic Ratio: this is the ration of the static ratios for two given 
            // indicators.  Before this block can actually create a value, it must recieve an
            // input from both indicators.  
            //      input: F array (float[,]), F/Fo array aka "static Ratio" (float[,]), 
            //              control subtraction array (float[,]), ExperimentIndicatorID (int),
            //              and time (int)
            //      output: F array (float[,]), F/Fo array aka "static Ratio" (float[,]), 
            //              control subtraction array (float[,]), dyanamic ratio array (float[,]),
            //              ExperimentIndicatorID (int), and time (int)
            var calculateDynamicRatio = new TransformBlock<Tuple<float[,], float[,], float[,], int, int>, Tuple<float[,], float[,], float[,], float[,], int, int>>(inputData =>
            {
                float[,] F = inputData.Item1;
                float[,] staticRatio = inputData.Item2;
                float[,] controlSubtraction = inputData.Item3;
                int expIndicatorID = inputData.Item4;
                int time = inputData.Item5;

                float[,] dynamicRatio = null;

                try
                {
                    if (staticRatio != null)
                    {
                        if (expIndicatorID == m_dynamicRatioNumeratorID)
                        {
                            for (int r = 0; r < mask.Rows; r++)
                                for (int c = 0; c < mask.Cols; c++)
                                {
                                    m_dynamicRatioNumeratorValues[r, c] = staticRatio[r, c];
                                }
                            m_dynamicRatioNumeratorReady = true;
                        }
                        else if (expIndicatorID == m_dynamicRatioDenominatorID)
                        {
                            for (int r = 0; r < mask.Rows; r++)
                                for (int c = 0; c < mask.Cols; c++)
                                {
                                    m_dynamicRatioDenominatorValues[r, c] = staticRatio[r, c];
                                }
                            m_dynamicRatioDenominatorReady = true;
                        }
                    }

                    if (m_dynamicRatioNumeratorReady && m_dynamicRatioDenominatorReady)
                    {
                        m_dynamicRatioNumeratorReady = false;
                        m_dynamicRatioDenominatorReady = false;

                        dynamicRatio = new float[mask.Rows, mask.Cols];

                        for (int r = 0; r < mask.Rows; r++)
                            for (int c = 0; c < mask.Cols; c++)
                            {
                                dynamicRatio[r, c] = m_dynamicRatioNumeratorValues[r, c] / m_dynamicRatioDenominatorValues[r, c];
                            }
                    }
                    else
                    {
                        dynamicRatio = null;
                    }

                    return Tuple.Create(F, staticRatio, controlSubtraction, dynamicRatio, expIndicatorID, time);

                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            },
                // Specify a task scheduler as that which is passed in
                // so that the action runs on the UI thread. 
               new ExecutionDataflowBlockOptions
               {
                   MaxDegreeOfParallelism = 1
               });







            // Post the analysis results to the charting display
            //      input: F array (float[,]), F/Fo array aka "static Ratio" (float[,]),  
            //              control subtraction array (float[,]), dyanamic ratio array (float[,]),
            //              ExperimentIndicatorID (int), and time (int)
            var PostAnalysisResults = new TransformBlock<Tuple<float[,], float[,], float[,], float[,], int, int>,
                Tuple<float[,], float[,], float[,], float[,], int, int>>(inputData =>
                {
                    float[,] F = inputData.Item1;
                    float[,] staticRatio = inputData.Item2;
                    float[,] controlSubtraction = inputData.Item3;
                    float[,] dynamicRatio = inputData.Item4;
                    int expIndicatorID = inputData.Item5;
                    int time = inputData.Item6;

                    try
                    {
                        // send the data to be displayed
                        chartArray.AppendNewData(ref F, ref staticRatio, ref controlSubtraction,
                                                 ref dynamicRatio, time, expIndicatorID);

                        return Tuple.Create(F, staticRatio, controlSubtraction, dynamicRatio, expIndicatorID, time);

                    }
                    catch (OperationCanceledException)
                    {
                        return null;
                    }
                    catch (Exception e)
                    {
                        string errmsg = e.Message;
                        return null;
                    }
                },
                // Specify a task scheduler as that which is passed in
                // so that the action runs on the UI thread. 
               new ExecutionDataflowBlockOptions
               {
                   TaskScheduler = m_chartArrayTask,
                   MaxDegreeOfParallelism = 8
               });






            // Post the analysis results to the charting display and store the results in the 
            // database.                          
            //      input: F array (float[,]), F/Fo array aka "static Ratio" (float[,]),  
            //              control subtraction array (float[,]), dyanamic ratio array (float[,]),
            //              ExperimentIndicatorID (int), and time (int)
            var StoreAnalysisResults = new ActionBlock<Tuple<float[,], float[,], float[,], float[,], int, int>>(inputData =>
            {
                float[,] F = inputData.Item1;
                float[,] staticRatio = inputData.Item2;
                float[,] controlSubtraction = inputData.Item3;
                float[,] dynamicRatio = inputData.Item4;
                int expIndicatorID = inputData.Item5;
                int time = inputData.Item6;


                try
                {
                    // write F to database
                    WaveguideDB wgDB2 = new WaveguideDB();

                    AnalysisContainer anal = (AnalysisContainer)m_analysis_Hash[expIndicatorID];

                    bool success2 = wgDB2.InsertAnalysisFrame(anal.AnalysisID, time, F);

                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    string errmsg = e.Message;
                }
            },
                // Specify a task scheduler as that which is passed in
                // so that the action runs on the UI thread. 
               new ExecutionDataflowBlockOptions
               {
                   MaxDegreeOfParallelism = 8
               });




            // link blocks
            calculateApertureSums.LinkTo(calculateStaticRatio);
            calculateStaticRatio.LinkTo(calculateControlSubtraction);
            calculateControlSubtraction.LinkTo(calculateDynamicRatio);
            calculateDynamicRatio.LinkTo(PostAnalysisResults);
            PostAnalysisResults.LinkTo(StoreAnalysisResults);


            // return head of display pipeline
            return calculateApertureSums;
          

        } // END CreateAnalysisPipeline()


        #endregion

    

    }




    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////// 

    public enum ImagerState
    {
        Idle,
        Busy,
        Error
    }

    public class ImagerEventArgs : EventArgs
    {
        private string _message;
        private ImagerState _state;
       

        public ImagerEventArgs(string TextMessage, ImagerState state)
        {
            _message = TextMessage;
            _state = state;
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public ImagerState State
        {
            get { return _state;}
            set { _state = value;}
        }
    }



    public class CameraEventArgs : EventArgs
    {
        private string _message;
        private bool _cameraImaging;

        public CameraEventArgs(string TextMessage, bool CameraBusyImaging)
        {
            _message = TextMessage;
            _cameraImaging = CameraBusyImaging;
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public bool CameraImaging
        {
            get { return _cameraImaging; }
            set { _cameraImaging = value; }
        }
    }


    public class TemperatureEventArgs : EventArgs
    {
        private bool _goodReading;
        private int _temperature;

        public bool GoodReading
        {
            get { return _goodReading; }
            set { _goodReading = value; }
        }

        public int Temperature
        {
            get { return _temperature; }
            set { _temperature = value; }
        }

        public TemperatureEventArgs(bool goodReading, int temperature)
        {
            GoodReading = goodReading;
            Temperature = temperature;
        }


    }


    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////


    public class TemperatureProgressReport
    {
        public bool GoodReading { get; set; }
        public int Temperature { get; set; }

        public TemperatureProgressReport(bool goodReading, int temp)
        {
            GoodReading = goodReading;
            Temperature = temp;
        }
    }





}
