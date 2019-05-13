using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Configuration;
using System.Windows;
using System.IO;
using System.ComponentModel;

namespace Waveguide
{


    public enum DOOR_STATUS
    {
        OPEN,
        CLOSED,
        LOCKED
    }



    public delegate void StatusChangeEventHandler(object sender, StatusChangeEventArgs e);

    public class GlobalVars
    {
        public event StatusChangeEventHandler m_statusChangeEvent;
        protected virtual void OnStatusChangeEvent(StatusChangeEventArgs e)
        {
            if (m_statusChangeEvent != null)
                m_statusChangeEvent(this, e);
        }



        private static readonly GlobalVars instance = new GlobalVars();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static GlobalVars()
        {
        }

        private GlobalVars()
        {
        }

        public static GlobalVars Instance
        {
            get
            {
                return instance;
            }
        }


        //////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////


        private WGStatus _status;
        public WGStatus Status
        {
            get { return _status; }
            set { _status = value; OnStatusChangeEvent(new StatusChangeEventArgs(_status)); }
        }

        private TaskScheduler _uiTask;
        public TaskScheduler UITask
        {
            get { return _uiTask; }
            set { _uiTask = value; }
        }


        private VWorks _vWorks;
        public VWorks VWorks
        {
            get { return _vWorks; }
            set { _vWorks = value; }
        }

        public enum USER_ROLE_ENUM
        {
            ADMIN,
            USER,
            OPERATOR
        }

        private USER_ROLE_ENUM _userRole;  // assigned role of this user
        public USER_ROLE_ENUM UserRole
        {
            get { return _userRole; }
            set { _userRole = value; }
        }

        private int _userID;  // database record ID for user
        public int UserID
        {
            get { return _userID; }
            set { _userID = value; }
        }

        private string _userDisplayName;  // display name for user, taken from DB after login
        public string UserDisplayName
        {
            get { return _userDisplayName; }
            set { _userDisplayName = value; }
        }

        private int _maxPixelValue;  // maximum pixel value from camera
        public int MaxPixelValue
        {
            get { return _maxPixelValue; }
            set { _maxPixelValue = value; }
        }

        private int _pixelWidth;  // pixel width of camera CCD
        public int PixelWidth
        {
            get { return _pixelWidth; }
            set { _pixelWidth = value; }
        }

        private int _pixelHeight;  // pixel height of camera CCD
        public int PixelHeight
        {
            get { return _pixelHeight; }
            set { _pixelHeight = value; }
        }


        private int _maxNumberImagesPerExperiment;  // maximum number of images that can be taken for a single experiment 
        public int MaxNumberImagesPerExperiment     //  This is related to memory allocation on the GPU
        {
            get { return _maxNumberImagesPerExperiment; }
            set { _maxNumberImagesPerExperiment = value; }
        }


        private int _cameraDefaultCycleTime;
        public int CameraDefaultCycleTime
        {
            get { return _cameraDefaultCycleTime; }
            set { _cameraDefaultCycleTime = value; }
        }



        private byte _filterChangeSpeed;
        public byte FilterChangeSpeed
        {
            get { return _filterChangeSpeed; }
            set { _filterChangeSpeed = value; }
        }


        private string _imageFileSaveLocation;
        public string ImageFileSaveLocation
        {
            get { return _imageFileSaveLocation; }
            set { _imageFileSaveLocation = value; }
        }

        private List<Color> _defaultTraceColorList;
        public List<Color> DefaultTraceColorList
        {
            get { return _defaultTraceColorList; }
            set { _defaultTraceColorList = value; }
        }

        private COMPRESSION_ALGORITHM _compressionAlgorithm;
        public COMPRESSION_ALGORITHM CompressionAlgorithm
        {
            get { return _compressionAlgorithm; }
            set { _compressionAlgorithm = value; }
        }


        private string _databaseConnectionString;
        public string DatabaseConnectionString
        {
            get { return _databaseConnectionString; }
            set { _databaseConnectionString = value; }
        }

        private string _vworksUsername;
        public string VWorksUsername
        {
            get { return _vworksUsername; }
            set { _vworksUsername = value; }
        }

        private string _vworksPassword;
        public string VWorksPassword
        {
            get { return _vworksPassword; }
            set { _vworksPassword = value; }
        }

