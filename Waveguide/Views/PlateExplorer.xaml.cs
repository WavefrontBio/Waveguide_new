using Infragistics.Windows.DataPresenter;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for PlateExplorer.xaml
    /// </summary>
    public partial class PlateExplorer : UserControl
   {


        WaveguideDB m_wgDB;


        PlateExplorer_ViewModel VM;

        public PlateExplorer()
        {
            InitializeComponent();

            // Initialize data in the XamDataGrid - NOTE: A blank record is added FIRST, this is key to this approach for the XamDataGrid
            m_wgDB = new WaveguideDB();


            VM = new PlateExplorer_ViewModel();

            DataContext = VM;

            VM.IncludeArchivedProjects = false;

        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshProjectList();        
        }


        public void RefreshProjectList()
        {
            bool success;

            // load project list
            ObservableCollection<ProjectContainer> projectList;
            if (GlobalVars.UserRole == GlobalVars.USER_ROLE_ENUM.ADMIN)
            {
                success = m_wgDB.GetAllProjects(VM.IncludeArchivedProjects);
                projectList = m_wgDB.m_projectList;
            }
            else
                success = m_wgDB.GetAllProjectsForUser(GlobalVars.UserID, out projectList);
 
            if(success && projectList!=null)
            {
                VM.ProjectList.Clear();
                VM.CurrentProject = null;
                
                foreach(ProjectContainer project in projectList)
                {
                    if(!project.Archived || (project.Archived && VM.IncludeArchivedProjects))
                    {
                        PlateExplorer_ViewModel.ProjectListItem pli = new PlateExplorer_ViewModel.ProjectListItem();
                        pli.Description = project.Description;
                        pli.ProjectID = project.ProjectID;
                        VM.ProjectList.Add(pli);
                    }
                }
            }            
        }


        private void RefreshPlateList()
        {
            VM.PlateList.Clear();
            VM.CurrentPlate = null;

            if (VM.CurrentProject == null) return;

            ObservableCollection<PlateContainer> plateList = null;
            bool success = m_wgDB.GetAllPlatesForProject(VM.CurrentProject.ProjectID);
            if (success) plateList = m_wgDB.m_plateList;

            if (plateList == null) return;

            foreach (PlateContainer plate in plateList)
            {
                PlateExplorer_ViewModel.PlateListItem pli = new PlateExplorer_ViewModel.PlateListItem();
                pli.Description = plate.Barcode.PadRight(15,' ') + " - " + plate.Description;
                pli.PlateID = plate.PlateID;
                VM.PlateList.Add(pli);
            }
        }

        private void RefreshExperimentList()
        {
            VM.ExperimentList.Clear();
            VM.CurrentExperiment = null;

            if (VM.CurrentPlate == null) return;

            ObservableCollection<ExperimentContainer> experimentList;
            bool success = m_wgDB.GetAllExperimentsForPlate(VM.CurrentPlate.PlateID, out experimentList);

            if (experimentList == null) return;

            foreach(ExperimentContainer experiment in experimentList)
            {
                PlateExplorer_ViewModel.ExperimentListItem eli = new PlateExplorer_ViewModel.ExperimentListItem();
                eli.Description = experiment.Description.PadRight(35,' ') + " - " + experiment.TimeStamp.ToString();
                eli.ExperimentID = experiment.ExperimentID;
                VM.ExperimentList.Add(eli);
            }
        }





        private void CreateReportPB_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<ExperimentIndicatorContainer> expIndicatorList;
            bool success = m_wgDB.GetAllExperimentIndicatorsForExperiment(VM.CurrentExperiment.ExperimentID, out expIndicatorList);
            if(success)
            {
                ProjectContainer project;
                success = m_wgDB.GetProject(VM.CurrentProject.ProjectID, out project);
                
                if(success && project != null)
                {
                    ExperimentContainer experiment;
                    success = m_wgDB.GetExperiment(VM.CurrentExperiment.ExperimentID, out experiment);

                    if(success && experiment != null)
                    {
                        ReportDialog dlg = new ReportDialog(project,experiment,expIndicatorList);

                        dlg.ShowDialog();
                    }
                }

                
            }
        }

       

        private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            VM.CurrentProject = (PlateExplorer_ViewModel.ProjectListItem)ProjectComboBox.SelectedItem;

            PlateComboBox.SelectedIndex = -1;
            ExperimentListView.SelectedIndex = -1;

            CreateReportPB.IsEnabled = false;

            VM.ExperimentList.Clear();

            RefreshPlateList();
        }

        private void PlateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VM.CurrentPlate = (PlateExplorer_ViewModel.PlateListItem)PlateComboBox.SelectedItem;

            ExperimentListView.SelectedIndex = -1;           
            
            CreateReportPB.IsEnabled = false;

            RefreshExperimentList();
        }

        private void ExperimentListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VM.CurrentExperiment = (PlateExplorer_ViewModel.ExperimentListItem)ExperimentListView.SelectedItem;
            CreateReportPB.IsEnabled = true;
        }

        private void IncludeArchivedProjectsCkBx_Checked(object sender, RoutedEventArgs e)
        {
            RefreshProjectList();
        }


    }








    ///////////////////////////////////////////////////////////////////////////////////////////////


    public class PlateExplorer_ViewModel : INotifyPropertyChanged
    {
        private ProjectListItem _currentProject;
        private PlateListItem _currentPlate;
        private ExperimentListItem _currentExperiment;
        private bool _includeArchivedProjects;

        private ObservableCollection<ProjectListItem> _projectList;
        private ObservableCollection<PlateListItem> _plateList;
        private ObservableCollection<ExperimentListItem> _experimentList;
        

        public PlateExplorer_ViewModel()
        {
            _projectList = new ObservableCollection<ProjectListItem>();
            _plateList = new ObservableCollection<PlateListItem>();
            _experimentList = new ObservableCollection<ExperimentListItem>();            
        }


      
        public ProjectListItem CurrentProject
        {
            get { return _currentProject; }
            set { _currentProject = value; NotifyPropertyChanged("CurrentProject"); }
        }

        public PlateListItem CurrentPlate
        {
            get { return _currentPlate; }
            set { _currentPlate = value; NotifyPropertyChanged("CurrentPlate"); }
        }

        public ExperimentListItem CurrentExperiment
        {
            get { return _currentExperiment; }
            set { _currentExperiment = value; NotifyPropertyChanged("CurrentExperiment"); }
        }

        public bool IncludeArchivedProjects
        {
            get { return _includeArchivedProjects; }
            set { _includeArchivedProjects = value; NotifyPropertyChanged("IncludeArchivedProjects"); }
        }


  

        public ObservableCollection<ProjectListItem> ProjectList
        {
            get { return _projectList; }
            set { _projectList = value; NotifyPropertyChanged("ProjectList"); }
        }

        public ObservableCollection<PlateListItem> PlateList
        {
            get { return _plateList; }
            set { _plateList = value; NotifyPropertyChanged("PlateList"); }
        }

        public ObservableCollection<ExperimentListItem> ExperimentList
        {
            get { return _experimentList; }
            set { _experimentList = value; NotifyPropertyChanged("ExperimentList"); }
        }

        

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }




        public class UserListItem
        {
            public string Description { get; set; }
            public int UserID;
        }

        public class ProjectListItem
        {
            public string Description { get; set; }
            public int ProjectID;
        }

        public class PlateListItem
        {
            public string Description { get; set; }
            public int PlateID;
        }

        public class ExperimentListItem
        {
            public string Description { get; set; }
            public int ExperimentID;
        }

    }



   


}
