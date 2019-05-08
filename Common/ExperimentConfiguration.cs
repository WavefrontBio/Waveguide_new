using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using XmlSettings;

namespace Waveguide
{


    public class ExperimentConfiguration : WPFTools.ObservableObject
    {
        public ExperimentConfiguration()
        {
            project = new Waveguide.ProjectContainer();
            method = new Waveguide.MethodContainer();
            plateType = new Waveguide.PlateTypeContainer();
            mask = new Waveguide.MaskContainer();
            numFoFrames = 5;
            controlSubtWells = new ObservableCollection<Tuple<int, int>>();
            dynamicRatioNum = new Waveguide.ExperimentIndicatorContainer();
            dynamicRatioDen = new Waveguide.ExperimentIndicatorContainer();
            waveguideReportLocation = "c:\\";
            excelReportLocation = "c:\\";
            writeWaveguideReport = true;
            writeExcelReport = false;
        }


        private Waveguide.ProjectContainer _project;
        public Waveguide.ProjectContainer project
        {
            get { return _project; }
            set { if (value != _project) { _project = value; OnPropertyChanged("project"); } }
        }

        private Waveguide.MethodContainer _method;
        public Waveguide.MethodContainer method
        {
            get { return _method; }
            set { if (value != _method) { _method = value; OnPropertyChanged("method"); } }
        }

        private Waveguide.PlateTypeContainer _plateType;
        public Waveguide.PlateTypeContainer plateType
        {
            get { return _plateType; }
            set { if (value != _plateType) { _plateType = value; OnPropertyChanged("plateType"); } }
        }

        private Waveguide.MaskContainer _mask;
        public Waveguide.MaskContainer mask
        {
            get { return _mask; }
            set { if (value != _mask) { _mask = value; OnPropertyChanged("mask"); } }
        }

        private int _numFoFrames;
        public int numFoFrames
        {
            get { return _numFoFrames; }
            set { if (value != _numFoFrames) { _numFoFrames = value; OnPropertyChanged("numFoFrames"); } }
        }


        private ObservableCollection<Tuple<int, int>> _controlSubtWells;
        public ObservableCollection<Tuple<int, int>> controlSubtWells { get { return _controlSubtWells; } set { _controlSubtWells = value; OnPropertyChanged("controlSubtWells"); } }

        private Waveguide.ExperimentIndicatorContainer _dynamicRatioNum;
        public Waveguide.ExperimentIndicatorContainer dynamicRatioNum { get { return _dynamicRatioNum; } set { _dynamicRatioNum = value; OnPropertyChanged("dynamicRatioNum"); } }

        private Waveguide.ExperimentIndicatorContainer _dynamicRatioDen;
        public Waveguide.ExperimentIndicatorContainer dynamicRatioDen { get { return _dynamicRatioDen; } set { _dynamicRatioDen = value; OnPropertyChanged("dynamicRatioDen"); } }

        private bool _writeWaveguideReport;
        public bool writeWaveguideReport { get { return _writeWaveguideReport; } set { _writeWaveguideReport = value; OnPropertyChanged("writeWaveguideReport"); } }

        private bool _writeExcelReport;
        public bool writeExcelReport { get { return _writeExcelReport; } set { _writeExcelReport = value; OnPropertyChanged("writeExcelReport"); } }

        private string _waveguideReportLocation;
        public string waveguideReportLocation { get { return _waveguideReportLocation; } set { _waveguideReportLocation = value; OnPropertyChanged("waveguideReportLocation"); } }

        private string _excelReportLocation;
        public string excelReportLocation { get { return _excelReportLocation; } set { _excelReportLocation = value; OnPropertyChanged("excelReportLocation"); } }

        private string _waveguideReportFilename;
        public string waveguideReportFilename { get { return _waveguideReportFilename; } set { _waveguideReportFilename = value; OnPropertyChanged("waveguideReportFilename"); } }

        private string _excelReportFilename;
        public string excelReportFilename { get { return _excelReportFilename; } set { _excelReportFilename = value; OnPropertyChanged("excelReportFilename"); } }




        /////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////


