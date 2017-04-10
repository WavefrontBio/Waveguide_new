using System;
using System.Collections.Generic;
using System.Text;
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
	/// Interaction logic for CameraSetup.xaml
	/// </summary>
	public partial class CameraSetup : UserControl
	{

        public CameraSetupModel vm;
        public Camera m_camera;
        public Imager m_imager;
        int m_ID;
        UInt16 m_lowerSliderValue;
        UInt16 m_upperSliderValue;
        WaveguideDB m_wgDB;

        private WPFTools.SpinnerDotCircle m_spinner;

		public CameraSetup()
		{
			this.InitializeComponent();
            m_ID = -1;
            m_lowerSliderValue = 0;
            m_upperSliderValue = (UInt16)GlobalVars.MaxPixelValue;

            m_wgDB = new WaveguideDB();
		}

        ~CameraSetup()
        {           
        }

        public void Shutdown()
        {
            if (vm.IsManualMode)
            {
                m_imager.m_ImagingDictionary.Remove(m_ID);
            }
        }

        public void Configure(Imager _imager, int indicatorID, bool AllowCameraConfiguration, bool IsManualMode)
        {
            m_imager = _imager;
            m_camera = m_imager.m_camera;
            vm = new CameraSetupModel(m_imager, AllowCameraConfiguration, IsManualMode);

            m_ID = indicatorID;


            ///////////////////////////////////////////////////////////////////////////////////
            // Set ImagingParamsStruct
            ImagingParamsStruct ips;
            if (m_imager.m_ImagingDictionary.TryGetValue(m_ID, out ips))
            {
                vm.Exposure = (int)(ips.exposure * 1000);
                vm.EMGain = ips.gain;

                FilterContainer filter;

                if (m_wgDB.GetEmissionFilterAtPosition(ips.emissionFilterPos, out filter))
                    vm.EmFilter = filter;
                else
                    vm.EmFilter = null;

                if (m_wgDB.GetExcitationFilterAtPosition(ips.excitationFilterPos, out filter))
                    vm.ExFilter = filter;
                else
                    vm.ExFilter = null;

                ips.histBitmap = BitmapFactory.New(m_imager.m_histogramImageWidth, m_imager.m_histogramImageHeight);
                HistImage.Source = ips.histBitmap;
                ips.ImageControl = ImageDisplay;
                ips.indicatorName = "Setup";
                ips.pSurface = IntPtr.Zero;

                m_imager.m_ImagingDictionary.Add(m_ID, ips); // Not sure if this is right

                m_imager.ConfigImageDisplaySurface(m_ID, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight, false);


            }
            else
            {
                ips = new ImagingParamsStruct();

                float exposure = 0;
                float accum = 0;
                float kin =0;
                m_imager.m_camera.MyCamera.GetAcquisitionTimings(ref exposure, ref accum, ref kin);
                if (exposure < 0.002) exposure = 0.002f;
                ips.exposure = exposure;
                ips.cycleTime = (int)(exposure * 1000) + 50;
                ips.d3dImage = null;
                ips.emissionFilterPos = 0;
                ips.excitationFilterPos = 0;
                ips.experimentIndicatorID = m_ID;
                ips.flatfieldType = FLATFIELD_SELECT.NONE;
                ips.gain = 5;
                ips.histBitmap = BitmapFactory.New(m_imager.m_histogramImageWidth, m_imager.m_histogramImageHeight);
                HistImage.Source = ips.histBitmap;
                ips.ImageControl = ImageDisplay;
                ips.indicatorName = "Setup";
                ips.pSurface = IntPtr.Zero;
          

                vm.EMGain = 5;
                vm.Exposure = (int)(ips.exposure * 1000);

                m_wgDB.GetAllEmissionFilters();
                if (m_wgDB.m_filterList.Count>0)
                    vm.EmFilter = m_wgDB.m_filterList[0];
                else
                    vm.EmFilter = null;

                m_wgDB.GetAllExcitationFilters();
                if (m_wgDB.m_filterList.Count>0)
                    vm.ExFilter = m_wgDB.m_filterList[0];
                else
                    vm.ExFilter = null;

                m_imager.m_ImagingDictionary.Add(m_ID, ips);
                m_imager.ConfigImageDisplaySurface(m_ID, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight, false);
                
            }


            ///////////////////////////////////////////////////////////////////////////////////
            // Set CameraParams and AcquisitionParams
            CameraParams cParams;
            AcquisitionParams aParams;
            m_imager.m_camera.GetCurrentCameraSettings(out cParams, out aParams);

            vm.Binning = aParams.HBin;
            vm.AcquisitionMode = aParams.AcquisitionMode;
            vm.ADChannel = aParams.ADChannel;
            vm.ApplyMask = m_imager.m_UseMask;
            vm.AutosizeROI = m_imager.m_ROIAdjustToMask;
            vm.EmFilterList = m_imager.m_emFilterList;
            vm.ExFilterList = m_imager.m_exFilterList;
            vm.PreAmpGainIndex = cParams.PreAmpGainIndex;
            vm.ReadMode = aParams.ReadMode;
            vm.RoiX = aParams.RoiX;
            vm.RoiY = aParams.RoiY;
            vm.RoiW = aParams.RoiW;
            vm.RoiH = aParams.RoiH;
            vm.TriggerMode = aParams.TriggerMode;
            vm.UseEMAmp = cParams.UseEMAmp;
            vm.UseFrameTransfer = cParams.UseFrameTransfer;
            vm.VertClockAmpIndex = cParams.VertClockAmpIndex;
            vm.VSSIndex = cParams.VSSIndex;
            vm.HSSIndex = cParams.HSSIndex;
            vm.EMGainMode = aParams.EMGainMode;
            vm.XPixels = m_camera.XPixels;
            vm.YPixels = m_camera.YPixels;

            vm.SliderLowPosition = 0.0;
            vm.SliderHighPosition = 100.0;

               
            this.DataContext = vm;

            m_imager.m_imagerEvent += m_imager_m_imagerEvent;

            m_camera.m_cameraParams.Updated += m_cameraParams_Updated;
            m_camera.m_acqParams.Updated += m_acqParams_Updated;

            RangeSlider.RangeChanged += RangeSlider_RangeChanged;
          
        }

        void RangeSlider_RangeChanged(object sender, WPFTools.RangeSliderEventArgs e)
        {
            m_lowerSliderValue = (UInt16)(e.Minimum/100.0f * (double)GlobalVars.MaxPixelValue);
            m_upperSliderValue = (UInt16)(e.Maximum / 100.0f * (double)GlobalVars.MaxPixelValue);

            m_imager.m_RangeSliderLowerSliderPosition = m_lowerSliderValue;
            m_imager.m_RangeSliderUpperSliderPosition = m_upperSliderValue;

            m_imager.RedisplayCurrentImage(m_ID, m_lowerSliderValue, m_upperSliderValue);
        }

        void m_acqParams_Updated(object sender, EventArgs e)
        {
            CameraParamsChangeEventArgs args = (CameraParamsChangeEventArgs)e;
            string message = args.Message;
        }

        void m_cameraParams_Updated(object sender, EventArgs e)
        {
            CameraParamsChangeEventArgs args = (CameraParamsChangeEventArgs)e;
            string message = args.Message;
        }

   
    

        void m_imager_m_imagerEvent(object sender, ImagerEventArgs e)
        {
            // use invoke here to make sure the code below runs on UI thread (sometimes this event is raised from non-UI threads)
             Application.Current.Dispatcher.Invoke(() =>
            {
                switch (e.State)
                {
                    case ImagerState.Idle: 
                        if (vm.IsOptimizing)
                        {
                            ImageGrid.Children.Remove(m_spinner);
                            vm.IsOptimizing = false;
                            if (m_imager.OptimizationResult_Success)
                            {
                                vm.Exposure = m_imager.OptimizationResult_Exposure;
                                vm.EMGain = m_imager.m_camera.m_cameraParams.EMGain;
                                vm.Binning = m_imager.m_camera.m_acqParams.HBin;
                                CalculateTimings();
                            }
                            else
                            {
                                MessageBox.Show("Optimization Failed");
                            }

                            TakePicturePB_Click(null, null);
                        }

                        TakePicturePB.IsEnabled = true;
                        OptimizePB.IsEnabled = true;
                        ExcitationFilterCB.IsEnabled = true;
                        EmissionFilterCB.IsEnabled = true;
                        BinningCB.IsEnabled = true;
                        Exposure.IsEnabled = true;
                        EMGain.IsEnabled = true;
                        ApplyMaskCkBx.IsEnabled = true;
                        SignalTypeGroupBox.IsEnabled = true;
                        StartVideoPB.IsEnabled = true;
                        StartVideoPB.Content = "Start Video";
                        OptimizePB.Content = "Optimize";

                        m_imager.m_kineticImagingON = false;

                        ErrorText.Text = "Ready";
                        ErrorText.Foreground = new SolidColorBrush(Colors.Green);
                    break;

                case ImagerState.Error:
                        ErrorText.Text = e.Message;
                        ErrorText.Foreground = new SolidColorBrush(Colors.Red);
                    break;

                case ImagerState.Busy:
                        ErrorText.Text = "Busy";
                        ErrorText.Foreground = new SolidColorBrush(Colors.Green);                       
                    break;
            }
            }); // END Invoke 

        }

        private void ConfigLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            vm.ShowConfigPanel = !vm.ShowConfigPanel;
            e.Handled = true;
        }

        private void DisplayGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //CameraSetupModel csm = (CameraSetupModel)LayoutRoot.DataContext;
            //csm.ShowConfigPanel = false;
        }

        private void LoadCameraSettingsPB_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveCameraSettingsPB_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TakePicturePB_Click(object sender, RoutedEventArgs e)
        {
            UInt16[] grayRoiImage;
            int exposure = Convert.ToInt32(Exposure.Text);
                                
            bool success = m_imager.AcquireImage(exposure, out grayRoiImage);
            if(success)
            {
                // display image
                m_imager.ProcessAndDisplayImage(grayRoiImage, m_ID, vm.ApplyMask, m_lowerSliderValue, m_upperSliderValue);
            }
        }

        private void StartVideoPB_Click(object sender, RoutedEventArgs e)
        {
            if (!m_imager.m_kineticImagingON)
            {
                TakePicturePB.IsEnabled = false;
                OptimizePB.IsEnabled = false;
                ExcitationFilterCB.IsEnabled = false;
                EmissionFilterCB.IsEnabled = false;
                BinningCB.IsEnabled = false;
                Exposure.IsEnabled = false;
                EMGain.IsEnabled = false;
                ApplyMaskCkBx.IsEnabled = false;
                SignalTypeGroupBox.IsEnabled = false;
                StartVideoPB.Content = "Stop Video";

                ImagingParamsStruct ips;
                if(m_imager.m_ImagingDictionary.TryGetValue(m_ID,out ips))
                {
                    ips.cycleTime = vm.Exposure + 50;
                }
                
                m_imager.StartVideo(m_ID, 1000);

                m_imager.m_kineticImagingON = true;
            }
            else
            {
                m_imager.StopVideo();
            }

        }

        private void OptimizePB_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.IsOptimizing)
            {
                m_spinner = new WPFTools.SpinnerDotCircle();
                Grid.SetRow(m_spinner, 0);
                Grid.SetColumn(m_spinner, 0);
                int count = ImageGrid.Children.Add(m_spinner);


                TakePicturePB.IsEnabled = false;
                StartVideoPB.IsEnabled = false;
                ExcitationFilterCB.IsEnabled = false;
                EmissionFilterCB.IsEnabled = false;
                BinningCB.IsEnabled = false;
                Exposure.IsEnabled = false;
                EMGain.IsEnabled = false;
                ApplyMaskCkBx.IsEnabled = false;
                SignalTypeGroupBox.IsEnabled = false;
                OptimizePB.Content = "Abort";

                m_imager.StartOptimization(m_ID, vm.IsIncreasingSignal, vm.Exposure);

                vm.IsOptimizing = true;
            }
            else
            {
                m_imager.StopOptimization();
            }

        }


        private void CalculateTimings()
        {
            bool success;
            success = m_camera.PrepareAcquisition(vm.ReadMode, vm.AcquisitionMode, vm.TriggerMode, vm.EMGainMode, vm.ADChannel, vm.Binning, vm.Binning, vm.RoiX, vm.RoiY, vm.RoiW, vm.RoiH);
            success = m_camera.ConfigureCamera(vm.VSSIndex, vm.HSSIndex, vm.VertClockAmpIndex, vm.PreAmpGainIndex, vm.UseEMAmp, vm.EMGain, vm.UseFrameTransfer);
            uint ecode = m_camera.MyCamera.SetExposureTime((float)(vm.Exposure) / 1000.0f);


            float actualExposureF = 0.0f;
            float actualAccumulationF = 0.0f;
            float actualKineticCycleF = 0.0f;
            m_camera.MyCamera.GetAcquisitionTimings(ref actualExposureF, ref actualAccumulationF, ref actualKineticCycleF);

            int minCycleTime = (int)(actualKineticCycleF*1000.0f) + 10; // allow 10 msecs to read data from camera and post to cuda thread
            float maxRate = 1000.0f / (float)(minCycleTime);

            ImagingParamsStruct ips;
            if (m_imager.m_ImagingDictionary.TryGetValue(m_ID, out ips))
            {                
                ips.cycleTime = minCycleTime > 100 ? minCycleTime : 100;              
            }     

            SpeedLimitText.Text = "Min Cycle Time = " + minCycleTime.ToString() + " (" + maxRate.ToString("N1") + " Hz)";
        }


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Property Change handlers

        private void ExcitationFilterCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Move Excitation Filter
            // TODO
        }

        private void EmissionFilterCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Move Emission Filter
            // TODO
        }

        private void BinningCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_camera.m_acqParams.HBin = vm.Binning;
            m_camera.m_acqParams.VBin = vm.Binning;

            m_imager.ConfigImageDisplaySurface(m_ID, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight, false);

            CalculateTimings();
        }

        private void VSSCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_camera.m_cameraParams.VSSIndex = vm.VSSIndex;

            CalculateTimings();
        }

        private void HorzReadoutRateCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_camera.m_cameraParams.HSSIndex = vm.HSSIndex;

            CalculateTimings();
        }

        private void PreAmpGainCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_camera.m_cameraParams.PreAmpGainIndex = vm.PreAmpGainIndex;

            CalculateTimings();
        }

        private void VertClockAmpCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_camera.m_cameraParams.VertClockAmpIndex = vm.VertClockAmpIndex;

            CalculateTimings();
        }

        private void UseEMGainCkBx_Checked(object sender, RoutedEventArgs e)
        {
            m_camera.m_cameraParams.UseEMAmp = vm.UseEMAmp;

            CalculateTimings();
        }

        private void UseEMGainCkBx_Unchecked(object sender, RoutedEventArgs e)
        {
            m_camera.m_cameraParams.UseEMAmp = vm.UseEMAmp;

            CalculateTimings();
        }

        private void UseFrameTransferCkBx_Checked(object sender, RoutedEventArgs e)
        {
            m_camera.m_cameraParams.UseFrameTransfer = vm.UseFrameTransfer;

            CalculateTimings();
        }

        private void UseFrameTransferCkBx_Unchecked(object sender, RoutedEventArgs e)
        {
            m_camera.m_cameraParams.UseFrameTransfer = vm.UseFrameTransfer;

            CalculateTimings();
        }

        private void AutosizeROICkBx_Checked(object sender, RoutedEventArgs e)
        {
            m_imager.m_ROIAdjustToMask = vm.AutosizeROI;

            CalculateTimings();
        }

        private void AutosizeROICkBx_Unchecked(object sender, RoutedEventArgs e)
        {
            m_imager.m_ROIAdjustToMask = vm.AutosizeROI;

            CalculateTimings();
        }

        private void EMGain_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            m_camera.m_cameraParams.EMGain = vm.EMGain;

            ImagingParamsStruct ips;
            if (m_imager.m_ImagingDictionary.TryGetValue(m_ID, out ips))
            {
                ips.gain = (int)vm.EMGain;
            }

            CalculateTimings();
        }

        private void Exposure_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ImagingParamsStruct ips; 
            if(m_imager.m_ImagingDictionary.TryGetValue(m_ID, out ips))
            {
                ips.exposure = (float)(vm.Exposure) / 1000.0f;

                CalculateTimings();
            }            
        }

        private void ApplyMaskCkBx_Checked(object sender, RoutedEventArgs e)
        {
            m_imager.m_UseMask = vm.ApplyMask;

            m_imager.UpdateMask(m_imager.m_mask);
        }

        private void ApplyMaskCkBx_Unchecked(object sender, RoutedEventArgs e)
        {
            m_imager.m_UseMask = vm.ApplyMask;

            m_imager.UpdateMask(m_imager.m_mask);
        }




	}
}