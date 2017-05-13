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
    /// Interaction logic for ListSelectionDialog.xaml
    /// </summary>
    public partial class ListSelectionDialog : Window
    {
        List<SelectionListItem> m_list;
        public bool m_itemSelected;
        public int m_databaseID;


        public ListSelectionDialog()
        {
            InitializeComponent();

            m_itemSelected = false;
            m_databaseID = 0;

            m_list = new List<SelectionListItem>();

            SelectionList.ItemsSource = m_list;
        }

        public void AddItemToList(string itemDescription, int dbID)
        {
            m_list.Add(new SelectionListItem() {Description = itemDescription, DatabaseID = dbID});
        }

        private void CancelPB_Click(object sender, RoutedEventArgs e)
        {
            m_itemSelected = false;
            m_databaseID = 0;

            Close();

        }

        private void OkPB_Click(object sender, RoutedEventArgs e)
        {
            if (SelectionList.SelectedItem != null)
            {
                m_databaseID = ((SelectionListItem)SelectionList.SelectedItem).DatabaseID;
                m_itemSelected = true;
            }
            else
            {
                m_itemSelected = false;
                m_databaseID = 0;
            }

            Close();
        }
    }

   

    public class SelectionListItem
    {
        public string Description { get; set; }
        public int DatabaseID { get; set; }
    }

}
