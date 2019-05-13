using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Waveguide;
using WPFTools;

namespace WaveExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindow_ViewModel m_vm;

        public MainWindow()
        {
            InitializeComponent();
            m_vm = new MainWindow_ViewModel();
            DataContext = m_vm;
        }

        
      
    }


    public class MainWindow_ViewModel : ObservableObject
    {
       


        public MainWindow_ViewModel()
        {
           
        }


    }
}