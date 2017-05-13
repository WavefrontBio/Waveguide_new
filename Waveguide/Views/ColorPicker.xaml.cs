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
    /// Interaction logic for ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : Window
    {
        public Color m_color;
        public bool m_colorSelected;

        public ColorPicker()
        {
            InitializeComponent();
            m_colorSelected = false;
        }

        private void OkPB_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ColorPickerControl_SelectedColorChanged(object sender, Infragistics.Controls.Editors.SelectedColorChangedEventArgs e)
        {
            m_colorSelected = true;
            m_color = (Color)ColorPickerControl.SelectedColor;
        }
    }
}
