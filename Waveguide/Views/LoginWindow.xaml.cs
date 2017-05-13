using System;
using System.Collections.Generic;
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
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        WaveguideDB wgDB;

        public bool LoginSuccess;

        public LoginWindow()
        {
            InitializeComponent();
            wgDB = new WaveguideDB();
            LoginSuccess = false;
        }

        private void btnQuit_Click(object sender, RoutedEventArgs e)
        {
            LoginSuccess = false;
            Close();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUserName.Text;
            string password = txtPassword.Password;
            UserContainer user;

            bool success = wgDB.IsServerConnected();

            if (success)
            {
                success = wgDB.GetUserByUsername(username, out user);

                if (success)
                {
                    if (user != null)
                    {
                        if (user.Password == password)
                        {
                            GlobalVars.UserID = user.UserID;
                            GlobalVars.UserDisplayName = user.Firstname + " " + user.Lastname;
                            GlobalVars.UserRole = user.Role;

                            LoginSuccess = true;

                            Close();
                        }
                        else
                        {  // password not correct
                            MessageBoxResult result = MessageBox.Show("Incorrect Password", "Login Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        // username not found
                        MessageBoxResult result = MessageBox.Show("Username: '" + username + "' not found", "Login Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // database query issue
                    MessageBoxResult result = MessageBox.Show("Database query failure!", "Database Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                }                
            }
            else
            {
                // database connection issue
                string errMsg = wgDB.GetLastErrorMsg();
                MessageBoxResult result = MessageBox.Show(errMsg, "Database Connection Failure", 
                                                          MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}
