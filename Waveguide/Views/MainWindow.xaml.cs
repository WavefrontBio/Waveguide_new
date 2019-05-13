﻿///  Waveguide 2019


using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.ServiceProcess;
using System.Security.Principal;
using SimpleTCP;
using System.Collections.Generic;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        Imager m_imager;
        MainWindowViewModel VM;
        VWorks m_vworks;
        WaveguideDB m_wgDB;
        EnclosureCameraViewer m_enclosureCameraViewer;
        SplashScreen m_splash;
        bool m_vworksReady;
        TaskScheduler m_uiTask;
        SimpleTcpServer m_tcpServer;
        int m_commandMessageCount;  // number of messages received from a tcp-connected client (i.e. Thermo Momentum)
        string m_timestamp;  // timestamp of last command message received

        public MainWindow()
        {          

            InitializeComponent();

            Application.Current.MainWindow.WindowState = WindowState.Maximized;

            m_wgDB = new WaveguideDB();
            m_enclosureCameraViewer = null;


            switch (GlobalVars.Instance.UserRole)
            {
                case GlobalVars.USER_ROLE_ENUM.ADMIN:
                    break;
                case GlobalVars.USER_ROLE_ENUM.USER:
                    UsersTab.Visibility = Visibility.Collapsed;
                    FiltersTab.Visibility = Visibility.Collapsed;
                    PlateTypesTab.Visibility = Visibility.Collapsed;
                    MaintenanceTab.Visibility = Visibility.Collapsed;
                    break;
                case GlobalVars.USER_ROLE_ENUM.OPERATOR:
                    MethodsTab.Visibility = Visibility.Collapsed;
                    ProjectsTab.Visibility = Visibility.Collapsed;
                    UsersTab.Visibility = Visibility.Collapsed;
                    FiltersTab.Visibility = Visibility.Collapsed;
                    PlateTypesTab.Visibility = Visibility.Collapsed;
                    MaintenanceTab.Visibility = Visibility.Collapsed;
                    break;
            }
            

        
            m_imager = null;
            m_vworks = null;
            m_vworksReady = false;

         
            

            VM = new MainWindowViewModel();

            this.DataContext = VM;
            
            VM.WindowTitle = "Waveguide     " + GlobalVars.Instance.UserDisplayName + "  (" + GlobalVars.Instance.UserRole.ToString() + ")";

            // catch close event caused by clicking X button
            this.Closing += new System.ComponentModel.CancelEventHandler(Window_Closing);

            m_uiTask = TaskScheduler.FromCurrentSynchronizationContext();
            GlobalVars.Instance.UITask = m_uiTask;

            MyRunExperimentControl.PostMessage_RunExperimentPanelEvent += MyRunExperimentControl_PostMessage_RunExperimentPanelEvent;
            MyRunExperimentControl.BringToFrontRunExperimentPanelEvent += MyRunExperimentControl_BringToFrontRunExperimentPanelEvent;
            
            m_tcpServer = new SimpleTcpServer();
            m_tcpServer.ClientConnected += M_tcpServer_ClientConnected;
            m_tcpServer.ClientDisconnected += M_tcpServer_ClientDisconnected;
            m_tcpServer.DataReceived += M_tcpServer_DataReceived;
            m_tcpServer.DelimiterDataReceived += M_tcpServer_DelimiterDataReceived;
            m_tcpServer.Start(GlobalVars.Instance.TCPCommand_Port);
            m_commandMessageCount = 0;

            GlobalVars.Instance.m_statusChangeEvent += StatusChangeEvent;

            GlobalVars.Instance.Status = WGStatus.ONLINE;

        }

        private void StatusChangeEvent(object sender, StatusChangeEventArgs e)
        {
            VM.WindowTitle = "Waveguide     " + GlobalVars.Instance.UserDisplayName + "  (" + GlobalVars.Instance.UserRole.ToString() + ")  " + 
                GlobalVars.Instance.Status.ToString();
        }

        private void M_tcpServer_DelimiterDataReceived(object sender, Message e)
        {
            throw new NotImplementedException();
        }

        async private void M_tcpServer_DataReceived(object sender, Message e)
        {
            WaveguideMessage message;
            byte[] packet;
            if (WaveguideMessageUtil.ParseMessage(e.Data, out message))
            {
                m_commandMessageCount++;
                m_timestamp = DateTime.Now.ToString();

                switch (message.messageType)
                {
                    case Waveguide.WGMessageType.GET_STATUS:
                        if (WaveguideMessageUtil.Build_Status_Message(GlobalVars.Instance.Status, GlobalVars.Instance.Status.ToString(), out packet))
                        {                            
                            e.Reply(packet);
                        }
                        break;
                    case Waveguide.WGMessageType.CONFIG_EXPERIMENT:
                        ExperimentConfiguration config;
                        PostMessage("Configuration Message Received");
                        // Waveguide must be in ONLINE or READY state to accept a config command
                        if (GlobalVars.Instance.Status == WGStatus.ONLINE || GlobalVars.Instance.Status == WGStatus.READY)
                        {
                            if (ExperimentConfiguration.ParseConfigurationPayload(message.payloadBytes, out config))
                            {
                                if (MyExperimentConfigurator.SetConfiguration(config))
                                {
                                    // Set Status to Ready
                                    GlobalVars.Instance.Status = WGStatus.READY;

                                    // send response
                                    if (WaveguideMessageUtil.Build_Status_Message(GlobalVars.Instance.Status, GlobalVars.Instance.Status.ToString(), out packet))
                                    {
                                        e.Reply(packet);
                                    }
                                }
                                else
                                {
                                    GlobalVars.Instance.Status = WGStatus.ONLINE;

                                    PostMessage("Configuration Message Format Error");
                                    if (WaveguideMessageUtil.Build_Status_Message(WGStatus.MESSAGE_FAILED__MESSAGE_FORMAT_ERROR,
                                      "Failed to parse message", out packet))
                                    {
                                        e.Reply(packet);                                       
                                    }
                                }
                            }
                            else
                            {
                                // received bad configuration data

                                GlobalVars.Instance.Status = WGStatus.ONLINE; 

                                if (WaveguideMessageUtil.Build_Status_Message(WGStatus.MESSAGE_FAILED__MESSAGE_FORMAT_ERROR,
                                    "Failed to parse message", out packet))
                                {
                                    e.Reply(packet);
                                    PostMessage("Configuration Message Parse Error");
                                }
                            }
                        }
                        else
                        {
                            if (WaveguideMessageUtil.Build_Status_Message(WGStatus.MESSAGE_FAILED__INCORRECT_MODE,
                                "Current Waveguide mode = " + GlobalVars.Instance.Status.ToString() +
                                "\nIt must be in ONLINE or READY mode in order to accept a configuration command",
                                out packet))
                            {
                                e.Reply(packet);
                                PostMessage("Configuration Message No Accepted.  Waveguide not in correct mode.");
                            }
                        }
                        break;
                    case Waveguide.WGMessageType.START_EXPERIMENT:
                        // Waveguide must be in "Ready" state in order to Start an experiment
                        PostMessage("Start Message Received");
                        if (GlobalVars.Instance.Status == WGStatus.READY)
                        {                           
                            GlobalVars.Instance.Status = WGStatus.RUNNING;
                            if (WaveguideMessageUtil.Build_Status_Message(GlobalVars.Instance.Status, GlobalVars.Instance.Status.ToString(), out packet))
                            {
                                e.Reply(packet);
                            }


                            // Start Experiment!
                            MyExperimentConfigurator.StartExperiment();

                            // wait for camera temperature  
                            bool overridden = false;
                            Application.Current.Dispatcher.Invoke(
                            () =>
                            {
                                // Code to run on the GUI thread.
                                TemperatureMonitorDialog tdlg = new TemperatureMonitorDialog(m_imager.m_camera);

                                tdlg.ShowDialog();

                                overridden = tdlg.WasOverridden();

                                if (overridden)
                                {
                                    MyRunExperimentControl.VM.CameraTemperatureTarget = MyRunExperimentControl.VM.CameraTemperatureActual;
                                }

                            });


                            await Task.Delay(2000);  // give time for RunExperimentControl to enable run


                            // run vworks protocol
                            MyRunExperimentControl.Run();


                        }
                        else
                        {
                            if (WaveguideMessageUtil.Build_Status_Message(WGStatus.MESSAGE_FAILED__INCORRECT_MODE,
                                "Current Waveguide mode = " + GlobalVars.Instance.Status.ToString() +
                                "\nIt must be in READY mode in order to accept a Start command",
                                out packet))
                            {
                                e.Reply(packet);
                            }
                        }
                        break;
                    case Waveguide.WGMessageType.STOP_EXPERIMENT:
                        PostMessage("Stop Message Received");
                        if (GlobalVars.Instance.Status == WGStatus.RUNNING)
                        {
                            GlobalVars.Instance.Status = WGStatus.READY;
                            if (WaveguideMessageUtil.Build_Status_Message(GlobalVars.Instance.Status, GlobalVars.Instance.Status.ToString(), out packet))
                            {
                                e.Reply(packet);
                            }
                        }
                        else
                        {
                            if (WaveguideMessageUtil.Build_Status_Message(WGStatus.MESSAGE_FAILED__INCORRECT_MODE,
                                "Current Waveguide mode = " + GlobalVars.Instance.Status.ToString() +
                                "\nIt must be in RUNNING mode in order to accept a Stop command",
                                out packet))
                            {
                                e.Reply(packet);
                            }
                        }
                        break;
                    case Waveguide.WGMessageType.STATUS:
                        break;
                }
            }
            else
            {
                //m_vm.status = WGStatus.ERROR;
                if (WaveguideMessageUtil.Build_Status_Message(WGStatus.MESSAGE_FAILED__MESSAGE_FORMAT_ERROR,
                    "Error Parsing Message", out packet))
                {
                    e.Reply(packet);
                }
            }
        }

        private void M_tcpServer_ClientDisconnected(object sender, System.Net.Sockets.TcpClient e)
        {
            PostMessage("Command Client Disconnected");
        }

        private void M_tcpServer_ClientConnected(object sender, System.Net.Sockets.TcpClient e)
        {
            PostMessage("Command Client Connected");
        }

        void MyRunExperimentControl_BringToFrontRunExperimentPanelEvent(object sender, EventArgs e)
        {            
            Dispatcher.Invoke((Action)(() =>
            {
                BringWindowToFront();
            }));
        }

        void MyRunExperimentControl_PostMessage_RunExperimentPanelEvent(object sender, RunExperimentPanel_PostMessageEventArgs e)
        {
            PostMessage(e.Message);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            m_splash = new SplashScreen("/Images/WG_Loading.png");
            m_splash.Show(false);

       
            bool admin = IsAdministrator();
            //if (admin) { MessageBox.Show("You ARE an administrator"); }
            //else { MessageBox.Show("You ARE NOT an administrator"); } 


            bool success = VerifySQLServerServiceRunning();

            // Don't have the security privileges to start services...need to fix
            //  Until this is fixed, just set success = true
            success = true;

            if (success)
            {
                m_imager = new Imager();

                bool done = false;

                while(!done)
                {
                    m_imager.Init();
                    if(m_imager.ImagerReady)
                    {
                        done = true;
                        m_imager.m_cameraEvent += Imager_CameraEvent;
                        m_imager.m_cameraTemperatureEvent += Imager_TemperatureEvent;

                        m_imager.m_camera.CoolerON(true);
                        VM.CoolingOn = true;
                        CameraTempOnIndicator.Fill = new SolidColorBrush(Colors.Blue);

                        if (GlobalVars.Instance.Enable_EthernetIOModule)
                        {
                            m_imager.m_ethernetIO.m_doorStatusEvent += m_ethernetIO_m_doorStatusEvent;
                            m_imager.m_ethernetIO.m_ioMessageEvent += m_ethernetIO_m_ioMessageEvent;
                        }

                        if (GlobalVars.Instance.Enable_EnclosureTemperatureController)
                        {
                            m_imager.m_insideTemperatureEvent += m_imager_m_insideTemperatureEvent;
                            m_imager.m_omegaTempController.MessageEvent += m_omegaTempController_MessageEvent;
                            m_imager.m_omegaTempController.TempEvent += m_omegaTempController_TempEvent;
                        }

                        MyExperimentConfigurator.Init(m_imager);
                        MyExperimentConfigurator.StartExperimentEvent += MyExperimentConfigurator_StartExperimentEvent;
                        MyRunExperimentControl.CloseRunExperimentPanelEvent += MyRunExperimentControl_CloseRunExperimentPanelEvent;

                        m_vworksReady = StartVWorks();

                        if (m_vworksReady)
                        {
                            MyRunExperimentControl.VM.StatusChange += VM_StatusChange;
                        }
                        
                    }
                    else
                    {
                        MessageBoxResult response = MessageBox.Show("Imager Failed to Initialize. Retry?" + Environment.NewLine + Environment.NewLine +
                                                    "   YES - to retry Imager initialization" + Environment.NewLine + Environment.NewLine +
                                                    "   NO - continue with Imager disabled" + Environment.NewLine, 
                                                    "Imager Error", MessageBoxButton.YesNo, MessageBoxImage.Error);

                        if (response == MessageBoxResult.No)
                        {
                            done = true;
                            Dispatcher.BeginInvoke((Action)(() => MainTabControl.SelectedIndex = 1));
                            ExperimentConfiguratorTab.IsEnabled = false;
                            //MaintenanceTab.IsEnabled = false;
                        }
                    }
                }               
            }
            else
            {
                MessageBox.Show("MS SQL Server Service could not be started.", "MS SQL Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            
        }

        void m_omegaTempController_TempEvent(object sender, OmegaTempCtrlTempEventArgs e)
        {
            VM.InsideTemp = (int)e.Temperature;
        }

        void m_omegaTempController_MessageEvent(object sender, OmegaTempCtrlMessageEventArgs e)
        {
            PostMessage(e.Message);
        }

        void MyRunExperimentControl_CloseRunExperimentPanelEvent(object sender, EventArgs e)
        {
            MyExperimentConfigurator.ResetExperimentConfigurator();
            VM.ShowRunExperimentPanel = false;
        }

        void MyExperimentConfigurator_StartExperimentEvent(object sender, EventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                MyRunExperimentControl.Configure(m_imager);
                MyRunExperimentControl.InitBarcodeResetRadioButtons();
                MyRunExperimentControl.VM.ExpParams.experimentCurrentPlateNumber = 0;
                MyRunExperimentControl.VM.CurrentPlateNumberText = MyRunExperimentControl.VM.ExpParams.experimentCurrentPlateNumber.ToString();
                VM.ShowRunExperimentPanel = true;
                MyRunExperimentControl.VM.EvalRunStatus();

                GlobalVars.Instance.Status = WGStatus.READY;

                MyRunExperimentControl.Run();
            }));            
        }

        void VM_StatusChange(ViewModel_RunExperimentControl VM_RunExperimentControl, RunExperimentControlViewModel_EventArgs e)
        {
            VM.ExperimentRunState = e.RunState;
        }


        void Configure_RunExperimentControl()
        {          
            MyRunExperimentControl.Configure(m_imager);
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


        public bool StartVWorks()
        {
            bool success = true;
            m_vworks = new VWorks();
            GlobalVars.Instance.VWorks = m_vworks;


            if(!m_vworks.m_vworksOK)
            {
                PostMessage("VWorks Failed to Start!");
                success = false;                
            }
            else if(!m_vworks.VWorks_CreatedSuccessfully())
            {
                PostMessage("VWorks Creation Failure!");
                success = false;
            }

            if(success)
            {
                m_vworks.PostVWorksCommandEvent += MyRunExperimentControl.m_vworks_PostVWorksCommandEvent;
            }
            
            return success;
        }


        void m_ethernetIO_m_ioMessageEvent(object sender, IOMessageEventArgs e)
        {
            PostMessage(e.Message);
        }

        void m_ethernetIO_m_doorStatusEvent(object sender, DoorStatusEventArgs e)
        {
            VM.DoorStatus = e.DoorStatus;           

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (VM.DoorStatus == DOOR_STATUS.LOCKED)
                    DoorLockedIndicator.Fill = new SolidColorBrush(Colors.Red);
                else
                    DoorLockedIndicator.Fill = new SolidColorBrush(Colors.Transparent);
            }));
        }

        void m_imager_m_insideTemperatureEvent(object sender, TemperatureEventArgs e)
        {
            if(e.GoodReading)
            {
                VM.InsideTemp = e.Temperature;
                VM.InsideTempString = e.Temperature.ToString();
            }
            else
            {
                VM.InsideTempString = "-";
            }

        }





        private void Window_ContentRendered(object sender, EventArgs e)
        {
            m_splash.Close(TimeSpan.FromSeconds(1));           
        }

  

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // catch closing event caused by hitting X button if experiment is running

            MessageBoxResult result = MessageBox.Show("Are you sure you want to Logout?", "Logout",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }

            if(m_imager!=null)
            {
                if(m_imager.m_camera != null)
                    m_imager.m_camera.CoolerON(false);

                if (m_imager.m_omegaTempController != null)
                {
                    m_imager.m_omegaTempController.updateSetPoint(1, 15);
                    m_imager.m_omegaTempController.EnableHeater(false);
                }

                if(m_imager.m_ethernetIO != null)
                    m_imager.m_ethernetIO.DoorLockON(false);
            }


            // set this if you want to cancel the close event: e.Cancel = true;
        }




        void Imager_TemperatureEvent(object sender, TemperatureEventArgs e)
        {
            if (this.Dispatcher.CheckAccess())
            {
                if (e.GoodReading)
                {
                    VM.CameraTemp = e.Temperature;
                    VM.CameraTempString = e.Temperature.ToString();
                    GlobalVars.Instance.CameraTemp = e.Temperature;
                }
                else
                {
                    VM.CameraTempString = "-";
                }
            }
            else
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (e.GoodReading)
                        {
                            VM.CameraTemp = e.Temperature;
                            VM.CameraTempString = e.Temperature.ToString();
                            GlobalVars.Instance.CameraTemp = e.Temperature;
                        }
                        else
                        {
                            VM.CameraTempString = "-";
                        }
                    }));
            }
        }

        void Imager_CameraEvent(object sender, CameraEventArgs e)
        {
            if (this.Dispatcher.CheckAccess())
            {
               PostMessage(e.Message);
            }
            else
            {
                this.Dispatcher.BeginInvoke(new Action(() => PostMessage(e.Message)));
            }
        }

   
        public void PostMessage(string msg)
        {
            if (this.Dispatcher.CheckAccess())
            {
                MainMessageWindow.AppendText(Environment.NewLine);
                MainMessageWindow.AppendText(msg);
                MainMessageWindow.ScrollToEnd();
            }
            else
            {
                this.Dispatcher.BeginInvoke(new Action(() =>{
                MainMessageWindow.AppendText(Environment.NewLine);
                MainMessageWindow.AppendText(msg);
                MainMessageWindow.ScrollToEnd();
                } ));
            }
            
        }


        private void Imager_Click(object sender, RoutedEventArgs e)
        {
            ManualControlDialog dlg = new ManualControlDialog(m_imager, -1, true, true);

            dlg.Owner = this;

            dlg.ShowDialog();
        }

       

        private void CameraTempOnIndicator_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CameraCoolerOnOffPopup.IsOpen = true;
            //string msg;
            //if (VM.CoolingOn)
            //    msg = "Turn Camera Cooler OFF?";
            //else
            //    msg = "Turn Camera Cooler ON?";

            //MessageBoxResult result = MessageBox.Show(msg, "Camera Cooler Control", MessageBoxButton.YesNo, MessageBoxImage.Question);

            //if (result == MessageBoxResult.Yes)
            //{
            //    VM.CoolingOn = !VM.CoolingOn;
            //    m_imager.m_camera.CoolerON(VM.CoolingOn);
            //    if (VM.CoolingOn)
            //    {
            //        CameraTempOnIndicator.Fill = new SolidColorBrush(Colors.Blue);
            //        PostMessage("Camera Cooler ON");
            //    }
            //    else
            //    {
            //        CameraTempOnIndicator.Fill = new SolidColorBrush(Colors.Transparent);
            //        PostMessage("Camera Cooler OFF");
            //    }
            //}
        }


        private void InsideTempOnIndicator_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            HeaterOnOffPopup.IsOpen = true;           
        }


        private void DoorLockedIndicator_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DoorLockPopup.IsOpen = true;
            if (m_imager != null)
            {
                //if (m_imager.m_ethernetIO != null)
                //{
                //    if (GlobalVars.Instance.DoorStatus == DOOR_STATUS.CLOSED)
                //        m_imager.m_ethernetIO.SetOutputON(0, true);
                //    else
                //        m_imager.m_ethernetIO.SetOutputON(0, false);
                //}
            }
        }





        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            // turn off camera cooler
            if (m_imager != null)
            {
                if(m_imager.m_camera != null)
                    m_imager.m_camera.CoolerON(false);

                m_imager.Shutdown();

                if(m_imager.m_ethernetIO != null)
                    m_imager.m_ethernetIO.SetOutputON(0, false);  // unlock door

                if(m_imager.m_omegaTempController != null)
                    m_imager.m_omegaTempController.EnableHeater(false);  // turn heater off
            }
        }

        private void LogoutPB_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ViewEnclosureCameraPB_Click(object sender, RoutedEventArgs e)
        {
            if (m_enclosureCameraViewer == null)
            {
                m_enclosureCameraViewer = new EnclosureCameraViewer();
                m_enclosureCameraViewer.Closed += m_enclosureCameraViewer_Closed;
                m_enclosureCameraViewer.Show();
            }
            else
            {
                // need code here to bring Enclosure Camera Viewer window to front
            }
 
        }

        void m_enclosureCameraViewer_Closed(object sender, EventArgs e)
        {
            m_enclosureCameraViewer = null;
        }


        private bool VerifySQLServerServiceRunning()
        {
            var ctl = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == "MSSQLSERVER");

            if(ctl != null)
            {
                if(StartService("MSSQLSERVER",5000)) return true;
            }

            return false;
        }


        private bool StartService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);

                return true;
            }
            catch
            {
                return false;
            }
        }


        public static void StopService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
            }
            catch
            {
                // ...
            }
        }


        public static void RestartService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                int millisec1 = Environment.TickCount;
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

                // count the rest of the timeout
                int millisec2 = Environment.TickCount;
                timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch
            {
                // ...
            }
        }


        public bool IsAdministrator()
        {
            bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()) .IsInRole(WindowsBuiltInRole.Administrator) ? true : false;
            return isAdmin;
        }

        private void ImagingModes_Click(object sender, RoutedEventArgs e)
        {
            
            CameraSettingsManager csm = new CameraSettingsManager(m_imager, m_wgDB);

            csm.ShowDialog();
        }

        private void ShowRunExperimentPanel_Click(object sender, RoutedEventArgs e)
        {

        }



    

        private void HeaterOnPB_Click(object sender, RoutedEventArgs e)
        {
            InsideTempOnIndicator.Fill = new SolidColorBrush(Colors.Red);
            PostMessage("Enclosure Heater ON");
            VM.HeatingOn = true;
            HeaterOnOffPopup.IsOpen = false;

            // TODO: Turn Heater ON
            m_imager.m_omegaTempController.EnableHeater(true);
        }

        private void HeaterOffPB_Click(object sender, RoutedEventArgs e)
        {
            InsideTempOnIndicator.Fill = new SolidColorBrush(Colors.Transparent);
            PostMessage("Enclosure Heater OFF");
            VM.HeatingOn = false;
            HeaterOnOffPopup.IsOpen = false;

            // TODO: Turn Heater OFF
            m_imager.m_omegaTempController.EnableHeater(false);
        }


        private void CameraCoolerOnPB_Click(object sender, RoutedEventArgs e)
        {
            VM.CoolingOn = true;
            m_imager.m_camera.CoolerON(VM.CoolingOn);
            CameraCoolerOnOffPopup.IsOpen = false;
            CameraTempOnIndicator.Fill = new SolidColorBrush(Colors.Blue);
            PostMessage("Camera Cooler ON");
        }

        private void CameraCoolerOffPB_Click(object sender, RoutedEventArgs e)
        {
            VM.CoolingOn = false;
            m_imager.m_camera.CoolerON(VM.CoolingOn);
            CameraCoolerOnOffPopup.IsOpen = false;
            CameraTempOnIndicator.Fill = new SolidColorBrush(Colors.Transparent);
            PostMessage("Camera Cooler OFF");
        }

        private void DoorLockOnPB_Click(object sender, RoutedEventArgs e)
        {
            DoorLockPopup.IsOpen = false;            

            if (m_imager != null)
            {
                if (m_imager.m_ethernetIO != null)
                {
                    VM.DoorStatus = DOOR_STATUS.LOCKED;
                    DoorLockedIndicator.Fill = new SolidColorBrush(Colors.Red);
                    PostMessage("Door Locked");

                    //if (GlobalVars.Instance.DoorStatus == DOOR_STATUS.CLOSED)
                        m_imager.m_ethernetIO.SetOutputON(0, true);
                   
                }
            }
        }

        private void DoorLockOffPB_Click(object sender, RoutedEventArgs e)
        {
            DoorLockPopup.IsOpen = false;           

            if (m_imager != null)
            {
                if (m_imager.m_ethernetIO != null)
                {
                    VM.DoorStatus = DOOR_STATUS.CLOSED;
                    DoorLockedIndicator.Fill = new SolidColorBrush(Colors.Transparent);
                    PostMessage("Door Unlocked");
                                        
                    m_imager.m_ethernetIO.SetOutputON(0, false);
                }
            }
        }

     

        private void InsideTemperatureEdit_ValueChanged(object sender, EventArgs e)
        {
            GlobalVars.Instance.InsideTargetTemperature = VM.InsideTargetTemp;

            if (m_imager != null)
                m_imager.SetInsideTemperatureTarget(VM.InsideTargetTemp);

            VM.CheckInsideTemperature();
        }

        private void RefreshDBPB_Click(object sender, RoutedEventArgs e)
        {
            if (m_wgDB == null) m_wgDB = new WaveguideDB();

            List<DatabaseTableInfo> tableSizes;
            bool success = m_wgDB.GetDatabaseInfo(out tableSizes);

            if(success)
                DatabaseDataGrid.ItemsSource = tableSizes;
        }

        private void CleanUpExperimentsDBPB_Click(object sender, RoutedEventArgs e)
        {
            ManageDatabaseDialog dlg = new ManageDatabaseDialog(m_wgDB);

            dlg.ShowDialog();
        }
    }





    // //////////////////////////////////////////////////////////////////////
    // //////////////////////////////////////////////////////////////////////
    // //////////////////////////////////////////////////////////////////////

   


    public class MainWindowViewModel : INotifyPropertyChanged
    {

        private string _windowTitle;

        private int _cameraTemp;
        private string _cameraTempString;
        private int _cameraTargetTemp;
        private bool _coolingOn;
        private bool _cameraTempReady;

        private int _insideTemp;
        private string _insideTempString;
        private int _insideTargetTemp;
        private bool _heatingOn;
        private bool _insideTempReady;

        private DOOR_STATUS _doorStatus;
        private bool _showHeaterOnOffPopup;     
        private bool _showRunExperimentPanel;
        private ViewModel_RunExperimentControl.RUN_STATE _experimentRunState;

        // make ExperimentParams Singleton part of view model (used to store selections made by user)
        private ExperimentParams _expParams;
        public ExperimentParams ExpParams { get { return _expParams; } }


        public string WindowTitle
        {
            get { return _windowTitle; }
            set { _windowTitle = value; NotifyPropertyChanged("WindowTitle"); }
        }


        public ViewModel_RunExperimentControl.RUN_STATE ExperimentRunState
        {
            get { return _experimentRunState; }
            set
            {
                _experimentRunState = value; NotifyPropertyChanged("ExperimentRunState");
            }
        }


        public bool ShowHeaterOnOffPopup
        {
            get { return _showHeaterOnOffPopup; }
            set
            {
                _showHeaterOnOffPopup = value; NotifyPropertyChanged("ShowHeaterOnOffPopup");
            }
        }



        public bool ShowRunExperimentPanel
        {
            get { return _showRunExperimentPanel; }
            set
            {
                _showRunExperimentPanel = value; NotifyPropertyChanged("ShowRunExperimentPanel");             
            }
        }

        public int CameraTemp
        {
            get { return _cameraTemp; }
            set { _cameraTemp = value; NotifyPropertyChanged("CameraTemp"); CameraTempString = _cameraTemp.ToString();
            CheckCameraTemperature();
            }
        }

        public int CameraTargetTemp
        {
            get { return _cameraTargetTemp; }
            set { _cameraTargetTemp = value; NotifyPropertyChanged("CameraTargetTemp");
            CheckCameraTemperature();
            }
        }

        public string CameraTempString
        {
            get { return _cameraTempString; }
            set { _cameraTempString = value; NotifyPropertyChanged("CameraTempString"); }
        }

        public bool CoolingOn
        {
            get { return _coolingOn; }
            set { _coolingOn = value; NotifyPropertyChanged("CoolingOn"); CheckCameraTemperature(); }
        }

        public bool CameraTempReady
        {
            get { return _cameraTempReady; }
            set { _cameraTempReady = value; NotifyPropertyChanged("CameraTempReady"); }
        }


        public int InsideTemp
        {
            get { return _insideTemp; }
            set
            {
                _insideTemp = value; NotifyPropertyChanged("InsideTemp"); InsideTempString = value.ToString();
                CheckInsideTemperature();
            }
        }

        public int InsideTargetTemp
        {
            get { return _insideTargetTemp; }
            set { _insideTargetTemp = value; NotifyPropertyChanged("InsideTargetTemp"); GlobalVars.Instance.InsideTargetTemperature = value;
            CheckInsideTemperature();
            }
        }

        public string InsideTempString
        {
            get { return _insideTempString; }
            set { _insideTempString = value; NotifyPropertyChanged("InsideTempString"); }
        }

        public bool HeatingOn
        {
            get { return _heatingOn; }
            set { _heatingOn = value; NotifyPropertyChanged("HeatingOn"); CheckInsideTemperature(); }
        }

        public bool InsideTempReady
        {
            get { return _insideTempReady; }
            set { _insideTempReady = value; NotifyPropertyChanged("InsideTempReady"); }
        }


        public DOOR_STATUS DoorStatus
        {
            get { return _doorStatus; }
            set { _doorStatus = value; NotifyPropertyChanged("DoorStatus"); }
        }


        public void CheckCameraTemperature()
        {
            if(!_coolingOn)
            {
                CameraTempReady = true;
                GlobalVars.Instance.CameraTempReady = true;
            }
            else if (_cameraTemp < (_cameraTargetTemp + GlobalVars.Instance.MaxCameraTemperatureThresholdDeviation))
            {
                CameraTempReady = true;
                GlobalVars.Instance.CameraTempReady = true;
            }
            else
            {
                CameraTempReady = false;
                GlobalVars.Instance.CameraTempReady = false;
            }
        }

        public void CheckInsideTemperature()
        {
            if (!_heatingOn)
            {
                InsideTempReady = true;
                GlobalVars.Instance.InsideTempReady = true;
            }
            else if (Math.Abs(_insideTemp - _insideTargetTemp) <= GlobalVars.Instance.MaxInsideTemperatureThresholdDeviation)
            {
                InsideTempReady = true;
                GlobalVars.Instance.InsideTempReady = true;
            }
            else
            {
                InsideTempReady = false;
                GlobalVars.Instance.InsideTempReady = false;
            }
        }

        public MainWindowViewModel()
        {
            WindowTitle = "WaveGuide";

            _expParams = ExperimentParams.GetExperimentParams;

            CameraTargetTemp = GlobalVars.Instance.CameraTargetTemperature;
            CameraTempString = "-";
            CoolingOn = true;
            
            InsideTargetTemp = GlobalVars.Instance.InsideTargetTemperature;
            InsideTempString = "-";
            HeatingOn = false;

            DoorStatus = DOOR_STATUS.OPEN;

            CameraTempReady = false;

            InsideTempReady = false;

            ShowRunExperimentPanel = false;

            ShowHeaterOnOffPopup = false;

            ExperimentRunState = ViewModel_RunExperimentControl.RUN_STATE.NEEDS_INPUT;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }

    }
}
