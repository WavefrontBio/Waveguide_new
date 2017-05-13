using Infragistics.Windows.DataPresenter;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for ProjectManager.xaml
    /// </summary>
    public partial class ProjectManager : UserControl
    {       
        ProjectViewModel ProjectVM;

        public ProjectManager()
        {
            InitializeComponent();

            ProjectVM = new ProjectViewModel();

            this.DataContext = ProjectVM;
           
        }

        private void EditProjectPB_Click(object sender, RoutedEventArgs e)
        {
            DataRecord record = (DataRecord)projectXamDataGrid.ActiveRecord;
            if (record == null) return;
                        
            if (record.DataItem.GetType() == typeof(UserFullname))
            {
                DataRecord recordParent = record.ParentDataRecord;
                if (recordParent.DataItem.GetType() == typeof(ProjectSimple))
                {
                    record = recordParent;
                }
            }

            if (record.DataItem.GetType() != typeof(ProjectSimple)) return;

            ProjectSimple ps = (ProjectSimple)record.DataItem;

            ProjectContainer pc = new ProjectContainer();

            pc.Description = ps.Description;
            pc.ProjectID = ps.ProjectID;

            EditProjectDialog dlg = new EditProjectDialog(pc.ProjectID);

            dlg.ShowDialog();

            if (dlg.m_OK) ProjectVM.Refresh();
            
        }

        private void AddProjectPB_Click(object sender, RoutedEventArgs e)
        {
            EditProjectDialog dlg = new EditProjectDialog(0);  // 0 here indicates we're adding a new project

            dlg.ShowDialog();

            if (dlg.m_OK) ProjectVM.Refresh();
        }

        private void DeleteProjectPB_Click(object sender, RoutedEventArgs e)
        {
            DataRecord record = (DataRecord)projectXamDataGrid.ActiveRecord;
            if (record == null) return;


            if (record != null)
            {
                ProjectSimple project = (ProjectSimple)record.DataItem;

                string MsgStr = "Are you sure that you want to DELETE Project: " + project.Description + "?";

                MessageBoxResult result =
                      MessageBox.Show(MsgStr, "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question,MessageBoxResult.No);
                if (result == MessageBoxResult.Yes)
                {
                    WaveguideDB wgDB = new WaveguideDB();                    
                    bool success = wgDB.RemoveProjectFromUserProjectTable(project.ProjectID);
                    if (success)
                    {
                        success = wgDB.DeleteProject(project.ProjectID);
                        if (success) ProjectVM.Refresh();
                    }
                }
            }
        }

        private void projectXamDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditProjectPB_Click(null, null);
        }

        private void ShowArchiveCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ProjectVM.Refresh();
        }




    }





    public class ProjectViewModel : INotifyPropertyChanged
    {
        public WaveguideDB wgDB;

        private bool _showArchivedProjects;
        public bool ShowArchivedProjects
        { get { return _showArchivedProjects; } set { _showArchivedProjects = value; NotifyPropertyChanged("ShowArchivedProjects"); } }

        private BindingList<ProjectSimple> _projects;
        public BindingList<ProjectSimple> Projects
        { get { return _projects; } set { _projects = value; NotifyPropertyChanged("Projects"); } }

        public ProjectViewModel()
        {
            wgDB = new WaveguideDB();
            _projects = new BindingList<ProjectSimple>();

            ShowArchivedProjects = false;

            Refresh();
        }

        public void Refresh()
        {
            Projects.Clear();

            bool success = wgDB.GetAllProjects(ShowArchivedProjects);
            if (success)
            {
                for (int i = 0; i < wgDB.m_projectList.Count(); i++)
                {
                    ProjectSimple project = new ProjectSimple();
                    project.Description = wgDB.m_projectList[i].Description;
                    project.ProjectID = wgDB.m_projectList[i].ProjectID;
                    project.Archived = wgDB.m_projectList[i].Archived;
                    project.TimeStamp = wgDB.m_projectList[i].TimeStamp;

                    Projects.Add(project);

                    List<UserContainer> users;
                    success = wgDB.GetAllUsersForProject(project.ProjectID, out users);

                    if (success)
                    {
                        for (int j = 0; j < users.Count(); j++)
                        {
                            UserFullname ufn = new UserFullname();
                            ufn.Fullname = users[j].Lastname + ", " + users[j].Firstname;
                            project.Users.Add(ufn);
                        }
                    }
                }

            }

        }


        void user_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("Users");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }




    public class ProjectSimple : INotifyPropertyChanged
    {
        private int _projectID;
        private string _description;
        private bool _archived;
        private DateTime _timeStamp;
        BindingList<UserFullname> _users;

        public ProjectSimple()
        {
            _users = new BindingList<UserFullname>();
        }

        public int ProjectID
        { get { return _projectID; } set { _projectID = value; NotifyPropertyChanged("ProjectID"); } }

        public string Description
        { get { return _description; } set { _description = value; NotifyPropertyChanged("Description"); } }

        public bool Archived
        { get { return _archived; } set { _archived = value; NotifyPropertyChanged("Archived"); } }

        public DateTime TimeStamp
        { get { return _timeStamp; } set { _timeStamp = value; NotifyPropertyChanged("TimeStamp"); } }

        public BindingList<UserFullname> Users
        { get { return _users; } set { _users = value; NotifyPropertyChanged("Users"); } }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }

    }

    public class UserFullname
    {
        private string _fullname;
        public string Fullname
        { get { return _fullname; } set { _fullname = value; } }
    }




    

}
