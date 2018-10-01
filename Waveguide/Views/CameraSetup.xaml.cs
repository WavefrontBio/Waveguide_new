using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
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

        public bool m_enableRangerSliderUpdate;


		public CameraSetup()
		{
     
			this.InitializeComponent();
            m_ID = -1;
            m_lowerSliderValue = 0;
            m_upperSliderValue = (UInt16)GlobalVars.MaxPixelValue;

            m_wgDB = new WaveguideDB();

            m_enableRangerSliderUpdate = false;
           
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
            vm = new CameraSetupModel(m_imager, m_wgDB, AllowCameraConfiguration, IsManualMode);

            m_ID = indicatorID;

            if(IsManualMode)  // if this is manual operation (i.e. NOT a verify, prep-for-run, operation), then enable filter changes based on combo-box changes
            {
                EmissionFilterCB.SelectionChanged+=EmissionFilterCB_SelectionChanged;
                ExcitationFilterCB.SelectionChanged+=ExcitationFilterCB_SelectionChanged;
            }
            

            ///////////////////////////////////////////////////////////////////////////////////
            // Set ImagingParamsStruct
            ImagingParamsStruct ips;
            if (m_imager.m_ImagingDictionary.ContainsKey(m_ID))
            {
                ips = m_imager.m_ImagingDictionary[m_ID];
                vm.Exposure = (int)(ips.exposure * 1000);
                vm.EMGain = ips.gain;
                vm.Binning = ips.binning;
                vm.MinCycleTime = m_imager.m_camera.GetCycleTime();
                if (ips.cycleTime < vm.MinCycleTime) vm.CycleTime = vm.MinCycleTime;
                else vm.CycleTime = ips.cycleTime; 
        
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
                ips.ImageControl = ImageDisplayControl;
                ips.indicatorName = "Setup";

                m_imager.m_ImagingDictionary[m_ID] = ips;

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
                ips.binning = 1;
                ips.cycleTime = (int)(exposure * 1000) + 50;
                ips.emissionFilterPos = 0;
                ips.excitationFilterPos = 0;
                ips.experimentIndicatorID = m_ID;
                ips.flatfieldType = FLATFIELD_SELECT.NONE;
                ips.gain = 5;
                ips.histBitmap = BitmapFactory.New(m_imager.m_histogramImageWidth, m_imager.m_histogramImageHeight);
                HistImage.Source = ips.histBitmap;
                ips.ImageControl = ImageDisplayControl;
                ips.indicatorName = "Setup";
          

                vm.EMGain = 1;
                vm.Binning = 1;
                vm.Exposure = (int)(ips.exposure * 1000);
                vm.CycleTime = GlobalVars.CameraDefaultCycleTime;
                vm.MinCycleTime = 100;

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

                m_imager.m_lambda.MoveFilterABandCloseShutterA((byte)vm.ExFilter.PositionNumber, (byte) vm.EmFilter.PositionNumber, GlobalVars.FilterChangeSpeed, GlobalVars.FilterChangeSpeed);
                
            }


            ///////////////////////////////////////////////////////////////////////////////////
            // Set CameraParams and AcquisitionParams
            CameraParams cParams;
            AcquisitionParams aParams;
            m_imager.m_camera.GetCurrentCameraSettings(out cParams, out aParams);

            vm.Binning = aParams.HBin;         
            vm.ApplyMask = m_imager.m_UseMask;
            vm.EmFilterList = m_imager.m_emFilterList;
            vm.ExFilterList = m_imager.m_exFilterList;
            vm.PreAmpGainIndex = cParams.PreAmpGainIndex;
            vm.UseEMAmp = cParams.UseEMAmp;
            vm.UseFrameTransfer = cParams.UseFrameTransfer;
            vm.VertClockAmpIndex = cParams.VertClockAmpIndex;
            vm.VSSIndex = cParams.VSSIndex;
            vm.HSSIndex = cParams.HSSIndex;

            vm.SliderLowPosition = 0.0;
            vm.SliderHighPosition = 100.0;

            if(ips.optimizeWellList != null)
                vm.WellSelectionPBLabel = ips.optimizeWellList.Count.ToString();
            else
                vm.WellSelectionPBLabel = (m_imager.m_mask.Rows * m_imager.m_mask.Cols).ToString();


            if(m_imager.m_mask == null)
                m_imager.SetMask(null);

               
            this.DataContext = vm;

            m_imager.m_imagerEvent += m_imager_m_imagerEvent;

            m_camera.m_cameraParams.Updated += m_cameraParams_Updated;
            m_camera.m_acqParams.Updated += m_acqParams_Updated;

            RangeSlider.RangeChanged += RangeSlider_RangeChanged;

            m_imager.m_lambda.MoveFilterABandCloseShutterA((byte)vm.ExFilter.PositionNumber, (byte)vm.EmFilter.PositionNumber, GlobalVars.FilterChangeSpeed, GlobalVars.FilterChangeSpeed);

        }

        void RangeSlider_RangeChanged(object sender, WPFTools.RangeSliderEventArgs e)
        {
            m_lowerSliderValue = (UInt16)(e.Minimum/100.0f * (double)GlobalVars.MaxPixelValue);
            m_upperSliderValue = (UInt16)(e.Maximum / 100.0f * (double)GlobalVars.MaxPixelValue);

            if (m_imager == null) return;

            m_imager.m_RangeSliderLowerSliderPosition = m_lowerSliderValue;
            m_imager.m_RangeSliderUpperSliderPosition = m_upperSliderValue;

            if(m_enableRangerSliderUpdate) m_imager.RedisplayCurrentImage(m_ID, m_lowerSliderValue, m_upperSliderValue);
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
                                vm.PreAmpGainIndex = m_imager.m_camera.m_cameraParams.PreAmpGainIndex;
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
                        CycleTime.IsEnabled = true;
                        PreAmpGainCombo.IsEnabled = true;
                        Exposure.IsEnabled = true;
                        EMGain.IsEnabled = true;
                        ApplyMaskCkBx.IsEnabled = true;
                        SignalTypeGroupBox.IsEnabled = true;
                        CameraSettingsCB.IsEnabled = true;
                        StartVideoPB.IsEnabled = true;
                        StartVideoPB.Content = "Start Video";
                        OptimizePB.Content = "Optimize";
                        SaveImagePB.IsEnabled = true;
                        FlatFieldCorrectionCB.IsEnabled = true;
                        WellSelectionPB.IsEnabled = true;


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
            if (m_imager == null) return;


            m_imager.m_lambda.OpenShutterA(); Thread.Sleep(5);


            UInt16[] grayRoiImage;
            int exposure = Convert.ToInt32(Exposure.Text);



            bool success = m_imager.AcquireImage(exposure, out grayRoiImage);
            if (success)
            {
                // display image
                m_imager.ProcessAndDisplayImage(grayRoiImage, m_ID, vm.ApplyMask, m_lowerSliderValue, m_upperSliderValue);
            }

            m_imager.m_lambda.CloseShutterA();

            m_enableRangerSliderUpdate = true;
        }

        private void StartVideoPB_Click(object sender, RoutedEventArgs e)
        {
            if (m_imager == null) return;

            if (!m_imager.m_kineticImagingON)
            {
                TakePicturePB.IsEnabled = false;
                OptimizePB.IsEnabled = false;
                CycleTime.IsEnabled = false;
                PreAmpGainCombo.IsEnabled = false;
                ExcitationFilterCB.IsEnabled = false;
                EmissionFilterCB.IsEnabled = false;
                BinningCB.IsEnabled = false;
                Exposure.IsEnabled = false;
                EMGain.IsEnabled = false;
                ApplyMaskCkBx.IsEnabled = false;
                SignalTypeGroupBox.IsEnabled = false;
                CameraSettingsCB.IsEnabled = false;
                StartVideoPB.Content = "Stop Video";
                SaveImagePB.IsEnabled = false;
                FlatFieldCorrectionCB.IsEnabled = false;
                WellSelectionPB.IsEnabled = false;

                ImagingParamsStruct ips;
                if(m_imager.m_ImagingDictionary.ContainsKey(m_ID))
                {
                    ips = m_imager.m_ImagingDictionary[m_ID];
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
            if (m_imager == null) return;


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
                CycleTime.IsEnabled = false;
                PreAmpGainCombo.IsEnabled = false;
                EMGain.IsEnabled = false;
                ApplyMaskCkBx.IsEnabled = false;
                SignalTypeGroupBox.IsEnabled = false;
                CameraSettingsCB.IsEnabled = false;
                OptimizePB.Content = "Abort";
                SaveImagePB.IsEnabled = false;
                FlatFieldCorrectionCB.IsEnabled = false;
                WellSelectionPB.IsEnabled = false;

                vm.CurrentCameraSettings.ExposureLimit = (int)((float)vm.CycleTime * 0.95);
                               
                m_imager.StartOptimization(m_ID, vm.CurrentCameraSettings); // vm.IsIncreasingSignal, vm.Exposure);

                vm.IsOptimizing = true;
            }
            else
            {
                m_imager.StopOptimization();
            }

            m_enableRangerSliderUpdate = true;

        }


        private void CalculateTimings()
        {
            if (m_imager == null) return;

            bool success;
            m_camera.SetCameraBinning(vm.Binning, vm.Binning); m_camera.PrepareAcquisition();          
            success = m_camera.ConfigureCamera(vm.VSSIndex, vm.HSSIndex, vm.VertClockAmpIndex, vm.PreAmpGainIndex, vm.UseEMAmp, vm.EMGain, vm.UseFrameTransfer);
            uint ecode = m_camera.MyCamera.SetExposureTime((float)(vm.Exposure) / 1000.0f);


            float actualExposureF = 0.0f;
            float actualAccumulationF = 0.0f;
            float actualKineticCycleF = 0.0f;
            m_camera.MyCamera.GetAcquisitionTimings(ref actualExposureF, ref actualAccumulationF, ref actualKineticCycleF);

            int minCycleTime = (int)(actualKineticCycleF*1000.0f) + 10; // allow 10 msecs to read data from camera and post to cuda thread
            float maxRate = 1000.0f / (float)(minCycleTime);

            ImagingParamsStruct ips;
            if (m_imager.m_ImagingDictionary.ContainsKey(m_ID))
            {
                ips = m_imager.m_ImagingDictionary[m_ID];
                ips.cycleTime = minCycleTime > 100 ? minCycleTime : 100;
                m_imager.m_ImagingDictionary[m_ID] = ips;
            }     

            SpeedLimitText.Text = "Min Cycle Time = " + minCycleTime.ToString() + " (" + maxRate.ToString("N1") + " Hz)";

            vm.MinCycleTime = minCycleTime;
            if (vm.CycleTime < vm.MinCycleTime) vm.CycleTime = vm.MinCycleTime;
        }


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Property Change handlers

        private void ExcitationFilterCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_imager == null) return;

            // Move Excitation Filter
            if (sender != null)
            {
                if (((ComboBox)sender).SelectedItem != null)
                {
                    FilterContainer fc = (FilterContainer)((ComboBox)sender).SelectedItem;

                    m_imager.SetExcitationFilter(fc);
                }
            }
        }

        private void EmissionFilterCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_imager == null) return;

            // Move Emission Filter
            if (sender != null)
            {
                if (((ComboBox)sender).SelectedItem != null)
                {
                    FilterContainer fc = (FilterContainer)((ComboBox)sender).SelectedItem;

                    m_imager.SetEmissionFilter(fc);
                }
            }
        }

        private void BinningCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_imager == null) return;

            m_camera.m_acqParams.HBin = vm.Binning;
            m_camera.m_acqParams.VBin = vm.Binning;

            m_imager.ConfigImageDisplaySurface(m_ID, m_camera.m_acqParams.BinnedFullImageWidth, m_camera.m_acqParams.BinnedFullImageHeight, false);

            UpdateImagingDictionary();

            m_imager.SetupFlatFieldCorrection(FLATFIELD_SELECT.USE_FLUOR, vm.Binning);
            m_imager.SetupFlatFieldCorrection(FLATFIELD_SELECT.USE_LUMI,  vm.Binning);
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

            UpdateImagingDictionary();
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

     
        private void EMGain_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            m_camera.m_cameraParams.EMGain = vm.EMGain;

            UpdateImagingDictionary();
        }

        private void Exposure_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdateImagingDictionary();          
        }

        private void ApplyMaskCkBx_Checked(object sender, RoutedEventArgs e)
        {
            if (m_imager == null) return;

            m_imager.m_UseMask = vm.ApplyMask;

            m_imager.UpdateMask(m_imager.m_mask);
        }

        private void ApplyMaskCkBx_Unchecked(object sender, RoutedEventArgs e)
        {
            if (m_imager == null) return;

            m_imager.m_UseMask = vm.ApplyMask;

            m_imager.UpdateMask(m_imager.m_mask);
        }



        private void UpdateImagingDictionary()
        {
            if (m_imager == null) return;

            ImagingParamsStruct ips;
            if (m_imager.m_ImagingDictionary.ContainsKey(m_ID))
            {
                ips = m_imager.m_ImagingDictionary[m_ID];
                ips.emissionFilterPos = (byte)vm.EmFilter.PositionNumber;
                ips.excitationFilterPos = (byte)vm.ExFilter.PositionNumber;
                ips.cycleTime = vm.CycleTime;
                ips.binning = (int)vm.Binning;
                ips.gain = (int)vm.EMGain;
                ips.exposure = (float)(vm.Exposure) / 1000.0f;
                ips.preAmpGainIndex = vm.PreAmpGainIndex;
                m_imager.m_ImagingDictionary[m_ID] = ips;

                CalculateTimings();
            }
        }

        private void HSSCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void IsDefaultCkBx_Checked(object sender, RoutedEventArgs e)
        {
            // make sure only one CameraSetting is set as Default
            foreach (var cs in vm.CameraSettingsList)
            {
                if (cs.CameraSettingID != vm.CurrentCameraSettings.CameraSettingID) cs.IsDefault = false;
            }
        }

        private void IsDefaultCkBx_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void StartingBinningCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void IncreasingSignalCkBx_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void IncreasingSignalCkBx_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void NewPB_Click(object sender, RoutedEventArgs e)
        {
            StringEntryDialog dlg = new StringEntryDialog("Add New Camera Settings Entry", "Enter Name:");
          
            dlg.ShowDialog();

            if(dlg.result == MessageBoxResult.OK)
            {
                CameraSettingsContainer cs = vm.AddNew(dlg.enteredString);
            }
        }


        private void CameraSettingsCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selection = vm.CurrentCameraSettings;

            if (DescriptionTextBox != null)
            {
                //CameraSettingIDTextBlock.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
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
                IncreasingSignalRB.GetBindingExpression(RadioButton.IsCheckedProperty).UpdateTarget();
                DecreasingSignalRB.GetBindingExpression(RadioButton.IsCheckedProperty).UpdateTarget();
                StartingBinningCombo.GetBindingExpression(ComboBox.SelectedValueProperty).UpdateTarget();
                EMGainLimitUpDown.GetBindingExpression(Xceed.Wpf.Toolkit.IntegerUpDown.ValueProperty).UpdateTarget();
            }
        }

        private void WellSelectionPB_Click(object sender, RoutedEventArgs e)
        {

            if (m_imager == null) return;

            ObservableCollection<Tuple<int,int>> wellList;

            ImagingParamsStruct ips;
            if (m_imager.m_ImagingDictionary.ContainsKey(m_ID))
            {
                ips = m_imager.m_ImagingDictionary[m_ID];
                wellList = ips.optimizeWellList;
                if (wellList == null) wellList = new ObservableCollection<Tuple<int, int>>();
                if(wellList.Count == 0)
                { // can't allow empty set of wells for optimization, so add them all
                    for (int r = 0; r < m_imager.m_mask.Rows; r++)
                        for (int c = 0; c < m_imager.m_mask.Cols; c++)
                        {
                            wellList.Add(Tuple.Create<int, int>(r, c));
                        }
                }
            }
            else
            {                
                wellList = new ObservableCollection<Tuple<int, int>>();
                ips.optimizeWellList = wellList;
            }


            WellSelectionDialog dlg = new WellSelectionDialog(m_imager.m_mask.Rows, m_imager.m_mask.Cols, "Select Wells to be used for Optimization", false, wellList);

            dlg.ShowDialog();

            if (dlg.m_accepted)
            {                   
                if (wellList == null)
                {
                    wellList = new ObservableCollection<Tuple<int, int>>();
                }

                wellList.Clear();
                foreach (Tuple<int, int> well in dlg.m_wellList)
                    wellList.Add(well);

                ips = m_imager.m_ImagingDictionary[m_ID];
                ips.optimizeWellList = wellList;
                m_imager.m_ImagingDictionary[m_ID] = ips;

                vm.WellSelectionPBLabel = ips.optimizeWellList.Count.ToString();
            }                
            
        }

        private void SaveImagePB_Click(object sender, RoutedEventArgs e)
        {
            bool allowSaveRefImage = false;
            if (vm.Binning == 1) allowSaveRefImage = true;

            SaveImageDialog dlg = new SaveImageDialog(ImageDisplayControl.m_imageBitmap,
                                                      ImageDisplayControl.m_grayImage, 
                                                      m_camera.m_acqParams.BinnedFullImageWidth, 
                                                      m_camera.m_acqParams.BinnedFullImageHeight,                                                      
                                                      allowSaveRefImage);

            dlg.ShowDialog();
        }

        private void FlatFieldCorrectionCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ImagingParamsStruct ips;
            if (m_imager.m_ImagingDictionary.ContainsKey(m_ID))
            {
                ips = m_imager.m_ImagingDictionary[m_ID];
                
                ips.flatfieldType = vm.FlatFieldSelect.FlatField_Select;

                m_imager.m_ImagingDictionary[m_ID] = ips;
            }
        }

	}
}