using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for ReportDialog.xaml
    /// </summary>
    public partial class ReportDialog : Window
    {
        ViewModel_ReportDialog VM;
        ReportWriter m_reportWriter;
        ProjectContainer m_project;
        ExperimentContainer m_experiment;
        ObservableCollection<ExperimentIndicatorContainer> m_expIndicatorList;
        WaveguideDB m_wgDB;
        

        public ReportDialog(ProjectContainer project, ExperimentContainer experiment, 
                            ObservableCollection<ExperimentIndicatorContainer> expIndicatorList)
        {
            m_project = project;
            m_experiment = experiment;
            m_expIndicatorList = expIndicatorList;

            VM = new ViewModel_ReportDialog();
            m_wgDB = new WaveguideDB();

            m_reportWriter = new ReportWriter(m_project, m_experiment);            

            InitializeComponent();

            this.DataContext = VM;

            VM.WaveguideDirectory = m_reportWriter.GetFormattedString(GlobalVars.DefaultWaveGuideReportFileDirectory);

            VM.WaveguideFilename = m_reportWriter.GetFormattedString(GlobalVars.DefaultWaveGuideFileNameFormat);

            VM.ExcelDirectory = m_reportWriter.GetFormattedString(GlobalVars.DefaultExcelReportFileDirectory);

            VM.ExcelFilename = m_reportWriter.GetFormattedString(GlobalVars.DefaultExcelFileNameFormat);

            VM.WaveguideSelected = true;

            VM.ExcelSelected = false;

            VM.ReportFormat = REPORT_FILEFORMAT.WAVEGUIDE;

            SetAnalysisList();


            bool ok = m_reportWriter.SuccessfullyInitialized();
            if (!ok)
            {
                string errMsg = m_reportWriter.GetLastErrorString();
                System.Windows.MessageBox.Show("Error initializing the Report Writer: " +
                    errMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        public void SetAnalysisList()
        {
            VM.AnalysisList.Clear();

            foreach (ExperimentIndicatorContainer expIndicator in m_expIndicatorList)
            {
                bool success = m_wgDB.GetAllAnalysesForExperimentIndicator(expIndicator.ExperimentIndicatorID);

                foreach (AnalysisContainer analCont in m_wgDB.m_analysisList)
                {
                    AnalysisContainer ac;
                    success = m_wgDB.GetAnalysis(analCont.AnalysisID, out ac);
                    if (success && ac != null)
                        if (ac.RuntimeAnalysis)
                        {
                            ac.Description = expIndicator.Description;
                            VM.AnalysisList.Add(ac);
                        }
                } 
            }     
        }



        

        private void CancelPB_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }



        private void WriteReportFilePB_Click(object sender, RoutedEventArgs e)
        {
            bool success = true;

            if(VM.WaveguideSelected)
            {
                success = m_reportWriter.VerifyDirectoryExists(VM.WaveguideDirectory);
                if(success)
                    success = m_reportWriter.WriteExperimentFile_WaveGuide(VM.WaveguideDirectory + "\\" + 
                                                                            VM.WaveguideFilename, 
                                                                            VM.AnalysisList); 
            }

            if(VM.ExcelSelected)
            {
                success = m_reportWriter.VerifyDirectoryExists(VM.ExcelDirectory);

                if (success)
                {
                    List<string> fileNameList =
                        m_reportWriter.GetFormattedStringList(VM.ExcelFilename, VM.AnalysisList);

                    int i = 0;
                    foreach (AnalysisContainer analysis in VM.AnalysisList)
                    {
                        string filename = "";
                        if (i + 1 > fileNameList.Count) filename = "UnknownIndicator_" + i.ToString();
                        else filename = fileNameList.ElementAt(i);

                        success = m_reportWriter.WriteExperimentFile_Excel(VM.ExcelDirectory + "\\" + filename, analysis);
                        if (!success) break;
                        i++;
                    }
                }
            }


            if(success)
            {
                Close();
            }
            else
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("Failed to write report: " + m_reportWriter.GetLastErrorString(), 
                    "Error",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }



        private string ReportWriteErrorStr;
        public string GetLastError()
        {
            return ReportWriteErrorStr;
        }

        public bool WriteReportFiles(bool writeWaveguideReport, bool writeExcelReport)
        {
            bool success1 = true;
            bool success2 = true;

            if (writeWaveguideReport)
            {
                success1 = m_reportWriter.VerifyDirectoryExists(VM.WaveguideDirectory);
                if (success1)
                    success1 = m_reportWriter.WriteExperimentFile_WaveGuide(VM.WaveguideDirectory + "\\" +
                                                                            VM.WaveguideFilename,
                                                                            VM.AnalysisList);

                if (!success1) ReportWriteErrorStr = "Failed to Write Waveguide Report";
            }

            if (writeExcelReport)
            {
                success2 = m_reportWriter.VerifyDirectoryExists(VM.ExcelDirectory);

                if (success2)
                {
                    List<string> fileNameList =
                        m_reportWriter.GetFormattedStringList(VM.ExcelFilename, VM.AnalysisList);

                    int i = 0;
                    foreach (AnalysisContainer analysis in VM.AnalysisList)
                    {
                        string filename = "";
                        if (i + 1 > fileNameList.Count) filename = "UnknownIndicator_" + i.ToString();
                        else filename = fileNameList.ElementAt(i);

                        success2 = m_reportWriter.WriteExperimentFile_Excel(VM.ExcelDirectory + "\\" + filename, analysis);
                        if (!success2) break;
                        i++;
                    }
                }

                if(!success2)
                {
                    if (ReportWriteErrorStr == "None") ReportWriteErrorStr = "Failed to Write Excel Report";
                }
            }


            if (success1 && success2) ReportWriteErrorStr = "None";
            else if (!success1 && success2) ReportWriteErrorStr = "Failed to write Waveguide Report";
            else if (success1 && !success2) ReportWriteErrorStr = "Failed to write Excel Report";
            else if (!success1 && !success2) ReportWriteErrorStr = "Frailed to write Waveguide and Excel Report";

            return (success1 && success2);
        }


       

        private void BrowseForWaveguideDirectoryPB_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = VM.WaveguideDirectory;
            DialogResult result = dlg.ShowDialog();
            if (result.ToString() == "OK")
            {
                VM.WaveguideDirectory = dlg.SelectedPath;                
            }
        }

        private void BrowseForExcelDirectoryPB_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = VM.ExcelDirectory;
            DialogResult result = dlg.ShowDialog();
            if (result.ToString() == "OK")
            {
                VM.ExcelDirectory = dlg.SelectedPath;
            }
        }

        
       
    }



    /////////////////////////////////////////////////////////
    // Analysis
    public class ViewModel_ReportDialog : INotifyPropertyChanged
    {
        private bool _waveguideSelected;
        private bool _excelSelected;

        private string _waveguideFilename;
        private string _waveguideDirectory;
        private string _excelFilename;
        private string _excelDirectory;
        private REPORT_FILEFORMAT _reportFormat;

        private ObservableCollection<AnalysisContainer> _analysisList;
        
        // constructor
        public ViewModel_ReportDialog()
        {
            _analysisList = new ObservableCollection<AnalysisContainer>();
        }

        public bool WaveguideSelected
        {
            get { return _waveguideSelected; }
            set { _waveguideSelected = value; NotifyPropertyChanged("WaveguideSelected"); }
        }

        public bool ExcelSelected
        {
            get { return _excelSelected; }
            set { _excelSelected = value; NotifyPropertyChanged("ExcelSelected"); }
        }


        public string WaveguideFilename
        {
            get { return _waveguideFilename; }
            set { _waveguideFilename = value; NotifyPropertyChanged("WaveguideFilename"); }
        }

        public string WaveguideDirectory
        {
            get { return _waveguideDirectory; }
            set { _waveguideDirectory = value; NotifyPropertyChanged("WaveguideDirectory"); }
        }


        public string ExcelFilename
        {
            get { return _excelFilename; }
            set { _excelFilename = value; NotifyPropertyChanged("ExcelFilename"); }
        }

        public string ExcelDirectory
        {
            get { return _excelDirectory; }
            set { _excelDirectory = value; NotifyPropertyChanged("ExcelDirectory"); }
        }

        public REPORT_FILEFORMAT ReportFormat
        {
            get { return _reportFormat; }
            set { _reportFormat = value; NotifyPropertyChanged("ReportFormat"); }
        }

        public ObservableCollection<AnalysisContainer> AnalysisList
        {
            get { return _analysisList; }
            set { _analysisList = value; NotifyPropertyChanged("AnalysisList"); }
        }
             

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }


   
}
