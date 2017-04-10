using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTDI64_NET;


namespace Waveguide
{
    public class Thor
    {
        public bool SystemInitialized = false;

        private const int NOT_INITIALIZED = -100;

        UInt32 ftdiDeviceCount = 0;
        FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

        FTDI myFtdiDevice = new FTDI();

        byte[] cmd = new byte[5];
        UInt32 numBytesWritten = 0;



        /////////////////////////////////////////////////////////////////////////////////////////////
        // Class Events

        public delegate void PostMessageEventHandler(object sender, WaveGuideEvents.StringMessageEventArgs e);
        public delegate void PostErrorEventHandler(object sender, WaveGuideEvents.ErrorEventArgs e);

        public event PostMessageEventHandler PostMessageEvent;
        public event PostErrorEventHandler PostErrorEvent;

        protected virtual void OnPostMessage(WaveGuideEvents.StringMessageEventArgs e)
        {
            if (PostMessageEvent != null) PostMessageEvent(this, e);
        }

        protected virtual void OnPostError(WaveGuideEvents.ErrorEventArgs e)
        {
            if (PostErrorEvent != null) PostErrorEvent(this, e);
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

      

        // ////////////////////////////////////////////////////////////




        public bool Initialize()
        {
            SystemInitialized = false;

            UInt32 index = 0;  // this is the index of the Lambda device found in the list of USB devices connected

            // Determine the number of FTDI devices connected to the machine
            ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);

            // Check if devices found
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                if (ftdiDeviceCount == 0)
                {
                    PostError("No USB devices found (Thor Light Controller)");
                    return false; // no devices found
                }
            }


            // Allocate storage for device info list
            FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];

            // Populate our device list
            ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);

            // Search list for Thor device
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                index = 100;
                for (UInt32 i = 0; i < ftdiDeviceCount; i++)
                {
                    if (ftdiDeviceList[i].Description.ToString().Contains("FT232R")) index = i;
                }

                if (index == 100)
                {
                    PostError("No Thor Light Controller found");
                    return false; // no Thor devices found
                }

            }
            else
            {
                PostError("FTDI didn't load correctly and Thor Device is not initilized");
                return false;
            }


            // Open the Thor device found
            ftStatus = myFtdiDevice.OpenBySerialNumber(ftdiDeviceList[index].SerialNumber);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                PostError("Failed to open Thor Light Controller");
                return false;  // failed to open device
            }

            // Set up device data parameters
            // Set Baud rate to 19200
            ftStatus = myFtdiDevice.SetBaudRate(19200);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                PostError("Failed to set Thor Light Controller buad rate");
                return false;  // failed to set baud rate
            }

            // Set data characteristics - Data bits, Stop bits, Parity            
            ftStatus = myFtdiDevice.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTDI.FT_PARITY.FT_PARITY_EVEN);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                PostError("Failed to set Thor Light Controller data characteristics");
                return false;  // failed to set data characteristics (data bits, stop bits, parity)
            }

            // Set flow control - set RTS/CTS flow control            
            ftStatus = myFtdiDevice.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_NONE, 0x00, 0x00);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                PostError("Failed to set Thor Light Controller flow control");
                return false;  // failed to set flow control
            }

            // Set read timeout to 5 seconds, write timeout to infinite
            ftStatus = myFtdiDevice.SetTimeouts(5000, 0);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                PostError("Failed to set Thor Light Controller read/write timeout durations");
                return false;  // failed to set read/write timeout durations
            }

            SystemInitialized = true;

            PostMessage("Thor Light Controller initialized");

            return true;
        }

        public int TurnOn()
        {
            if (!SystemInitialized) return NOT_INITIALIZED;

            cmd[0] = 0x01;
            ftStatus = myFtdiDevice.Write(cmd, 1, ref numBytesWritten);

            return (int)ftStatus;
        }


        public int TurnOff()
        {
            if (!SystemInitialized) return NOT_INITIALIZED;
            cmd[0] = 0x02;
            ftStatus = myFtdiDevice.Write(cmd, 1, ref numBytesWritten);
            return (int)ftStatus;
        }

        public int SetIntensity(byte percent)
        {
            if (!SystemInitialized) return NOT_INITIALIZED;

            // percent must range between 20 and 100 percent
            // 100% = 128, 20% = 255
            float fval =  -1.5875f * (float)(percent) + 286.75f;

            cmd[0] = (byte)fval;
            ftStatus = myFtdiDevice.Write(cmd, 1, ref numBytesWritten);

            return (int)ftStatus;
        }



        public bool CheckThorResult(int code, ref string errorMsg)
        {
            bool ok = true;

            errorMsg = "SUCCESS";

            if (code != (int)FTDI.FT_STATUS.FT_OK)
            {
                ok = false;
                switch (code)
                {
                    case NOT_INITIALIZED: errorMsg = "Light Source Not Initialized"; break;
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
