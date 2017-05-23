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

    class OmegaTempCtrl
    {
        //System.Net.Sockets.TcpClient m_client;
        float m_temp;
        Timer m_updateTimer;

        string m_ipAddr;
        ushort m_port;

        string m_lastErrorMessage;

        SimpleAsyncClient m_simpleClient;


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



        public OmegaTempCtrl(string _ipAddr, ushort _port)
        {
            m_ipAddr = _ipAddr;
            m_port = _port;
            m_lastErrorMessage = "No Error";
            //m_client = new TcpClient();
            m_simpleClient = new SimpleAsyncClient();

            m_simpleClient.OnConnect += m_simpleClient_OnConnect;
            m_simpleClient.OnDisconnect += m_simpleClient_OnDisconnect;
            m_simpleClient.OnError += m_simpleClient_OnError;
            m_simpleClient.OnMessageReceived += m_simpleClient_OnMessageReceived;
        }

        void m_simpleClient_OnMessageReceived(SimpleAsyncClient client, SACMessageReceivedEventArgs args)
        {
            ParseReceivedMessage(args.MessageData);
        }

        void m_simpleClient_OnError(SimpleAsyncClient client, SACErrorEventArgs args)
        {
            OnMessage(new OmegaTempCtrlMessageEventArgs("Error Temp Ctrl: " + args.Exception.Message));
        }

        void m_simpleClient_OnDisconnect(SimpleAsyncClient client)
        {
            OnMessage(new OmegaTempCtrlMessageEventArgs("Disconnected from Temp Controller"));
        }

        void m_simpleClient_OnConnect(SimpleAsyncClient client)
        {
            OnMessage(new OmegaTempCtrlMessageEventArgs("Connected to Temp Controller"));
        }

        public bool Connect()
        {
            bool success = true;

            try
            {
                //m_client.Connect(m_ipAddr,m_port);

                m_simpleClient.Connect(m_ipAddr, m_port);
            }
            catch (Exception e)
            {
                m_lastErrorMessage = e.Message;
                success = false;
                OnMessage(new OmegaTempCtrlMessageEventArgs("Temp Ctrl Error: " + m_lastErrorMessage));
            }

            return success;
        }

        public void StartTempUpdate(double secondsBetweenUpdates)
        {
            m_updateTimer = new Timer(updateTemperatureCallback, null, TimeSpan.FromSeconds(secondsBetweenUpdates), TimeSpan.FromSeconds(secondsBetweenUpdates));
        }

        public void StopTempUpdate()
        {
            if (m_updateTimer != null) m_updateTimer.Dispose();
        }


        public bool EnableOutput(int outputNumber)
        {
            bool success = true;

            // send: *E01<cr>  or  *E02<cr>  depending on outputNumber          
            byte[] message = new byte[5] { 0x2a, 0x45, 0x30, 0x31, 0x0d };
            if (outputNumber == 2) message[3] = 0x32;
            m_simpleClient.Send(message);

            return success;
        }

        public bool DisableOutput(int outputNumber)
        {
            bool success = true;

            // send: *D01<cr>  or  *D02<cr>  depending on outputNumber   
            byte[] message = new byte[5] { 0x2a, 0x44, 0x30, 0x31, 0x0d };
            if (outputNumber == 2) message[3] = 0x32;
            m_simpleClient.Send(message);

            return success;
        }

        private void updateTemperatureCallback(object state)
        {
            requestTemperature();
        }

        public void requestTemperature()
        {
            // send: *D01<cr>  or  *D02<cr>  depending on outputNumber   
            byte[] message = new byte[5] { 0x2a, 0x58, 0x30, 0x31, 0x0d };
            m_simpleClient.Send(message);
        }


        public bool updateSetPoint(int setpointNum, int setpoint)
        {
            bool success = true;

            byte[] message = BuildCommand_SetSetPoint(setpointNum, setpoint);

            m_simpleClient.Send(message);

            return success;           
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
