using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waveguide
{
    public enum WGMessageType
    {
        STATUS = 0,         // message FROM WG, reports the status of WG    
        GET_STATUS,         // message TO WG, requests a STATUS message
        CONFIG_EXPERIMENT,  // message TO WG, pass in an Experiment Configuration File
        START_EXPERIMENT,   // message TO WG, command WG to start running a previously configured experiment
        STOP_EXPERIMENT     // message TO WG, command to stop running experiment
    }

    public enum WGStatus
    {
        ONLINE = 0,
        READY,
        RUNNING,
        UNKNOWN,
        ERROR,
        MESSAGE_FAILED__INCORRECT_MODE,
        MESSAGE_FAILED__MESSAGE_FORMAT_ERROR,
        MESSAGE_FAILED__BAD_CONFIGURATION_DATA,
        MESSAGE_FAILED__WAVEGUIDE_ERROR,
        MESSAGE_FAILED__VWORKS_ERROR
    }


    public class WaveguideMessage
    {
        public byte[] payloadBytes;
        public string payloadString;
        public WGMessageType messageType;
        public WGStatus status;

        public WaveguideMessage(WGMessageType type, WGStatus _status, byte[] payload, string payloadStr)
        {
            payloadBytes = payload;
            messageType = type;
            payloadString = payloadStr;
            status = _status;
        }
    }

    public class WaveguideMessageUtil
    {
        public static bool ParseMessage(byte[] data, out WaveguideMessage message)
        {
            bool success = true;
            message = null;

            if (data.Length < 4)
            {
                success = false;
            }
            else
            {
                try
                {
                    WGMessageType messageType = (WGMessageType)BitConverter.ToInt16(data, 0);
                    short payloadSize = BitConverter.ToInt16(data, 2);
                    WGStatus status = WGStatus.UNKNOWN;
                    string payloadStr = "";
                    byte[] payload = null;

                    switch (messageType)
                    {
                        case WGMessageType.STATUS:
                            status = (WGStatus)data[4];
                            payload = new byte[payloadSize - 1];
                            Buffer.BlockCopy(data, 5, payload, 0, payloadSize - 1);
                            payloadStr = Encoding.ASCII.GetString(payload);
                            break;
                        case WGMessageType.GET_STATUS:
                            break;
                        case WGMessageType.CONFIG_EXPERIMENT:
                            payload = new byte[payloadSize];
                            Buffer.BlockCopy(data, 4, payload, 0, payloadSize);
                            break;
                        case WGMessageType.START_EXPERIMENT:
                            break;
                        case WGMessageType.STOP_EXPERIMENT:
                            break;
                    }

                    message = new WaveguideMessage(messageType, status, payload, payloadStr);
                }
                catch (Exception)
                {
                    success = false;
                }

            }

            return success;
        }

        public static bool Build_GetStatus_Message(out byte[] message)
        {
            bool success = true;
            message = new byte[4] { (byte)WGMessageType.GET_STATUS, 0, 0, 0 };
            return success;
        }

        public static bool Build_StartExperiment_Message(out byte[] message)
        {
            bool success = true;
            message = new byte[4] { (byte)WGMessageType.START_EXPERIMENT, 0, 0, 0 };
            return success;
        }

        public static bool Build_StopExperiment_Message(out byte[] message)
        {
            bool success = true;
            message = new byte[4] { (byte)WGMessageType.STOP_EXPERIMENT, 0, 0, 0 };
            return success;
        }

        public static bool Build_ConfigureExperiment_Message(string filename, out byte[] message)
        {
            bool success = true;
            message = null;

            if (File.Exists(filename))
            {
                try
                {
                    byte[] payloadBytes = File.ReadAllBytes(filename);

                    short messageType = (short)WGMessageType.CONFIG_EXPERIMENT;
                    byte[] messageTypeBytes = BitConverter.GetBytes(messageType);

                    short payloadSize = (short)payloadBytes.Length;
                    byte[] payloadSizeBytes = BitConverter.GetBytes(payloadSize);

                    message = new byte[4 + payloadSize];

                    Buffer.BlockCopy(messageTypeBytes, 0, message, 0, messageTypeBytes.Length);
                    Buffer.BlockCopy(payloadSizeBytes, 0, message, messageTypeBytes.Length, payloadSizeBytes.Length);
                    Buffer.BlockCopy(payloadBytes, 0, message, messageTypeBytes.Length + payloadSizeBytes.Length, payloadBytes.Length);
                }
                catch (Exception)
                {
                    success = false;
                    message = null;
                }
            }
            else
            {
                success = false;  // file does not exist
            }

            return success;
        }

        public static bool Build_ConfigureExperiment_Message(ExperimentConfiguration config, out byte[] message)
        {
            bool success = true;
            message = null;

            string configStr;
            success = ExperimentConfiguration.ConvertToXmlString(config, out configStr);

            if (success)
            {
                try
                {
                    byte[] payloadBytes = Encoding.ASCII.GetBytes(configStr);

                    short messageType = (short)WGMessageType.CONFIG_EXPERIMENT;
                    byte[] messageTypeBytes = BitConverter.GetBytes(messageType);

                    short payloadSize = (short)payloadBytes.Length;
                    byte[] payloadSizeBytes = BitConverter.GetBytes(payloadSize);

                    message = new byte[4 + payloadSize];

                    Buffer.BlockCopy(messageTypeBytes, 0, message, 0, messageTypeBytes.Length);
                    Buffer.BlockCopy(payloadSizeBytes, 0, message, messageTypeBytes.Length, payloadSizeBytes.Length);
                    Buffer.BlockCopy(payloadBytes, 0, message, messageTypeBytes.Length + payloadSizeBytes.Length, payloadBytes.Length);
                }
                catch (Exception)
                {
                    success = false;
                    message = null;
                }
            }

            return success;
        }

        public static bool Build_Status_Message(WGStatus status, string statusMsg, out byte[] message)
        {
            bool success = true;
            message = null;

            if (success)
            {
                try
                {
                    byte[] payloadBytes = Encoding.ASCII.GetBytes(statusMsg);

                    short messageType = (short)WGMessageType.STATUS;
                    byte[] messageTypeBytes = BitConverter.GetBytes(messageType);

                    short payloadSize = (short)(payloadBytes.Length + 1);
                    byte[] payloadSizeBytes = BitConverter.GetBytes(payloadSize);

                    byte statusByte = (byte)status;

                    message = new byte[4 + payloadSize];

                    Buffer.BlockCopy(messageTypeBytes, 0, message, 0, 2);
                    Buffer.BlockCopy(payloadSizeBytes, 0, message, 2, 2);
                    message[4] = statusByte;
                    Buffer.BlockCopy(payloadBytes, 0, message, 5, payloadBytes.Length);
                }
                catch (Exception)
                {
                    success = false;
                    message = null;
                }
            }

            return success;

        }

    }
}               
