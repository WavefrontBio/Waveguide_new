using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.IO;

namespace Waveguide
{
    public class Lambda
    {
        // Events
        public delegate void SerialPortEventHandler(object sender, SerialPortEventArgs e);
        public event SerialPortEventHandler SerialPortEvent;
        protected virtual void OnSerialPortEvent(SerialPortEventArgs e)
        {
            if (SerialPortEvent != null) SerialPortEvent(this, e);
        }


        // Class Variables
        SerialPort m_port;
        const int mc_blockLimit = 1024;
        byte[] m_cmd = new byte[5];
        bool m_systemInitialized;


        // Constructor
        public Lambda(string portName)
        {
            int baudRate = 9600;
            Parity parity = Parity.None;
            int dataBits = 8;
            m_port = new SerialPort(portName, baudRate, parity, dataBits);
            StopBits sb = m_port.StopBits;

            m_port.DataReceived += m_port_DataReceived;
            m_port.ErrorReceived += m_port_ErrorReceived;

            m_systemInitialized = false;
        }

        void m_port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            OnSerialPortEvent(new SerialPortEventArgs(SerialPortEventType.ERROR, "", e.ToString(), null));
        }

       

        // Destructor
        ~Lambda()
        {
            if(m_port != null)
                m_port.Close();
        }


        public bool Initialize()
        {
            bool success = false;
            if (Open())
            {
                success = true;
                GoOnLine();
                m_systemInitialized = true;
            }

            return success;
        }

        public bool IsSystemInitialized()
        {
            return m_systemInitialized;
        }


        public bool Open()
        {
            try
            {
                m_port.Open();                
            }
            catch (Exception e)
            {
                OnSerialPortEvent(new SerialPortEventArgs(SerialPortEventType.ERROR, "", "Error opening port: " + e.Message, null));
            }

            if (m_port != null)
                return m_port.IsOpen;
            else
                return false;
        }

        public bool IsOpen()
        {
            return m_port.IsOpen;
        }

        public void Close()
        {
            try
            {
                m_port.Close();
            }
            catch (Exception e)
            {
                OnSerialPortEvent(new SerialPortEventArgs(SerialPortEventType.ERROR, "", "Error closing port: " + e.Message, null));
            }
        }

        public void Write(byte[] data)
        {
            try
            {
                m_port.Write(data, 0, data.Length);
            }
            catch(Exception e)
            {
                OnSerialPortEvent(new SerialPortEventArgs(SerialPortEventType.ERROR, "", "Error writing data: " + e.Message, null));
            }
        }

        public void Write(byte[] data, int numBytes)
        {
            try
            {
                m_port.Write(data, 0, numBytes);
            }
            catch (Exception e)
            {
                OnSerialPortEvent(new SerialPortEventArgs(SerialPortEventType.ERROR, "", "Error writing data: " + e.Message, null));
            }
        }


        public void Write(string str)
        {
            try
            {
                m_port.Write(str);
            }
            catch (Exception e)
            {
                OnSerialPortEvent(new SerialPortEventArgs(SerialPortEventType.ERROR, "", "Error writing data: " + e.Message, null));
            }
        }



        void m_port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buffer = new byte[mc_blockLimit];

            Action kickoffRead = null;
            kickoffRead = delegate
            {
                m_port.BaseStream.BeginRead(buffer, 0, buffer.Length, delegate(IAsyncResult ar)
                {
                    try
                    {
                        int actualLength = m_port.BaseStream.EndRead(ar);
                        byte[] received = new byte[actualLength];
                        Buffer.BlockCopy(buffer, 0, received, 0, actualLength);
                        OnSerialPortEvent(new SerialPortEventArgs(SerialPortEventType.DATA, "", "", received));
                    }
                    catch (IOException exc)
                    {
                        OnSerialPortEvent(new SerialPortEventArgs(SerialPortEventType.ERROR, "", exc.Message, null));
                    }
                    kickoffRead();
                }, null);
            };
            kickoffRead();
        }




        // //////////////////////////////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////////////////////////////////////
        //
        //  Lambda 10-3 Commands


        public void OpenShutterA()
        { 
            m_cmd[0] = 170;
            Write(m_cmd,1);
        }

        public void OpenShutterA_Conditional()
        {
            m_cmd[0] = 171;
            Write(m_cmd, 1);
        }

        public void CloseShutterA()
        {
            m_cmd[0] = 172;
            Write(m_cmd, 1);
        }


        public void OpenShutterB()
        {
            m_cmd[0] = 186;
            Write(m_cmd, 1);
        }

        public void OpenShutterB_Conditional()
        {
            m_cmd[0] = 187;
            Write(m_cmd, 1);
        }

        public void CloseShutterB()
        {
            m_cmd[0] = 188;
            Write(m_cmd, 1);
        }

        public void MoveFilterA(byte pos, byte speed)
        {
            m_cmd[0] = (byte)((speed * 16) + pos);
            Write(m_cmd, 1);
        }

        public void MoveFilterB(byte pos, byte speed)
        {
            m_cmd[0] = (byte)(128 + (speed * 16) + pos);
            Write(m_cmd, 1);
        }

        public void MoveFilterAB(byte posA, byte posB, byte aSpeed, byte bSpeed)
        {
            m_cmd[0] = 189;  // batch start (valid only for Lambda 10-3
            m_cmd[1] = (byte)((aSpeed * 16) + posA);
            m_cmd[2] = (byte)(128 + (bSpeed * 16) + posB);
            m_cmd[3] = 190;  // batch end
            Write(m_cmd, 4);
        }


        public void MoveFilterABandCloseShutterA(byte posA, byte posB, byte aSpeed, byte bSpeed)
        {
            m_cmd[0] = 189;  // batch start (valid only for Lambda 10-3
            m_cmd[1] = 172;  // close shutter A
            m_cmd[2] = (byte)((aSpeed * 16) + posA);  // move filter A
            m_cmd[3] = (byte)(128 + (bSpeed * 16) + posB);  // move filter B
            m_cmd[4] = 190;  // batch end
            Write(m_cmd, 5);
        }


        public void MoveFilterABandOpenShutterA(byte posA, byte posB, byte aSpeed, byte bSpeed)
        {
            m_cmd[0] = 189;  // batch start (valid only for Lambda 10-3
            m_cmd[1] = 170;  // open shutter A
            m_cmd[2] = (byte)((aSpeed * 16) + posA);  // move filter A
            m_cmd[3] = (byte)(128 + (bSpeed * 16) + posB);  // move filter B
            m_cmd[4] = 190;  // batch end
            Write(m_cmd, 5);
        }


        public void GoOnLine()
        {
            m_cmd[0] = 0xEE;
            Write(m_cmd, 1);
        }

    }


    public enum SerialPortEventType
    {
        ERROR,
        DATA,
        MESSAGE
    }

    public class SerialPortEventArgs : EventArgs
    {
        private SerialPortEventType _eventType;
        public SerialPortEventType EventType
        {
            get { return this._eventType; }
            set { this._eventType = value; }
        }
        
        private string _message;
        public string Message
        {
            get { return this._message; }
            set { this._message = value; }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return this._errorMessage; }
            set { this._errorMessage = value; }
        }

        private byte[] _data;
        public byte[] Data
        {
            get { return this._data; }
            set { this._data = value; }
        }

        public SerialPortEventArgs(SerialPortEventType type, string msg, string errMsg, byte[] data)
        {
            EventType = type;
            Message = msg;
            ErrorMessage = errMsg;
            Data = data;
        }
    }
}
