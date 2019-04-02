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
using Infragistics.Controls.Gauges;
using System.ComponentModel;
using System.Windows.Threading;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for TemperatureMonitorDialog.xaml
    /// </summary>
    public partial class TemperatureMonitorDialog : Window
    {
        bool _over = false;
        Brush _previousBrush = null;

        TemperatureMonitorDialog_ViewModel VM;

        Camera m_camera;
        
        DispatcherTimer m_timer;
        
        int m_thresholdTemperature;

        bool m_overridden;

        public TemperatureMonitorDialog(Camera camera)
        {
            m_camera = camera;

            m_thresholdTemperature = GlobalVars.Instance.CameraTargetTemperature + GlobalVars.Instance.MaxCameraTemperatureThresholdDeviation;

            InitializeComponent();
            VM = new TemperatureMonitorDialog_ViewModel();
            DataContext = VM;

            m_overridden = false;

            VM.GoodZoneStart = m_thresholdTemperature;
            VM.GoodZoneEnd = m_thresholdTemperature - 20;
            if (VM.GoodZoneEnd < -100) VM.GoodZoneEnd = -100;

            m_timer = new DispatcherTimer();
            m_timer.Interval = TimeSpan.FromMilliseconds(1000);
            m_timer.Tick += m_timer_Tick;
            m_timer.Start();
        }

        void m_timer_Tick(object sender, EventArgs e)
        {
            int temp = 0;
            m_camera.GetCoolerTemp(ref temp);
            VM.Temperature = temp;

            if(VM.Temperature<=m_thresholdTemperature)
            {
                Close();
            }
        }

        private void OverridePB_Click(object sender, RoutedEventArgs e)
        {
            m_overridden = true;
            Close();
        }

        public bool WasOverridden()
        {
            return m_overridden;
        }

        /////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////


       

       


        ////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////

    }





    /// <summary>
    /// /////////////////////////////////////////////////////////////////////////
    /// </summary>








    public class TemperatureMonitorDialog_ViewModel : INotifyPropertyChanged
    {
        private int _temperature;
        public int Temperature 
        { 
            get { return _temperature; } 
            set { _temperature = value; NotifyPropertyChanged("Temperature");
            TemperatureString = _temperature.ToString();
            } 
        }

        private string _temperatureString;
        public string TemperatureString
        {
            get { return _temperatureString; }
            set { _temperatureString = value; NotifyPropertyChanged("TemperatureString"); }
        }

        private int _goodZoneStart;
        public int GoodZoneStart
        {
            get { return _goodZoneStart; }
            set { _goodZoneStart = value; NotifyPropertyChanged("GoodZoneStart"); }
        }

        private int _goodZoneEnd;
        public int GoodZoneEnd
        {
            get { return _goodZoneEnd; }
            set { _goodZoneEnd = value; NotifyPropertyChanged("GoodZoneEnd"); }
        }

        public TemperatureMonitorDialog_ViewModel()
        {
            GoodZoneStart = -70;
            GoodZoneEnd = -100;
            Temperature = 24;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }
    }
}
