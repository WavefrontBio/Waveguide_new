using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for RunExperiment.xaml
    /// </summary>
    public partial class RunExperiment : Window
    {
        WaveguideDB m_wgDB;

        Imager m_imager;
        public VWorks m_vworks;
        public int m_imageCount;

        RunExperiment_ViewModel VM;

        DispatcherTimer m_timer;
        int m_delayTime;

        Dispatcher m_dispatcher;

        DispatcherTimer m_simulationTimer;
        private DateTime m_timerStartDateTime { get; set; }
        List<ushort[]> m_simulationImageList;
        int m_simulationTime;
        bool m_simulationRunning;
        int m_indicatorIndex;
        int m_imageIndex;        
        ITargetBlock<Tuple<ushort[], int, int, int, WG_Color[]>> m_displayPipeline;
        ITargetBlock<Tuple<ushort[], int, int, int>> m_storagePipeline = null;
        ITargetBlock<Tuple<ushort[],int>> m_histogramPipeline = null;
        ITargetBlock<Tuple<ushort[], int, int>> m_analysisPipeline = null;

        EnclosureCameraViewer m_enclosureCameraViewer = null;       


        Progress<int> m_progress;


        ObservableCollection<Tuple<int,int>> m_controlSubtractionWellList;
        int m_numFoFrames;
        ExperimentIndicatorContainer m_dynamicRatioNumerator;
        ExperimentIndicatorContainer m_dynamicRatioDenominator;

        TaskScheduler m_uiTask;
        CancellationTokenSource m_tokenSource;
        CancellationToken m_cancelToken;
        ColorModel m_colorModel;
     
        // containers for the Experiment data        
        List<AnalysisContainer> m_analysisList;

        public bool m_ReadyToRun;


        string m_vworksProtocolFilename;

       
        public RunExperiment(Imager imager, TaskScheduler uiTask)
        {           
            m_ReadyToRun = false;            

            m_vworks = new VWorks();

            if(!m_vworks.m_vworksOK)
            {
                //PostMessage("VWorks Failed to Start!!");
                return;
            }

            InitializeComponent();

            m_wgDB = new WaveguideDB();

            VM = new RunExperiment_ViewModel();

            if(m_vworks.VWorks_CreatedSuccessfully())
            {
                m_ReadyToRun = true;
            }
            else
            {                
                //ShowErrorDialog("VWorks Error", "VWorks could not be launched!");                
                Close();
                return;
            }



            // get user
            UserContainer user;
            bool success = m_wgDB.GetUser(GlobalVars.UserID, out user);
            if(success)
            {
                VM.User = user;
            }
            else
            {                
                ShowErrorDialog("Database Error", "Failed to retrieve User from database.  UserID = " + GlobalVars.UserID.ToString());
                Close();
                return;
            }


            // capture event from ChartArray that fires when the VM for the ChartArray has a status change
            ChartArrayControl.VM.StatusChange += new ViewModel_ChartArray.StatusChange_EventHandler(ChartArray_StatusChanged);

            m_uiTask = uiTask;

            m_dispatcher = this.Dispatcher;

            this.DataContext = VM;

            m_timer = new DispatcherTimer();
            m_timer.Tick += m_timer_Tick;
            m_timer.Interval = TimeSpan.FromMilliseconds(1000);

          
            VM.RunState = RunExperiment_ViewModel.RUN_STATE.NEEDS_INPUT;


            
            m_imager = imager;

            if(m_imager == null)
            {
                m_imager = new Imager();               
            }

            m_imager.m_cameraEvent += m_imager_cameraEvent;
            m_imager.m_cameraTemperatureEvent += m_imager_temperatureEvent;


            m_imager.m_imagerEvent += m_imager_m_imagerEvent;
            m_imager.m_insideTemperatureEvent += m_imager_m_insideTemperatureEvent;


            m_vworks.PostVWorksCommandEvent += m_vworks_PostVWorksCommandEvent;


            m_simulationRunning = false;
            m_indicatorIndex = 0;
            m_imageIndex = 0;

            LoadDefaultColorModel();


            // catch close event caused by clicking X button
            this.Closing += new System.ComponentModel.CancelEventHandler(Window_Closing);                   

        }

        void m_imager_m_imagerEvent(object sender, ImagerEventArgs e)
        {
            PostMessage(e.Message);
        }

        void m_imager_m_insideTemperatureEvent(object sender, TemperatureEventArgs e)
        {
            if (e.GoodReading)
            {
                VM.InsideTemperatureText = e.Temperature.ToString();

                ChartArrayControl.VM.InsideTemperatureActual = e.Temperature;
            }
            throw new NotImplementedException();
        }



        private void ChartArray_StatusChanged(ViewModel_ChartArray caVM, ChartArrayViewModel_EventArgs e)
        {            
            switch (e.RunStatus)
            {
                case ViewModel_ChartArray.RUN_STATUS.NEEDS_INPUT:
                    VM.RunState = RunExperiment_ViewModel.RUN_STATE.NEEDS_INPUT;
                    break;
                case ViewModel_ChartArray.RUN_STATUS.READY_TO_RUN:
                    VM.RunState = RunExperiment_ViewModel.RUN_STATE.READY_TO_RUN;
                    break;
                case ViewModel_ChartArray.RUN_STATUS.RUN_FINISHED:
                    break;
                case ViewModel_ChartArray.RUN_STATUS.RUNNING:
                    break;
            }
        }



        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // catch closing event caused by hitting X button if experiment is running

            if (VM.RunState == RunExperiment_ViewModel.RUN_STATE.RUNNING)
            {
                e.Cancel = true;

                MessageBoxResult result = MessageBox.Show("Are you sure you want to Abort?", "Abort Experiment",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        RunPB_Click(null, null); // this will Abort the Experiment (calling RunPB_Click when VM.RunState == RUNNING)                        
                        break;
                    case MessageBoxResult.No:                        
                        break;
                }
            }

            // Good time to unload/shutdown VWorks


            m_vworks = null;
            
            // set this if you want to cancel the close event: e.Cancel = true;
        }

        public void LoadDefaultColorModel()
        {
            m_colorModel = null;

            bool success = m_wgDB.GetAllColorModels();
            if (success)
            {
                foreach(ColorModelContainer cModel in m_wgDB.m_colorModelList)
                {
                    if(cModel.IsDefault || m_colorModel == null)
                    {
                        m_colorModel = new ColorModel(cModel, GlobalVars.MaxPixelValue);                        
                    }
                }
            }
        }




        void m_imager_temperatureEvent(object sender, TemperatureEventArgs e)
        {
            if (e.GoodReading)
            {
                VM.CameraTemperatureText = e.Temperature.ToString();

                ChartArrayControl.VM.CameraTemperatureActual = e.Temperature;
            }
        }

        void m_imager_cameraEvent(object sender, CameraEventArgs e)
        {
            PostMessage(e.Message);
        }



        public void Configure(ProjectContainer project,
                              MethodContainer method,
                              PlateTypeContainer plateType,
                              MaskContainer mask, 
                              ObservableCollection<ExperimentIndicatorContainer> indicatorList,
                              ObservableCollection<ExperimentCompoundPlateContainer> compoundPlateList,
                              ObservableCollection<Tuple<int,int>> controlSubtractionWellList,
                              int numFoFrames,
                              ExperimentIndicatorContainer dynamicRatioNumerator,
                              ExperimentIndicatorContainer dynamicRatioDenominator,
                              int RoiX, int RoiY, int RoiW, int RoiH, 
                              int RoiMaskStartRow, int RoiMaskEndRow, int RoiMaskStartCol, int RoiMaskEndCol )
        {
            VM.Project = project;
            VM.Method = method;
            VM.PlateType = plateType;
            VM.Mask = mask;            
            ChartArrayControl.VM.IndicatorList = indicatorList;
            ChartArrayControl.VM.CompoundPlateList = compoundPlateList;

            m_controlSubtractionWellList = controlSubtractionWellList;
            m_numFoFrames = numFoFrames;
            m_dynamicRatioNumerator = dynamicRatioNumerator;
            m_dynamicRatioDenominator = dynamicRatioDenominator;

            VM.RoiX = RoiX; VM.RoiY = RoiY; VM.RoiW = RoiW; VM.RoiH = RoiH;
            VM.RoiMaskStartRow = RoiMaskStartRow;
            VM.RoiMaskEndRow = RoiMaskEndRow;
            VM.RoiMaskStartCol = RoiMaskStartCol; 
            VM.RoiMaskEndCol = RoiMaskEndCol;            

            m_vworksProtocolFilename = VM.Method.BravoMethodFile;

            ChartArrayControl.Configure(m_imager, VM.Mask);

            //ChartArrayControl.BuildChartArray(VM.Mask.Rows, VM.Mask.Cols, 
            //                                  ChartArrayControl.VM.IndicatorList, 
            //                                  ChartArrayControl.VM.CompoundPlateList);

        }





        public void PostMessage(string msg)
        {
            if (this.Dispatcher.CheckAccess())
            {
                MessageDisplay.AppendText(Environment.NewLine);
                MessageDisplay.AppendText(msg);
                MessageDisplay.ScrollToEnd();
            }
            else
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    MessageDisplay.AppendText(Environment.NewLine);
                    MessageDisplay.AppendText(msg);
                    MessageDisplay.ScrollToEnd();
                }));
            }
        }



        void m_vworks_PostVWorksCommandEvent(object sender, WaveGuideEvents.VWorksCommandEventArgs e)
        {
            VWORKS_COMMAND command = e.Command;
            int param1 = e.Param1;
            string name = e.Name;
            string desc = e.Description;
            int sequenceNumber = (int)m_imager.GetImagingSequenceTime();
            bool success;
            EventMarkerContainer eventMarker;

            switch (command)
            {
                case VWORKS_COMMAND.Protocol_Aborted:
                    PostMessage("VWorks - Protocol Aborted");

                    m_dispatcher.Invoke((Action)(() =>
                    {
                        m_timer.Stop();
                        VM.DelayText = "";
                        m_tokenSource.Cancel();  // stops the imaging task
                        VM.RunState = RunExperiment_ViewModel.RUN_STATE.RUN_ABORTED;
                        SetButton(VM.RunState);
                    }));
                    
                    break;
                case VWORKS_COMMAND.Protocol_Resumed:
                    PostMessage("VWorks - Protocol Resumed");
                     m_dispatcher.Invoke((Action)(() =>
                        {
                            m_timer.Stop();
                            VM.DelayText = "";
                            VM.DelayHeaderVisible = false;
                        }));
                    break;
                case VWORKS_COMMAND.Protocol_Complete:
                    PostMessage("VWorks - Protocol Complete");
                    
                        m_dispatcher.Invoke((Action)(() =>
                        {
                            m_timer.Stop();
                            VM.DelayText = "";
                            VM.DelayHeaderVisible = false;
                            m_tokenSource.Cancel(); // make sure the imaging task stops
                            VM.RunState = RunExperiment_ViewModel.RUN_STATE.RUN_FINISHED;
                            ChartArrayControl.SetStatus(ViewModel_ChartArray.RUN_STATUS.RUN_FINISHED);
                            SetButton(VM.RunState);

                            ReportDialog dlg = new ReportDialog(VM.Project, 
                                                                ChartArrayControl.VM.Experiment, 
                                                                ChartArrayControl.VM.IndicatorList);
                            dlg.ShowDialog();
                        }));
                     
                    
                    break;
                case VWORKS_COMMAND.Protocol_Paused:
                    PostMessage("VWorks - Protocol Paused");

                    m_dispatcher.Invoke((Action)(() =>
                    {
                        m_delayTime = param1;
                        VM.DelayHeaderVisible = true;
                        m_timer.Start();
                    }));
                    
                    break;
                case VWORKS_COMMAND.Event_Marker:
                    PostMessage("VWorks - Event Marker");

                    m_dispatcher.Invoke((Action)(() =>
                    {
                        ChartArrayControl.AddEventMarker(sequenceNumber, desc);
                        eventMarker = new EventMarkerContainer();
                        eventMarker.Description = desc;
                        eventMarker.ExperimentID = ChartArrayControl.VM.Experiment.ExperimentID;
                        eventMarker.Name = name;
                        eventMarker.SequenceNumber = sequenceNumber - GlobalVars.EventMarkerLatency;
                        eventMarker.TimeStamp = DateTime.Now;
                        success = m_wgDB.InsertEventMarker(ref eventMarker);
                        if (!success) PostMessage("Database Error in InsertEventMarker: " + m_wgDB.GetLastErrorMsg());
                    }));

                    
                    break;
                case VWORKS_COMMAND.Initialization_Complete:
                    PostMessage("VWorks Initialization Complete");
                    
                    m_dispatcher.Invoke((Action)(() =>
                    {
                        BringWindowToFront();
                    }));

                    break;
                case VWORKS_COMMAND.Pause_Until:
                    //PostMessage("VWorks - Pause Until");

                    m_dispatcher.Invoke((Action)(() =>
                    {
                        m_delayTime = param1;
                        VM.DelayHeaderVisible = true;
                        VM.DelayText = ((int)(m_delayTime / 1000)).ToString();
                        m_timer.Start();
                    }));

                    
                    break;
                case VWORKS_COMMAND.Set_Time_Marker:
                    //PostMessage("VWorks - Set Time Marker");
                    break;
                case VWORKS_COMMAND.Start_Imaging:
                    PostMessage("VWorks - Start Imaging");
                                   
                    /////////////////////////////////////////////////////////////////////////////////

                    /// Start Imaging Task 
                    m_dispatcher.Invoke((Action)(() =>
                    {
                        BringWindowToFront();
                    }));
                    
                    StartImaging(10000);
                   
                    ////////////////////////////////////////////////////////////////////////////////


                    break;
                case VWORKS_COMMAND.Stop_Imaging:
                    PostMessage("VWorks - Stop Imaging");

                    m_dispatcher.Invoke((Action)(() =>
                    {
                        StopImaging();
                    }));                                      

                    break;
                case VWORKS_COMMAND.Unrecoverable_Error:
                    PostMessage("VWorks - Unrecoverable Error" + ", " + name + ", " + desc);

                    m_dispatcher.Invoke((Action)(() =>
                    {
                        m_timer.Stop();
                        VM.DelayText = "";
                        m_tokenSource.Cancel();  // stops the imaging task
                        VM.RunState = RunExperiment_ViewModel.RUN_STATE.ERROR;
                        SetButton(VM.RunState);
                    }));
                    
                    break;
                case VWORKS_COMMAND.Error:
                    PostMessage("VWorks - Error" + ", " + name + ", " + desc);

                    m_dispatcher.Invoke((Action)(() =>
                    {
                        m_timer.Stop();
                        VM.DelayText = "";
                        VM.DelayHeaderVisible = false;
                        m_tokenSource.Cancel();  // stops the imaging task
                        VM.RunState = RunExperiment_ViewModel.RUN_STATE.ERROR;
                        SetButton(VM.RunState);
                    }));
                                            
                    break;                    
                case VWORKS_COMMAND.Message:
                    PostMessage("VWorks - Message, " + name + ", " + desc);
                    break;
                default:
                    break;
            }
        }


        public void StopImaging()
        {
            m_tokenSource.Cancel();
        }

      
        public void StartImaging(int numberOfImagesToTake)
        {
            int experimentID =  ChartArrayControl.VM.Experiment.ExperimentID;
            int plateID = ChartArrayControl.VM.ExperimentPlate.PlateID;
            int projectID = VM.Project.ProjectID;
            m_imager.StartKineticImaging(numberOfImagesToTake, true, projectID, plateID, experimentID);
        }


   
        void BringWindowToFront()
        {
            // Bring this window into view (on top of VWorks)
                if (!this.IsVisible)
                {
                    this.Show();
                }

                if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                }

                this.Activate();
                this.Topmost = true;  // important
                this.Topmost = false; // important
                this.Focus();         // important 
            
        }



        void m_timer_Tick(object sender, EventArgs e)
        {
            m_delayTime -= 1000;
            if (m_delayTime <= 0)
            {
                m_timer.Stop();
                VM.DelayHeaderVisible = false;
                VM.DelayText = "";
            }
            else
            {
                VM.DelayHeaderVisible = true;
                VM.DelayText = ((int)(m_delayTime / 1000)).ToString();
            }
        }


        private void AbortExperiment()
        {
            m_vworks.VWorks_AbortProtocol();
            m_tokenSource.Cancel();  // stops the imaging task
        }



        private bool PrepForRun()
        {

            //////////////////////////////////////////////////////////////////
            // Create ExperimentPlate, if doesn't already exist 
            bool success;
            string barcode = ChartArrayControl.VM.ImagePlateBarcode;
            
            if(GetExperimentPlate(barcode))
            {
                // successfully created/retrieved ExperimentPlate, so now create Experiment
                if(CreateExperiment())
                {
                    foreach(ExperimentIndicatorContainer ei in ChartArrayControl.VM.IndicatorList)
                    {
                        ExperimentIndicatorContainer ind = new ExperimentIndicatorContainer();
                        ind.Description = ei.Description;
                        ind.EmissionFilterDesc = ei.EmissionFilterDesc;
                        ind.EmissionFilterPos = ei.EmissionFilterPos;
                        ind.ExcitationFilterDesc = ei.ExcitationFilterDesc;
                        ind.ExcitationFilterPos = ei.ExcitationFilterPos;
                        ind.ExperimentID = ChartArrayControl.VM.Experiment.ExperimentID;
                        ind.Exposure = ei.Exposure;
                        ind.Gain = ei.Gain;
                        ind.MaskID = ei.MaskID;                        
                        ind.SignalType = ei.SignalType;
                        ind.FlatFieldCorrection = ei.FlatFieldCorrection;

                        success = m_wgDB.InsertExperimentIndicator(ref ind);

                        if (success)
                        {
                            ei.ExperimentIndicatorID = ind.ExperimentIndicatorID;
                        }
                        else
                        {
                            ei.ExperimentIndicatorID = 0;
                            ShowErrorDialog("Database Error", "Failed to Insert ExperimentIndicator: " +
                                m_wgDB.GetLastErrorMsg());
                            return false;
                        }
                    }

                    foreach(ExperimentCompoundPlateContainer cp in ChartArrayControl.VM.CompoundPlateList)
                    {
                        ExperimentCompoundPlateContainer comp = new ExperimentCompoundPlateContainer();
                        comp.Barcode = cp.Barcode;
                        comp.Description = cp.Description;
                        comp.ExperimentID = ChartArrayControl.VM.Experiment.ExperimentID;

                        success = m_wgDB.InsertExperimentCompoundPlate(ref comp);

                        if(success)
                        {
                            cp.ExperimentCompoundPlateID = comp.ExperimentCompoundPlateID;
                        }
                        else
                        {
                            cp.ExperimentCompoundPlateID = 0;
                            ShowErrorDialog("Database Error", "Failed to Insert ExperimentCompoundPlate: " +
                                m_wgDB.GetLastErrorMsg());
                            return false;
                        }
                    }
                }
                else 
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

         

            ChartArrayControl.BuildChartArray(VM.Mask.Rows, VM.Mask.Cols,
                                              ChartArrayControl.VM.IndicatorList,
                                              ChartArrayControl.VM.CompoundPlateList);

            // this function builds the display grid (one display for each indicator) and it also
            // sets up the m_imager.m_ImagingDictionary
            ChartArrayControl.BuildDisplayGrid();

            // NOTE: cycle times have not been set!!!  They are currently set to 1000 msecs...hard coded...yikes!!

        
            int numerID = 0;
            int denomID = 0;

            // find DynamicRatioNumerator and DynamicRatioDenominator record IDs
            if (m_dynamicRatioNumerator != null && m_dynamicRatioDenominator != null)
            {
                foreach (ExperimentIndicatorContainer ind in ChartArrayControl.VM.IndicatorList)
                {
                    if (ind.Description == m_dynamicRatioNumerator.Description) numerID = ind.ExperimentIndicatorID;
                    if (ind.Description == m_dynamicRatioDenominator.Description) denomID = ind.ExperimentIndicatorID;
                }
            }
         
            return true;
            
        }


        private bool GetExperimentPlate(string barcode)
        {
            // plate, may or may not already exist in database, so check first to see if it exists, and if not create it
            PlateContainer plate;
            bool success = m_wgDB.GetPlateByBarcode(barcode, out plate);
            if (!success)
            {
                ShowErrorDialog("Datebase Error: GetPlateByBarcode", m_wgDB.GetLastErrorMsg());
                return false;
            }
            if (plate == null)  // plate does not exist, so create it
            {                
                plate = new PlateContainer();
                plate.Barcode = barcode;
                plate.Description = DateTime.Now.ToString() + "/" + 
                    VM.Project.Description + "/" + VM.Method.Description + "/" + VM.User.Lastname + ", " + VM.User.Firstname;
                plate.IsPublic = false;
                plate.OwnerID = GlobalVars.UserID;
                plate.PlateTypeID = VM.PlateType.PlateTypeID;
                plate.ProjectID = VM.Project.ProjectID;

                success = m_wgDB.InsertPlate(ref plate);

                if (!success)
                {
                    ChartArrayControl.VM.ExperimentPlate = null;
                    ShowErrorDialog("Database Error: InsertPlate", m_wgDB.GetLastErrorMsg());
                    return false;
                }
            }

            if (success)
            {
                ChartArrayControl.VM.ExperimentPlate = plate;
            }

            if (ChartArrayControl.VM.ExperimentPlate == null)
            {
                ShowErrorDialog("Experiment Error", "Unable to assign Experiment Plate");
                return false;
            }

            return true;
        }


        private bool CreateExperiment()
        {
            // experiment, create new
            ExperimentContainer experiment = new ExperimentContainer();

            experiment.Description = DateTime.Now.ToString() + "/" + VM.Project.Description + "/" + VM.Method.Description + "/" + 
                                     VM.User.Lastname + ", " + VM.User.Firstname;
            experiment.HorzBinning = ChartArrayControl.VM.HorzBinning;
            experiment.VertBinning = ChartArrayControl.VM.VertBinning;
            experiment.MethodID = VM.Method.MethodID;
            experiment.PlateID = ChartArrayControl.VM.ExperimentPlate.PlateID;
            experiment.ROI_Height = VM.RoiH;
            experiment.ROI_Width = VM.RoiW;
            experiment.ROI_Origin_X = VM.RoiX;
            experiment.ROI_Origin_Y = VM.RoiY;
            experiment.TimeStamp = DateTime.Now;

            bool success = m_wgDB.InsertExperiment(ref experiment);

            if (success)
            {
                if(experiment.ExperimentID!=0)
                {
                    ChartArrayControl.VM.Experiment = experiment;
                }
                else
                {
                    ShowErrorDialog("Datebase Error: InsertExperiment", m_wgDB.GetLastErrorMsg());
                    return false;
                }
            }
            else
            {
                ShowErrorDialog("Datebase Error: InsertExperiment", m_wgDB.GetLastErrorMsg());
                return false;
            }

            return true;
        }




        private void ShowErrorDialog(string title, string errMsg)
        {
            // this is just a convenience function
           
            MessageBox.Show(errMsg, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void RunPB_Click(object sender, RoutedEventArgs e)
        {

            switch (VM.RunState)
            {
                case RunExperiment_ViewModel.RUN_STATE.NEEDS_INPUT:
                    Close_EnclosureCameraViewer();
                    Close();
                break;

                case RunExperiment_ViewModel.RUN_STATE.READY_TO_RUN:

                    if (PrepForRun())
                    {
                        m_tokenSource = new CancellationTokenSource();
                        m_cancelToken = m_tokenSource.Token;
                        m_progress = new Progress<int>();

                        // RUN EXPERIMENT !!
                        VM.RunState = RunExperiment_ViewModel.RUN_STATE.RUNNING;
                        ChartArrayControl.SetStatus(ViewModel_ChartArray.RUN_STATUS.RUNNING);
                        //SetButton(VM.RunState);

                        PostMessage("Starting VWorks Method: " + m_vworksProtocolFilename);
                        m_vworks.StartMethod(m_vworksProtocolFilename);
                    }
               break;

                case RunExperiment_ViewModel.RUN_STATE.RUNNING:                                        
                    AbortExperiment();
                    VM.RunState = RunExperiment_ViewModel.RUN_STATE.RUN_ABORTED;
                    ChartArrayControl.SetStatus(ViewModel_ChartArray.RUN_STATUS.RUN_FINISHED);
                    //SetButton(VM.RunState);
               break;

                case RunExperiment_ViewModel.RUN_STATE.RUN_FINISHED:                    
                    Close_EnclosureCameraViewer();
                    Close();
               break;

                case RunExperiment_ViewModel.RUN_STATE.RUN_ABORTED:                    
                    Close_EnclosureCameraViewer();
                    Close();
               break;

                case RunExperiment_ViewModel.RUN_STATE.ERROR:                    
                    Close_EnclosureCameraViewer();
                    Close();
               break;
            }  // END switch (m_state)

        } // END RunPB_Click




        public void SetButton(RunExperiment_ViewModel.RUN_STATE state)
        {            
            switch (state)
            {
                case RunExperiment_ViewModel.RUN_STATE.READY_TO_RUN:
                    RunPB.Content = "Run";
                    break;
                case RunExperiment_ViewModel.RUN_STATE.RUNNING:
                    RunPB.Content = "Abort";
                    break;
                case RunExperiment_ViewModel.RUN_STATE.RUN_FINISHED:
                    RunPB.Content = "Close";
                    break;
                case RunExperiment_ViewModel.RUN_STATE.RUN_ABORTED:
                    RunPB.Content = "Close";
                    break;
                case RunExperiment_ViewModel.RUN_STATE.ERROR:
                    RunPB.Content = "Close";
                    break;

            }
        }



        //////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////

        


        public void StartVideo(int maxNumImages)
        {
            if (m_imager == null) return;
 
            //m_uiTask = TaskScheduler.FromCurrentSynchronizationContext();

            m_tokenSource = new CancellationTokenSource();
            m_cancelToken = m_tokenSource.Token;

            Progress<int> progress = new Progress<int>();
            progress.ProgressChanged += (sender1, num) =>
            {
                VM.MessageText = num.ToString() + " images";
            };


            // This is probably no necessary since during verification, everything was already set up
            foreach (KeyValuePair<int, ImagingParamsStruct> entry in m_imager.m_ImagingDictionary)
            {
                ImagingParamsStruct ips = entry.Value;
                int key = entry.Key;
                m_imager.ConfigImageDisplaySurface(key, m_imager.m_camera.m_acqParams.BinnedFullImageWidth, m_imager.m_camera.m_acqParams.BinnedFullImageHeight, false);
            }


            if (!m_imager.m_kineticImagingON)
            {
                m_imager.StartKineticImaging(maxNumImages, true);

                m_imager.m_kineticImagingON = true;
            }
           

        }






        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //TemperatureMonitorDialog dlg = new TemperatureMonitorDialog(m_imager.m_camera);

            //dlg.ShowDialog();
        }

        private void EnclosureCameraPB_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (m_enclosureCameraViewer == null)
            {
                m_enclosureCameraViewer = new EnclosureCameraViewer();

                m_enclosureCameraViewer.Closed += m_enclosureCameraViewer_Closed;

                m_enclosureCameraViewer.Show();
            }
            else
            {
                m_enclosureCameraViewer.BringWindowToFront();
            }
        }

        void m_enclosureCameraViewer_Closed(object sender, EventArgs e)
        {
            m_enclosureCameraViewer = null;
        }

        void Close_EnclosureCameraViewer()
        {
            if (m_enclosureCameraViewer != null)
                m_enclosureCameraViewer.Close();
        }

        private void ResetPB_Click(object sender, RoutedEventArgs e)
        {
            ChartArrayControl.Reset();
            VM.RunState = RunExperiment_ViewModel.RUN_STATE.NEEDS_INPUT;
        }




       

        
        private FilterContainer GetExcitationFilterAtPosition(int pos)
        {
            // make sure filter list is updated
            m_wgDB.GetAllFilters();

            foreach(FilterContainer fc in m_wgDB.m_filterList)
            {
                // Filter Changer Designation
                //   0 = Excitation Filter Changer
                //   1 = Emission Filter Changer
                if (fc.FilterChanger == 0 && fc.PositionNumber == pos) return fc;
            }

            return null;
        }


        private FilterContainer GetEmissionFilterAtPosition(int pos)
        {
            // make sure filter list is updated
            m_wgDB.GetAllFilters();

            foreach (FilterContainer fc in m_wgDB.m_filterList)
            {
                // Filter Changer Designation
                //   0 = Excitation Filter Changer
                //   1 = Emission Filter Changer
                if (fc.FilterChanger == 1 && fc.PositionNumber == pos) return fc;
            }

            return null;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////

    } // END RunExperiment Class

    

    public class RunExperiment_ViewModel : INotifyPropertyChanged
    {
        public enum RUN_STATE
        {
            NEEDS_INPUT,
            READY_TO_RUN,
            RUNNING,
            RUN_FINISHED,
            RUN_ABORTED,
            ERROR
        };

        private RUN_STATE _runState;

        private string _messageString;
        private string _delayText;
        private bool _delayHeaderVisible;
        private string _cameraTemperatureText;
        private string _insideTemperatureText;
        private UserContainer _user;
        private ProjectContainer _project;        
        private MethodContainer _method;
        private MaskContainer _mask;
        private PlateTypeContainer _plateType;

        private int _roiX;  // pixel coordinates for ROI
        private int _roiY;
        private int _roiW;
        private int _roiH;
        private int _roiMaskStartRow; // mask apertures inside ROI
        private int _roiMaskEndRow;
        private int _roiMaskStartCol;
        private int _roiMaskEndCol;

        public int RoiX
        {
            get { return this._roiX; }
            set { if (value != this._roiX) { this._roiX = value; NotifyPropertyChanged("RoiX"); } }
        }
        public int RoiY
        {
            get { return this._roiY; }
            set { if (value != this._roiY) { this._roiY = value; NotifyPropertyChanged("RoiY"); } }
        }
        public int RoiW
        {
            get { return this._roiW; }
            set { if (value != this._roiW) { this._roiW = value; NotifyPropertyChanged("RoiW"); } }
        }
        public int RoiH
        {
            get { return this._roiH; }
            set { if (value != this._roiH) { this._roiH = value; NotifyPropertyChanged("RoiH"); } }
        }
        public int RoiMaskStartRow
        {
            get { return this._roiMaskStartRow; }
            set { if (value != this._roiMaskStartRow) { this._roiMaskStartRow = value; NotifyPropertyChanged("RoiMaskStartRow"); } }
        }
        public int RoiMaskEndRow
        {
            get { return this._roiMaskEndRow; }
            set { if (value != this._roiMaskEndRow) { this._roiMaskEndRow = value; NotifyPropertyChanged("RoiMaskEndRow"); } }
        }
        public int RoiMaskStartCol
        {
            get { return this._roiMaskStartCol; }
            set { if (value != this._roiMaskStartCol) { this._roiMaskStartCol = value; NotifyPropertyChanged("RoiMaskStartCol"); } }
        }
        public int RoiMaskEndCol
        {
            get { return this._roiMaskEndCol; }
            set { if (value != this._roiMaskEndCol) { this._roiMaskEndCol = value; NotifyPropertyChanged("RoiMaskEndCol"); } }
        }

        public RUN_STATE RunState { 
            get { return _runState; }
            set { _runState = value; NotifyPropertyChanged("RunState"); } 
        }

      

        public string MessageText { get { return _messageString; } set { _messageString = value; NotifyPropertyChanged("MessageText"); } }
        public string DelayText { get { return _delayText; } set { _delayText = value; NotifyPropertyChanged("DelayText"); } }
        public bool DelayHeaderVisible { get { return _delayHeaderVisible; } set { _delayHeaderVisible = value; NotifyPropertyChanged("DelayHeaderVisible"); } }
        public string CameraTemperatureText { get { return _cameraTemperatureText; } set { _cameraTemperatureText = value; NotifyPropertyChanged("CameraTemperatureText"); } }
        public string InsideTemperatureText { get { return _insideTemperatureText; } set { _insideTemperatureText = value; NotifyPropertyChanged("InsideTemperatureText"); } }

        public UserContainer User { get { return _user; } set { _user = value; NotifyPropertyChanged("User"); } }
        public ProjectContainer Project { get { return _project; } set { _project = value; NotifyPropertyChanged("Project"); } }        
        public MethodContainer Method { get { return _method; } set { _method = value; NotifyPropertyChanged("Method"); } }
        public MaskContainer Mask { get { return _mask; } set { _mask = value; NotifyPropertyChanged("Mask"); } }
        public PlateTypeContainer PlateType { get { return _plateType; } set { _plateType = value; NotifyPropertyChanged("PlateType"); } }

        public RunExperiment_ViewModel()
        {
            DelayText = "";
            DelayHeaderVisible = false;
            CameraTemperatureText = "--";
            InsideTemperatureText = "--";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }

}
