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
using System.Windows.Shapes;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for EditProjectDialog.xaml
    /// </summary>
    public partial class EditProjectDialog : Window
    {
        EditProjectVM ProjectVM;

        public bool m_OK;
        bool m_addingNew;

        public EditProjectDialog(int projectID)
        {
            InitializeComponent();

            m_OK = false;
            m_addingNew = false;

            if (projectID == 0)
            {
                m_addingNew = true;
            }

            ProjectVM = new EditProjectVM(projectID);
            ProjectVM.Refresh();

            this.DataContext = ProjectVM;
        }

        private void SavePB_Click(object sender, RoutedEventArgs e)
        {
            bool goodData = true;
            string errStr = "No Error";

            WaveguideDB wgDB = new WaveguideDB();


            bool success = wgDB.GetAllProjects(true);
            bool ProjectNameUnique = true;

            // if creating a new project, make sure project name isn't already used in database
            if (m_addingNew)
            {
                for (int i = 0; i < wgDB.m_projectList.Count(); i++)
                {
                    if (wgDB.m_projectList[i].Description.Equals(ProjectVM.ProjectDescription, StringComparison.OrdinalIgnoreCase))
                    {
                        ProjectNameUnique = false;
                        break;
                    }
                }
            }


            if (ProjectVM.ProjectDescription.Length < 1)
            {
                goodData = false;
                errStr = "Project must have a Name";
            }
            else if (!ProjectNameUnique)
            {
                goodData = false;
                errStr = "Project Name: " + ProjectVM.ProjectDescription + " is already in use by another project (Includes Archived Projects).";
            }

            if (goodData)
            {
                ProjectContainer pc = new ProjectContainer();
                if (m_addingNew)  // creating a new project
                {                    
                    pc.Description = ProjectVM.ProjectDescription;
                    pc.Archived = ProjectVM.Archived;
                    pc.TimeStamp = ProjectVM.TimeStamp;

                    success = wgDB.InsertProject(ref pc);
                }
                else // updating current project instead of creating a new one
                {                 
                    pc.Description = ProjectVM.ProjectDescription;
                    pc.ProjectID = ProjectVM.ProjectID;
                    pc.Archived = ProjectVM.Archived;
                    pc.TimeStamp = ProjectVM.TimeStamp;
                    
                    success = wgDB.UpdateProject(pc);
                }

                if (success)
                {
                    ProjectVM.ProjectDescription = pc.Description;
                    ProjectVM.ProjectID = pc.ProjectID;
                    ProjectVM.Archived = pc.Archived;
                    ProjectVM.TimeStamp = pc.TimeStamp;

                    // delete all current UserProject records for this Project
                    success = wgDB.RemoveProjectFromUserProjectTable(pc.ProjectID);

                    if (success)
                    {
                        // add UserProject records as designed by ProjectVM.Users list
                        for (int i = 0; i < ProjectVM.Users.Count(); i++)
                        {
                            if (ProjectVM.Users[i].AssignedToProject)
                            {
                                success = wgDB.AddUserToProject(ProjectVM.Users[i].UserID, ProjectVM.ProjectID);
                                if (!success)
                                {
                                    errStr = wgDB.GetLastErrorMsg();
                                    MessageBox.Show(errStr, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    break;
                                }
                            }
                        }
                    }
                }

                if (success)
                {
                    m_OK = true;
                    Close();
                }
            }


            if (!goodData)               
                MessageBox.Show(errStr, "Error in Project Data", MessageBoxButton.OK, MessageBoxImage.Error);

        }


        private void CancelPB_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        class EditProjectVM : INotifyPropertyChanged
        {
            private int _projectID;
            private string _projectDescription;
            private bool _archived;
            private DateTime _timeStamp;
            private BindingList<UserItem> _users;

            public int ProjectID
            { get { return _projectID; } set { _projectID = value; NotifyPropertyChanged("ProjectID"); } }

            public string ProjectDescription
            { get { return _projectDescription; } set { _projectDescription = value; NotifyPropertyChanged("ProjectDescription"); } }

            public bool Archived
            { get { return _archived; } set { _archived = value; NotifyPropertyChanged("Archived"); } }

            public DateTime TimeStamp
            { get { return _timeStamp; } set { _timeStamp = value; NotifyPropertyChanged("TimeStamp"); } }

            public BindingList<UserItem> Users
            { get { return _users; } set { _users = value; NotifyPropertyChanged("Users"); } }

            public EditProjectVM(int projectID)
            {
                ProjectID = projectID;

                WaveguideDB wgDB = new WaveguideDB();
                bool success;

                ProjectContainer pc;
                if (projectID != 0)
                {
                    success = wgDB.GetProject(ProjectID, out pc);
                }
                else
                {
                    pc = new ProjectContainer();
                    pc.Description = "";
                    pc.ProjectID = 0;
                    pc.Archived = false;
                    pc.TimeStamp = DateTime.Now;
                    success = true;
                }

                if (success)
                {
                    ProjectDescription = pc.Description;
                    Archived = pc.Archived;
                    TimeStamp = pc.TimeStamp;
                    ProjectID = pc.ProjectID;
                }

                Users = new BindingList<UserItem>();
            }

            public void Refresh()
            {
                WaveguideDB wgDB = new WaveguideDB();

                bool success = wgDB.GetAllUsers();

                Users.Clear();

                if (success)
                {
                    for (int i = 0; i < wgDB.m_userList.Count(); i++)
                    {
                        UserItem uitem = new UserItem();
                        uitem.UserID = wgDB.m_userList[i].UserID;
                        uitem.Fullname = wgDB.m_userList[i].Lastname + ", " + wgDB.m_userList[i].Firstname;
                        uitem.AssignedToProject = false;

                        bool IsAssigned = new bool();
                        IsAssigned = false;
                        success = wgDB.IsUserAssignedToProject(wgDB.m_userList[i].UserID, ProjectID, ref IsAssigned);
                        if (success)
                        {
                            uitem.AssignedToProject = IsAssigned;
                        }

                        Users.Add(uitem);
                    }
                }
            }


            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String info)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
            }

        }

        class UserItem : INotifyPropertyChanged
        {
            private int _userID;
            private string _fullname;
            private bool _assignedToProject;

            public int UserID { get { return _userID; } set { _userID = value; NotifyPropertyChanged("UserID"); } }
            public string Fullname { get { return _fullname; } set { _fullname = value; NotifyPropertyChanged("Fullname"); } }
            public bool AssignedToProject { get { return _assignedToProject; } set { _assignedToProject = value; NotifyPropertyChanged("AssignedToProject"); } }

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String info)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
            }
        }


    }


    
}
