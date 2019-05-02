using Infragistics.Controls.Editors;
using Infragistics.Windows.DataPresenter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
using System.Windows.Threading;
using System.Threading.Tasks.Dataflow;
using System.Threading;
using Infragistics.Windows.Controls;
using WPFTools;
using CudaTools;

namespace Waveguide
{

    public class RunExperimentPanel_PostMessageEventArgs : EventArgs
    {
        private string _message;
        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }
        public RunExperimentPanel_PostMessageEventArgs(string msg)
        {
            _message = msg;
        }
    }

    public partial class RunExperimentControl : UserControl
    {
        ////////////////////////////////////////////////////////////////////////////
        // Close RunExperimentControl Panel Event
        public delegate void CloseRunExperimentPanelEventHandler(object sender, EventArgs e);
        public event CloseRunExperimentPanelEventHandler CloseRunExperimentPanelEvent;

        protected virtual void OnCloseRunExperimentPanel(EventArgs e)
        {
            if (CloseRunExperimentPanelEvent != null) CloseRunExperimentPanelEvent(this, e);
        }

        public void CloseRunExperimentPanel()
        {
            Reset();
            OnCloseRunExperimentPanel(null);
        }

        ////////////////////////////////////////////////////////////////////////////

        // PostMessage RunExperimentControl Panel Event
        public delegate void PostMessage_RunExperimentPanelEventHandler(object sender, RunExperimentPanel_PostMessageEventArgs e);
        public event PostMessage_RunExperimentPanelEventHandler PostMessage_RunExperimentPanelEvent;

        protected virtual void OnPostMessage_RunExperimentPanel(RunExperimentPanel_PostMessageEventArgs e)
        {
            if (PostMessage_RunExperimentPanelEvent != null) PostMessage_RunExperimentPanelEvent(this, e);
        }

        public void PostMessageRunExperimentPanel(string msg)
        {
            OnPostMessage_RunExperimentPanel(new RunExperimentPanel_PostMessageEventArgs(msg));
        }

        ////////////////////////////////////////////////////////////////////////////
        // Bring Window to Front RunExperimentControl Panel Event
        public delegate void BringToFrontRunExperimentPanelEventHandler(object sender, EventArgs e);
        public event BringToFrontRunExperimentPanelEventHandler BringToFrontRunExperimentPanelEvent;

        protected virtual void OnBringToFrontRunExperimentPanel(EventArgs e)
        {
            if (BringToFrontRunExperimentPanelEvent != null) BringToFrontRunExperimentPanelEvent(this, e);
        }

        public void BringToFrontRunExperimentPanel()
        {
            OnBringToFrontRunExperimentPanel(null);
            VM.RunningAutoVerify = false;
        }

        ////////////////////////////////////////////////////////////////////////////

            
  

        public class RangeClass
        {
            public int RangeMin
            {
                get;
                set;
            }

            public int RangeMax
            {
                get;
                set;
            }

        }

        

        public ColorModel m_colorModel;
        WriteableBitmap m_colorMapBitmap;

        public ViewModel_RunExperimentControl VM;

        DispatcherTimer m_runStatusTimer;

        RangeClass m_range;  // limits for color model slider

   

        // a couple of member variables to help us figure out if we need to build the chart array
        int m_chartRows;
        int m_chartCols;
        
        private MultiChartArray.SIGNAL_TYPE m_visibleSignal;

  

                
        private Dictionary<int, ExperimentIndicatorContainer> m_indicatorDictionary;
        private Dictionary<int, bool> m_indicatorVisibleDictionary;
        private Dictionary<int, Color> m_indicatorColor;

  
        public int m_numPoints;

        public int m_rows;
        public int m_cols;

        int m_mouseDownRow, m_mouseUpRow;
        int m_mouseDownCol, m_mouseUpCol;
        Point m_mouseDownPoint, m_mouseUpPoint;
        Point m_dragDown, m_drag;
        double m_colWidth, m_rowHeight;


        WaveguideDB wgDB;

        Imager m_imager;

        VWorks m_vworks;

        Dispatcher m_dispatcher;

        DispatcherTimer m_timer;
        int m_delayTime;

        CancellationTokenSource m_cancelTokenSource;
        CancellationToken m_cancelToken;

        ITargetBlock<Tuple<UInt32[], int, int>> m_analysisPipeline;
        ITargetBlock<Tuple<ushort[], int, int>> m_imageProcessingPipeline;



        bool m_createDisplayForEachIndicator;

        public bool m_optimizeWellsAlreadySet;

        int m_totalPoints;

        public RunExperimentControl()
        {
            m_mouseDownRow = -1;
            m_mouseDownCol = -1; 

            VM = new ViewModel_RunExperimentControl();

            m_indicatorDictionary = new Dictionary<int, ExperimentIndicatorContainer>();
            m_indicatorVisibleDictionary = new Dictionary<int, bool>();
            m_indicatorColor = new Dictionary<int, Color>();

            m_numPoints = 0;

            m_chartRows = 0;
            m_chartCols = 0;
            m_totalPoints = 0;

            m_createDisplayForEachIndicator = false;

            InitializeComponent();

            this.DataContext = VM;
           
            m_visibleSignal = MultiChartArray.SIGNAL_TYPE.RAW;

            m_range = new RangeClass();
            RangeSlider.DataContext = m_range;
            m_range.RangeMin = 0;
            m_range.RangeMax = 100;

            wgDB = new WaveguideDB();


            bool success = wgDB.GetDefaultColorModel(out m_colorModel, GlobalVars.Instance.MaxPixelValue, 1024);

            if(!success || m_colorModel == null)   
            {
                // setup default color model
                m_colorModel = new ColorModel("Default");
                m_colorModel.InsertColorStop(0, 0, 0, 0);
                m_colorModel.InsertColorStop(1023, 255, 255, 255);                     
            }


            m_colorModel.SetMaxPixelValue(GlobalVars.Instance.MaxPixelValue);

            m_colorModel.m_controlPts.Clear();
            m_colorModel.m_controlPts.Add(new ColorControlPoint(0, 0));
            m_colorModel.m_controlPts.Add(new ColorControlPoint(0, 0));
            m_colorModel.m_controlPts.Add(new ColorControlPoint(m_colorModel.m_maxPixelValue, m_colorModel.m_gradientSize - 1));
            m_colorModel.m_controlPts.Add(new ColorControlPoint(m_colorModel.m_maxPixelValue, m_colorModel.m_gradientSize - 1));

            m_colorModel.m_controlPts[1].m_value = 0;
            m_colorModel.m_controlPts[1].m_colorIndex = 0;
            m_colorModel.m_controlPts[2].m_value = 100;
            m_colorModel.m_controlPts[2].m_colorIndex = 1023;
            m_colorModel.BuildColorGradient();
            m_colorModel.BuildColorMap();

            DrawColorMap();


            VM.CameraTemperatureTarget = GlobalVars.Instance.CameraTargetTemperature;
            //VM.CycleTime = GlobalVars.Instance.CameraDefaultCycleTime;
            VM.CameraTemperatureReady = false;

            VM.InsideTemperatureTarget = 10;
            VM.InsideTemperatureReady = false;

            m_runStatusTimer = new DispatcherTimer();
            m_runStatusTimer.Interval = TimeSpan.FromMilliseconds(1000);
            m_runStatusTimer.Tick += m_runStatusTimer_Tick;
            m_runStatusTimer.Start();

            m_dispatcher = this.Dispatcher;

            m_timer = new DispatcherTimer();
            m_timer.Tick += m_timer_Tick;
            m_timer.Interval = TimeSpan.FromMilliseconds(1000);

            VM.RunState = ViewModel_RunExperimentControl.RUN_STATE.NEEDS_INPUT;

            m_optimizeWellsAlreadySet = false;
                     

            // Initialize Reset Behavior            
            //ImagePlateBarcode_ConstantRB.IsChecked = true;
            //foreach(ExperimentCompoundPlateContainer ecpc in VM.ExpParams.compoundPlateList)
            //{

            //}

        }



        void m_imager_m_optimizeEvent(object sender, OptimizeEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => { 
                
                AutoOptimizeViewer.IsOptimizing(e.IndicatorID);
                AutoOptimizeViewer.UpdateData(e.IndicatorID, e.Exposure, e.Gain, e.PreAmpGain, e.Binning);
                if (e.ImageData != null)
                {
                    AutoOptimizeViewer.UpdateImage(e.IndicatorID, e.ImageWidth, e.ImageHeight, e.ImageData);
                }
                   
            }));
          
        }


        void m_imager_m_optimizeStateEvent(object sender, OptimizeStateEventArgs e)
        {
            VM.RunningAutoVerify = e.Running;
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

       

        public async void m_vworks_PostVWorksCommandEvent(object sender, WaveGuideEvents.VWorksCommandEventArgs e)
        {
            VWORKS_COMMAND command = e.Command;
            int param1 = e.Param1;            
            string name = e.Name;
            string desc = e.Description;
            int sequenceNumber = (int)m_imager.GetImagingSequenceTime();
            bool success;
            EventMarkerContainer eventMarker;

            switch(command)
            {
                case VWORKS_COMMAND.Error:
                    PostMessageRunExperimentPanel("VWorks Error: " + e.Description);
                    m_dispatcher.Invoke((Action)(() =>
                    {
                        m_timer.Stop();
                        VM.DelayText = "";
                        VM.DelayHeaderVisible = false;

                        MessageBoxResult result =  MessageBox.Show("VWorks reported an error: " + e.Description + ".  Do you want to continue?\n\n" +
                            "YES = Continue Imaging     NO = Cancel Imaging", "VWorks Error", MessageBoxButton.YesNo, MessageBoxImage.Error);

                        if(result == MessageBoxResult.No)
                        {
                            m_cancelTokenSource.Cancel();  // stops the imaging task
                                                           //VM.RunState = ViewModel_RunExperimentControl.RUN_STATE.ERROR;

                            GlobalVars.Instance.Status = WGStatus.ERROR;
                        }
                        else
                        {
                            GlobalVars.Instance.Status = WGStatus.READY;
                            m_timer.Start();
                        }

                        //VWorksErrorDialog dlg = new VWorksErrorDialog(name + ", " + desc);                        
                    }));
                    break;
                case VWORKS_COMMAND.Event_Marker:
                    PostMessageRunExperimentPanel("VWorks Event Marker");
                    m_dispatcher.Invoke((Action)(() =>
                    {
                        AddEventMarker(sequenceNumber, desc);
                        eventMarker = new EventMarkerContainer();
                        eventMarker.Description = desc;
                        eventMarker.ExperimentID = VM.ExpParams.experiment.ExperimentID;
                        eventMarker.Name = name;
                        eventMarker.SequenceNumber = sequenceNumber - GlobalVars.Instance.EventMarkerLatency;
                        eventMarker.TimeStamp = DateTime.Now;
                        success = wgDB.InsertEventMarker(ref eventMarker);
                        if (!success) PostMessageRunExperimentPanel("Database Error in InsertEventMarker: " + wgDB.GetLastErrorMsg());
                    }));
                    break;
                case VWORKS_COMMAND.Initialization_Complete:
                    PostMessageRunExperimentPanel("VWorks Initialization Complete");
                    m_dispatcher.Invoke((Action)(() =>
                    {
                        BringToFrontRunExperimentPanel();
                    }));
                    break;
                case VWORKS_COMMAND.Message:
                    PostMessageRunExperimentPanel("VWorks Message: " + name + ", " + desc);
                    break;
                case VWORKS_COMMAND.Pause_Until:
                    PostMessageRunExperimentPanel("VWorks Pause Until");
                    m_dispatcher.Invoke((Action)(() =>
                    {
                        m_delayTime = param1;
                        VM.DelayHeaderVisible = true;
                        VM.DelayText = ((int)(m_delayTime / 1000)).ToString();
                        m_timer.Start();
                    }));
                    break;
                case VWORKS_COMMAND.Protocol_Aborted:
                    PostMessageRunExperimentPanel("VWorks Protocol Aborted");                    

                    m_dispatcher.Invoke((Action)(() =>
                    {
                        m_timer.Stop();
                        VM.DelayText = "";
                        m_cancelTokenSource.Cancel();  // stops the imaging task
                        VM.RunState = ViewModel_RunExperimentControl.RUN_STATE.RUN_ABORTED;

                        GlobalVars.Instance.Status = WGStatus.READY;
                    }));
                    break;
                case VWORKS_COMMAND.Protocol_Complete:
                    switch (e.Description)
                    {
                        case "Startup":
                            PostMessageRunExperimentPanel("VWorks Startup Protocol Complete");
                            break;
                        case "Main":
                            PostMessageRunExperimentPanel("VWorks Main Protocol Complete");
                            m_dispatcher.Invoke((Action)(() =>
                            {
                                m_timer.Stop();
                                VM.DelayText = "";
                                VM.DelayHeaderVisible = false;
                                m_cancelTokenSource.Cancel(); // make sure the imaging task stops
                                VM.RunState = ViewModel_RunExperimentControl.RUN_STATE.RUN_FINISHED;

                                if (!VM.ExpParams.method.IsAuto) // on present Report Dialog if this is a Manual method
                                {
                                    ReportDialog dlg = new ReportDialog(VM.ExpParams.project,
                                                                        VM.ExpParams.experiment,
                                                                        VM.ExpParams.indicatorList);
                                    dlg.ShowDialog();
                                }

                                GlobalVars.Instance.Status = WGStatus.READY;
                            }));
                            break;
                        default:
                            break;
                    }
                    
                    break;
                case VWORKS_COMMAND.Protocol_Paused:
                    PostMessageRunExperimentPanel("VWorks Protocol Paused");
                    m_dispatcher.Invoke((Action)(() =>
                    {
                        m_delayTime = param1;
                        VM.DelayHeaderVisible = true;
                        m_timer.Start();
                    }));
                    break;
                case VWORKS_COMMAND.Protocol_Resumed:
                    PostMessageRunExperimentPanel("VWorks Protocol Resumed");
                     m_dispatcher.Invoke((Action)(() =>
                        {
                            m_timer.Stop();
                            VM.DelayText = "";
                            VM.DelayHeaderVisible = false;
                        }));
                    break;
                case VWORKS_COMMAND.Set_Time_Marker:
                    PostMessageRunExperimentPanel("VWorks Set Time Marker");
                    break;
                case VWORKS_COMMAND.Start_Imaging:
                    PostMessageRunExperimentPanel("VWorks Start Imaging");
                    BringToFrontRunExperimentPanel();
                    m_imager.StartKineticImaging(m_imageProcessingPipeline, (int)1000000, m_cancelToken, m_cancelTokenSource);
                    break;
                case VWORKS_COMMAND.Stop_Imaging:
                    PostMessageRunExperimentPanel("VWorks Stop Imaging");
                    m_imager.StopKineticImaging();
                    break;
                case VWORKS_COMMAND.Unrecoverable_Error:
                    PostMessageRunExperimentPanel("VWorks Unrecoverable Error");
                    m_dispatcher.Invoke((Action)(() =>
                    {
                        m_timer.Stop();
                        VM.DelayText = "";
                        m_cancelTokenSource.Cancel();  // stops the imaging task
                        VM.RunState = ViewModel_RunExperimentControl.RUN_STATE.ERROR;

                        GlobalVars.Instance.Status = WGStatus.ERROR;

                        VWorksErrorDialog dlg = new VWorksErrorDialog(name + ", " + desc);

                        GlobalVars.Instance.Status = WGStatus.READY;
                    }));
                    break;
                case VWORKS_COMMAND.Barcode:
                    // Here's how this works:  the barcode passed here from VWorks to Waveguide is assigned to
                    // the first plate checked that has a PlateIDResetBehavior == VWorks (which means we're expecting a barcode for this plate)
                    // AND
                    // doesn't already have a barcode assigned.
                    //
                    // Since when a new run is started, all plates with PlateIDResetBehavior == VWorks have their barcodes cleared, the VWorks
                    // protocol can provide the barcode for the image plate and/or any/all of the compound plates.
                    //
                    // The order of filling barcodes is as follows:
                    //      1 - image plate will be checked first.  If it has PlateIDResetBehavior == VWorks and has no barcode yet, 
                    //          the barcode passed in here would be assigned to the image plate.  If the image plate already has a barcode
                    //          or PlateIDResetBehavior != VWorks, move on to step 2 below.
                    //      2 - iterate the compound plate list (m_expParams.compoundPlateList), assigning this barcode to the first plate
                    //          that has PlateIDResetBehavior == VWorks AND has an empty barcode.
                    //
                    //  If no plates meet this criteria, the barcode is not assigned to any plate, and thus is lost


                    // Step 1 

                    if (VM.ExpParams.experimentPlate.PlateIDResetBehavior == PLATE_ID_RESET_BEHAVIOR.VWORKS && VM.ExpParams.experimentPlate.Barcode == "")
                    {
                        PostMessageRunExperimentPanel("Barcode for Image Plate: " + e.Description);
                        VM.ExpParams.experimentPlate.Barcode = e.Description;
                        // update database
                        success = wgDB.UpdatePlate(VM.ExpParams.experimentPlate);
                        if (!success) MessageBox.Show("Failed to update Experiment Plate in database after receiving barcode from VWorks.", "Database Error", MessageBoxButton.OK);
                    }
                    // Step 2
                    else
                    {
                        foreach (ExperimentCompoundPlateContainer plate in VM.ExpParams.compoundPlateList)
                        {
                            if (plate.PlateIDResetBehavior == PLATE_ID_RESET_BEHAVIOR.VWORKS && plate.Barcode == "")
                            {
                                PostMessageRunExperimentPanel("Barcode for " + plate.Description + ": " + e.Description);
                                plate.Barcode = e.Description;
                                // update database
                                success = wgDB.UpdateExperimentCompoundPlate(plate);
                                if (!success) MessageBox.Show("Failed to update Experiment Compound Plate in database after receiving barcode from VWorks.", "Database Error", MessageBoxButton.OK);
                                break;
                            }
                        }
                    }
                    break;
                case VWORKS_COMMAND.VerifyImaging:
                    // this is used to signal that it's time to verify all of the indicators

                    m_vworks.VWorks_PauseProtocol();  // pause the VWorks protocol (this should already be paused inside the VWorks protocol)


                    AutoOptimizeViewer.Reset();
                    foreach(ExperimentIndicatorContainer eic in VM.ExpParams.indicatorList)
                    {
                        AutoOptimizeViewer.AddIndicator(eic.ExperimentIndicatorID, eic.Description, 1, 1, 0, 1, eic.ExcitationFilterDesc, eic.EmissionFilterDesc);
                    }


                    VM.RunningAutoVerify = true;

                   
                    bool allIndicatorsPassed = await m_imager.StartAutoOptimization(VM.ExpParams.cameraSettings);
                    if(allIndicatorsPassed)
                    {
                        Thread.Sleep(3000);
                        VM.RunningAutoVerify = false;

                        // successfully optimized imaging
                        m_vworks.VWorks_ResumeProtcol();
                        PostMessageRunExperimentPanel("Auto-Optimized All Indicators Successfully");
                    }
                    else
                    {
                        // failed to optimize imaging
                        m_vworks.VWorks_AbortProtocol();
                        AbortExperiment();
                        PostMessageRunExperimentPanel("Auto-Opitimization Failed! Aborting Experiment Run and VWorks Protocol.");
                    }

                    VM.RunningAutoVerify = false;

                    break;
                case VWORKS_COMMAND.PlateStart:
                     Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                if (PrepForRun())
                                {
                                    VM.ExpParams.experimentCurrentPlateNumber++;  // increment the displayed plate count
                                    VM.CurrentPlateNumberText = VM.ExpParams.experimentCurrentPlateNumber.ToString();
                                }
                                else
                                {
                                    MessageBox.Show("Failed to PrepForRun.  Aborting Experiment.");
                                    m_vworks.VWorks_AbortProtocol();
                                    VM.SetRunState(ViewModel_RunExperimentControl.RUN_STATE.READY_TO_RUN);
                                    SetState(ViewModel_RunExperimentControl.RUN_STATE.READY_TO_RUN);
                                }
                            }));
                    break;
                case VWORKS_COMMAND.PlateComplete:
                    // VWorks should have paused the protocol right after sending this, so it will wait until we resume it when we're done
                    // Signal to perform the following:
                    //  1 - Write the report for this plate
                    //  2 - Clear the data and reset the barcodes
                    //  3 - Resume the VWorks protocol
                    if (VM.ExpParams.method.IsAuto)
                    {
                        m_vworks.VWorks_PauseProtocol();  // pause the VWorks protocol 

                        // stop all timers, timer messages, and imaging tasks
                        m_timer.Stop();
                        VM.DelayText = "";
                        VM.DelayHeaderVisible = false;
                        m_cancelTokenSource.Cancel(); // make sure the imaging task stops

                         Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                // 1 - write report
                                {
                           
                                        ReportDialog dlg = new ReportDialog(VM.ExpParams.project,
                                                                        VM.ExpParams.experiment,
                                                                        VM.ExpParams.indicatorList);

                                        bool reportSuccess = dlg.WriteReportFiles(VM.ExpParams.writeWaveguideReport, VM.ExpParams.writeExcelReport);

                                        if (!reportSuccess)
                                        {
                                            PostMessageRunExperimentPanel("Error writing Reports: " + dlg.GetLastError());
                                        }

                           
                                }


                                // 2 - clear data and reset barcodes
                                Reset();  // clears plot data

                                // 3 - resume VWorks protocol
                                m_vworks.VWorks_ResumeProtcol();
                               
                            }));
                    }

                    break;

                case VWORKS_COMMAND.EnableBurstCycleTime:
                    string[] messageList = desc.Split(',');

                    List<int> burstSpeeds = new List<int>();
                    int val;
                    success = true;
                    foreach(string s in messageList)
                    {
                        success = int.TryParse(s, out val);
                        if (success)
                            burstSpeeds.Add(val);
                        else
                            break;
                    }
                    
                    if(success)
                        m_imager.EnableBurstImaging(burstSpeeds);
                    else
                        PostMessageRunExperimentPanel("Error parsing burst speed(s): " + name);
                    break;

                case VWORKS_COMMAND.DisableBurstCycleTime:
                    m_imager.DisableBurstImaging();
                    break;

                   
            }
        }




        void ResetBarcodes()
        {
            // reset image plate
            switch (VM.ExpParams.experimentPlate.PlateIDResetBehavior)
            { 
                case PLATE_ID_RESET_BEHAVIOR.CLEAR:
                    VM.ExpParams.experimentPlate.Barcode = "";
                    break;
                case PLATE_ID_RESET_BEHAVIOR.CONSTANT:
                    break;
                case PLATE_ID_RESET_BEHAVIOR.INCREMENT:
                    {
                        string bc = VM.ExpParams.experimentPlate.Barcode;
                        IncrementBarcode(ref bc);
                        VM.ExpParams.experimentPlate.Barcode = bc;
                    }
                    break;
                case PLATE_ID_RESET_BEHAVIOR.VWORKS:
                    VM.ExpParams.experimentPlate.Barcode = "";
                    break;
            }
            
                       
            // reset compound plates
            foreach (ExperimentCompoundPlateContainer plate in VM.ExpParams.compoundPlateList)
            {
                switch (plate.PlateIDResetBehavior)
                {
                    case PLATE_ID_RESET_BEHAVIOR.CLEAR:
                        plate.Barcode = "";
                        break;
                    case PLATE_ID_RESET_BEHAVIOR.CONSTANT:
                        break;
                    case PLATE_ID_RESET_BEHAVIOR.INCREMENT:
                        {
                            string bc = plate.Barcode;
                            IncrementBarcode(ref bc);
                            plate.Barcode = bc;
                        }
                        break;
                    case PLATE_ID_RESET_BEHAVIOR.VWORKS:
                        plate.Barcode = "";
                        break;
                }
            }
            
        }



        //async void AutoRunPrep(string experimentPlateBarcode, string compoundPlateBarcodeTemplate)
        //{

        //    VM.ExpParams.experimentPlate.Barcode = experimentPlateBarcode;

        //    int i = 1;
        //    foreach(ExperimentCompoundPlateContainer cp in VM.ExpParams.compoundPlateList)
        //    {
        //        cp.Barcode = compoundPlateBarcodeTemplate + "_" + i.ToString();
        //        i++;
        //    }

            
        //    AutoOptimizeViewer.Reset();
        //    foreach (ExperimentIndicatorContainer eic in VM.ExpParams.indicatorList)
        //    {
        //        AutoOptimizeViewer.AddIndicator(eic.ExperimentIndicatorID, eic.Description, 1, 1, 0, 1, eic.ExcitationFilterDesc, eic.EmissionFilterDesc);
        //    }

        //    VM.RunningAutoVerify = true;

        //    bool allIndicatorsPassed = await m_imager.StartAutoOptimization(VM.ExpParams.cameraSettings);
        //    if (allIndicatorsPassed)
        //    {
        //        Thread.Sleep(3000);
        //        VM.RunningAutoVerify = false;

        //        // successfully optimized imaging
        //        PostMessageRunExperimentPanel("Auto-Optimized All Indicators Successfully");
        //    }
        //    else
        //    {
        //        // failed to optimize imaging                                
        //        PostMessageRunExperimentPanel("Auto-Opitimization Failed!");
        //    }

        //    VM.RunningAutoVerify = false;

        //}




        public void IncrementBarcode(ref string barcode)
        {
            if (barcode == null) return;

            if (barcode.Length < 1)
            {   // handle barcode = "", so give a default barcode and start numbering at 1.  Here, the default barcode is "Barcode"
                barcode = "Barcode_01";
            }
            else
            {
                // break apart barcode pieces and put in List
                string[] words = barcode.Split('_');
                List<string> wordList = new List<string>();
                for (int i = 0; i < words.Length; i++) wordList.Add(words[i]);

                if (wordList.Count == 1)
                {   // handle barcode has no underscore, so has no number on end.  Start numbering.
                    barcode = wordList[0] + "_01";
                }
                else
                {
                    // handle if barcode ended in "_"
                    if (wordList[wordList.Count - 1].Length < 1)
                    {
                        wordList[wordList.Count - 1] = "00";
                    }
                    // handle last word is not a number, so add number to end                      
                    else
                    {                       
                        try
                        {
                            if (Convert.ToInt32(wordList[wordList.Count - 1]) < 1)
                            {
                                wordList.Add("00");
                            }
                        }
                        catch (Exception)
                        {
                            wordList.Add("00");
                        }
                    }

                    int intValueOfLastWord = Convert.ToInt32(wordList[wordList.Count - 1]);

                    intValueOfLastWord++;

                    wordList.RemoveAt(wordList.Count - 1);
                    wordList.Add(intValueOfLastWord.ToString("D2"));

                    // build new barcode with incremented value
                    barcode = "";
                    foreach (string s in wordList)
                    {
                        barcode += s + "_";
                    }
                    // remove last "_"
                    barcode = barcode.TrimEnd('_');
                }
            }

        }


        void m_runStatusTimer_Tick(object sender, EventArgs e)
        {
            VM.CameraTemperatureActual = GlobalVars.Instance.CameraTemp;
            VM.InsideTemperatureActual = GlobalVars.Instance.InsideTemp;

            VM.EvalCameraTemperature();
            VM.EvalInsideTemperature();
            VM.EvalRunStatus();
        }
          

     

        void ChartArrayGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGridLines();
        }

      
        public void SetState(ViewModel_RunExperimentControl.RUN_STATE state)
        {
            VM.RunState = state;
        }


        public void Reset()
        {
            VM.Reset();
            ResetBarcodes();

            if(m_cancelTokenSource != null)
                m_cancelTokenSource.Cancel();
        
            CameraSettingsContainer csc;
            bool success = wgDB.GetCameraSettingsDefault(out csc);
            if(success)
            {
                VM.ExpParams.cameraSettings = csc;
            }
        }


        public TaskScheduler GetTaskScheduler()
        {
            return TaskScheduler.FromCurrentSynchronizationContext();
        }


        public void DrawGridLines()
        {           

            VM.GridLines.Clear(); 

            double width = VM.GridLines.Width;
            double height = VM.GridLines.Height;

            double xStep = width / m_cols;
            double yStep = height / m_rows;

            double x = xStep;
            double y = yStep;
            for (int c = 1; c < m_cols; c++ )
            {
                VM.GridLines.DrawLine((int)(c * xStep), 0, (int)(c * xStep), (int)height, Colors.LightGray);
            }

            int offset = 0;
            for (int r = 1; r < m_rows; r++)
            {
                VM.GridLines.DrawLine(0, (int)(r * yStep + offset), (int)width, (int)(r * yStep + offset), Colors.LightGray);
            }         
        }


        public void Configure(Imager imager)
        {
            m_imager = imager;

            m_optimizeWellsAlreadySet = false;

            AutoOptimizeViewer.Configure(m_imager);

            if(m_imager != null)
            {
                VM.InsideHeaterEnabled = m_imager.m_insideHeatingON;

                BuildChartArray();  

                m_imager.SetMask(VM.ExpParams.mask);

                m_imager.m_optimizeEvent += m_imager_m_optimizeEvent;
                m_imager.m_optimizeStateEvent += m_imager_m_optimizeStateEvent;
            }

       
            m_vworks = GlobalVars.Instance.VWorks;

            //m_vworks.PostVWorksCommandEvent += m_vworks_PostVWorksCommandEvent;  // this is now assigned inside the MainWindow.xaml.cs 

            m_vworks.ShowVWorks();

        }

    


        public void BuildChartArray()
        {
            // build the chart array using the data stored in the ExperimentParams Singleton
            BuildChartArray(VM.ExpParams.mask.Rows, VM.ExpParams.mask.Cols);

        }



        public void BuildChartArray(int rows, int cols)
        {
            //if (rows == m_chartRows && cols == m_chartCols)
            //{
            //    return;
            //}

            Stopwatch sw = new Stopwatch();
            sw.Start();


            m_chartRows = rows;
            m_chartCols = cols;

            m_indicatorDictionary = new Dictionary<int, ExperimentIndicatorContainer>();

            m_indicatorVisibleDictionary = new Dictionary<int, bool>();

            m_indicatorColor = new Dictionary<int, Color>();

            m_rows = rows;
            m_cols = cols;



            ////////////////////////////////////////////////////////////////////////////////////////////////////////////

           

            List<WPFTools.MultiChartArray_TraceItem> traces = new List<WPFTools.MultiChartArray_TraceItem>();



            int i = 0;
            foreach (ExperimentIndicatorContainer indicator in VM.ExpParams.indicatorList)
            {
                m_indicatorDictionary.Add(indicator.ExperimentIndicatorID, indicator);
                m_indicatorVisibleDictionary.Add(indicator.ExperimentIndicatorID, true);
                m_indicatorColor.Add(indicator.ExperimentIndicatorID, GlobalVars.Instance.DefaultTraceColorList[i]);

                traces.Add(new WPFTools.MultiChartArray_TraceItem(indicator.Description, i, indicator.ExperimentIndicatorID));

                i++;
            }
            

            MyMultiChartArray.Init(m_rows, m_cols, GlobalVars.Instance.MaxNumberImagesPerExperiment, traces, MyAggregateChart);  // NOTE: allocate for expected max number of points
                                                                                                                        // ~ 120 Megabytes/trace of GPU memory required for every 10,000 points

            i = 0;
            foreach (ExperimentIndicatorContainer indicator in VM.ExpParams.indicatorList)
            {
                MyMultiChartArray.SetTraceColor(WPFTools.MultiChartArray.SIGNAL_TYPE.RAW, i, GlobalVars.Instance.DefaultTraceColorList[i]);
                MyMultiChartArray.SetTraceColor(WPFTools.MultiChartArray.SIGNAL_TYPE.CONTROL_SUBTRACTION, i, GlobalVars.Instance.DefaultTraceColorList[i]);
                MyMultiChartArray.SetTraceColor(WPFTools.MultiChartArray.SIGNAL_TYPE.STATIC_RATIO, i, GlobalVars.Instance.DefaultTraceColorList[i]);
                MyMultiChartArray.SetTraceColor(WPFTools.MultiChartArray.SIGNAL_TYPE.DYNAMIC_RATIO, i, GlobalVars.Instance.DefaultTraceColorList[i]);

                i++;
            }


            MyMultiChartArray.SetDefaultChartRanges(WPFTools.MultiChartArray.SIGNAL_TYPE.RAW, 0, 1, 0, 1);
            MyMultiChartArray.SetDefaultChartRanges(WPFTools.MultiChartArray.SIGNAL_TYPE.CONTROL_SUBTRACTION, 0, 1, 0, 1);
            MyMultiChartArray.SetDefaultChartRanges(WPFTools.MultiChartArray.SIGNAL_TYPE.STATIC_RATIO, 0, 1, 0, 1);
            MyMultiChartArray.SetDefaultChartRanges(WPFTools.MultiChartArray.SIGNAL_TYPE.DYNAMIC_RATIO, 0, 1, 0, 1);



            ////////////////////////////////////////////////////////////////////////////////////////////////////////////


         

            DrawColorMap();

        }


        public void BuildDisplayGrid()
        {           
            VM.Binning = m_imager.m_camera.m_acqParams.HBin;
         
            ImageGrid.Children.Clear();
            ImageGrid.RowDefinitions.Clear();
            ImageGrid.ColumnDefinitions.Clear();

            RowDefinition row0 = new RowDefinition();
            row0.Height = new GridLength(30, GridUnitType.Pixel);

            RowDefinition row1= new RowDefinition();
            row1.Height = new GridLength(1, GridUnitType.Star);

            ImageGrid.RowDefinitions.Add(row0);  // row for the indicator color selector(s)
            ImageGrid.RowDefinitions.Add(row1);  // row for the image display

            SolidColorBrush brush = new SolidColorBrush(Colors.LightGray);


            int i = 0;
            foreach(KeyValuePair<int, ExperimentIndicatorContainer> entry in m_indicatorDictionary)
            {               
                ExperimentIndicatorContainer indicator = entry.Value;
                int expIndicatorID = entry.Key;
                ColumnDefinition colDef = new ColumnDefinition();
                colDef.Width = new GridLength(1, GridUnitType.Star);
                
                ImageGrid.ColumnDefinitions.Add(colDef);

                //StackPanel stack = new StackPanel();
                //CheckBox chkBox = new CheckBox();
                //chkBox.Width = Double.NaN; // sets the width to "Auto"
                //chkBox.Content = indicator.Description;
                //chkBox.FontSize = 16;
                //chkBox.FontWeight = FontWeights.Bold;
                //chkBox.Tag = expIndicatorID;
                //chkBox.IsChecked = true;
                //chkBox.Checked += Indicator_ChkBox_Checked;
                //chkBox.Unchecked += Indicator_ChkBox_Checked;                

                //XamColorPicker colorPicker = new XamColorPicker();
                //colorPicker.DerivedPalettesCount = 10;
                //colorPicker.SelectedColor = m_indicatorColor[expIndicatorID];
                //colorPicker.Width = 50;
                //colorPicker.Height = 10;
                //colorPicker.Tag = expIndicatorID;
                //colorPicker.Margin = new Thickness(10, 0, 0, 0);
                //colorPicker.ShowAdvancedEditorButton = true;
                //colorPicker.SelectedColorChanged += colorPicker_SelectedColorChanged;

                //ColorPalette MyColorPalette = new ColorPalette();
                //MyColorPalette.Colors.Add(Colors.Red);
                //MyColorPalette.Colors.Add(Colors.Orange);
                //MyColorPalette.Colors.Add(Colors.Yellow);
                //MyColorPalette.Colors.Add(Colors.Green);
                //MyColorPalette.Colors.Add(Colors.Blue);
                //MyColorPalette.Colors.Add(Colors.Purple);
                //MyColorPalette.Colors.Add(Colors.Magenta);
                //MyColorPalette.Colors.Add(Colors.White);
                //MyColorPalette.Colors.Add(Colors.LightBlue);
                //MyColorPalette.Colors.Add(Colors.HotPink);
                //colorPicker.ColorPalettes.Clear();
                //colorPicker.ColorPalettes.Add(MyColorPalette);                

                //stack.Orientation = Orientation.Horizontal;
                //stack.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                //stack.Children.Add(chkBox);
                //stack.Children.Add(colorPicker);
                //Grid.SetColumn(stack, i);
                //Grid.SetRow(stack, 0);
                //ImageGrid.Children.Add(stack);


                ImagingParamsStruct ips;
                if (m_imager.m_ImagingDictionary.TryGetValue(indicator.ExperimentIndicatorID, out ips))
                {
                    if (m_createDisplayForEachIndicator || i == 0)  // createDisplayForEachIndicator == true, then create new ImageDisplay for each indicator
                    {
                        ips.ImageControl.Margin = new Thickness(2);
                        Grid.SetColumn(ips.ImageControl, i);
                        Grid.SetRow(ips.ImageControl, 1);

                        ImageGrid.Children.Add(ips.ImageControl);

                        m_imager.ConfigImageDisplaySurface(indicator.ExperimentIndicatorID,
                                               m_imager.m_camera.m_acqParams.BinnedFullImageWidth,
                                               m_imager.m_camera.m_acqParams.BinnedFullImageHeight, false);                        
                    }

                    if (!m_createDisplayForEachIndicator) Grid.SetColumnSpan(ips.ImageControl, i + 1);

                }



                i++;
            }

       
        }




        public Dictionary<int,ImagingParamsStruct> GetImageDisplayDictionary()
        {
            return m_imager.m_ImagingDictionary;
        }

        public Dictionary<int,ExperimentIndicatorContainer> GetExperimentIndicatorDictionary()
        {
            return m_indicatorDictionary;
        }



        public void CleanUp()
        {
          
            GC.SuppressFinalize(this);
        }


    



        public void AddEventMarker(int sequenceNumber, string text)
        {
          
        }

 





        public void AppendNewData(float[] dataRaw, 
                                  float[] dataStaticRatio,
                                  float[] dataControlSubtraction, 
                                  float[] dataDynamicRatio,
                                  int sequenceNumber, int indicatorID)
        {
            
            Stopwatch sw = new Stopwatch();

            sw.Start();

            int numValues = dataRaw.Length;
            var t = Enumerable.Repeat<float>((float)sequenceNumber, numValues).ToArray();  // Or ToList(), etc.
            
            if(dataRaw != null)
                MyMultiChartArray.AppendData(t, dataRaw, WPFTools.MultiChartArray.SIGNAL_TYPE.RAW, indicatorID);

            if(dataControlSubtraction != null)
                MyMultiChartArray.AppendData(t, dataControlSubtraction, WPFTools.MultiChartArray.SIGNAL_TYPE.CONTROL_SUBTRACTION, indicatorID);

            if(dataStaticRatio != null)
                MyMultiChartArray.AppendData(t, dataStaticRatio, WPFTools.MultiChartArray.SIGNAL_TYPE.STATIC_RATIO, indicatorID);

            if(dataDynamicRatio != null)
                MyMultiChartArray.AppendData(t, dataDynamicRatio, WPFTools.MultiChartArray.SIGNAL_TYPE.DYNAMIC_RATIO, indicatorID);

            sw.Stop();

            long t3 = sw.ElapsedMilliseconds;

        }
























        private void RangeSlider_TrackFillDragCompleted(object sender, object e)        
        {
            return;
            UInt16 lowerSliderValue = (UInt16)(RangeMinThumb.Value);
            UInt16 upperSliderValue = (UInt16)(RangeMaxThumb.Value);

            //m_colorModel.m_controlPts[1].m_value = (int)lowerSliderValue;
            //m_colorModel.m_controlPts[1].m_colorIndex = 0;
            //m_colorModel.m_controlPts[2].m_value = (int)upperSliderValue;
            //m_colorModel.m_controlPts[2].m_colorIndex = 1023;
            //m_colorModel.BuildColorMap();
            //DrawColorMap();

            foreach (KeyValuePair<int,ImagingParamsStruct> entry in m_imager.m_ImagingDictionary)
            {
                ushort lowerVal = (ushort)(((float)lowerSliderValue / 100.0f) * (float)GlobalVars.Instance.MaxPixelValue);
                ushort upperVal = (ushort)(((float)upperSliderValue / 100.0f) * (float)GlobalVars.Instance.MaxPixelValue);

                m_imager.m_RangeSliderLowerSliderPosition = lowerVal;
                m_imager.m_RangeSliderUpperSliderPosition = upperVal;

                m_imager.RedisplayCurrentImage(entry.Key, lowerVal, upperVal);
                break;
            }
        }

        private void RangeMinThumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            UInt16 lowerSliderValue = (UInt16)(RangeMinThumb.Value);
            UInt16 upperSliderValue = (UInt16)(RangeMaxThumb.Value);

            //m_colorModel.m_controlPts[1].m_value = (int)RangeMinThumb.Value;
            //m_colorModel.m_controlPts[1].m_colorIndex = 0;
            //m_colorModel.BuildColorMap();
            //DrawColorMap();

            m_imager.SetColorModel(m_colorModel);

            foreach (KeyValuePair<int,ImagingParamsStruct> entry in m_imager.m_ImagingDictionary)
            {
                ushort lowerVal = (ushort)(((float)lowerSliderValue / 100.0f) * (float)GlobalVars.Instance.MaxPixelValue);
                ushort upperVal = (ushort)(((float)upperSliderValue / 100.0f) * (float)GlobalVars.Instance.MaxPixelValue);

                m_imager.m_RangeSliderLowerSliderPosition = lowerVal;
                m_imager.m_RangeSliderUpperSliderPosition = upperVal;

                m_imager.RedisplayCurrentImage(entry.Key, lowerVal, upperVal);
                break;
            }
        }

        private void RangeMaxThumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            UInt16 lowerSliderValue = (UInt16)(RangeMinThumb.Value);
            UInt16 upperSliderValue = (UInt16)(RangeMaxThumb.Value);

            //m_colorModel.m_controlPts[2].m_value = (int)RangeMaxThumb.Value;
            //m_colorModel.m_controlPts[2].m_colorIndex = 1023;
            //m_colorModel.BuildColorMap();
            //DrawColorMap();

            foreach (KeyValuePair<int, ImagingParamsStruct> entry in m_imager.m_ImagingDictionary)
            {
                ushort lowerVal = (ushort)(((float)lowerSliderValue / 100.0f) * (float)GlobalVars.Instance.MaxPixelValue);
                ushort upperVal = (ushort)(((float)upperSliderValue / 100.0f) * (float)GlobalVars.Instance.MaxPixelValue);

                m_imager.m_RangeSliderLowerSliderPosition = lowerVal;
                m_imager.m_RangeSliderUpperSliderPosition = upperVal;

                m_imager.RedisplayCurrentImage(entry.Key, lowerVal, upperVal);
                break;
            }

            sw.Stop();
            long t1 = sw.ElapsedMilliseconds;
            t1 += 0;
        }

       

        public void DrawColorMap()
        {
            if (m_colorModel == null) return;

            int colorMapWidth = 40;

            if (m_colorMapBitmap == null)
            {
                m_colorMapBitmap = BitmapFactory.New(colorMapWidth, m_colorModel.m_maxPixelValue);
                ColorMapImage.Source = m_colorMapBitmap;
            }

            for (int i = 0; i < m_colorModel.m_maxPixelValue; i++)
            {
                Color color = new Color();
                color.A = 255;
                color.R = m_colorModel.m_colorMap[i].m_red;
                color.G = m_colorModel.m_colorMap[i].m_green;
                color.B = m_colorModel.m_colorMap[i].m_blue;

                m_colorMapBitmap.DrawLine(0, m_colorModel.m_maxPixelValue - 1 - i, colorMapWidth, m_colorModel.m_maxPixelValue - 1 - i, color);
            }
        }





        private void ColorMapImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WaveguideDB wgDB = new WaveguideDB();

            bool success = wgDB.GetAllColorModels();

            if (success)
            {
                ColorModelSelectDialog diag = new ColorModelSelectDialog(wgDB.m_colorModelList);
                diag.ShowDialog();

                int colorModelID = diag.dbID;

                for (int i = 0; i < wgDB.m_colorModelList.Count(); i++)
                {
                    if (wgDB.m_colorModelList[i].ColorModelID == colorModelID)
                    {
                        ColorModel model = new ColorModel(wgDB.m_colorModelList[i].Description, wgDB.m_colorModelList[i].MaxPixelValue, wgDB.m_colorModelList[i].GradientSize);
                        for (int j = 0; j < wgDB.m_colorModelList[i].Stops.Count(); j++)
                        {
                            model.InsertColorStop(wgDB.m_colorModelList[i].Stops[j].ColorIndex,
                                                  wgDB.m_colorModelList[i].Stops[j].Red,
                                                  wgDB.m_colorModelList[i].Stops[j].Green,
                                                  wgDB.m_colorModelList[i].Stops[j].Blue);
                        }

                        model.SetMaxPixelValue(GlobalVars.Instance.MaxPixelValue);

                        model.m_controlPts.Clear();
                        model.m_controlPts.Add(new ColorControlPoint(0, 0));
                        model.m_controlPts.Add(new ColorControlPoint(0, 0));
                        model.m_controlPts.Add(new ColorControlPoint(model.m_maxPixelValue, model.m_gradientSize - 1));
                        model.m_controlPts.Add(new ColorControlPoint(model.m_maxPixelValue, model.m_gradientSize - 1));

                        model.m_controlPts[1].m_value = (int)RangeMinThumb.Value;
                        model.m_controlPts[1].m_colorIndex = 0;
                        model.m_controlPts[2].m_value = (int)RangeMaxThumb.Value;
                        model.m_controlPts[2].m_colorIndex = 1023;
                        model.BuildColorGradient();
                        model.BuildColorMap();

                        m_colorModel = model;

                        DrawColorMap();

                        UInt16 lowerSliderValue = (UInt16)(RangeMinThumb.Value);
                        UInt16 upperSliderValue = (UInt16)(RangeMaxThumb.Value);

                        foreach (KeyValuePair<int, ImagingParamsStruct> entry in m_imager.m_ImagingDictionary)
                        {
                            int experimentIndicatorID = entry.Key;

                            m_imager.SetColorModel(m_colorModel);

                            m_imager.RedisplayCurrentImage(experimentIndicatorID,lowerSliderValue,upperSliderValue);
                        }

                        break;
                    }
                }
            }
           
        }


        private void CameraTemperatureEdit_ValueChanged(object sender, EventArgs e)
        {
            GlobalVars.Instance.CameraTargetTemperature = VM.CameraTemperatureTarget;

            if (m_imager != null)
                m_imager.m_camera.SetCoolerTemp(VM.CameraTemperatureTarget);

            VM.EvalCameraTemperature();
        }


     
   

        private void VerifyPB_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            DataRecord record = btn.DataContext as DataRecord;
            ExperimentIndicatorContainer indicator = (ExperimentIndicatorContainer)record.DataItem;


            ObservableCollection<ExperimentIndicatorContainer> expParam_indicators = VM.ExpParams.indicatorList;
            Dictionary<int,ImagingParamsStruct> imagingDictionary = m_imager.m_ImagingDictionary;



            ManualControlDialog dlg = new ManualControlDialog(m_imager, indicator.ExperimentIndicatorID, false, false);


            FilterContainer exFilt, emFilt;
            exFilt = null;
            emFilt = null;
            int previousBinning = VM.Binning; 
            CameraSettingsContainer previousCameraSettings = VM.ExpParams.cameraSettings;

            bool success = wgDB.GetAllExcitationFilters();
            if (success)
            {
                foreach (FilterContainer fc in wgDB.m_filterList)
                {
                    if (fc.PositionNumber == indicator.ExcitationFilterPos)
                        exFilt = fc;
                }
            }

            success = wgDB.GetAllEmissionFilters();
            if (success)
            {
                foreach (FilterContainer fc in wgDB.m_filterList)
                {
                    if (fc.PositionNumber == indicator.EmissionFilterPos)
                        emFilt = fc;
                }
            }


            dlg.Title = indicator.Description;
            dlg.CameraSetupControl.vm.Exposure = indicator.Exposure;
            dlg.CameraSetupControl.vm.EMGain = indicator.Gain;
            dlg.CameraSetupControl.vm.EmFilter = emFilt;
            dlg.CameraSetupControl.vm.ExFilter = exFilt;
            dlg.CameraSetupControl.vm.CycleTime = indicator.CycleTime;

            foreach(FlatFieldCorrectionItem ffci in dlg.CameraSetupControl.vm.FlatFieldCorrectionItems)
            {
                if (ffci.FlatField_Select == indicator.FlatFieldCorrection)
                    dlg.CameraSetupControl.vm.FlatFieldSelect = ffci;
            }
            
            foreach(FilterContainer fc in dlg.CameraSetupControl.vm.EmFilterList)
            {
                if (fc.PositionNumber == indicator.EmissionFilterPos)
                    dlg.CameraSetupControl.vm.EmFilter = fc;
            }

            foreach (FilterContainer fc in dlg.CameraSetupControl.vm.ExFilterList)
            {
                if (fc.PositionNumber == indicator.ExcitationFilterPos)
                    dlg.CameraSetupControl.vm.ExFilter = fc;
            }


            if (VM.CurrentCameraSettings == null)
            {
                CameraSettingsContainer cameraSettings;
                success = wgDB.GetCameraSettingsDefault(out cameraSettings);
                if(success)
                {
                    dlg.CameraSetupControl.vm.CurrentCameraSettings = cameraSettings;
                }
                else
                {
                    dlg.CameraSetupControl.vm.CurrentCameraSettings = dlg.CameraSetupControl.vm.CameraSettingsList[0];
                }
            }
            else
                dlg.CameraSetupControl.vm.CurrentCameraSettings = VM.CurrentCameraSettings;

            foreach(CameraSettingsContainer csc in dlg.CameraSetupControl.vm.CameraSettingsList)
            {
                if (csc.CameraSettingID == dlg.CameraSetupControl.vm.CurrentCameraSettings.CameraSettingID)
                    dlg.CameraSetupControl.CameraSettingsCB.SelectedItem = dlg.CameraSetupControl.vm.CurrentCameraSettings;
            }


            dlg.QuitPB.Content = "Done";

            dlg.CameraSetupControl.CameraSettingsCB.IsEnabled = true;
            dlg.CameraSetupControl.StartVideoPB.Visibility = System.Windows.Visibility.Hidden;
            dlg.CameraSetupControl.SaveImagePB.Visibility = System.Windows.Visibility.Hidden;    
           

            dlg.ShowDialog();

            indicator.Exposure = dlg.CameraSetupControl.vm.Exposure;
            indicator.Gain = dlg.CameraSetupControl.vm.EMGain;
            indicator.PreAmpGain = dlg.CameraSetupControl.vm.PreAmpGainIndex;
            indicator.CycleTime = dlg.CameraSetupControl.vm.CycleTime;
            indicator.FlatFieldCorrection = dlg.CameraSetupControl.vm.FlatFieldSelect.FlatField_Select;
            VM.CurrentCameraSettings = dlg.CameraSetupControl.vm.CurrentCameraSettings;


            if(m_imager.m_ImagingDictionary.ContainsKey(indicator.ExperimentIndicatorID))
            {
                ImagingParamsStruct ips = m_imager.m_ImagingDictionary[indicator.ExperimentIndicatorID];
                ips.exposure = indicator.Exposure;
                ips.gain = indicator.Gain;
                ips.binning = dlg.CameraSetupControl.vm.Binning;
                ips.cycleTime = indicator.CycleTime;
                ips.flatfieldType = indicator.FlatFieldCorrection;
                ips.preAmpGainIndex = indicator.PreAmpGain;
            }


            //switch(dlg.CameraSetupControl.vm.Binning)
            //{
            //    case 1:
            //        Binning_1x1_RadioButton.IsChecked = true;
            //        break;
            //    case 2:
            //        Binning_2x2_RadioButton.IsChecked = true;
            //        break;
            //    case 4:
            //        Binning_4x4_RadioButton.IsChecked = true;
            //        break;                
            //}

         
            // if the binning or camera settings were changed, un-verify all other indicators         
            if (VM.Binning != previousBinning || VM.ExpParams.cameraSettings != previousCameraSettings) // binning or camera settings were changed
            {
                foreach (ExperimentIndicatorContainer ind in VM.ExpParams.indicatorList)
                {
                    ind.Verified = false;
                }
            }

            indicator.Verified = true;

            VM.EvalRunStatus();

            dlg = null;
            GC.Collect();
            
        }

             


        private void CompoundPlateDataGrid_CellUpdated(object sender, Infragistics.Windows.DataPresenter.Events.CellUpdatedEventArgs e)
        {
            VM.EvalRunStatus();
        }

        private void EnclosureCameraPB_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@"C:/Users/greenwb1/Documents/Visual Studio 2013/Projects/CameraViewer/CameraViewer/bin/Debug/CameraViewer.exe");
            }
            catch(Exception exp)
            {
                MessageBox.Show("Failed to Start Camera Viewer: " + exp.Message, "Error");
            }
        }

        private void ResetPB_Click(object sender, RoutedEventArgs e)
        {
            Reset();
            VM.RunState = ViewModel_RunExperimentControl.RUN_STATE.NEEDS_INPUT;
        }

        private void ClosePB_Click(object sender, RoutedEventArgs e)
        {
            ResetPB_Click(null, null);
            CloseRunExperimentPanel();

            GlobalVars.Instance.Status = WGStatus.ONLINE;
        }

        private void AbortExperiment()
        {
            m_vworks.VWorks_AbortProtocol();
           
            if(m_cancelTokenSource != null)
                m_cancelTokenSource.Cancel();  // stops the imaging task
        }

        public void Run()
        {
            RunPB_Click(null,null);
        }

        private void RunPB_Click(object sender, RoutedEventArgs e)
        {
            switch (VM.RunState)
            {
                case ViewModel_RunExperimentControl.RUN_STATE.NEEDS_INPUT:
                    //Close_EnclosureCameraViewer();
                    //Close();
                    //CloseRunExperimentPanel();
                    break;

                case ViewModel_RunExperimentControl.RUN_STATE.READY_TO_RUN:
                    VM.SetRunState(ViewModel_RunExperimentControl.RUN_STATE.RUNNING);
                    SetState(ViewModel_RunExperimentControl.RUN_STATE.RUNNING);
                    
                    if (PrepForRun())
                    {                      
                        // RUN EXPERIMENT !!                        
                        PostMessageRunExperimentPanel("Starting VWorks Method: " + VM.ExpParams.method.BravoMethodFile);
                        if (!VM.ExpParams.method.IsAuto) VM.ExpParams.experimentRunPlateCount = 1;
                        if (VM.ExpParams.experimentRunPlateCount<1) VM.ExpParams.experimentRunPlateCount = 1;
                        VM.ExpParams.experimentCurrentPlateNumber = 0;
                        VM.CurrentPlateNumberText = VM.ExpParams.experimentCurrentPlateNumber.ToString();
                        m_vworks.StartMethod(VM.ExpParams.method.BravoMethodFile, VM.ExpParams.experimentRunPlateCount);

                        GlobalVars.Instance.Status = WGStatus.RUNNING;
                    }
                    else
                    {
                        MessageBox.Show("Failed to Run Experiment");
                        VM.SetRunState(ViewModel_RunExperimentControl.RUN_STATE.READY_TO_RUN);
                        SetState(ViewModel_RunExperimentControl.RUN_STATE.READY_TO_RUN);
                    }
                    break;

                case ViewModel_RunExperimentControl.RUN_STATE.RUNNING:
                    AbortExperiment();
                    VM.RunState = ViewModel_RunExperimentControl.RUN_STATE.RUN_ABORTED;
                    //ChartArrayControl.SetStatus(ViewModel_ChartArray.RUN_STATUS.RUN_FINISHED); 
                    GlobalVars.Instance.Status = WGStatus.READY;                 
                    break;

                case ViewModel_RunExperimentControl.RUN_STATE.RUN_FINISHED:
                    //Close_EnclosureCameraViewer();
                    //Close();
                    //CloseRunExperimentPanel();
                    break;

                case ViewModel_RunExperimentControl.RUN_STATE.RUN_ABORTED:
                    //Close_EnclosureCameraViewer();
                    //Close();
                    //CloseRunExperimentPanel();
                    break;

                case ViewModel_RunExperimentControl.RUN_STATE.ERROR:
                    //Close_EnclosureCameraViewer();
                    //Close();
                    //CloseRunExperimentPanel();
                    break;
            }
        }



        private bool PrepForRun()
        {         

            //////////////////////////////////////////////////////////////////
            // Create ExperimentPlate, if doesn't already exist 
            bool success;
            string barcode = VM.ExpParams.experimentPlate.Barcode; // if VM.ExpParams.method.ImagePlateBarcodeReset == PLATE_ID_RESET_BEHAVIOR.VWORKS, then barcode will be empty.  Don't worry, it will be set during the run.

            if (GetExperimentPlate(barcode))
            {
                // successfully created/retrieved ExperimentPlate, so now create Experiment
                if (CreateExperiment())
                {
                    foreach (ExperimentIndicatorContainer ei in VM.ExpParams.indicatorList)
                    {
                        ExperimentIndicatorContainer ind = new ExperimentIndicatorContainer();
                        ind.Description = ei.Description;
                        ind.EmissionFilterDesc = ei.EmissionFilterDesc;
                        ind.EmissionFilterPos = ei.EmissionFilterPos;
                        ind.ExcitationFilterDesc = ei.ExcitationFilterDesc;
                        ind.ExcitationFilterPos = ei.ExcitationFilterPos;
                        ind.ExperimentID = VM.ExpParams.experiment.ExperimentID;
                        ei.ExperimentID = VM.ExpParams.experiment.ExperimentID;
                        ind.Exposure = ei.Exposure;
                        ind.Gain = ei.Gain;
                        ind.PreAmpGain = ei.PreAmpGain;
                        ind.MaskID = VM.ExpParams.mask.MaskID;
                        ei.MaskID = VM.ExpParams.mask.MaskID;
                        ind.SignalType = ei.SignalType;
                        ind.FlatFieldCorrection = ei.FlatFieldCorrection;

                        success = wgDB.InsertExperimentIndicator(ref ind);

                        if (success)
                        {
                            ei.ExperimentIndicatorID = ind.ExperimentIndicatorID;
                        }
                        else
                        {
                            ei.ExperimentIndicatorID = 0;
                            ShowErrorDialog("Database Error", "Failed to Insert ExperimentIndicator: " +
                                wgDB.GetLastErrorMsg());
                            return false;
                        }
                    }

                    foreach (ExperimentCompoundPlateContainer cp in VM.ExpParams.compoundPlateList)
                    {
                        ExperimentCompoundPlateContainer comp = new ExperimentCompoundPlateContainer();
                        comp.Barcode = cp.Barcode;
                        comp.Description = cp.Description;
                        comp.ExperimentID = VM.ExpParams.experiment.ExperimentID;
                        cp.ExperimentID = VM.ExpParams.experiment.ExperimentID;

                        success = wgDB.InsertExperimentCompoundPlate(ref comp);

                        if (success)
                        {
                            cp.ExperimentCompoundPlateID = comp.ExperimentCompoundPlateID;
                        }
                        else
                        {
                            cp.ExperimentCompoundPlateID = 0;
                            ShowErrorDialog("Database Error", "Failed to Insert ExperimentCompoundPlate: " +
                                wgDB.GetLastErrorMsg());
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


            

            // rebuild m_imager.m_imagingDictionary

                // get binning from first entry in m_imager.m_imagingDictionary (all are same, so getting first one will suffice)
                int binning = 1;
                foreach(ImagingParamsStruct ips in m_imager.m_ImagingDictionary.Values)
                {
                    binning = ips.binning;
                    break;
                }

                ImageDisplay imageDisplay = new ImageDisplay();

                m_imager.ResetImagingDictionary();
                foreach(ExperimentIndicatorContainer eic in VM.ExpParams.indicatorList)
                {
                    ImagingParamsStruct ips = new ImagingParamsStruct();

                    ips.binning = binning;
                    ips.cycleTime = eic.CycleTime;
                    ips.emissionFilterPos = (byte)eic.EmissionFilterPos;
                    ips.excitationFilterPos = (byte)eic.ExcitationFilterPos;
                    ips.experimentIndicatorID = eic.ExperimentIndicatorID;
                    ips.exposure = (float)eic.Exposure / 1000.0f;
                    ips.flatfieldType = eic.FlatFieldCorrection;                    
                    ips.gain = eic.Gain;
                    ips.indicatorName = eic.Description;
                    ips.optimizeWellList = null;
                    ips.preAmpGainIndex = eic.PreAmpGain;

                    // these next two are set up in BuildDisplayGrid() called below
                    ips.histBitmap = null;
                    if (m_createDisplayForEachIndicator)
                    {
                        ips.ImageControl = new ImageDisplay();
                    }
                    else
                    {
                        ips.ImageControl = imageDisplay;
                    }

                    // set up the optimize well list
                    if(VM.ExpParams.method.IsAuto)
                    {
                        if (!m_optimizeWellsAlreadySet)
                        {
                            int rows = VM.ExpParams.plateType.Rows;
                            int cols = VM.ExpParams.plateType.Cols;
                            string desc = "Select Optimize Wells for Indicator: " + eic.Description;
                            WellSelectionDialog dlg = new WellSelectionDialog(rows, cols, desc, true);

                            dlg.ShowDialog();

                            if (dlg.m_accepted)
                            {
                                if (ips.optimizeWellList == null)
                                {
                                    ips.optimizeWellList = new ObservableCollection<Tuple<int, int>>();
                                }

                                eic.OptimizeWellList.Clear();
                                ips.optimizeWellList.Clear();
                                foreach (Tuple<int, int> well in dlg.m_wellList)
                                {
                                    eic.OptimizeWellList.Add(well);
                                    ips.optimizeWellList.Add(well);
                                }
                            }
                        }
                        else
                        {
                            // optimize well list was already set at the start of this experiment, so  just copy them from the ExpParams.indicatorList
                            if (ips.optimizeWellList == null)
                            {
                                ips.optimizeWellList = new ObservableCollection<Tuple<int, int>>();
                            }
                            ips.optimizeWellList.Clear();
                            foreach (Tuple<int, int> well in eic.OptimizeWellList)
                            {                                
                                ips.optimizeWellList.Add(well);
                            }
                        }
                    }

                    m_imager.m_ImagingDictionary.Add(ips.experimentIndicatorID, ips);
                    
                }


                m_optimizeWellsAlreadySet = true;

                BuildChartArray();                
                            
                BuildDisplayGrid();

      
            // prepare flat field correction for each indicator
            Dictionary<int, FLATFIELD_SELECT> ffcDictionary = new Dictionary<int, FLATFIELD_SELECT>();
            foreach (ExperimentIndicatorContainer ei in VM.ExpParams.indicatorList)
            {
                ffcDictionary.Add(ei.ExperimentIndicatorID, ei.FlatFieldCorrection);
            }

      

            // build Analysis Pipeline
            int numeratorID = 0;
            int denominatorID = 0;
            if (VM.ExpParams.dynamicRatioNumerator != null) numeratorID = VM.ExpParams.dynamicRatioNumerator.ExperimentIndicatorID;
            if (VM.ExpParams.dynamicRatioDenominator != null) denominatorID = VM.ExpParams.dynamicRatioDenominator.ExperimentIndicatorID;

            m_analysisPipeline = m_imager.CreateAnalysisPipeline(this, VM.ExpParams.controlSubtractionWellList, VM.ExpParams.numFoFrames, numeratorID, denominatorID);

         

            // build Image Processing Pipeline
            m_cancelTokenSource = new CancellationTokenSource();
            m_cancelToken = m_cancelTokenSource.Token;

            m_imageProcessingPipeline = m_imager.CreateImageProcessingPipeline(GlobalVars.Instance.UITask,m_cancelToken,VM.ExpParams.mask,true,true,VM.ExpParams.project.ProjectID,VM.ExpParams.experimentPlate.PlateID,VM.ExpParams.experiment.ExperimentID,m_analysisPipeline);

            return true;

        }





        private bool CreateExperiment()
        {
            // experiment, create new
            ExperimentContainer experiment = new ExperimentContainer();

            experiment.Description = DateTime.Now.ToString() + "/" + VM.ExpParams.project.Description + "/" + VM.ExpParams.method.Description + "/" +
                                     VM.ExpParams.user.Lastname + ", " + VM.ExpParams.user.Firstname;
            experiment.HorzBinning = VM.Binning;
            experiment.VertBinning = VM.Binning;
            experiment.MethodID = VM.ExpParams.method.MethodID;
            experiment.PlateID = VM.ExpParams.experimentPlate.PlateID;
            experiment.ROI_Height = m_imager.m_camera.m_acqParams.RoiH;
            experiment.ROI_Width = m_imager.m_camera.m_acqParams.RoiW;
            experiment.ROI_Origin_X = m_imager.m_camera.m_acqParams.RoiX;
            experiment.ROI_Origin_Y = m_imager.m_camera.m_acqParams.RoiY;
            experiment.TimeStamp = DateTime.Now;

            bool success = wgDB.InsertExperiment(ref experiment);

            if (success)
            {
                if (experiment.ExperimentID != 0)
                {
                    VM.ExpParams.experiment = experiment;
                }
                else
                {
                    ShowErrorDialog("Datebase Error: InsertExperiment", wgDB.GetLastErrorMsg());
                    return false;
                }
            }
            else
            {
                ShowErrorDialog("Datebase Error: InsertExperiment", wgDB.GetLastErrorMsg());
                return false;
            }

            return true;
        }




        private bool GetExperimentPlate(string barcode)
        {
            // plate, may or may not already exist in database, so check first to see if it exists, and if not create it
            PlateContainer plate;
            bool success = wgDB.GetPlateByBarcode(barcode, out plate);
            if (!success)
            {
                ShowErrorDialog("Database Error: GetPlateByBarcode", wgDB.GetLastErrorMsg());
                return false;
            }
            if (plate == null)  // plate does not exist, so create it
            {
                plate = new PlateContainer();
                plate.Barcode = barcode;
                plate.BarcodeValid = true;
                plate.Description = DateTime.Now.ToString() + "/" +
                    VM.ExpParams.project.Description + "/" + VM.ExpParams.method.Description + "/" + VM.ExpParams.user.Lastname + ", " + VM.ExpParams.user.Firstname;
                plate.IsPublic = false;
                plate.OwnerID = VM.ExpParams.user.UserID;
                plate.PlateTypeID = VM.ExpParams.plateType.PlateTypeID;
                plate.ProjectID = VM.ExpParams.project.ProjectID;                
                success = wgDB.InsertPlate(ref plate); // this sets plate.PlateID

                if (!success)
                {                   
                    ShowErrorDialog("Database Error: InsertPlate", wgDB.GetLastErrorMsg());
                    return false;
                }
            }

            if (success)
            {
                VM.ExpParams.experimentPlate = plate;
            }

            if (VM.ExpParams.experimentPlate == null)
            {
                ShowErrorDialog("Experiment Error", "Unable to assign Experiment Plate");
                return false;
            }

            return true;
        }


        private void ShowErrorDialog(string title, string errMsg)
        {
            // this is just a convenience function

            MessageBox.Show(errMsg, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }



        public void InitBarcodeResetRadioButtons()
        {
            switch(VM.ExpParams.method.ImagePlateBarcodeReset)
            {
                case PLATE_ID_RESET_BEHAVIOR.CLEAR:
                    ImagePlateBarcode_ClearRB.IsChecked = true;
                    break;
                case PLATE_ID_RESET_BEHAVIOR.CONSTANT:
                    ImagePlateBarcode_ConstantRB.IsChecked = true;
                    break;
                case PLATE_ID_RESET_BEHAVIOR.INCREMENT:
                    ImagePlateBarcode_IncrementRB.IsChecked = true;
                    break;
                case PLATE_ID_RESET_BEHAVIOR.VWORKS:
                    ImagePlateBarcode_VWorksRB.IsChecked = true;
                    break;
            }

            // clear barcodes
           
                VM.ExpParams.experimentPlate.Barcode = "";       
          
                foreach (ExperimentCompoundPlateContainer plate in VM.ExpParams.compoundPlateList)
                {
                    plate.Barcode = "";                   
                }
         
        }


        private void ImagePlateBarcode_ConstantRB_Checked(object sender, RoutedEventArgs e)
        {
            VM.ExpParams.experimentPlate.PlateIDResetBehavior = PLATE_ID_RESET_BEHAVIOR.CONSTANT;
            ImagePlateBarcodeTextBox.IsEnabled = true;
        }

        private void ImagePlateBarcode_IncrementRB_Checked(object sender, RoutedEventArgs e)
        {
            VM.ExpParams.experimentPlate.PlateIDResetBehavior = PLATE_ID_RESET_BEHAVIOR.INCREMENT;
            ImagePlateBarcodeTextBox.IsEnabled = true;
        }

        private void ImagePlateBarcode_ClearRB_Checked(object sender, RoutedEventArgs e)
        {
            VM.ExpParams.experimentPlate.PlateIDResetBehavior = PLATE_ID_RESET_BEHAVIOR.CLEAR;
            ImagePlateBarcodeTextBox.IsEnabled = true;
        }

        private void ImagePlateBarcode_VWorksRB_Checked(object sender, RoutedEventArgs e)
        {
            VM.ExpParams.experimentPlate.PlateIDResetBehavior = PLATE_ID_RESET_BEHAVIOR.VWORKS;
            ImagePlateBarcodeTextBox.IsEnabled = false;
            ImagePlateBarcodeTextBox.Text = "";
        }

  
        private void CompoundPlateBarcode_ConstantRB_Checked(object sender, RoutedEventArgs e)
        {          
            var rb = (RadioButton)sender;            
            CellValuePresenter cvp = VisualTreeHelpers.FindAncestor<CellValuePresenter>(rb);
            ExperimentCompoundPlateContainer ecpc = (ExperimentCompoundPlateContainer)cvp.Record.DataItem;
            ecpc.PlateIDResetBehavior = PLATE_ID_RESET_BEHAVIOR.CONSTANT;

            ContentPresenter cp = VisualTreeHelpers.FindAncestor<ContentPresenter>(cvp);
            if(cp != null)
            {
                TextBox tb = VisualTreeHelpers.FindChild<TextBox>(cp);
                if(tb != null)
                {
                    tb.IsEnabled = true;
                }
            }

            PostMessageRunExperimentPanel(ecpc.Description + " set to " + ecpc.PlateIDResetBehavior.ToString());
        }

        private void CompoundPlateBarcode_IncrementRB_Checked(object sender, RoutedEventArgs e)
        {
            var rb = (RadioButton)sender;
            CellValuePresenter cvp = VisualTreeHelpers.FindAncestor<CellValuePresenter>(rb);
            ExperimentCompoundPlateContainer ecpc = (ExperimentCompoundPlateContainer)cvp.Record.DataItem;
            ecpc.PlateIDResetBehavior = PLATE_ID_RESET_BEHAVIOR.INCREMENT;

            ContentPresenter cp = VisualTreeHelpers.FindAncestor<ContentPresenter>(cvp);
            if (cp != null)
            {
                TextBox tb = VisualTreeHelpers.FindChild<TextBox>(cp);
                if (tb != null)
                {
                    tb.IsEnabled = true;
                }
            }

            PostMessageRunExperimentPanel(ecpc.Description + " set to " + ecpc.PlateIDResetBehavior.ToString());
        }

        private void CompoundPlateBarcode_ClearRB_Checked(object sender, RoutedEventArgs e)
        {
            var rb = (RadioButton)sender;
            CellValuePresenter cvp = VisualTreeHelpers.FindAncestor<CellValuePresenter>(rb);
            ExperimentCompoundPlateContainer ecpc = (ExperimentCompoundPlateContainer)cvp.Record.DataItem;
            ecpc.PlateIDResetBehavior = PLATE_ID_RESET_BEHAVIOR.CLEAR;

            ContentPresenter cp = VisualTreeHelpers.FindAncestor<ContentPresenter>(cvp);
            if (cp != null)
            {
                TextBox tb = VisualTreeHelpers.FindChild<TextBox>(cp);
                if (tb != null)
                {
                    tb.IsEnabled = true;
                }
            }

            PostMessageRunExperimentPanel(ecpc.Description + " set to " + ecpc.PlateIDResetBehavior.ToString());
        }

        private void CompoundPlateBarcode_VWorksRB_Checked(object sender, RoutedEventArgs e)
        {
            var rb = (RadioButton)sender;
            CellValuePresenter cvp = VisualTreeHelpers.FindAncestor<CellValuePresenter>(rb);
            ExperimentCompoundPlateContainer ecpc = (ExperimentCompoundPlateContainer)cvp.Record.DataItem;
            ecpc.PlateIDResetBehavior = PLATE_ID_RESET_BEHAVIOR.VWORKS;

            ContentPresenter cp = VisualTreeHelpers.FindAncestor<ContentPresenter>(cvp);
            if (cp != null)
            {
                TextBox tb = VisualTreeHelpers.FindChild<TextBox>(cp);
                if (tb != null)
                {
                    tb.IsEnabled = false;
                    tb.Text = "";
                }
            }
          

            PostMessageRunExperimentPanel(ecpc.Description + " set to " + ecpc.PlateIDResetBehavior.ToString());
        }




        private async void AutoOptimizePB_Click(object sender, RoutedEventArgs e)
        {          
            AutoOptimizeViewer.Init();  // loads all current indicators
            bool allIndicatorsPassed = await m_imager.StartAutoOptimization(null);  // passing null uses current camera settings

        }

        private void HideAutoOptimizeViewer_Click(object sender, RoutedEventArgs e)
        {
            VM.RunningAutoVerify = false;
        }


    }




/// <summary>
/// ////////////////////////////////
///  VIEW MODEL
///  
/// </summary>


    public class ViewModel_RunExperimentControl : INotifyPropertyChanged
    {
        //public enum RUN_STATUS { NEEDS_INPUT, READY_TO_RUN, RUNNING, RUN_FINISHED };

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
        public RUN_STATE RunState
        {
            get
            {
                return this._runState;
            }

            set
            {
                if (value != this._runState)
                {
                    this._runState = value;
                    NotifyPropertyChanged();

                    switch(_runState)
                    {
                        case RUN_STATE.ERROR:
                            RunStateText = "Error";
                            break;
                        case RUN_STATE.NEEDS_INPUT:
                            RunStateText = "Input Required";
                            break;
                        case RUN_STATE.READY_TO_RUN:
                            RunStateText = "Ready To Run";
                            break;
                        case RUN_STATE.RUN_ABORTED:
                            RunStateText = "Run Aborted";
                            break;
                        case RUN_STATE.RUN_FINISHED:
                            RunStateText = "Run Finished";
                            break;
                        case RUN_STATE.RUNNING:
                            RunStateText = "Running...";
                            break;
                    }
                }
            }
        }

        private string _runStateText;
        public string RunStateText
        {
            get
            {
                return this._runStateText;
            }

            set
            {
                if (value != this._runStateText)
                {
                    this._runStateText = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private bool _runningAutoVerify;
        public bool RunningAutoVerify
        {
            get
            {
                return this._runningAutoVerify;
            }

            set
            {
                if (value != this._runningAutoVerify)
                {
                    this._runningAutoVerify = value;
                    NotifyPropertyChanged();
                }
            }
        }


        // make ExperimentParams Singleton part of view model (used to store selections made by user)
        private ExperimentParams _expParams;
        public ExperimentParams ExpParams { get { return _expParams; } }

        private CameraSettingsContainer _currentCameraSettings;
        public CameraSettingsContainer CurrentCameraSettings
        {
            get
            {
                return this._currentCameraSettings;
            }

            set
            {
                if (value != this._currentCameraSettings)
                {
                    this._currentCameraSettings = value;
                    NotifyPropertyChanged();
                }
            }
        }

    
        private int _activeBinning;
        public int ActiveBinning
        {
            get
            {
                return this._activeBinning;
            }

            set
            {
                if (value != this._activeBinning)
                {
                    this._activeBinning = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private int _cameraTemperatureTarget;
        public int CameraTemperatureTarget
        {
            get
            {
                return this._cameraTemperatureTarget;
            }

            set
            {
                if (value != this._cameraTemperatureTarget)
                {
                    this._cameraTemperatureTarget = value;
                    NotifyPropertyChanged();
                }                
            }
        }

        private int _cameraTemperatureActual;
        public int CameraTemperatureActual
        {
            get
            {
                return this._cameraTemperatureActual;
            }

            set
            {
                if (value != this._cameraTemperatureActual)
                {
                    this._cameraTemperatureActual = value;
                    NotifyPropertyChanged();
                }

                EvalCameraTemperature();
            }
        }

        private bool _cameraTemperatureReady;
        public bool CameraTemperatureReady
        {
            get
            {
                return this._cameraTemperatureReady;
            }

            set
            {
                if (value != this._cameraTemperatureReady)
                {
                    this._cameraTemperatureReady = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private int _insideTemperatureTarget;
        public int InsideTemperatureTarget
        {
            get
            {
                return this._insideTemperatureTarget;
            }

            set
            {
                if (value != this._insideTemperatureTarget)
                {
                    this._insideTemperatureTarget = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private int _insideTemperatureActual;
        public int InsideTemperatureActual
        {
            get
            {
                return this._insideTemperatureActual;
            }

            set
            {
                if (value != this._insideTemperatureActual)
                {
                    this._insideTemperatureActual = value;
                    NotifyPropertyChanged();
                }

                EvalInsideTemperature();
            }
        }

        private bool _insideTemperatureReady;
        public bool InsideTemperatureReady
        {
            get
            {
                return this._insideTemperatureReady;
            }

            set
            {
                if (value != this._insideTemperatureReady)
                {
                    this._insideTemperatureReady = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _insideHeaterEnabled;
        public bool InsideHeaterEnabled
        {
            get
            {
                return this._insideHeaterEnabled;
            }

            set
            {
                if (value != this._insideHeaterEnabled)
                {
                    this._insideHeaterEnabled = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private int _binning;
        public int Binning
        {
            get
            {
                return this._binning;
            }

            set
            {
                if (value != this._binning)
                {
                    this._binning = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private WriteableBitmap _overlay;
        public WriteableBitmap Overlay
        {
            get
            {
                return this._overlay;
            }

            set
            {
                if (value != this._overlay)
                {
                    this._overlay = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private WriteableBitmap _gridLines;
        public WriteableBitmap GridLines
        {
            get
            {
                return this._gridLines;
            }

            set
            {
                if (value != this._gridLines)
                {
                    this._gridLines = value;
                    NotifyPropertyChanged();
                }
            }
        }



        private string _currentPlateNumberText;
        public string CurrentPlateNumberText
        {
            get
            {
                return this._currentPlateNumberText;
            }

            set
            {
                if (value != this._currentPlateNumberText)
                {
                    this._currentPlateNumberText = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _delayText;
        public string DelayText
        {
            get
            {
                return this._delayText;
            }

            set
            {
                if (value != this._delayText)
                {
                    this._delayText = value;
                    NotifyPropertyChanged();
                }
            }
        }

                
        private bool _delayHeaderVisible;
        public bool DelayHeaderVisible
        {
            get
            {
                return this._delayHeaderVisible;
            }

            set
            {
                if (value != this._delayHeaderVisible)
                {
                    this._delayHeaderVisible = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public ViewModel_RunExperimentControl()
        {
            _expParams = ExperimentParams.GetExperimentParams;

            _overlay = BitmapFactory.New(800, 450);
            _gridLines = BitmapFactory.New(800, 450);

            _runningAutoVerify = false;

            InsideHeaterEnabled = false;
        }

        public void Reset()
        {
            if (!ExpParams.method.IsAuto)
            {
                foreach (ExperimentIndicatorContainer ind in ExpParams.indicatorList)
                {
                    ind.Verified = false;
                }
                
                SetRunState(RUN_STATE.NEEDS_INPUT);
            }
        }


        public void EvalCameraTemperature()
        {
            //int deviation = CameraTemperatureActual - (CameraTemperatureTarget + GlobalVars.Instance.MaxCameraTemperatureThresholdDeviation);

            //if (deviation <= 0)
            //    CameraTemperatureReady = true;
            //else
            //    CameraTemperatureReady = false;

            CameraTemperatureReady = GlobalVars.Instance.CameraTempReady;

            EvalRunStatus();
        }


        public void EvalInsideTemperature()
        {
            //int deviation = InsideTemperatureActual - (InsideTemperatureTarget + GlobalVars.Instance.MaxInsideTemperatureThresholdDeviation);

            //if (deviation <= 0 || !InsideHeaterEnabled)
            //    InsideTemperatureReady = true;
            //else
            //    InsideTemperatureReady = false;

            InsideTemperatureReady = GlobalVars.Instance.InsideTempReady;

            EvalRunStatus();
        }

        
        public void EvalRunStatus()
        {  // Possible Status: NEEDS_INPUT, READY_TO_RUN, RUNNING, RUN_FINISHED  

            if (RunState == RUN_STATE.RUN_FINISHED || RunState == RUN_STATE.RUNNING) return;

            if(ExpParams.experimentPlate.BarcodeValid || ExpParams.experimentPlate.PlateIDResetBehavior == PLATE_ID_RESET_BEHAVIOR.VWORKS)
            {
                bool allIndicatorsVerified = false;
                if (ExpParams.method != null)
                {
                    allIndicatorsVerified = true;
                    if (!ExpParams.method.IsAuto)
                    {
                        foreach (ExperimentIndicatorContainer ind in ExpParams.indicatorList)
                        {
                            if (!ind.Verified) allIndicatorsVerified = false;
                        }
                    }
                }

                bool allCompoundPlatesVerified = false;
                if (ExpParams.compoundPlateList != null)
                {
                    allCompoundPlatesVerified = true;
                    foreach (ExperimentCompoundPlateContainer cp in ExpParams.compoundPlateList)
                    {
                        if (!cp.BarcodeValid && cp.PlateIDResetBehavior != PLATE_ID_RESET_BEHAVIOR.VWORKS) allCompoundPlatesVerified = false;
                    }
                }

                if (allIndicatorsVerified && allCompoundPlatesVerified && CameraTemperatureReady && InsideTemperatureReady) RunState = RUN_STATE.READY_TO_RUN;
                else RunState = RUN_STATE.NEEDS_INPUT;
            }
            else
            {
                RunState = RUN_STATE.NEEDS_INPUT;
            }

            RunExperimentControlViewModel_EventArgs e = new RunExperimentControlViewModel_EventArgs();
            e.RunState = RunState;
            if(StatusChange != null)
                StatusChange(this, e);
        }

        public void SetRunState(RUN_STATE runState)
        {
            RunState = runState;

            RunExperimentControlViewModel_EventArgs e = new RunExperimentControlViewModel_EventArgs();
            e.RunState = runState;
            StatusChange(this, e);
        }



        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property. 
        // The CallerMemberName attribute that is applied to the optional propertyName 
        // parameter causes the property name of the caller to be substituted as an argument. 
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        // ////////////////////////////////////////////////
        // Set up the event
        public event StatusChange_EventHandler StatusChange;
        public delegate void StatusChange_EventHandler(ViewModel_RunExperimentControl VM_ChartArray, RunExperimentControlViewModel_EventArgs e);

    }



    public class RunExperimentControlViewModel_EventArgs : EventArgs
    {
        private ViewModel_RunExperimentControl.RUN_STATE _runState;
        public ViewModel_RunExperimentControl.RUN_STATE RunState
        {
            set { _runState = value; }
            get { return this._runState; }
        }
    }






    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // TEST CODE

    public class BindingProxy : Freezable
    {
        #region Overrides of Freezable

        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        #endregion

        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }




}
