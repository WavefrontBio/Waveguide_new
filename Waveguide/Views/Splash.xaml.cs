using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for Splash.xaml
    /// </summary>
    public partial class Splash : Window
    {
      
        SplashScreenViewModel ssvm = new SplashScreenViewModel();
         
        public Splash()
        {
            InitializeComponent();
            DataContext = ssvm;
        }
        
    }


    internal class SplashScreenHelper
    {
        public static Splash SplashScreen { get; set; }
 
        public static void Show()
        {
            if (SplashScreen != null)
                SplashScreen.Show();
        }
 
        public static void Hide()
        {
            if (SplashScreen == null) return;
 
            if (!SplashScreen.Dispatcher.CheckAccess())
            {
                Thread thread = new Thread(
                    new System.Threading.ThreadStart(
                        delegate()
                        {
                            SplashScreen.Dispatcher.Invoke(
                                DispatcherPriority.Normal,
                                new Action(delegate()
                                    {
                                        Thread.Sleep(2000);
                                        SplashScreen.Hide();
                                    }
                            ));
                        }
                ));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
            else
                SplashScreen.Hide();
        }
 
        public static void ShowText(string text)
        {
            if (SplashScreen == null) return;
 
            if (!SplashScreen.Dispatcher.CheckAccess())
            {
                Thread thread = new Thread(
                    new System.Threading.ThreadStart(
                        delegate()
                        {
                            SplashScreen.Dispatcher.Invoke(
                                DispatcherPriority.Normal,
 
                                new Action(delegate()
                                    {
                                        ((SplashScreenViewModel)SplashScreen.DataContext).SplashScreenText = text;
                                    }
                            ));
                            SplashScreen.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
                        }
                ));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
            else
                ((SplashScreenViewModel)SplashScreen.DataContext).SplashScreenText = text;            
        }
    }


    public class SplashScreenViewModel : INotifyPropertyChanged
    {
        private string splashScreenText = "Initializing...";
        public string SplashScreenText
        {
            get { return splashScreenText; }
            set
            {
                splashScreenText = value;
                NotifyPropertyChanged("SplashScreenText");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }

}