        public static bool ReadSettingsFile(string filename, out ExperimentConfiguration config)
        {
            bool success = true;

            config = new ExperimentConfiguration();

            if (File.Exists(filename))
            {
                try
                {
                    var xmlSettings = new Settings(filename);

                    IList<KeyValuePair<string, string>> settings = new List<KeyValuePair<string, string>>();

                    settings = xmlSettings.GetValues("main");

                    foreach (KeyValuePair<string, string> kvp in settings)
                    {
                        switch (kvp.Key)
                        {
                            case "ProjectID":
                                config.project.ProjectID = Convert.ToInt32(kvp.Value);
                                break;
                            case "MethodID":
                                config.method.MethodID = Convert.ToInt32(kvp.Value);
                                break;
                            case "PlateTypeID":
                                config.plateType.PlateTypeID = Convert.ToInt32(kvp.Value);
                                break;
                            case "MaskID":
                                config.mask.MaskID = Convert.ToInt32(kvp.Value);
                                break;
                            case "NumFoFrames":
                                config.numFoFrames = Convert.ToInt32(kvp.Value);
                                break;
                            case "ControlWells":
                                config.controlSubtWells = ParseWellListString(kvp.Value);
                                break;
                            case "DynamicRatioNumerator":
                                config.dynamicRatioNum.Description = kvp.Value;
                                break;
                            case "DynamicRatioDenominator":
                                config.dynamicRatioDen.Description = kvp.Value;
                                break;
                            case "WaveguideReportLocation":
                                config.waveguideReportLocation = kvp.Value;
                                break;
                            case "ExcelReportLocation":
                                config.excelReportLocation = kvp.Value;
                                break;
                            case "CreateWaveguideReport":
                                config.writeWaveguideReport = Convert.ToBoolean(kvp.Value);
                                break;
                            case "CreateExcelReport":
                                config.writeExcelReport = Convert.ToBoolean(kvp.Value);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    MessageBox.Show("Error reading settings file: " + filename + "\n\n" + ex.Message, "File Error",
                       MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                success = false;
                MessageBox.Show("File does not exists: " + filename, "File Error",
                       MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return success;
        }

        public static bool ReadSettingsFile(string filename, out string settingsString)
        {
            bool success = true;

            settingsString = "";

            if (File.Exists(filename))
            {
                try
                {
                    settingsString = System.IO.File.ReadAllText(filename);
                }
                catch (Exception ex)
                {
                    success = false;
                    MessageBox.Show("Error reading settings file: " + filename + "\n\n" + ex.Message, "File Error",
                       MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                success = false;
                MessageBox.Show("File does not exists: " + filename, "File Error",
                       MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return success;
        }

        public static bool ReadSettingsFile(string filename, out byte[] array)
        {
            bool success = true;

            array = null;

            if (File.Exists(filename))
            {
                try
                {
                    array = File.ReadAllBytes(filename);
                    int ndx = 0;
                    while (array[0] != 60 && ndx < 3)
                    {
                        ndx++;
                    }
                    int newArrayLength = array.Length - ndx;
                    byte[] xmlArray = new byte[newArrayLength];
                    Buffer.BlockCopy(array, ndx, xmlArray, 0, newArrayLength);
                    array = xmlArray;
                }
                catch (Exception ex)
                {
                    success = false;
                    MessageBox.Show("Error reading settings file: " + filename + "\n\n" + ex.Message, "File Error",
                       MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                success = false;
                MessageBox.Show("File does not exists: " + filename, "File Error",
                       MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return success;
        }

        public static bool WriteSettingsFile(string filename, ExperimentConfiguration config)
        {
            bool success = true;

            try
            {
                var settings = new List<KeyValuePair<string, string>>();

                var xmlSettings = new Settings(filename);

                if(config.project != null)
                    settings.Add(new KeyValuePair<string, string>("ProjectID", config.project.ProjectID.ToString()));

                if (config.method != null)
                    settings.Add(new KeyValuePair<string, string>("MethodID", config.method.MethodID.ToString()));

                if (config.plateType != null)
                    settings.Add(new KeyValuePair<string, string>("PlateTypeID", config.plateType.PlateTypeID.ToString()));

                if (config.mask != null)
                    settings.Add(new KeyValuePair<string, string>("MaskID", config.mask.MaskID.ToString()));
                                
                settings.Add(new KeyValuePair<string, string>("NumFoFrames", config.numFoFrames.ToString()));

                if (config.controlSubtWells != null)
                    settings.Add(new KeyValuePair<string, string>("ControlWells", ConvertWellListToString(config.controlSubtWells)));

                if (config.dynamicRatioNum != null)
                    settings.Add(new KeyValuePair<string, string>("DynamicRatioNumerator", config.dynamicRatioNum.Description));

                if (config.dynamicRatioDen != null)
                    settings.Add(new KeyValuePair<string, string>("DynamicRatioDenominator", config.dynamicRatioDen.Description));

                
                settings.Add(new KeyValuePair<string, string>("WaveguideReportLocation", config.waveguideReportLocation));
                settings.Add(new KeyValuePair<string, string>("ExcelReportLocation", config.excelReportLocation));
                settings.Add(new KeyValuePair<string, string>("CreateWaveguideReport", config.writeWaveguideReport.ToString()));
                settings.Add(new KeyValuePair<string, string>("CreateExcelReport", config.writeExcelReport.ToString()));

                xmlSettings.SetValues("main", settings);
            }
            catch (Exception ex)
            {
                success = false;
                System.Windows.MessageBox.Show("Error writing settings file: " + filename + "\n\n" + ex.Message, "File Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return success;
        }

        public static bool WriteSettingsFile(string filename, byte[] array)
        {
            bool success = true;

            try
            {
                File.WriteAllBytes(filename, array);
            }
            catch (Exception ex)
            {
                success = false;
                MessageBox.Show("Error writing settings file: " + filename + "\n\n" + ex.Message, "File Error",
                   MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return success;
        }

        public static ObservableCollection<Tuple<int, int>> ParseWellListString(string wellListString)
        {
            ObservableCollection<Tuple<int, int>> wellList = new ObservableCollection<Tuple<int, int>>();

            string[] wells = wellListString.Split(',');

            foreach (string s in wells)
            {
                int row = -1;
                int col = -1;
                int ndx = 0;

                foreach (char c in s)
                {
                    int val = (int)c;

                    if (val > 64) // it's a letter
                    {
                        row = ((row + 1) * 26) + (val - 65);
                    }
                    else // it's a number
                    {
                        string colStr = s.Substring(ndx);
                        col = Convert.ToInt32(colStr);
                        break;
                    }

                    ndx++;
                }

                wellList.Add(Tuple.Create<int, int>(row, col));
            }

            return wellList;
        }

        public static string ConvertWellListToString(ObservableCollection<Tuple<int, int>> wellList)
        {
            string wellListString = "";

            bool first = true;
            foreach (Tuple<int, int> item in wellList)
            {
                if (!first) wellListString += ",";

                string row = "" + (char)(item.Item1 + 65);
                if (item.Item1 < 90)
                    row = "" + (char)(item.Item1 + 65);
                else
                    row = "A" + (char)(item.Item1 - 91);

                string col = item.Item2.ToString();

                wellListString += row + col;

                first = false;
            }

            return wellListString;
        }

        public static bool BuildConfigurationMessagePacket(string filename, out byte[] array)
        {
            //  Message Structure
            //
            //  byte 0 - 1, Message Type ID
            //  byte 2 - 3, Message Payload length (bytes)
            //  byte 4 - N, Message Payload (where N = Payload length + 3)

            bool success = true;

            // Get the selected file name and display in a TextBox 
            string fileContents = "";
            array = null;

            if (File.Exists(filename))
            {
                try
                {
                    fileContents = System.IO.File.ReadAllText(filename);

                    short messageType = 2;
                    byte[] messageTypeBytes = BitConverter.GetBytes(messageType);

                    short payloadSize = (short)fileContents.Length;
                    byte[] payloadSizeBytes = BitConverter.GetBytes(payloadSize);

                    byte[] payloadBytes = Encoding.ASCII.GetBytes(fileContents);

                    array = new byte[4 + payloadSize];

                    Buffer.BlockCopy(messageTypeBytes, 0, array, 0, messageTypeBytes.Length);
                    Buffer.BlockCopy(payloadSizeBytes, 0, array, messageTypeBytes.Length, payloadSizeBytes.Length);
                    Buffer.BlockCopy(payloadBytes, 0, array, messageTypeBytes.Length + payloadSizeBytes.Length, payloadBytes.Length);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to read selected Experiment Configuration File:\n " + ex.Message,
                        "Error Reading File", MessageBoxButton.OK, MessageBoxImage.Error);
                    array = null;
                    success = false;
                }

            }
            else
            {
                success = false;
            }

            return success;
        }

        public static bool GetMessageType(byte[] array, ref int messageType)
        {
            //  Message Structure
            //
            //  byte 0 - 1, Message Type ID
            //  byte 2 - 3, Message Payload length (bytes)
            //  byte 4 - N, Message Payload (where N = Payload length + 3)

            bool success = true;

            if (array.Length > 1)
            {
                try
                {
                    messageType = BitConverter.ToInt16(array, 0);
                }
                catch (Exception)
                {
                    success = false;
                }
            }
            else
            {
                success = false;
            }

            return success;
        }

        public static bool GetPayloadArray(byte[] fullMessageBytes, out byte[] payloadBytes)
        {
            bool success = true;

            short payloadLength = BitConverter.ToInt16(fullMessageBytes, 2);
            payloadBytes = new byte[payloadLength];

            Buffer.BlockCopy(fullMessageBytes, 4, payloadBytes, 0, payloadLength);

            return success;
        }

        public static bool ParseConfigurationPayload(byte[] payloadBytes, out ExperimentConfiguration config)
        {
            bool success = true;
            config = new ExperimentConfiguration();

            try
            {
                string payloadString = System.Text.Encoding.ASCII.GetString(payloadBytes);

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(payloadString);

                foreach (XmlNode node1 in xmlDoc.DocumentElement.ChildNodes)
                {
                    if (node1.Name == "main")
                    {
                        foreach (XmlNode node2 in node1)
                        {
                            if (node2.Name == "add")
                            {
                                string attr = node2.Attributes["key"].Value;
                                string value = node2.Attributes["value"].Value;

                                switch (attr)
                                {
                                    case "ProjectID":
                                        config.project.ProjectID = Convert.ToInt32(value);
                                        break;
                                    case "MethodID":
                                        config.method.MethodID = Convert.ToInt32(value);
                                        break;
                                    case "PlateTypeID":
                                        config.plateType.PlateTypeID = Convert.ToInt32(value);
                                        break;
                                    case "MaskID":
                                        config.mask.MaskID = Convert.ToInt32(value);
                                        break;
                                    case "NumFoFrames":
                                        config.numFoFrames = Convert.ToInt32(value);
                                        break;
                                    case "ControlWells":
                                        config.controlSubtWells = ParseWellListString(value);
                                        break;
                                    case "DynamicRatioNumerator":
                                        config.dynamicRatioNum.Description = value;
                                        break;
                                    case "DynamicRatioDenominator":
                                        config.dynamicRatioDen.Description = value;
                                        break;
                                    case "WaveguideReportLocation":
                                        config.waveguideReportLocation = value;
                                        break;
                                    case "ExcelReportLocation":
                                        config.excelReportLocation = value;
                                        break;
                                    case "CreateWaveguideReport":
                                        config.writeWaveguideReport = Convert.ToBoolean(value);
                                        break;
                                    case "CreateExcelReport":
                                        config.writeExcelReport = Convert.ToBoolean(value);
                                        break;
                                }

                            }
                        }
                    }
                }

                //WriteSettingsFile("tempSettings.xml", payloadBytes);

                //ReadSettingsFile("tempSettings.xml", out config);

                //File.Delete("tempSettings.xml");

            }
            catch (Exception ex)
            {
                success = false;
                Debug.Print(ex.Message);
            }



            return success;
        }

        public static bool ConvertToXmlString(ExperimentConfiguration config, out string xmlString)
        {
            bool success = true;
            xmlString = "";

            try
            {
                XmlDocument xmlDoc = new XmlDocument();

                XmlDeclaration xmlDec = xmlDoc.CreateXmlDeclaration("1.0", null, null);
                xmlDec.Encoding = "utf-8";

                XmlNode rootNode = xmlDoc.CreateElement("settings");
                xmlDoc.AppendChild(rootNode);

                xmlDoc.InsertBefore(xmlDec, rootNode);

                XmlNode mainNode = xmlDoc.CreateElement("main");
                rootNode.AppendChild(mainNode);

                XmlNode settingNode;
                XmlAttribute keyAttribute;
                XmlAttribute valAttribute;

                // ProjectID
                settingNode = xmlDoc.CreateElement("add");
                keyAttribute = xmlDoc.CreateAttribute("key");
                keyAttribute.Value = "ProjectID";
                valAttribute = xmlDoc.CreateAttribute("value");
                valAttribute.Value = config.project.ProjectID.ToString();
                settingNode.Attributes.Append(keyAttribute);
                settingNode.Attributes.Append(valAttribute);
                mainNode.AppendChild(settingNode);

                // MethodID
                settingNode = xmlDoc.CreateElement("add");
                keyAttribute = xmlDoc.CreateAttribute("key");
                keyAttribute.Value = "MethodID";
                valAttribute = xmlDoc.CreateAttribute("value");
                valAttribute.Value = config.method.MethodID.ToString();
                settingNode.Attributes.Append(keyAttribute);
                settingNode.Attributes.Append(valAttribute);
                mainNode.AppendChild(settingNode);

                // PlateTypeID
                settingNode = xmlDoc.CreateElement("add");
                keyAttribute = xmlDoc.CreateAttribute("key");
                keyAttribute.Value = "PlateTypeID";
                valAttribute = xmlDoc.CreateAttribute("value");
                valAttribute.Value = config.plateType.PlateTypeID.ToString();
                settingNode.Attributes.Append(keyAttribute);
                settingNode.Attributes.Append(valAttribute);
                mainNode.AppendChild(settingNode);

                // MaskID
                settingNode = xmlDoc.CreateElement("add");
                keyAttribute = xmlDoc.CreateAttribute("key");
                keyAttribute.Value = "MaskID";
                valAttribute = xmlDoc.CreateAttribute("value");
                valAttribute.Value = config.mask.MaskID.ToString();
                settingNode.Attributes.Append(keyAttribute);
                settingNode.Attributes.Append(valAttribute);
                mainNode.AppendChild(settingNode);

                // NumFoFrames
                settingNode = xmlDoc.CreateElement("add");
                keyAttribute = xmlDoc.CreateAttribute("key");
                keyAttribute.Value = "NumFoFrames";
                valAttribute = xmlDoc.CreateAttribute("value");
                valAttribute.Value = config.numFoFrames.ToString();
                settingNode.Attributes.Append(keyAttribute);
                settingNode.Attributes.Append(valAttribute);
                mainNode.AppendChild(settingNode);

                // ControlWells
                settingNode = xmlDoc.CreateElement("add");
                keyAttribute = xmlDoc.CreateAttribute("key");
                keyAttribute.Value = "ControlWells";
                valAttribute = xmlDoc.CreateAttribute("value");
                valAttribute.Value = ConvertWellListToString(config.controlSubtWells);
                settingNode.Attributes.Append(keyAttribute);
                settingNode.Attributes.Append(valAttribute);
                mainNode.AppendChild(settingNode);

                // DynamicRatioNumerator Description
                settingNode = xmlDoc.CreateElement("add");
                keyAttribute = xmlDoc.CreateAttribute("key");
                keyAttribute.Value = "DynamicRatioNumerator";
                valAttribute = xmlDoc.CreateAttribute("value");
                valAttribute.Value = config.dynamicRatioNum.Description;
                settingNode.Attributes.Append(keyAttribute);
                settingNode.Attributes.Append(valAttribute);
                mainNode.AppendChild(settingNode);

                // DynamicRatioDenominator Description
                settingNode = xmlDoc.CreateElement("add");
                keyAttribute = xmlDoc.CreateAttribute("key");
                keyAttribute.Value = "DynamicRatioDenominator";
                valAttribute = xmlDoc.CreateAttribute("value");
                valAttribute.Value = config.dynamicRatioDen.Description;
                settingNode.Attributes.Append(keyAttribute);
                settingNode.Attributes.Append(valAttribute);
                mainNode.AppendChild(settingNode);

                // WaveguideReportLocation
                settingNode = xmlDoc.CreateElement("add");
                keyAttribute = xmlDoc.CreateAttribute("key");
                keyAttribute.Value = "WaveguideReportLocation";
                valAttribute = xmlDoc.CreateAttribute("value");
                valAttribute.Value = config.waveguideReportLocation;
                settingNode.Attributes.Append(keyAttribute);
                settingNode.Attributes.Append(valAttribute);
                mainNode.AppendChild(settingNode);

                // ExcelReportLocation
                settingNode = xmlDoc.CreateElement("add");
                keyAttribute = xmlDoc.CreateAttribute("key");
                keyAttribute.Value = "ExcelReportLocation";
                valAttribute = xmlDoc.CreateAttribute("value");
                valAttribute.Value = config.excelReportLocation;
                settingNode.Attributes.Append(keyAttribute);
                settingNode.Attributes.Append(valAttribute);
                mainNode.AppendChild(settingNode);

                // CreateWaveguideReport
                settingNode = xmlDoc.CreateElement("add");
                keyAttribute = xmlDoc.CreateAttribute("key");
                keyAttribute.Value = "CreateWaveguideReport";
                valAttribute = xmlDoc.CreateAttribute("value");
                valAttribute.Value = config.writeWaveguideReport.ToString();
                settingNode.Attributes.Append(keyAttribute);
                settingNode.Attributes.Append(valAttribute);
                mainNode.AppendChild(settingNode);

                // CreateExcelReport
                settingNode = xmlDoc.CreateElement("add");
                keyAttribute = xmlDoc.CreateAttribute("key");
                keyAttribute.Value = "CreateExcelReport";
                valAttribute = xmlDoc.CreateAttribute("value");
                valAttribute.Value = config.writeExcelReport.ToString();
                settingNode.Attributes.Append(keyAttribute);
                settingNode.Attributes.Append(valAttribute);
                mainNode.AppendChild(settingNode);

                xmlString = xmlDoc.OuterXml;
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        public static bool VerifyConfig(ExperimentConfiguration config)
        {
            bool success = true;

            if (Verify_Project(config.project.ProjectID))
            {
                if (Verify_Method(config.method.MethodID))
                {
                    if (Verify_PlateType(config.plateType.PlateTypeID))
                    {
                        if (Verify_Mask(config.mask.MaskID))
                        {
                            if (Verify_RuntimeAnalysis(config.numFoFrames, config.controlSubtWells,
                                config.dynamicRatioNum.ExperimentIndicatorID, config.dynamicRatioDen.ExperimentIndicatorID))
                            {
                                if (Verify_ReportSetup(config.writeWaveguideReport, config.waveguideReportLocation,
                                       config.writeExcelReport, config.excelReportLocation))
                                {

                                }
                                else
                                    success = false;
                            }
                            else
                                success = false;
                        }
                        else
                            success = false;
                    }
                    else
                        success = false;
                }
                else
                    success = false;
            }
            else
                success = false;

            return success;
        }

        public static bool Verify_Project(int id)
        {
            bool success = true;

            return success;
        }

        public static bool Verify_Method(int id)
        {
            bool success = true;

            return success;
        }

        public static bool Verify_PlateType(int id)
        {
            bool success = true;

            return success;
        }

        public static bool Verify_Mask(int id)
        {
            bool success = true;

            return success;
        }

        public static bool Verify_RuntimeAnalysis(int numF0Frames, ObservableCollection<Tuple<int, int>> wellList, int dynRatioNumID, int dynRatioDenID)
        {
            bool success = true;

            return success;
        }

        public static bool Verify_ReportSetup(bool writeWaveguideReport, string waveguideReportLocation, bool writeExcelReport, string excelReportPath)
        {
            bool success = true;

            return success;
        }

    }


}
