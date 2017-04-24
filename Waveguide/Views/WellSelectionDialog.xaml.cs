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
using System.Windows.Shapes;

namespace Waveguide
{
    /// <summary>
    /// Interaction logic for WellSelectionDialog.xaml
    /// </summary>
    public partial class WellSelectionDialog : Window
    {
        public bool m_accepted;
        public ObservableCollection<Tuple<int, int>> m_wellList;
        public bool m_allowEmptySelectionList;
        public WellSelectionDialog_ViewModel vm;

        public WellSelectionDialog(int rows, int cols, string title, bool allowEmptySelectionList, ObservableCollection<Tuple<int, int>> wellList = null)
        {          
            InitializeComponent();

            vm = new WellSelectionDialog_ViewModel();
            vm.DialogTitle = title;
            DataContext = vm;

            m_wellList = new ObservableCollection<Tuple<int, int>>();

            if(wellList == null)
            {
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++ )
                    {
                        m_wellList.Add(Tuple.Create<int, int>(r, c));
                    }
            }          
            else
            {
                foreach(Tuple<int,int> well in wellList)
                {
                    m_wellList.Add(well);
                }
            }
                       
            WellControl.Init(rows, cols, m_wellList);
            WellControl.NewWellSetSelected += WellControl_NewWellSetSelected;

            m_accepted = false;
        }

        void WellControl_NewWellSetSelected(object sender, EventArgs e)
        {
            WellSelectionEventArgs ev = (WellSelectionEventArgs)e;
            m_wellList.Clear();

            foreach (Tuple<int, int> well in ev.WellList)
            {
                m_wellList.Add(well);
            }
        }

        private void CancelPB_Click(object sender, RoutedEventArgs e)
        {
            m_accepted = false;
            Close();
        }

        private void OKPB_Click(object sender, RoutedEventArgs e)
        {
            if (!m_allowEmptySelectionList && m_wellList.Count == 0)
            {
                MessageBox.Show("You must select at least 1 well to be used for optimization", "Error Selecting Optimization Wells", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            m_accepted = true;
            Close();
        }
    }


    public class WellSelectionDialog_ViewModel : INotifyPropertyChanged
    {
        private string dialogTitle;

        public string DialogTitle
        {
            get { return dialogTitle; }
            set
            {
                dialogTitle = value;
                NotifyPropertyChanged("DialogTitle");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
