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
    /// Interaction logic for VWorksErrorDialog.xaml
    /// </summary>
    public partial class VWorksErrorDialog : Window
    {
        public VWorksErrorDialog(string errorMsg)
        {
            InitializeComponent();

            ErrorDescription.Text = errorMsg;
        }

        private void OkPB_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
