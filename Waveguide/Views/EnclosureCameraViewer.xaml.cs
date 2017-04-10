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
using MjpegProcessor;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for EnclosureCameraViewer.xaml
    /// </summary>
    public partial class EnclosureCameraViewer : Window
    {
        MjpegDecoder m_decoder;

        public EnclosureCameraViewer()
        {
            InitializeComponent();

            m_decoder = new MjpegDecoder();

            m_decoder.FrameReady += m_decoder_FrameReady;
            m_decoder.Error += m_decoder_Error;

            // Axis camera must be set to allow anonymous viewer login

            // The IP address below and setting the anonymous viewer login can be 
            // done using the Axis Camera Management Client

            string ipAddr = GlobalVars.EnclosureCameraIPAddress;
            string uriString = "http://" + ipAddr + "/axis-cgi/mjpg/video.cgi";

            m_decoder.ParseStream(new Uri(uriString));

            //m_decoder.ParseStream(new Uri("http://10.103.28.91/axis-cgi/mjpg/video.cgi"));
        }

        void m_decoder_Error(object sender, ErrorEventArgs e)
        {
            
        }

        void m_decoder_FrameReady(object sender, FrameReadyEventArgs e)
        {
            DisplayImage.Source = e.BitmapImage;
        }

        private void ClosePB_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void Shutdown()
        {
            Close();
        }

        public void BringWindowToFront()
        {
            // Bring this window into view 
            if (!this.IsVisible)
            {
                this.Show();
            }

            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }

            this.Activate();
            this.Topmost = true;  // important
            this.Topmost = false; // important
            this.Focus();         // important 

        }
    }
}
