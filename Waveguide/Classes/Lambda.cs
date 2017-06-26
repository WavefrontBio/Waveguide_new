using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTDI64_NET;
using System.ComponentModel;
using System.Diagnostics;

namespace Waveguide
{
   
    public class Lambda
    {
        public bool SystemInitialized = false;

        private const int NOT_INITIALIZED = -100;

        UInt32 ftdiDeviceCount = 0;
        FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

        FTDI myFtdiDevice = new FTDI();

        byte[] cmd = new byte[5];
        UInt32 numBytesWritten = 0;

        BackgroundWorker m_bw;
        private static System.Timers.Timer m_timer;
        private bool m_timeout;


        // /////////////////////////////////////////////////////////////       
        /// Events

        /////////////////////////////////////////////////////////////////////////////////////////////
        // Class Events

        public delegate void PostMessageEventHandler(object sender, WaveGuideEvents.StringMessageEventArgs e);
        public delegate void PostErrorEventHandler(object sender, WaveGuideEvents.ErrorEventArgs e);             

        public event PostMessageEventHandler PostMessageEvent;
        public event PostErrorEventHandler   PostErrorEvent;
        public event PostMessageEventHandler PostCommandCompleteEvent;

        protected virtual void OnPostMessage(WaveGuideEvents.StringMessageEventArgs e)
        {
            if (PostMessageEvent != null) PostMessageEvent(this, e);
        }

        protected virtual void OnPostError(WaveGuideEvents.ErrorEventArgs e)
        {
            if (PostErrorEvent != null) PostErrorEvent(this, e);
        }

        protected virtual void OnPostCommandComplete(WaveGuideEvents.StringMessageEventArgs e)
        {
            if (PostCommandCompleteEvent != null) PostCommandCompleteEvent(this, e);
        }

        public void PostMessage(string msg)
        {
            WaveGuideEvents.StringMessageEventArgs e = new WaveGuideEvents.StringMessageEventArgs(msg);
            OnPostMessage(e);
        }

        public void PostError(string errMsg)
        {
            WaveGuideEvents.ErrorEventArgs e = new WaveGuideEvents.ErrorEventArgs(errMsg);
            OnPostError(e);
        }

        public void PostCommandComplete(string msg)
        {
            WaveGuideEvents.StringMessageEventArgs e = new WaveGuideEvents.StringMessageEventArgs(msg);
            OnPostCommandComplete(e);
        }



        // ////////////////////////////////////////////////////////////
    
        public Lambda()
        {
            SystemInitialized = false;

            ftdiDeviceCount = 0;
            ftStatus = FTDI.FT_STATUS.FT_OK;           

            cmd = new byte[5];
            numBytesWritten = 0;
        }

        public bool Initialize()
        {
           
            SystemInitialized = false;

            myFtdiDevice = new FTDI();

            UInt32 index = 0;  // this is the index of the Lambda device found in the list of USB devices connected

            // Determine the number of FTDI devices connected to the machine
            ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);

            // Check if devices found
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                if (ftdiDeviceCount == 0)
                {
                    PostError("No USB devices found (Lambda Filter Controller)");
                    return false; // no devices found
                }
            }
            else
            {
                PostError("Error Communicating with FTDI Device");
                return false; // no devices found
            }
            

