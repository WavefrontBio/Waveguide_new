using Infragistics.Windows.DataPresenter;
using Infragistics.Windows.Editors;
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
    /// Interaction logic for UserManager.xaml
    /// </summary>
    /// 


   

    public partial class UserManager : UserControl
    {
        UserViewModel UserVM;
        
            

        public UserManager()
        {
            InitializeComponent();          

            ComboBoxItemsProvider userRoleProvider = this.userXamDataGrid.TryFindResource("UserRoleItemsProvider") as ComboBoxItemsProvider;
            if (userRoleProvider != null)
            {
                userRoleProvider.ItemsSource = new ComboBoxDataItem[]
                {
                    new ComboBoxDataItem(GlobalVars.USER_ROLE_ENUM.ADMIN, "Admin"),
                    new ComboBoxDataItem(GlobalVars.USER_ROLE_ENUM.USER, "User"),                    
                };
            }

            UserVM = new UserViewModel();

            this.DataContext = UserVM;
               
        }

       

        private void AddUserPB_Click(object sender, RoutedEventArgs e)
        {            
            EditUserDialog dlg = new EditUserDialog(null);

            dlg.ShowDialog();

            if (dlg.m_OK) UserVM.Refresh();
        }


        private void DeleteUserPB_Click(object sender, RoutedEventArgs e)
        {
            DataRecord record = (DataRecord)userXamDataGrid.ActiveRecord;
            if (record == null) return;
                    

            if (record != null)
            {
                UserSimple user = (UserSimple)record.DataItem;

                string MsgStr = "Are you sure that you want to DELETE User: " + user.Firstname + " " + user.Lastname + "?";

                MessageBoxResult result =
                      MessageBox.Show(MsgStr, "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (result == MessageBoxResult.Yes)
                {
                    WaveguideDB wgDB = new WaveguideDB();
                    bool success = wgDB.RemoveUserFromUserProjectTable(user.UserID);
                    if (success)
                    {
                        success = wgDB.DeleteUser(user.UserID);
                        if (success) UserVM.Refresh();
                    }
                }
            }
        }

        private void EditUserPB_Click(object sender, RoutedEventArgs e)
        {  

            DataRecord record = (DataRecord)userXamDataGrid.ActiveRecord;
            if (record == null) return;

            if (record.DataItem.GetType() == typeof(ProjectFullname))
            {
                DataRecord recordParent = record.ParentDataRecord;
                if (recordParent.DataItem.GetType() == typeof(UserSimple))
                {
                    record = recordParent;
                }
            }

            if (record.DataItem.GetType() != typeof(UserSimple)) return;


            UserSimple user = (UserSimple)record.DataItem;

            UserContainer u1 = new UserContainer();

            u1.UserID = user.UserID;
            u1.Lastname = user.Lastname;
            u1.Firstname = user.Firstname;
            u1.Username = user.Username;
            u1.Password = user.Password;
            u1.Role = user.Role;

            EditUserDialog dlg = new EditUserDialog(u1);

            dlg.ShowDialog();

            if (dlg.m_OK) UserVM.Refresh();
        }

        private void userXamDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditUserPB_Click(null, null);
        }
                  
    }



    public class UserViewModel : INotifyPropertyChanged
    {
        public WaveguideDB wgDB;

        private BindingList<UserSimple> _users;
        public BindingList<UserSimple> Users
        { get { return _users; } set { _users = value; NotifyPropertyChanged("Users"); } }

        public UserViewModel()
        {
            wgDB = new WaveguideDB();
            _users = new BindingList<UserSimple>();

            Refresh();
        }

        public void Refresh()
        {           
            Users.Clear();

            bool success = wgDB.GetAllUsers();
            if (success)
            {
                for (int i = 0; i < wgDB.m_userList.Count(); i++)
                {
                    UserSimple user = new UserSimple();
                    user.Firstname = wgDB.m_userList[i].Firstname;
                    user.Lastname = wgDB.m_userList[i].Lastname;
                    user.Username = wgDB.m_userList[i].Username;
                    user.Role = wgDB.m_userList[i].Role;
                    user.UserID = wgDB.m_userList[i].UserID;
                    user.Password = wgDB.m_userList[i].Password;

                    Users.Add(user);


                    ObservableCollection<ProjectContainer> projects;
                    success = wgDB.GetAllProjectsForUser(user.UserID, out projects);

                    if (success)
                    {
                        for (int j = 0; j < projects.Count(); j++)
                        {
                            if (!projects[j].Archived)
                            {
                                ProjectFullname pfn = new ProjectFullname();
                                pfn.Fullname = projects[j].Description;
                                user.Projects.Add(pfn);
                            }
                        }
                    }

                }

                int count = Users.Count();
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


    public class UserSimple : INotifyPropertyChanged
    {
        private string _firstname;
        private string _lastname;
        private string _username;
        private GlobalVars.USER_ROLE_ENUM _role;
        private int _userID;
        private string _password;
        private BindingList<ProjectFullname> _projects;
        
        public string Firstname
        { get { return _firstname; } set { _firstname = value; NotifyPropertyChanged("Firstname"); } }

        public string Lastname
        { get { return _lastname; } set { _lastname = value; NotifyPropertyChanged("Lastname"); } }

        public string Username
        { get { return _username; } set { _username = value; NotifyPropertyChanged("Username"); } }

        public GlobalVars.USER_ROLE_ENUM Role
        { get { return _role; } set { _role = value; NotifyPropertyChanged("Role"); } }

        public int UserID
        { get { return _userID; } set { _userID = value; NotifyPropertyChanged("UserID"); } }

        public string Password
        { get { return _password; } set { _password = value; NotifyPropertyChanged("Password"); } }

        public BindingList<ProjectFullname> Projects
        { get { return _projects; } set { _projects = value; NotifyPropertyChanged("Projects"); } }


        public UserSimple()
        {
            _projects = new BindingList<ProjectFullname>();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }

    }


    public class ProjectFullname
    {
        private string _fullname;
        public string Fullname
        { get { return _fullname; } set { _fullname = value; } }
    }
    

}
