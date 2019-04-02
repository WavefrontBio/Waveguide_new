﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Waveguide
{
    public class VWorks 
    {
        VWorks4Lib.VWorks4API VWorks_;
        string VWorks_LogMessage;
        VWorks4Lib._IVWorks4APIEvents_LogMessageEventHandler logEventHandler;

        Stopwatch m_stopwatch;

        Timer m_timer;
        
        bool m_protocolAborted;

        DateTime m_protocolStartTime;

        string m_bravoMethodFile;

        public bool m_vworksOK;

        ExperimentParams m_expParams;
 

        /////////////////////////////////////////////////////////////////////////////////////////////
        // Class Events

        public delegate void PostVWorksCommandHandler(object sender, WaveGuideEvents.VWorksCommandEventArgs e);
        public event PostVWorksCommandHandler PostVWorksCommandEvent;

        protected virtual void OnPostVWorksCommand(WaveGuideEvents.VWorksCommandEventArgs e)
        {
            if (PostVWorksCommandEvent != null) PostVWorksCommandEvent(this, e);
        }

        public void PostVWorksCommand(VWORKS_COMMAND command)
        {
            WaveGuideEvents.VWorksCommandEventArgs e = new WaveGuideEvents.VWorksCommandEventArgs(command);
            OnPostVWorksCommand(e);
        }

        public void PostVWorksCommand(VWORKS_COMMAND command, int param1)
        {
            WaveGuideEvents.VWorksCommandEventArgs e = new WaveGuideEvents.VWorksCommandEventArgs(command,param1);
            OnPostVWorksCommand(e);
        }

        public void PostVWorksCommand(VWORKS_COMMAND command, string name, string description)
        {
            WaveGuideEvents.VWorksCommandEventArgs e = new WaveGuideEvents.VWorksCommandEventArgs(command, 0, name, description);
            OnPostVWorksCommand(e);            
        }


        public void PostVWorksCommand(VWORKS_COMMAND command, int param1, string name, string description)
        {
            WaveGuideEvents.VWorksCommandEventArgs e = new WaveGuideEvents.VWorksCommandEventArgs(command, param1, name, description);
            OnPostVWorksCommand(e);
        }


        public delegate void PostVWorksMessageHandler(object sender, WaveGuideEvents.StringMessageEventArgs e);
        public event PostVWorksMessageHandler PostVWorksMessageEvent;

        protected virtual void OnPostVWorksMessage(WaveGuideEvents.StringMessageEventArgs e)
        {
            if (PostVWorksMessageEvent != null) PostVWorksMessageEvent(this, e);
        }

        public void PostVWorksMessage(string msg)
        {
            WaveGuideEvents.StringMessageEventArgs e = new WaveGuideEvents.StringMessageEventArgs(msg);
            OnPostVWorksMessage(e);
        }
    

        /////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////
        // Constructor

        public VWorks()
        {

            m_expParams = ExperimentParams.GetExperimentParams;

            // Set up VWorks Event handlers
            try
            {               
                VWorks_ = new VWorks4Lib.VWorks4API();
            }
            catch (Exception e)
            {
                m_vworksOK = false;
                PostVWorksCommand(VWORKS_COMMAND.Error, "VWorks Exception", e.Message);
                return;
            }

            if (VWorks_ != null)
            {
                m_vworksOK = true;

                VWorks_.InitializationComplete += new VWorks4Lib._IVWorks4APIEvents_InitializationCompleteEventHandler(VWorks__InitializationComplete);

                // uncomment the following 2 lines if you want to see the VWorks log messages (they are already shown in VWorks)
                logEventHandler = new VWorks4Lib._IVWorks4APIEvents_LogMessageEventHandler(VWorks__LogMessage);
                VWorks_.LogMessage += logEventHandler;

                VWorks_.MessageBoxAction += new VWorks4Lib._IVWorks4APIEvents_MessageBoxActionEventHandler(VWorks__MessageBoxAction);
                VWorks_.ProtocolAborted += new VWorks4Lib._IVWorks4APIEvents_ProtocolAbortedEventHandler(VWorks__ProtocolAborted);
                VWorks_.ProtocolComplete += new VWorks4Lib._IVWorks4APIEvents_ProtocolCompleteEventHandler(VWorks__ProtocolComplete);
                VWorks_.RecoverableError += new VWorks4Lib._IVWorks4APIEvents_RecoverableErrorEventHandler(VWorks__RecoverableError);
                VWorks_.UnrecoverableError += new VWorks4Lib._IVWorks4APIEvents_UnrecoverableErrorEventHandler(VWorks__UnrecoverableError);
                VWorks_.UserMessage += new VWorks4Lib._IVWorks4APIEvents_UserMessageEventHandler(VWorks__UserMessage);
                
                //TODO Change this Login this seems like a security risk.
                VWorks_.Login(GlobalVars.Instance.VWorksUsername, GlobalVars.Instance.VWorksPassword);
                VWorks_.ShowVWorks(false);

                m_stopwatch = new Stopwatch();

                m_protocolStartTime = new DateTime();
            }
            else 
            {
                m_vworksOK = false;
            }
            
        }


        /////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////
        // Destructor
        ~VWorks()
        {
            
        }


        public bool VWorks_CreatedSuccessfully()
        {
            return m_vworksOK;
        }


        public void ShowVWorks()
        {
            VWorks_.ShowVWorks(true);
        }

        public void HideVWorks()
        {
            VWorks_.ShowVWorks(false);
        }


        ///////////////////////////////////////////////////////////////////////////////////////////
        // Start Method


        public void StartMethod(string bravoMethodFileName, int repeatProtocolCount)
        {
            // bravoMethodFileName = the full path and filename of the VWorks protocol file to be executed
            // repeatProtocolCount = when starting a protocol in VWorks, it always asks how many times to repeat the protocol.  This parameters answers that question.

            ShowVWorks();

            m_protocolAborted = false;

            if (!File.Exists(bravoMethodFileName))
            {
                PostVWorksCommand(VWORKS_COMMAND.Error, "VWorks Method File was not found", bravoMethodFileName);
                return;
            }

            m_bravoMethodFile = bravoMethodFileName;

            ///////////////////////////////////
            // Run the Method (launch the Bravo method)


            String runString = bravoMethodFileName + " : " + DateTime.Now.ToString();
            PostVWorksCommand(VWORKS_COMMAND.Message, "", runString);

            // VWorks fires the LogMessage event while loading the protocol, which
            // in this simple example causes things to lock, because this client can't 
            // respond to the event while waiting for RunProtocol to return, and RunProtocol
            // won't return unless the client responds to the event!  

            // Temporarily unsubscribe from log events
            //VWorks_.LogMessage -= logEventHandler;

            // Start the protocol running
            m_protocolStartTime = DateTime.Now; 
            VWorks_.RunProtocol(bravoMethodFileName, repeatProtocolCount);

            // Again subscribe to events
            //VWorks_.LogMessage += logEventHandler;

        }





        ////////////////////////////////////////////////////////////////////////
        // START: VWorks Interface


        public void VWorks_AbortProtocol()
        {
            try
            {
                m_protocolAborted = true;
                VWorks_.AbortProtocol();

                PostVWorksCommand(VWORKS_COMMAND.Protocol_Aborted);
            }
            catch(Exception ex)
            {
                PostVWorksMessage("VWorks Error: " + ex.Message);
            }
        }

        public void VWorks_PauseProtocol()
        {
            try
            {
                VWorks_.PauseProtocol();
                PostVWorksCommand(VWORKS_COMMAND.Protocol_Paused);
            }
            catch(Exception ex)
            {
                PostVWorksMessage("VWorks Error: " + ex.Message);
            }
        }

        public void VWorks_ResumeProtcol()
        {
            VWorks_.ResumeProtocol();
            PostVWorksCommand(VWORKS_COMMAND.Protocol_Resumed); 
        }


        void VWorks__UserMessage(int session, string caption, string message, bool wantsData, out string userData)
        {
            // caption = vworks Title, message = vworks Body
            userData = "";


            // received command to pause Bravo's protocol for a given number of milliseconds
            // assumes that within the Bravo's UserMessage parameters are set to: Title = "Timer" and Body = <milliseconds>
            if (String.Compare(caption.Trim(), "Timer", true) == 0)
            {
                try
                {
                    int timerDelay = Convert.ToInt32(message);

                    VWorks_.PauseProtocol();
                    PostVWorksCommand(VWORKS_COMMAND.Protocol_Paused, timerDelay);

                    WaitResumeAsync(timerDelay);
                }
                catch (Exception ex)
                {
                    PostVWorksMessage("VWorks Error: " + ex.Message);
                }
            }

            else if (String.Compare(caption.Trim(), "TimeMarker", true) == 0)
            {
               
                    // this starts the stopwatch timer that will be used by the ResumeAfter
                    m_stopwatch.Restart();

                    PostVWorksCommand(VWORKS_COMMAND.Set_Time_Marker);
               
            }

            else if (String.Compare(caption.Trim(), "PauseUntil", true) == 0)
            {
                try
                {
                    // sends a Protocol Resume after given milliseconds after last TimeMarker
                    int timerDelay = Convert.ToInt32(message);
                    if (!m_stopwatch.IsRunning)
                    {
                        PostVWorksCommand(VWORKS_COMMAND.Error, "Time Marker Never Set", "PauseUntil will be Ignored");
                    }
                    else if (m_stopwatch.ElapsedMilliseconds >= timerDelay)
                    {
                        PostVWorksCommand(VWORKS_COMMAND.Error, "PauseUntil time too short", "Actual Pause time: " + m_stopwatch.ElapsedMilliseconds.ToString() + " msecs");
                    }
                    else
                    {
                        VWorks_.PauseProtocol();
                        PostVWorksCommand(VWORKS_COMMAND.Pause_Until, timerDelay - (int)m_stopwatch.ElapsedMilliseconds,
                            "Protocol Paused", "Waiting for " + timerDelay.ToString() + " msecs past last TimerMarker");
                        WaitResumeAsync(timerDelay - (int)m_stopwatch.ElapsedMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    PostVWorksMessage("VWorks Error: " + ex.Message);
                }

            }

            else if (String.Compare(caption.Trim(), "EventMarker", true) == 0)
            {
               
                    string[] parameter = message.Split(',');
                    string name = parameter[0].Trim();
                    string desc = parameter[1].Trim();
                    PostVWorksCommand(VWORKS_COMMAND.Event_Marker, name, desc);
               
            }

            else if (String.Compare(caption.Trim(), "StartImaging", true) == 0)
            {
               
                    PostVWorksCommand(VWORKS_COMMAND.Start_Imaging);
               
            }

            else if (String.Compare(caption.Trim(), "StopImaging", true) == 0)
            {
               
                    PostVWorksCommand(VWORKS_COMMAND.Stop_Imaging);
               
            }

            else if (String.Compare(caption.Trim(), "Barcode", true) == 0)
            {
                
                    PostVWorksCommand(VWORKS_COMMAND.Barcode, "Barcode", message);
                
            }
           
            else if (String.Compare(caption.Trim(), "VerifyImaging", true) == 0)
            {   
                // This is a signal to perform an Auto Verification (Optimization) on all indicators.  The VWorks protocol needs to be paused while this happens since we don't know 
                // how long it might take.  

              
                    PostVWorksCommand(VWORKS_COMMAND.VerifyImaging);
               

            }

            else if (String.Compare(caption.Trim(), "PlateComplete", true) == 0)
            {
              
                    PostVWorksCommand(VWORKS_COMMAND.PlateComplete, "PlateComplete", message);
               
            }

            else if (String.Compare(caption.Trim(), "PlateStart", true) == 0)
            {
              
                    PostVWorksCommand(VWORKS_COMMAND.PlateStart, "PlateStart", message);
               
            }

            else if (String.Compare(caption.Trim(), "EnableBurst", true) == 0)
            {
               
                    PostVWorksCommand(VWORKS_COMMAND.EnableBurstCycleTime);
               
            }

            else if (String.Compare(caption.Trim(), "DisableBurst", true) == 0)
            {
               
                    PostVWorksCommand(VWORKS_COMMAND.DisableBurstCycleTime);
                
            }

            else
            {
              
                    PostVWorksCommand(VWORKS_COMMAND.Error, "Unknown Command Received", caption + ", " + message);
              
            }
        }

        void VWorks__UnrecoverableError(int session, string description)
        {
            try
            {
                PostVWorksCommand(VWORKS_COMMAND.Unrecoverable_Error, "VWorks Unrecoverable Error", description);
                VWorks_.CloseProtocol(m_bravoMethodFile);
            }
            catch (Exception ex)
            {
                PostVWorksMessage("VWorks Error: " + ex.Message);
            }
        }

        void VWorks__RecoverableError(int session, string device, string location, string description, out int actionToTake, out bool vworksHandlesError)
        {
           
                actionToTake = 2;
                vworksHandlesError = true;
                PostVWorksCommand(VWORKS_COMMAND.Error, "VWorks Error", description);
          
        }

        void VWorks__ProtocolComplete(int session, string protocol, string protocol_type)
        {
           
                PostVWorksCommand(VWORKS_COMMAND.Protocol_Complete, protocol, protocol_type);

                switch (protocol_type)
                {
                    case "Startup":
                        break;
                    case "Main":
                        VWorks_.CloseProtocol(m_bravoMethodFile);
                        break;
                    default:
                        break;
                }
            
        }

        void VWorks__ProtocolAborted(int session, string protocol, string protocol_type)
        {
            try
            {
                PostVWorksCommand(VWORKS_COMMAND.Protocol_Aborted, protocol, protocol_type);
                VWorks_.CloseProtocol(m_bravoMethodFile);
            }
            catch (Exception ex)
            {
                PostVWorksMessage("VWorks Error: " + ex.Message);
            }
        }

        void VWorks__MessageBoxAction(int session, int type, string message, string caption, out int actionToTake)
        {
            actionToTake = 1;
        }

        void VWorks__InitializationComplete(int session)
        {
          
                PostVWorksCommand(VWORKS_COMMAND.Initialization_Complete);
          
        }

        void VWorks__LogMessage(int session, int logClass, string timeStamp, string device, string location, string process, string task, string fileName, string message)
        {
            VWorks_LogMessage = timeStamp + " " + message;     
        }

        

        async void WaitResumeAsync(int delay)
        {
            try
            {
                await SleepAsync(delay);

                if (!m_protocolAborted)
                {
                    VWorks_.ResumeProtocol();
                    PostVWorksCommand(VWORKS_COMMAND.Protocol_Resumed);
                }
            }
            catch (Exception ex)
            {
                PostVWorksMessage("VWorks Error: " + ex.Message);
            }
        }


        void WaitResumeTimer(int delay)
        {
            m_timer = new Timer(delay);
            m_timer.Elapsed += m_timer_Elapsed;            
            m_timer.Start();
        }

        void m_timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!m_protocolAborted)
                {
                    VWorks_.ResumeProtocol();
                    PostVWorksCommand(VWORKS_COMMAND.Protocol_Resumed);
                    m_timer.Stop();
                    m_timer.Dispose();
                }
            }
            catch (Exception ex)
            {
                PostVWorksMessage("VWorks Error: " + ex.Message);
            }
            
        }
        
       

        // END: VWorks Interface
        ////////////////////////////////////////////////////////////////////////


        public Task SleepAsync(int timeout)
        {
            TaskCompletionSource<bool> tcs = null;
            var t = new System.Threading.Timer(delegate { tcs.TrySetResult(true); }, null, -1, -1);
            tcs = new TaskCompletionSource<bool>(t);
            t.Change(timeout, -1);
            return tcs.Task;
        }



    }



    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////


    public enum VWORKS_COMMAND
    {
        Protocol_Aborted,
        Protocol_Paused,
        Protocol_Resumed,
        Set_Time_Marker,
        Pause_Until,
        Event_Marker,
        Start_Imaging,
        Stop_Imaging,
        Unrecoverable_Error,
        Protocol_Complete,
        Initialization_Complete,
        Error, 
        Message,
        Barcode,        // results of a barcode read was received
        VerifyImaging,  // run verification on all indicators
        PlateStart,     // signals that the experiment is beginning for this plate 
        PlateComplete,  // signals that experiment is complete with this plate: write report and reset data/graphs/etc.
        EnableBurstCycleTime, // signals that imaging cycle times should change to burst rates
        DisableBurstCycleTime, // signals that imaging cycle times should change to normal rates
    };




    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //namespace WaveGuideEvents
    //{


    //    public class VWorksCommandEventArgs : EventArgs 
    //    {
    //        private VWORKS_COMMAND _command;
    //        private int _param1;
    //        private string _name;
    //        private string _description;


    //        public VWORKS_COMMAND Command
    //        {
    //            get { return this._command; }
    //            set { this._command = value; }
    //        }

    //        public int Param1
    //        {
    //            get { return this._param1; }
    //            set { this._param1 = value; }
    //        }

    //        public string Name
    //        {
    //            get { return this._name; }
    //            set { this._name = value; }
    //        }

    //        public string Description
    //        {
    //            get { return this._description; }
    //            set { this._description = value; }
    //        }


    //        public VWorksCommandEventArgs(VWORKS_COMMAND command, int param1 = 0, string name = "", string description = "")
    //        {
    //            _command = command;
    //            _param1 = param1;
    //            _name = name;
    //            _description = description;
    //        }
    //    }
    //}



}