            // Allocate storage for device info list
            FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];

            // Populate our device list
            ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);  

            // Search list for Lambda device
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                index = 100;
                for (UInt32 i = 0; i < ftdiDeviceCount; i++)
                {
                    if (ftdiDeviceList[i].Description.ToString().Contains("Lambda"))
                    {
                        index = i;
                    }
                }

                if (index == 100)
                {
                    PostError("Lambda Filter Controller not found");
                    return false; // no Lambda devices found
                }

            }


            
                       
                // Open the Lambda device found
                ftStatus = myFtdiDevice.OpenBySerialNumber(ftdiDeviceList[index].SerialNumber);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    PostError("Failed to open Lambda Filter Controller");
                   // return false;  // failed to open device
                }

                // Set up device data parameters
                // Set Baud rate to 9600 or 128000, may have to check to see what the speed is set at on the filter controller (this is the speed used by the USB to RS232 converter inside the filter controller)
                ftStatus = myFtdiDevice.SetBaudRate(9600);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    PostError("Failed to set Lambda Filter Controller baud rate");
                    //return false;  // failed to set baud rate
                }

                // Set data characteristics - Data bits, Stop bits, Parity
                ftStatus = myFtdiDevice.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTDI.FT_PARITY.FT_PARITY_NONE);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    PostError("Failed to Lambda Filter Controller data characteristics");
                    //return false;  // failed to set data characteristics (data bits, stop bits, parity)
                }

                // Set flow control - set RTS/CTS flow control
                ftStatus = myFtdiDevice.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x11, 0x13);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    PostError("Failed to set Lambda Filter Controller flow control");
                    //return false;  // failed to set flow control
                }

                // Set read timeout to 5 seconds, write timeout to infinite
                ftStatus = myFtdiDevice.SetTimeouts(5000, 0);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    PostError("Failed to set Lambda Filter Controller read/write timeout durations");
                   // return false;  // failed to set read/write timeout durations
                }
            

            SystemInitialized = true;

            PostMessage("Lambda Filter Controller initialized");

            return true;
        }

        public int ShutterAFast()
        {
            if (!SystemInitialized) return NOT_INITIALIZED;

            cmd[0] = 220;
            cmd[1] = 1;
            ftStatus = myFtdiDevice.Write(cmd, 1, ref numBytesWritten);

            return (int)ftStatus;
        }


        public int OpenShutterA()
        {
            if (!SystemInitialized) return NOT_INITIALIZED;

            cmd[0] = 170;
            ftStatus = myFtdiDevice.Write(cmd, 1, ref numBytesWritten);

            return (int)ftStatus;
        }

        public int OpenShutterA_Conditional()
        {
            if (!SystemInitialized) return NOT_INITIALIZED;

            cmd[0] = 171;
            ftStatus = myFtdiDevice.Write(cmd, 1, ref numBytesWritten);

            return (int)ftStatus;
        }

        public int CloseShutterA()
        {
            if (!SystemInitialized) return NOT_INITIALIZED;

            cmd[0] = 172;
            ftStatus = myFtdiDevice.Write(cmd, 1, ref numBytesWritten);

            return (int)ftStatus;
        }


        public int OpenShutterB()
        {
            if (!SystemInitialized) return NOT_INITIALIZED;

            cmd[0] = 186;
            ftStatus = myFtdiDevice.Write(cmd, 1, ref numBytesWritten);

            return (int)ftStatus;
        }

        public int OpenShutterB_Conditional()
        {
            if (!SystemInitialized) return NOT_INITIALIZED;

            cmd[0] = 187;
            ftStatus = myFtdiDevice.Write(cmd, 1, ref numBytesWritten);

            return (int)ftStatus;
        }

        public int CloseShutterB()
        {
            if (!SystemInitialized) return NOT_INITIALIZED;

            cmd[0] = 188;
            ftStatus = myFtdiDevice.Write(cmd, 1, ref numBytesWritten);

            return (int)ftStatus;
        }

        public int MoveFilterA(byte pos, byte speed)
        {
            ClearBuffer();
            if (!SystemInitialized) return NOT_INITIALIZED;

            cmd[0] = (byte)((speed * 16) + pos);
            ftStatus = myFtdiDevice.Write(cmd, 1, ref numBytesWritten);

            return (int)ftStatus;
        }

        public int MoveFilterB(byte pos, byte speed)
        {
            ClearBuffer();
            if (!SystemInitialized) return NOT_INITIALIZED;

            cmd[0] = (byte)(128 + (speed * 16) + pos);
            ftStatus = myFtdiDevice.Write(cmd, 1, ref numBytesWritten);

            return (int)ftStatus;
        }

        public int MoveFilterAB(byte posA, byte posB, byte aSpeed, byte bSpeed)
        {
            ClearBuffer();
            if (!SystemInitialized) return NOT_INITIALIZED;

            cmd[0] = 189;  // batch start (valid only for Lambda 10-3
            cmd[1] = (byte)((aSpeed * 16) + posA);
            cmd[2] = (byte)(128 + (bSpeed * 16) + posB);
            cmd[3] = 190;  // batch end
            PostMessage("Moving Filters");
            ftStatus = myFtdiDevice.Write(cmd, 4, ref numBytesWritten);            

            return (int)ftStatus;
        }


        public int MoveFilterABandCloseShutterA(byte posA, byte posB, byte aSpeed, byte bSpeed)
        {
            ClearBuffer();
            if (!SystemInitialized) return NOT_INITIALIZED;

            cmd[0] = 189;  // batch start (valid only for Lambda 10-3
            cmd[1] = 172;  // close shutter A
            cmd[2] = (byte)((aSpeed * 16) + posA);  // move filter A
            cmd[3] = (byte)(128 + (bSpeed * 16) + posB);  // move filter B
            cmd[4] = 190;  // batch end
            PostMessage("Moving Filters");
            ftStatus = myFtdiDevice.Write(cmd, 5, ref numBytesWritten);

            return (int)ftStatus;
        }


        public int MoveFilterABandOpenShutterA(byte posA, byte posB, byte aSpeed, byte bSpeed)
        {
            ClearBuffer();
            if (!SystemInitialized) return NOT_INITIALIZED;

            cmd[0] = 189;  // batch start (valid only for Lambda 10-3
            cmd[1] = 170;  // open shutter A
            cmd[2] = (byte)((aSpeed * 16) + posA);  // move filter A
            cmd[3] = (byte)(128 + (bSpeed * 16) + posB);  // move filter B
            cmd[4] = 190;  // batch end
            PostMessage("Moving Filters");
            ftStatus = myFtdiDevice.Write(cmd, 5, ref numBytesWritten);

            return (int)ftStatus;
        }



        public void WaitForCommandToComplete()
        {
            m_bw = new BackgroundWorker();

            m_bw.DoWork += new DoWorkEventHandler(m_bw_DoWork);

            m_bw.RunWorkerAsync();
        }

        void m_bw_DoWork(object sender, DoWorkEventArgs e)
        {
            byte loop = 1;
            byte byteCR = 13; // Carriage Return
            double waitMilliseconds = 5000;
            m_timeout = false;

            m_timer = new System.Timers.Timer(waitMilliseconds);
            m_timer.Elapsed += new System.Timers.ElapsedEventHandler(m_timer_Elapsed);
    
            loop = ReadByte();
            while (loop != byteCR && IsOpen()  && !m_timeout)
            {
                loop = ReadByte();                
            }

            ClearBuffer();//You might read the CR twice if you do not do this!
            ClearBuffer();

            if (!m_timeout) PostCommandComplete("Lambda Command Complete");
            else PostError("Lambda Timeout");
        }

        void m_timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            m_timeout = true;
        }


       
        public void ClearBuffer()
        {
            ftStatus = myFtdiDevice.Purge(0);
            ftStatus = myFtdiDevice.Purge(1);
        }


        public byte ReadByte()
        {
            UInt32 test = 0;
            byte[] testArray = new byte[5];
            ftStatus = myFtdiDevice.Read(testArray, 1, ref test);
            return testArray[0];
        }

        public bool IsOpen()
        {
            bool isOpen = true;
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                isOpen = false;
            }
            return isOpen;
        }



        public bool CheckLambdaResult(int code, ref string errorMsg)
        {
            bool ok = true;

            errorMsg = "SUCCESS";

            if (code != (int)FTDI.FT_STATUS.FT_OK)
            {
                ok = false;
                switch (code)
                {
                    case NOT_INITIALIZED: errorMsg = "Filter Controller Not Initialized"; break;
                    case -1: errorMsg = "No Devices Found"; break;
                    case -2: errorMsg = "No Thor Devices"; break;
                    case -3: errorMsg = "Failed to Open Device"; break;
                    case -4: errorMsg = "Failed to Set Baud Rate"; break;
                    case -5: errorMsg = "Failed to set data characteristics (data bits, stop bits, parity)"; break;
                    case -6: errorMsg = "Failed to set flow control"; break;
                    case -7: errorMsg = "Failed to set READ/WRITE timeout durations"; break;
                    case 0: errorMsg = "FT_OK"; break;
                    case 1: errorMsg = "FT_INVALID_HANDLE"; break;
                    case 2: errorMsg = "FT_DEVICE_NOT_FOUND"; break;
                    case 3: errorMsg = "FT_DEVICE_NOT_OPENED"; break;
                    case 4: errorMsg = "FT_IO_ERROR"; break;
                    case 5: errorMsg = "FT_INSUFFICIENT_RESOURCES"; break;
                    case 6: errorMsg = "FT_INVALID_PARAMETER"; break;
                    case 7: errorMsg = "FT_INVALID_BAUD_RATE"; break;
                    case 8: errorMsg = "FT_DEVICE_NOT_OPENED_FOR_ERASE"; break;
                    case 9: errorMsg = "FT_DEVICE_NOT_OPENED_FOR_WRITE"; break;
                    case 10: errorMsg = "FT_FAILED_TO_WRITE_DEVICE"; break;
                    case 11: errorMsg = "FT_EEPROM_READ_FAILED"; break;
                    case 12: errorMsg = "FT_EEPROM_WRITE_FAILED"; break;
                    case 13: errorMsg = "FT_EEPROM_ERASE_FAILED"; break;
                    case 14: errorMsg = "FT_EEPROM_NOT_PRESENT"; break;
                    case 15: errorMsg = "FT_EEPROM_NOT_PROGRAMMED"; break;
                    case 16: errorMsg = "FT_INVALID_ARGS"; break;
                    case 17: errorMsg = "FT_OTHER_ERROR"; break;

                }
            }

            return ok;
        }

    }
}