        private string _vworksProtocolFileDirectory;  // display name for user, taken from DB after login
        public string VWorksProtocolFileDirectory
        {
            get { return _vworksProtocolFileDirectory; }
            set { _vworksProtocolFileDirectory = value; }
        }

        private int _upSignalOptimizePercentCountThreshold;
        public int UpSignalOptimizePercentCountThreshold
        {
            get { return _upSignalOptimizePercentCountThreshold; }
            set { _upSignalOptimizePercentCountThreshold = value; }
        }

        private int _downSignalOptimizePercentCountThreshold;
        public int DownSignalOptimizePercentCountThreshold
        {
            get { return _downSignalOptimizePercentCountThreshold; }
            set { _downSignalOptimizePercentCountThreshold = value; }
        }

        private int _upDownSignalOptimizePercentCountThreshold;
        public int UpDownSignalOptimizePercentCountThreshold
        {
            get { return _upDownSignalOptimizePercentCountThreshold; }
            set { _upDownSignalOptimizePercentCountThreshold = value; }
        }

        private int _maxCameraTemperatureThresholdDeviation;
        public int MaxCameraTemperatureThresholdDeviation
        {
            get { return _maxCameraTemperatureThresholdDeviation; }
            set { _maxCameraTemperatureThresholdDeviation = value; }
        }

        private int _maxInsideTemperatureThresholdDeviation;
        public int MaxInsideTemperatureThresholdDeviation
        {
            get { return _maxInsideTemperatureThresholdDeviation; }
            set { _maxInsideTemperatureThresholdDeviation = value; }
        }

        private string _enclosureCameraIPAddress;
        public string EnclosureCameraIPAddress
        {
            get { return _enclosureCameraIPAddress; }
            set { _enclosureCameraIPAddress = value; }
        }


        private string _defaultExcelReportFileDirectory;
        public string DefaultExcelReportFileDirectory
        {
            get { return _defaultExcelReportFileDirectory; }
            set { _defaultExcelReportFileDirectory = value; }
        }

        private string _defaultWaveGuideReportFileDirectory;
        public string DefaultWaveGuideReportFileDirectory
        {
            get { return _defaultWaveGuideReportFileDirectory; }
            set { _defaultWaveGuideReportFileDirectory = value; }
        }

        private string _defaultExcelFileNameFormat;
        public string DefaultExcelFileNameFormat
        {
            get { return _defaultExcelFileNameFormat; }
            set { _defaultExcelFileNameFormat = value; }
        }

        private string _defaultWaveGuideFileNameFormat;
        public string DefaultWaveGuideFileNameFormat
        {
            get { return _defaultWaveGuideFileNameFormat; }
            set { _defaultWaveGuideFileNameFormat = value; }
        }


        private double _defaultPixelMaskThresholdPercent;
        public double DefaultPixelMaskThresholdPercent
        {
            get { return _defaultPixelMaskThresholdPercent; }
            set { _defaultPixelMaskThresholdPercent = value; }
        }


        private int _eventMarkerLatency;
        public int EventMarkerLatency
        {
            get { return _eventMarkerLatency; }
            set { _eventMarkerLatency = value; }
        }


        private string _dbServerName;
        public string DBServerName
        {
            get { return _dbServerName; }
            set { _dbServerName = value; }
        }


        private string _dbName;
        public string DBName
        {
            get { return _dbName; }
            set { _dbName = value; }
        }


        private string _dbUsername;
        public string DBUsername
        {
            get { return _dbUsername; }
            set { _dbUsername = value; }
        }

        private string _dbPassword;
        public string DBPassword
        {
            get { return _dbPassword; }
            set { _dbPassword = value; }
        }


        private string _tempControllerIP;
        public string TempControllerIP
        {
            get { return _tempControllerIP; }
            set { _tempControllerIP = value; }
        }

        private string _ethernetIOModuleIP;
        public string EthernetIOModuleIP
        {
            get { return _ethernetIOModuleIP; }
            set { _ethernetIOModuleIP = value; }
        }

        private DOOR_STATUS _doorStatus;
        public DOOR_STATUS DoorStatus
        {
            get { return _doorStatus; }
            set { _doorStatus = value; }
        }


        private bool _cameraCoolerON;
        public bool CameraCoolerON
        {
            get { return _cameraCoolerON; }
            set { _cameraCoolerON = value; }
        }

