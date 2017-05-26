using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {


        void App_Startup(object sender, StartupEventArgs e)
        {

            //Disable shutdown when the dialog closes
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            bool m_done = false;

            GlobalVars.LoadConfiguration(); 

            while (!m_done)
            {
                LoginWindow loginDlg = new LoginWindow();
                loginDlg.ShowDialog();

                if (!loginDlg.LoginSuccess)
                {
                    m_done = true;
                    Current.Shutdown();
                }
                else
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.ShowDialog();
                }
            }
        }
    }
}
