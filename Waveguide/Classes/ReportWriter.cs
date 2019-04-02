using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace Waveguide
{

    public enum REPORT_FILEFORMAT
        {            
            WAVEGUIDE,
            EXCEL
        };

    class ReportWriter
    {        

        WaveguideDB m_wgDB;        
        ProjectContainer m_project;
        ExperimentContainer m_experiment;        
        MethodContainer m_method;
        PlateContainer m_plate;
        PlateTypeContainer m_plateType;
        UserContainer m_user;
        
        string m_excelReportDirectory;
        string m_waveguideReportDirectory;
        REPORT_FILEFORMAT m_format;

        bool m_initializationSuccess;
        string m_lastErrorString;
        

        public ReportWriter(ProjectContainer project, ExperimentContainer experiment)
        {
            m_initializationSuccess = false;
            m_lastErrorString = "";
            
            m_waveguideReportDirectory = GlobalVars.Instance.DefaultWaveGuideReportFileDirectory;
            m_format = REPORT_FILEFORMAT.EXCEL;

            m_wgDB = new WaveguideDB();
            m_project = project;
            m_experiment = experiment;
            

            bool success = m_wgDB.GetMethod(m_experiment.MethodID, out m_method);
            if (m_method == null) success = false;
            if(success)
            {                
                success = m_wgDB.GetPlate(m_experiment.PlateID, out m_plate);
                if (m_plate == null) success = false;
                if(success)
                {
                    success = m_wgDB.GetUser(m_plate.OwnerID, out m_user);
                    if (m_user == null) success = false;
                    if(success)
                    {
                        success = m_wgDB.GetPlateType(m_plate.PlateTypeID, out m_plateType);
                        if (m_plateType == null) success = false;                        
                    }
                }
            }

            m_initializationSuccess = success;
        }


        public bool SuccessfullyInitialized()
        {
            return m_initializationSuccess;
        }

        public string GetLastErrorString()
        {
            return m_lastErrorString;
        }

        //public void SetReportDirectory(string path)
        //{
        //    StringBuilder sb = new StringBuilder();

        //    // make sure it's not empty
        //    if(sb.Length < 3)
        //    {
        //        m_reportDirectory = "C:\\";
        //        return;
        //    }

        //    // make sure it has a "\" on the end of it
        //    Char ch = sb[sb.Length-1];
        //    if (ch != '\\') sb.Append("\\");
        //}


        //public void SetFileType(REPORT_FILEFORMAT format)
        //{
        //    m_format = format;
        //}


        public bool VerifyDirectoryExists(string path)
        {
            bool success = true;
            try
            {
                // Determine whether the directory exists. 
                if (Directory.Exists(path))
                {
                    success = true;
                }
                else
                {
                    // Try to create the directory
                    DirectoryInfo di = Directory.CreateDirectory(path);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to Create Directory: " + path + " with error: " + e.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                success = false;
            }
            finally { }

            return success;
        }




        public List<string> GetFormattedStringList(string formatString, ObservableCollection<AnalysisContainer> analysisList)
        {
            List<string> strList = new List<string>();

            int indNum = 1;

            foreach (AnalysisContainer analysis in analysisList)
            {
                string tempStr = formatString;

                if (!formatString.Contains("[INDICATOR_NUM]") && !formatString.Contains("[INDICATOR_NAME]"))
                {
                    // doesn't have [INDICATOR_NUM] or [INDICATOR_NAME] in format string, so add an indicator number
                    string[] str = formatString.Split('.');
                    if (str.Length > 1)
                        tempStr = str[0] + "_" + indNum.ToString() + str[1];
                    else tempStr = formatString + "_" + indNum.ToString();
                }

                if (formatString.Contains("[INDICATOR_NUM]"))
                {
                    tempStr = tempStr.Replace("[INDICATOR_NUM]", indNum.ToString());
                }

                if (formatString.Contains("[INDICATOR_NAME]"))
                {
                    tempStr = tempStr.Replace("[INDICATOR_NAME]", analysis.Description);
                }

                string newString = GetFormattedString(tempStr);

                strList.Add(newString);

                indNum++;
            }

            return strList;
        }


        public string GetFormattedString(string formatString)
        {
            // find all substrings inclosed in [BRACKETS] indicating variables to be replaced
            string[] s2 = formatString.Split(new char[] { '[', ']' }, StringSplitOptions.None);
            List<string> variableList = new List<string>();
            List<string> valueList = new List<string>();


            // for each of the bracketed variables found, find the intended values
            foreach (string s in s2)
            {
                switch (s)
                {
                    case "PROJECT":
                        valueList.Add(m_project.Description);
                        variableList.Add("PROJECT");
                        break;
                    case "EXPERIMENT":
                        valueList.Add(m_experiment.Description);
                        variableList.Add("EXPERIMENT");
                        break;
                    case "METHOD":
                        valueList.Add(m_method.Description);
                        variableList.Add("METHOD");
                        break;
                    case "BARCODE":
                        valueList.Add(m_plate.Barcode);
                        variableList.Add("BARCODE");
                        break;
                    case "USER":
                        valueList.Add(m_user.Lastname + "_" + m_user.Firstname);
                        variableList.Add("USER");
                        break;
                    case "DATE":
                        valueList.Add(m_experiment.TimeStamp.ToString("MMM_dd_yy"));
                        variableList.Add("DATE");
                        break;
                    case "TIME":
                        valueList.Add(m_experiment.TimeStamp.ToString("HHmm"));  // add "tt" to format to get AM or PM added
                        variableList.Add("TIME");
                        break;
                    case "INDICATOR_NUM":
                        valueList.Add("[INDICATOR_NUM]");
                        variableList.Add("INDICATOR_NUM");
                        break;
                    case "INDICATOR_NAME":
                        valueList.Add("[INDICATOR_NAME]");
                        variableList.Add("INDICATOR_NAME");
                        break;

                }
            }

            // remove all the variables from the originial string and replace with an easy delimiter, '|'
            string temp = formatString;
            foreach (string s in variableList)
            {
                temp = temp.Replace(s, ""); // empties the bracket containing s
                temp = temp.Replace("[]", "|"); // replace empty brackets with '|'
            }

            //  build full filename
            string filename = "";
            List<string> strList = new List<string>();
            foreach (char c in temp)
            {
                switch (c)
                {
                    case '|':
                        if (valueList.Count > 0)
                        {
                            filename += valueList.ElementAt(0);
                            valueList.RemoveAt(0);
                        }
                        break;
                    default:
                        filename += c;
                        break;
                }
            }

            return filename;
        }



       


        public bool WriteExperimentFile_WaveGuide(string filename, ObservableCollection<AnalysisContainer> analysisList)
        {
            bool success = true;

            if (File.Exists(filename))
            {
                MessageBoxResult result = MessageBox.Show("File: " + filename + " already exists! Do you want to over write it?", "File Already Exists",
                    MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        File.Delete(filename);
                        break;
                    case MessageBoxResult.No:
                        success = false;
                        m_lastErrorString = "File already exists";
                        break;
                }
            }

            

            if (success)
            {
                try
                {
                    string delimiter = "\t";   // \t = tab

                    FileStream fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write);

                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        //  Start writing HEADER

                        sw.WriteLine("<HEADER>");
                        DateTime dt = m_experiment.TimeStamp;
                        sw.WriteLine("Date" + delimiter + dt.ToString("yyyy-MM-dd")); // delimiter + dt.Year.ToString() + "-" + dt.Month.ToString() + "-" + dt.Day.ToString());
                        sw.WriteLine("Time" + delimiter + dt.ToString("HH:mm:ss")); //dt.Hour.ToString() + ":" + dt.Minute.ToString() + ":" + dt.Second.ToString());
                        sw.WriteLine("Instrument" + delimiter + "Panoptic");
                        sw.WriteLine("ProtocolName" + delimiter + m_method.Description);
                        sw.WriteLine("AssayPlateBarcode" + delimiter + m_plate.Barcode);

                        success = m_wgDB.GetAllExperimentCompoundPlatesForExperiment(m_experiment.ExperimentID);
                        if (success)
                        {
                            foreach (ExperimentCompoundPlateContainer ecPlate in m_wgDB.m_experimentCompoundPlateList)
                            {
                                sw.WriteLine("AddPlateBarcode" + delimiter + ecPlate.Barcode);
                            }
                        }


                        bool alreadyHaveBinning = false;
                        string binningString = "";

                        ObservableCollection<ExperimentIndicatorContainer> expIndicatorList = new ObservableCollection<ExperimentIndicatorContainer>();
                        foreach(AnalysisContainer ac in analysisList)
                        {
                            ExperimentIndicatorContainer expIndicator;
                            success = m_wgDB.GetExperimentIndicator(ac.ExperimentIndicatorID,out expIndicator);
                            if(success && expIndicator!=null)
                            {
                                // get the experiment binning if we don't already have it
                                if(!alreadyHaveBinning)
                                {
                                    ExperimentContainer experiment;
                                    success = m_wgDB.GetExperiment(expIndicator.ExperimentID, out experiment);

                                    if(success)
                                    {
                                        alreadyHaveBinning = true;
                                        binningString = experiment.HorzBinning.ToString() + " x " + experiment.VertBinning.ToString();
                                    }
                                }

                                // make sure this experiment indicator isn't already in the list
                                bool alreadyInList = false;
                                foreach(ExperimentIndicatorContainer expCont in expIndicatorList)
                                {
                                    if(expIndicator.ExperimentIndicatorID == expCont.ExperimentIndicatorID)
                                    {
                                        alreadyInList = true;
                                        break;
                                    }
                                }

                                if(!alreadyInList)
                                {                                   
                                  sw.WriteLine("Indicator" + delimiter +
                                                expIndicator.Description + delimiter +
                                                "Excitation" + delimiter + 
                                                expIndicator.ExcitationFilterDesc + delimiter +
                                                "Emission" + delimiter +
                                                expIndicator.EmissionFilterDesc + delimiter +
                                                "Exposure" + delimiter +
                                                expIndicator.Exposure.ToString() + delimiter +
                                                "Gain" + delimiter +
                                                expIndicator.Gain.ToString());

                                  expIndicatorList.Add(expIndicator);
                                }
                            }

                            
                        }


                        success = m_wgDB.GetAllExperimentIndicatorsForExperiment(m_experiment.ExperimentID, out expIndicatorList);
                        if (success)
                        {
                            foreach (ExperimentIndicatorContainer expIndicator in expIndicatorList)
                            {
                                
                            }
                        }

                       
                        sw.WriteLine("Binning" + delimiter + binningString);
                        sw.WriteLine("NumRows" + delimiter + m_plateType.Rows.ToString());
                        sw.WriteLine("NumCols" + delimiter + m_plateType.Cols.ToString());

                        List<EventMarkerContainer> eventMarkerList;
                        success = m_wgDB.GetAllEventMarkersForExperiment(m_experiment.ExperimentID, out eventMarkerList);
                        if (success)
                        {
                            foreach (EventMarkerContainer eventMarker in eventMarkerList)
                            {
                                string timeString = String.Format("{0:0.000}", (float)eventMarker.SequenceNumber / 1000);
                                sw.WriteLine("Event" + delimiter + 
                                                eventMarker.Name + delimiter +
                                                eventMarker.Description + delimiter +
                                                "Time,sec" + delimiter +
                                                timeString);
                            }
                        }

                        sw.WriteLine("Operator" + delimiter + m_user.Username + delimiter +
                                        m_user.Lastname + delimiter +
                                        m_user.Firstname);

                        sw.WriteLine("Project" + delimiter + m_project.Description);

                        sw.WriteLine("</HEADER>");

                        // END writing HEADER


                        if (success)
                        {
                            foreach (AnalysisContainer analysis in analysisList)
                            {
                                ExperimentIndicatorContainer expIndicator;
                                success = m_wgDB.GetExperimentIndicator(analysis.ExperimentIndicatorID, out expIndicator);

                                sw.WriteLine("<INDICATOR_DATA" + delimiter + expIndicator.Description + delimiter + ">");

                                // START write column headers
                                sw.Write("Time" + delimiter);
                                
                                StringBuilder builder = new StringBuilder();
                                for (int r = 0; r < m_plateType.Rows; r++)
                                    for (int c = 0; c < m_plateType.Cols; c++)
                                    {
                                        builder.Append((char)(65 + r)).Append(c + 1).Append(delimiter);                                        
                                    }
                                builder.Remove(builder.Length - delimiter.Length, delimiter.Length); // remove last delimiter
                                sw.WriteLine(builder.ToString());
                                // END write column headers

                                // START writing data frames
                                success = m_wgDB.GetAllAnalysisFramesForAnalysis(analysis.AnalysisID);
                                if (success)
                                {
                                    foreach (AnalysisFrameContainer aFrame in m_wgDB.m_analysisFrameList)
                                    {
                                        string timeString = String.Format("{0:0.000}", (float)aFrame.SequenceNumber / 1000);
                                        sw.Write(timeString + delimiter);

                                        string[] values = aFrame.ValueString.Split(',');
                                        foreach (string val in values)
                                        {
                                            sw.Write(val + delimiter);
                                        }

                                        sw.WriteLine("");
                                    }
                                }
                                // END writing data frames

                                sw.WriteLine("</INDICATOR_DATA>");
                            }
                        }
                    }

                    if (!success) m_lastErrorString = m_wgDB.GetLastErrorMsg();

                } // end try
                catch (Exception e)
                {
                    success = false;
                    m_lastErrorString = e.Message;
                }
            }

            return success;

        } // end function

       


        public bool WriteExperimentFile_Excel(string filename, AnalysisContainer analysis)
        {
            if (filename == null) { m_lastErrorString = "Filename == null"; return false; }

            bool success = true;

            if(File.Exists(filename))
            {
                MessageBoxResult result = MessageBox.Show("Files for: " +  filename + " already exists! Do you want to over write it?", "File Already Exists",
                    MessageBoxButton.YesNo,MessageBoxImage.Exclamation);

                switch(result)
                {
                    case MessageBoxResult.Yes:
                        File.Delete(filename);         
                        break;
                    case MessageBoxResult.No:
                        success = false;
                        m_lastErrorString = "File already exists";                        
                        break;
                }
            }


            if (success)
            {

                try
                {
                    string delimiter = "\t";   // \t = tab
                    

                            using (FileStream fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write))
                            {

                                using (StreamWriter sw = new StreamWriter(fs))
                                {
                                    // START write column headers
                                    sw.Write("Time" + delimiter);
                                    
                                    StringBuilder builder = new StringBuilder();
                                    for (int r = 0; r < m_plateType.Rows; r++)
                                        for (int c = 0; c < m_plateType.Cols; c++)
                                        {
                                            builder.Append((char)(65 + r)).Append(c + 1).Append(delimiter);
                                        }
                                    builder.Remove(builder.Length - delimiter.Length, delimiter.Length); // remove last delimiter
                                    sw.WriteLine(builder.ToString());
                                    // END write column headers

                                    // START writing data frames
                                    success = m_wgDB.GetAllAnalysisFramesForAnalysis(analysis.AnalysisID);
                                    if (success)
                                    {
                                        foreach (AnalysisFrameContainer aFrame in m_wgDB.m_analysisFrameList)
                                        {
                                            string timeString = aFrame.SequenceNumber.ToString();
                                            sw.Write(timeString + delimiter);

                                            string[] values = aFrame.ValueString.Split(',');
                                            foreach (string val in values)
                                            {
                                                sw.Write(val + delimiter);
                                            }

                                            sw.WriteLine("");
                                        }
                                    }
                                    // END writing data frames

                                } // END using StreamWriter

                            } // END using FileStream                

                } // end try            
                catch (Exception e)
                {
                    success = false;
                    m_lastErrorString = e.Message;
                }
            }

            return success;

        } // end function

    }
    
}
