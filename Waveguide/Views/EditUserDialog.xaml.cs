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
    /// Interaction logic for EditUserDialog.xaml
    /// </summary>
    public partial class EditUserDialog : Window
    {
        EditUserVM UserVM;

        WaveguideDB wgDB;

        public bool m_OK;
        bool m_addingNew;

        public EditUserDialog(UserContainer user)
        {
            InitializeComponent();

            wgDB = new WaveguideDB();

            m_OK = false;
            m_addingNew = false;

            if (user == null)  // if null is passed in for user, the intent is to create/insert a new User
            {
                m_addingNew = true;
                user = new UserContainer();
                user.Firstname = "";
                user.Lastname = "";
                user.Username = "";
                user.Password = "";
                user.Role = GlobalVars.USER_ROLE_ENUM.USER;
                user.UserID = 0;
            }


            UserVM = new EditUserVM(user);
            MyPasswordBox.Password = user.Password;
            UserVM.Refresh();

            this.DataContext = UserVM.User;
            RoleComboBox.DataContext = UserVM;
            ProjectsListBox.DataContext = UserVM;

        }

        private void SavePB_Click(object sender, RoutedEventArgs e)
        {
            UserVM.User.Password = MyPasswordBox.Password;

            bool success = wgDB.GetAllUsers();
            if (success)
            {
                bool goodCredentials = true;
                string ErrorStr = "No Error";
                for (int i = 0; i < wgDB.m_userList.Count(); i++)
                {
                    UserContainer existingUser = wgDB.m_userList[i];
                                        
                    if((existingUser.Firstname+existingUser.Lastname).Equals(UserVM.User.Firstname+UserVM.User.Lastname, StringComparison.OrdinalIgnoreCase) && m_addingNew)
                    {
                        // Firstname/Lastname already exists
                        goodCredentials = false;
                        ErrorStr = "Name: " + UserVM.User.Firstname + " " + UserVM.User.Lastname + " already exists.";
                    }
                    else if (existingUser.Username.Equals(UserVM.User.Username, StringComparison.OrdinalIgnoreCase) && m_addingNew)
                    {
                        // Username already taken
                        goodCredentials = false;
                        ErrorStr = "Username: " + UserVM.User.Lastname + " already exists.";
                    }
                    else if (UserVM.User.Password.Length < 6)
                    {
                        // Password length not long enough
                        goodCredentials = false;
                        ErrorStr = "Password must be at least 6 characters";
                    }

                    if (!goodCredentials)
                    {                        
                        break;
                    }
                }

                if (goodCredentials)
                {
                    if (UserVM.User.Firstname.Length < 1 || UserVM.User.Lastname.Length < 1 || UserVM.User.Username.Length < 1)
                    {
                        goodCredentials = false;
                        ErrorStr = "Firstname, Lastname, or Username cannot be empty";
                    }
                    else
                    {
                        if (m_addingNew)
                        {
                            UserContainer user = new UserContainer();
                            user.Firstname = UserVM.User.Firstname;
                            user.Lastname = UserVM.User.Lastname;
                            user.Username = UserVM.User.Username;
                            user.Role = UserVM.User.Role;
                            user.Password = UserVM.User.Password;

                            success = wgDB.InsertUser(ref user);

                            if (success) UserVM.User = user;
                        }
                        else
                        {
                            success = wgDB.UpdateUser(UserVM.User);
                        }


                        if (success)
                        {                           
                            // delete all current UserProject records for this User                           
                            success = wgDB.RemoveUserFromUserProjectTable(UserVM.User.UserID);

                            if (success)
                            {
                                // add UserProject records as designed by UserVM.Projects list
                                for (int i = 0; i < UserVM.Projects.Count(); i++)
                                {
                                    if (UserVM.Projects[i].AssignedToProject)
                                    {
                                        success = wgDB.AddUserToProject(UserVM.User.UserID, UserVM.Projects[i].ProjectID);
                                        if (!success)
                                        {
                                            ErrorStr = wgDB.GetLastErrorMsg();
                                            MessageBox.Show(ErrorStr, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        else
                        {                            
                            ErrorStr = wgDB.GetLastErrorMsg();
                            MessageBox.Show(ErrorStr, "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }

                if (!goodCredentials)
                    MessageBox.Show(ErrorStr, "Error in User Credentials", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }



        private void CancelPB_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }




        class EditUserVM : INotifyPropertyChanged
        {
            private UserContainer _user;
            private BindingList<ProjectItem> _projects;
            private bool _showArchivedProjects;

            
            public UserContainer User
            {
                get { return _user; }
                set { _user = value; NotifyPropertyChanged("User"); }
            }

            public BindingList<ProjectItem> Projects
            {
                get { return _projects; }
                set { _projects = value; NotifyPropertyChanged("Projects"); }
            }

            public bool ShowArchivedProjects
            {
                get { return _showArchivedProjects; }
                set { _showArchivedProjects = value; NotifyPropertyChanged("ShowArchivedProjects"); }
            }



            public EditUserVM(UserContainer user)  // constructor
            {
                if (user == null)
                {
                    User = new UserContainer();
                    User.Firstname = "";
                    User.Lastname = "";
                    User.Username = "";
                    User.Password = "";
                    User.Role = GlobalVars.USER_ROLE_ENUM.USER;
                    User.UserID = 0;
                }
                else
                {
                    User = user;
                }
                
                Projects = new BindingList<ProjectItem>();
                ShowArchivedProjects = false;
            }



            public void Refresh()
            {
                WaveguideDB wgDB = new WaveguideDB();

                bool success = wgDB.GetAllProjects(ShowArchivedProjects);

                if (success)
                {
                    Projects.Clear();

                    for (int i = 0; i < wgDB.m_projectList.Count(); i++)
                    {
                        ProjectItem pitem = new ProjectItem();
                        pitem.ProjectName = wgDB.m_projectList[i].Description;
                        pitem.ProjectID = wgDB.m_projectList[i].ProjectID;
                        pitem.Archived = wgDB.m_projectList[i].Archived;
                        pitem.TimeStamp = wgDB.m_projectList[i].TimeStamp;

                        bool IsAssigned = new bool();
                        IsAssigned = false;
                        success = wgDB.IsUserAssignedToProject(User.UserID, pitem.ProjectID, ref IsAssigned);
                        if (success)
                        {
                            pitem.AssignedToProject = IsAssigned;
                        }

                        Projects.Add(pitem);
                    }
                }
            }



            public GlobalVars.USER_ROLE_ENUM SelectedUserRole
            {
                get { return User.Role; }
                set
                {
                    User.Role = value;
                    NotifyPropertyChanged("SelectedUserRole");
                }
            }

            public IEnumerable<GlobalVars.USER_ROLE_ENUM> UserRoleEnumTypeValues
            {
                get
                {
                    return Enum.GetValues(typeof(GlobalVars.USER_ROLE_ENUM))
                        .Cast<GlobalVars.USER_ROLE_ENUM>();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String info)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
            }

        }




        class ProjectItem : INotifyPropertyChanged
        {
            private int _projectID;
            private string _projectName;
            private bool _archived;
            private DateTime _timeStamp;
            private bool _assignedToProject;

            public int ProjectID { get { return _projectID; } set { _projectID = value; NotifyPropertyChanged("ProjectID"); } }
            public string ProjectName { get { return _projectName; } set { _projectName = value; NotifyPropertyChanged("ProjectName"); } }
            public bool Archived { get { return _archived; } set { _archived = value; NotifyPropertyChanged("Archived"); } }
            public DateTime TimeStamp { get { return _timeStamp; } set { _timeStamp = value; NotifyPropertyChanged("TimeStamp"); } }
            public bool AssignedToProject { get { return _assignedToProject; } set { _assignedToProject = value; NotifyPropertyChanged("AssignedToProject"); } }

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String info)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
            }
        }


        
    }

    
}