        private int _cameraTemp;
        public int CameraTemp
        {
            get { return _cameraTemp; }
            set { _cameraTemp = value; }
        }

        private int _cameraTargetTemperature;
        public int CameraTargetTemperature
        {
            get { return _cameraTargetTemperature; }
            set { _cameraTargetTemperature = value; }
        }

        private bool _cameraTempReady;
        public bool CameraTempReady
        {
            get { return _cameraTempReady; }
            set { _cameraTempReady = value; }
        }


        private bool _insideHeaterON;
        public bool InsideHeaterON
        {
            get { return _insideHeaterON; }
            set { _insideHeaterON = value; }
        }

        private int _insideTemp;
        public int InsideTemp
        {
            get { return _insideTemp; }
            set { _insideTemp = value; }
        }

        private int _insideTargetTemperature;
        public int InsideTargetTemperature
        {
            get { return _insideTargetTemperature; }
            set { _insideTargetTemperature = value; }
        }

        private bool _insideTempReady;
        public bool InsideTempReady
        {
            get { return _insideTempReady; }
            set { _insideTempReady = value; }
        }


        private string _lambdaComPortName;
        public string LambdaComPortName
        {
            get { return _lambdaComPortName; }
            set { _lambdaComPortName = value; }
        }


        private bool _enable_EthernetIOModule;
        public bool Enable_EthernetIOModule
        {
            get { return _enable_EthernetIOModule; }
            set { _enable_EthernetIOModule = value; }
        }



        private bool _enable_EnclosureTemperatureController;
        public bool Enable_EnclosureTemperatureController
        {
            get { return _enable_EnclosureTemperatureController; }
            set { _enable_EnclosureTemperatureController = value; }
        }



        private int _tcpCommand_Port;
        public int TCPCommand_Port
        {
            get { return _tcpCommand_Port; }
            set { _tcpCommand_Port = value; }
        }

