using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Brainboxes.IO;
using System.Timers;

namespace Waveguide
{
    public delegate void IOEventHandler(object sender, IOEventArgs e);
    public delegate void IOConnectionEventHandler(object sender, IOConnectionEventArgs e);
    public delegate void IOMessageEventHandler(object sender, IOMessageEventArgs e);
    public delegate void DoorStatusEventHandler(object sender, DoorStatusEventArgs e);


    public class EthernetIO
    {
        string m_ipAddr;
        IConnection m_connection;
        EDDevice m_device;
        bool m_connected;
        bool m_tryingToConnect;
   
        static Timer m_watchdogTimer;

        public event IOEventHandler m_ioEvent;
        protected virtual void OnIOEvent(IOEventArgs e)
        {
            if (m_ioEvent != null)
                m_ioEvent(this, e);
        }

        public event IOConnectionEventHandler m_ioConnectionEvent;
        protected virtual void OnIOConnectionEvent(IOConnectionEventArgs e)
        {
            if (m_ioConnectionEvent != null)
                m_ioConnectionEvent(this, e);
        }

        public event IOMessageEventHandler m_ioMessageEvent;
        protected virtual void OnIOMessageEvent(IOMessageEventArgs e)
        {
            if (m_ioMessageEvent != null)
                m_ioMessageEvent(this, e);
        }

        public event DoorStatusEventHandler m_doorStatusEvent;
        protected virtual void OnDoorStatusEvent(DoorStatusEventArgs e)
        {
            if (m_doorStatusEvent != null)
                m_doorStatusEvent(this, e);
        }

        public EthernetIO(string ipAddr)
        {
            m_connected = false;
            m_ipAddr = ipAddr;
            
            m_watchdogTimer = new Timer(5.0); // monitor Ethernet connection
            m_watchdogTimer.Elapsed += m_watchdogTimer_Elapsed;
            m_watchdogTimer.Start();

            m_tryingToConnect = false;

            m_watchdogTimer_Elapsed(null, null);
        }

        void m_watchdogTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(!m_connected && !m_tryingToConnect && m_device != null)
            {
                m_tryingToConnect = true;

                m_connection = new TCPConnection(m_ipAddr);
                m_connection.ConnectionStatusChangedEvent += m_connection_ConnectionStatusChangedEvent;
                m_connection.Connect();      
            }
        }

        void m_connection_ConnectionStatusChangedEvent(IConnection connection, string property, bool newValue)
        {
            m_tryingToConnect = false;

            if(connection.IsConnected)
            {
                m_connected = true;

                m_device = new ED588(m_connection);
                m_device.Label = "BrainBoxes IO Module";
          
                m_device.Inputs.IOLineChange += Inputs_IOLineChange;
                m_device.Outputs.IOLineChange += Outputs_IOLineChange;
                m_device.DeviceStatusChangedEvent += m_device_DeviceStatusChangedEvent;
                OnIOConnectionEvent(new IOConnectionEventArgs(true));
                OnIOMessageEvent(new IOMessageEventArgs("Ethernet IO Module Connected"));
            }
            else
            {
                m_connected = false;  
                m_device = null;
                OnIOConnectionEvent(new IOConnectionEventArgs(false));
                OnIOMessageEvent(new IOMessageEventArgs("Ethernet IO Module Disconnected"));

                //m_connection.Connect(); // retry connection
            }          
        }

        

        void m_device_DeviceStatusChangedEvent(IDevice<IConnection,IIOProtocol> device, string property, bool newValue)
        {
            OnIOMessageEvent(new IOMessageEventArgs("IO Module Status Change: " + property));
        }

        void Inputs_IOLineChange(IOLine line, EDDevice device, IOChangeTypes changeType)
        {            
            switch(line.IONumber)
            {
                case 0:
                    if (changeType == IOChangeTypes.RisingEdge)
                    {
                        if (m_device.Outputs[0].Value == 1) // magnetic latch is ON
                            GlobalVars.DoorStatus = DOOR_STATUS.LOCKED;
                        else
                            GlobalVars.DoorStatus = DOOR_STATUS.CLOSED;
                    }
                    else if (changeType == IOChangeTypes.FallingEdge)
                    {
                        GlobalVars.DoorStatus = DOOR_STATUS.OPEN;
                    }
                    OnDoorStatusEvent(new DoorStatusEventArgs(GlobalVars.DoorStatus));                    
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
                case 7:
                    break;
            }
            OnIOEvent(new IOEventArgs(IO_TYPE.INPUT, line.IONumber, line.Value == 1 ? true : false));
        }

        void Outputs_IOLineChange(IOLine line, EDDevice device, IOChangeTypes changeType)
        {
            OnIOEvent(new IOEventArgs(IO_TYPE.OUTPUT, line.IONumber, line.Value == 1 ? true : false));
        }


        public void SetOutputON(int outputNumber, bool turnON)
        {
            if (m_connected && m_device != null)
            {
                if(outputNumber < m_device.Outputs.Count)
                    m_device.Outputs[outputNumber].Value = turnON ? 1 : 0;
            }
        }

        public bool ReadInput(int inputNumber)
        {
            bool isON = false;

            if (m_connected && m_device != null)
            {
                if(inputNumber < m_device.Inputs.Count)
                    isON = m_device.Inputs[inputNumber].Value == 1 ? true : false;
            }

            return isON;
        }


        public void DoorLockON(bool turnON)
        {
            if(m_connected && m_device != null)
            {
                if (turnON)
                {
                    m_device.Outputs[0].Value = 1;
                    if (m_device.Inputs[0].Value == 1)
                        GlobalVars.DoorStatus = DOOR_STATUS.LOCKED;
                    else
                        GlobalVars.DoorStatus = DOOR_STATUS.OPEN;
                }
                else
                {
                    m_device.Outputs[0].Value = 0;
                    if(m_device.Inputs[0].Value == 1)
                        GlobalVars.DoorStatus = DOOR_STATUS.CLOSED;
                    else
                        GlobalVars.DoorStatus = DOOR_STATUS.OPEN;
                }
            }
        }




    }



    public enum IO_TYPE
    {
        INPUT,
        OUTPUT
    }


    public class IOEventArgs : EventArgs
    {        
        private IO_TYPE _lineType; 
        private int  _lineNumber;  
        private bool _lineON;

        public IO_TYPE LineType
        {
            get { return _lineType; }
            set { _lineType = value; }
        }

        public int LineNumber
        {
            get { return _lineNumber; }
            set { _lineNumber = value; }
        }

        public bool LineON
        {
            get { return _lineON; }
            set { _lineON = value; }
        }

        public IOEventArgs(IO_TYPE lineType, int lineNumber, bool lineON)
        {
            _lineType = lineType;
            _lineNumber = lineNumber;
            _lineON = lineON;
        }
    }

    public class DoorStatusEventArgs : EventArgs
    {
        private DOOR_STATUS _doorStatus;
        public DOOR_STATUS DoorStatus
        {
            get { return _doorStatus; }
            set { _doorStatus = value; }
        }

        public DoorStatusEventArgs(DOOR_STATUS doorStatus)
        {
            _doorStatus = doorStatus;
        }
    }

    public class IOConnectionEventArgs : EventArgs
    {
        private bool _connected;
        public bool Connected
        {
            get { return _connected; }
            set { _connected = value; }
        }

        public IOConnectionEventArgs(bool connected)
        {
            _connected = connected;
        }
    }

    public class IOMessageEventArgs : EventArgs
    {
        private string _message;
        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public IOMessageEventArgs(string message)
        {
            _message = message;
        }
    }

       
}
