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

        public WellSelectionDialog(int rows, int cols, ObservableCollection<Tuple<int, int>> wellList = null)
        {          
            InitializeComponent();

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
                       
            WellControl.Init(rows, cols, wellList);
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
            m_accepted = true;
            Close();
        }
    }
}