        public void LoadConfiguration(string settingsFile)
        {
            if (!File.Exists(settingsFile))
            {
                MessageBox.Show("Settings file not found: " + settingsFile, "Settins File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Status = WGStatus.UNKNOWN;


            try
            {

                XmlSettings.Settings settings = new XmlSettings.Settings(settingsFile);

                DefaultTraceColorList = new List<Color>();

                LambdaComPortName = settings.GetValue("MAIN", "LambdaComPortName");
                DBServerName = settings.GetValue("MAIN", "DBServerName");
                DBName = settings.GetValue("MAIN", "DBName");
                DBUsername = settings.GetValue("MAIN", "DBUsername");
                DBPassword = settings.GetValue("MAIN", "DBPassword");
                MaxPixelValue = Convert.ToInt32(settings.GetValue("MAIN", "MaxPixelValue"));
                PixelWidth = Convert.ToInt32(settings.GetValue("MAIN", "CameraSensorPixelWidth"));
                PixelHeight = Convert.ToInt32(settings.GetValue("MAIN", "CameraSensorPixelHeight"));
                MaxNumberImagesPerExperiment = Convert.ToInt32(settings.GetValue("MAIN", "MaxNumberImagesPerExperiment"));


                switch (settings.GetValue("MAIN", "CompressionAlgorithm"))
                {
                    case "GZIP":
                        CompressionAlgorithm = COMPRESSION_ALGORITHM.GZIP;
                        break;
                    default:
                        CompressionAlgorithm = COMPRESSION_ALGORITHM.NONE;
                        break;
                }


                CameraTargetTemperature = Convert.ToInt32(settings.GetValue("MAIN", "CameraTargetTemperature"));
                CameraDefaultCycleTime = Convert.ToInt32(settings.GetValue("MAIN", "CameraDefaultCycleTime"));
                InsideTargetTemperature = Convert.ToInt32(settings.GetValue("MAIN", "InsideTargetTemperature"));
                EventMarkerLatency = Convert.ToInt32(settings.GetValue("MAIN", "EventMarkerLatency"));
                FilterChangeSpeed = Convert.ToByte(settings.GetValue("MAIN", "FilterChangeSpeed"));
                ImageFileSaveLocation = settings.GetValue("MAIN", "ImageFileSaveLocation");
                Color color = (Color)ColorConverter.ConvertFromString(settings.GetValue("MAIN", "Color1"));
                if (color != null) DefaultTraceColorList.Add(color);
                color = (Color)ColorConverter.ConvertFromString(settings.GetValue("MAIN", "Color2"));
                if (color != null) DefaultTraceColorList.Add(color);
                color = (Color)ColorConverter.ConvertFromString(settings.GetValue("MAIN", "Color3"));
                if (color != null) DefaultTraceColorList.Add(color);
                color = (Color)ColorConverter.ConvertFromString(settings.GetValue("MAIN", "Color4"));
                if (color != null) DefaultTraceColorList.Add(color);
                color = (Color)ColorConverter.ConvertFromString(settings.GetValue("MAIN", "Color5"));
                if (color != null) DefaultTraceColorList.Add(color);
                color = (Color)ColorConverter.ConvertFromString(settings.GetValue("MAIN", "Color6"));
                if (color != null) DefaultTraceColorList.Add(color);
                color = (Color)ColorConverter.ConvertFromString(settings.GetValue("MAIN", "Color7"));
                if (color != null) DefaultTraceColorList.Add(color);
                color = (Color)ColorConverter.ConvertFromString(settings.GetValue("MAIN", "Color8"));
                if (color != null) DefaultTraceColorList.Add(color);
                VWorksUsername = settings.GetValue("MAIN", "VWorksUsername");
                VWorksPassword = settings.GetValue("MAIN", "VWorksPassword");
                VWorksProtocolFileDirectory = settings.GetValue("MAIN", "VWorksProtocolFileDirectory");
                UpSignalOptimizePercentCountThreshold = Convert.ToInt32(settings.GetValue("MAIN", "UpSignalOptimizePercentCountThreshold"));
                DownSignalOptimizePercentCountThreshold = Convert.ToInt32(settings.GetValue("MAIN", "DownSignalOptimizePercentCountThreshold"));
                UpDownSignalOptimizePercentCountThreshold = Convert.ToInt32(settings.GetValue("MAIN", "UpDownSignalOptimizePercentCountThreshold"));
                MaxCameraTemperatureThresholdDeviation = Convert.ToInt32(settings.GetValue("MAIN", "MaxCameraTemperatureThresholdDeviation"));
                MaxInsideTemperatureThresholdDeviation = Convert.ToInt32(settings.GetValue("MAIN", "MaxInsideTemperatureThresholdDeviation"));
                EnclosureCameraIPAddress = settings.GetValue("MAIN", "EnclosureCameraIPAddress");
                DefaultExcelReportFileDirectory = settings.GetValue("MAIN", "DefaultExcelReportFileDirectory");
                DefaultWaveGuideReportFileDirectory = settings.GetValue("MAIN", "DefaultWaveGuideReportFileDirectory");
                DefaultExcelFileNameFormat = settings.GetValue("MAIN", "DefaultExcelFileNameFormat");
                DefaultWaveGuideFileNameFormat = settings.GetValue("MAIN", "DefaultWaveGuideFileNameFormat");
                DefaultPixelMaskThresholdPercent = Convert.ToDouble(settings.GetValue("MAIN", "DefaultPixelMaskThresholdPercent"));
                TempControllerIP = settings.GetValue("MAIN", "TemperatureController_IP");
                EthernetIOModuleIP = settings.GetValue("MAIN", "EthernetIOModule_IP");
                Enable_EthernetIOModule = settings.GetValue("MAIN", "Enable_EthernetIOModule").ToUpper() == "TRUE" ? true : false;
                Enable_EnclosureTemperatureController = settings.GetValue("MAIN", "Enable_EnclosureTemperatureController").ToUpper() == "TRUE" ? true : false;
                TCPCommand_Port = Convert.ToInt32(settings.GetValue("MAIN", "TCPCommand_Port"));


                DatabaseConnectionString = "Data Source=" + DBServerName + ";Initial Catalog=" + DBName +
                                               ";User ID=" + DBUsername + ";Password=" + DBPassword;

            }
            catch (ConfigurationErrorsException)
            {
                MessageBox.Show("Error reading Settings file: " + settingsFile, "Settins File Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





    }




    public class StatusChangeEventArgs : EventArgs
    {
        // used to signal start/stop of auto-optimize

        private WGStatus _status;
        public WGStatus status
        {
            get { return _status; }
            set { _status = value; }
        }

        public StatusChangeEventArgs(WGStatus Status)
        {
            _status = Status;
        }
    }

    
 

}
