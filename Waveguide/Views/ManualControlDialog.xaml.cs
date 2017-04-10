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
    /// Interaction logic for ManualControlDialog.xaml
    /// </summary>
    public partial class ManualControlDialog : Window
    {
        Imager imager;
        Camera camera;

        public ManualControlDialog(Imager _imager, int indicatorID, bool AllowConfiguration, bool isManualMode)
        {
            imager = _imager;
            camera = imager.m_camera;                      

            InitializeComponent();
            
            CameraSetupControl.Configure(imager, indicatorID, AllowConfiguration, isManualMode);

            imager.m_imagerEvent += imager_m_imagerEvent;

        }

        void imager_m_imagerEvent(object sender, ImagerEventArgs e)
        {
            // use invoke here to make sure the code below runs on UI thread (sometimes this event is raised from non-UI threads)
             Application.Current.Dispatcher.Invoke(() =>
            {
                switch (e.State)
                {
                    case ImagerState.Idle:
                        QuitPB.IsEnabled = true;
                        break;

                    case ImagerState.Error:
                        QuitPB.IsEnabled = true;
                        break;

                    case ImagerState.Busy:
                        QuitPB.IsEnabled = false;
                        break;
                }
            }); // END Invoke 
        }

        private void QuitPB_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
