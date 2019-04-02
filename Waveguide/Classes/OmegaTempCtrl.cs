using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.ComponentModel;
using TcpTools;

namespace Waveguide
{
    public delegate void TempCtrl_MessageEventHandler(object sender, OmegaTempCtrlMessageEventArgs e);
    public delegate void TempCtrl_TemperatureEventHandler(object sender, OmegaTempCtrlTempEventArgs e);

    public class OmegaTempCtrl
    {
        //System.Net.Sockets.TcpClient m_client;
        float m_temp;
        Timer m_updateTimer;

        string m_ipAddr;
        int m_port;

        string m_lastErrorMessage;

        EventDrivenTCPClient m_simpleClient;


        public event TempCtrl_MessageEventHandler MessageEvent;
        protected virtual void OnMessage(OmegaTempCtrlMessageEventArgs e)
        {
            if (MessageEvent != null)
                MessageEvent(this, e);
        }


        public event TempCtrl_TemperatureEventHandler TempEvent;
        protected virtual void OnNewTemperature(OmegaTempCtrlTempEventArgs e)
        {
            if (TempEvent != null)
                TempEvent(this, e);
        }



        public OmegaTempCtrl(string _ipAddr, int _port)
        {
            m_ipAddr = _ipAddr;
            m_port = _port;
            m_lastErrorMessage = "No Error";
            m_simpleClient = new EventDrivenTCPClient(IPAddress.Parse(_ipAddr), _port, true);

            m_simpleClient.ConnectionStatusChanged += m_simpleClient_ConnectionStatusChanged;
            m_simpleClient.DataReceived += m_simpleClient_DataReceived;
            
        }

        void m_simpleClient_DataReceived(EventDrivenTCPClient sender, object data)
        {
            ParseReceivedMessage((string)data);
        }

        void m_simpleClient_ConnectionStatusChanged(EventDrivenTCPClient sender, EventDrivenTCPClient.ConnectionStatus status)
        {
            string msg = "Unknown Status";

            switch(status)
            {
                case EventDrivenTCPClient.ConnectionStatus.AutoReconnecting:
                    msg = "Reconnecting...";
                    break;
                case EventDrivenTCPClient.ConnectionStatus.Connected:
                    msg = "Connected";
                    break;
                case EventDrivenTCPClient.ConnectionStatus.ConnectFail_Timeout:
                    msg = "Connection Fail, Timeout";
                    break;
                case EventDrivenTCPClient.ConnectionStatus.Connecting:
                    msg = "Connecting...";
                    break;
                case EventDrivenTCPClient.ConnectionStatus.DisconnectedByHost:
                    msg = "Disconnected by Host";
                    break;
                case EventDrivenTCPClient.ConnectionStatus.DisconnectedByUser:
                    msg = "Disconnected by User";
                    break;
                case EventDrivenTCPClient.ConnectionStatus.Error:
                    msg = "Error";
                    break;
                case EventDrivenTCPClient.ConnectionStatus.NeverConnected:
                    msg = "Never Connected";
                    break;
                case EventDrivenTCPClient.ConnectionStatus.ReceiveFail_Timeout:
                    msg = "Recieve Failure, Timeout";
                    break;
                case EventDrivenTCPClient.ConnectionStatus.SendFail_NotConnected:
                    msg = "Send Failure, Not Connected";
                    break;
                case EventDrivenTCPClient.ConnectionStatus.SendFail_Timeout:
                    msg = "Send Failure, Timeout";
                    break;
            }

            OnMessage(new OmegaTempCtrlMessageEventArgs("Temp Ctrl: " + msg));
        }


        public bool IsConnected()
        {
            return (m_simpleClient.ConnectionState == EventDrivenTCPClient.ConnectionStatus.Connected);
        }



        public void StartTempUpdate(double secondsBetweenUpdates)
        {
            m_updateTimer = new Timer(updateTemperatureCallback, null, TimeSpan.FromSeconds(secondsBetweenUpdates), TimeSpan.FromSeconds(secondsBetweenUpdates));

            if(!IsConnected())
            {
                m_simpleClient.Connect();
            }
        }

        public void StopTempUpdate()
        {
            if (m_updateTimer != null)
            {
                m_updateTimer.Dispose();
                m_updateTimer = null;
            }
        }

        public void EnableHeater(bool enable)
        {
            // This seems backwards!!!  I think it has something to do with how the Omega controller has it's output 1 configured.  Leaving it like this for now.  Yikes!!

            //if (enable)
            //    EnableOutput(1);
            //else
            //    DisableOutput(1);

            if (enable)
                DisableOutput(1);
            else
                EnableOutput(1);
        }


        private bool EnableOutput(int outputNumber)
        {
            bool success = true;

            if (IsConnected())
            {
                // send: *E01<cr>  or  *E02<cr>  depending on outputNumber          
                byte[] message = new byte[5] { 0x2a, 0x45, 0x30, 0x31, 0x0d };
                if (outputNumber == 2) message[3] = 0x32;
                m_simpleClient.Send(message);

                GlobalVars.Instance.InsideHeaterON = true;

                return success;
            }
            else
                return false;
        }

        private bool DisableOutput(int outputNumber)
        {
            bool success = true;

            if (IsConnected())
            {
                // send: *D01<cr>  or  *D02<cr>  depending on outputNumber   
                byte[] message = new byte[5] { 0x2a, 0x44, 0x30, 0x31, 0x0d };
                if (outputNumber == 2) message[3] = 0x32;
                m_simpleClient.Send(message);

                GlobalVars.Instance.InsideHeaterON = false;

                return success;
            }
            else
                return false;
        }

        private void updateTemperatureCallback(object state)
        {
            if (IsConnected())
            {
                requestTemperature();
            }
        }

        public void requestTemperature()
        {
            if (IsConnected())
            {
                // send: *D01<cr>  or  *D02<cr>  depending on outputNumber   
                byte[] message = new byte[5] { 0x2a, 0x58, 0x30, 0x31, 0x0d };
                m_simpleClient.Send(message);
            }
            else
            {
                OnMessage(new OmegaTempCtrlMessageEventArgs("Attempted to communicate with disconnected Temp Controller"));
            }
        }


        public bool updateSetPoint(int setpointNum, int setpoint)
        {
            bool success = true;

            if (IsConnected())
            {

                byte[] message = BuildCommand_SetSetPoint(setpointNum, setpoint);

                m_simpleClient.Send(message);

                GlobalVars.Instance.InsideTargetTemperature = setpoint;

                return success;
            }
            else
                return false;

        }


        private void ParseReceivedMessage(string message)
        {
            // check to see if it was an error
            if (message[0] == 0x3f)  // if first byte of message is a "?", then this is an error
            {
                // TODO:  Handle Error
            }
            else
            {
                if (message.Substring(0, 3).Equals("X01"))
                {
                    // received a temperature update message
                    string tString = message.Substring(3, 5);
                    m_temp = (float)Convert.ToDouble(tString);
                    OnNewTemperature(new OmegaTempCtrlTempEventArgs(m_temp));

                    GlobalVars.Instance.InsideTemp = (int)m_temp;
                }
            }
        }


        private void ParseReceivedMessage(byte[] message)
        {
            // check to see if it was an error
            if (message[0] == 0x3f)  // if first byte of message is a "?", then this is an error
            {
                // TODO:  Handle Error
            }
            else
            {
                var str = System.Text.Encoding.Default.GetString(message);

                if (str.Substring(0, 3).Equals("X01"))
                {
                    // received a temperature update message
                    string tString = str.Substring(3, 5);
                    m_temp = (float)Convert.ToDouble(tString);
                    OnNewTemperature(new OmegaTempCtrlTempEventArgs(m_temp));

                    GlobalVars.Instance.InsideTemp = (int)m_temp;
                }
            }
        }


        public byte[] BuildCommand_SetSetPoint(int setpointNum, int setpointValue)
        {
            // command is "*W01" + 6 ASCII characters determined from 24 bits of flags + carriage return
            // turn 24-bit pattern into 

            // Examples:
            //   set point  100 ->  command = "*W012003E8<cr>"
            //   set point -100 ->  command = "*W01A003E8<cr>"

            //                               "*"   "W"   "0"   "1"       [          set point         ] <cr> 
            byte[] command = new byte[11] { 0x2a, 0x57, 0x30, 0x31, 0x32, 0x30, 0x30, 0x33, 0x45, 0x38, 0x0d };

            if (setpointNum == 2) command[3] = 0x32;

            if (setpointValue >= 0)
            {
                command[4] = 0x32;
            }
            else
            {
                command[4] = 0x41;
            }

            setpointValue *= 10;  // multiply times ten because value set to device is in tenths of a degree
            string s = setpointValue.ToString("X5");

            command[5] = (byte)s[0];
            command[6] = (byte)s[1];
            command[7] = (byte)s[2];
            command[8] = (byte)s[3];
            command[9] = (byte)s[4];

            return command;
        }


    }




    public class OmegaTempCtrl_ViewModel : INotifyPropertyChanged
    {
        private float _temp;

        public float Temperature
        {
            get { return _temp; }
            set
            {
                _temp = value;
                NotifyPropertyChanged("Temperature");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }



    public class OmegaTempCtrlMessageEventArgs : EventArgs
    {
        private string message;

        public OmegaTempCtrlMessageEventArgs(string _message)
        {
            message = _message;
        }

        public string Message
        {
            get { return message; }
            set { message = value; }
        }        
    }


    public class OmegaTempCtrlTempEventArgs : EventArgs
    {        
        private float temp;

        public OmegaTempCtrlTempEventArgs(float _temp)
        {
            temp = _temp;
        }
             
        public float Temperature
        {
            get { return temp; }
            set { temp = value; }
        }
    }


}
