using Infragistics.Windows.DataPresenter;
using Infragistics.Windows.Editors;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Waveguide
{
  
    public partial class FilterManager : UserControl
    {

        
        FilterViewModel FilterVM; 
        

        public FilterManager()
        {
            InitializeComponent();

           
            FilterVM = new FilterViewModel();
            FilterVM.Refresh();

            this.DataContext = FilterVM;
        }



        private void EditPB_Click(object sender, RoutedEventArgs e)
        {
            DataRecord record = (DataRecord)filterXamDataGrid.ActiveRecord;
            if (record == null) return;

            FilterContainer filter = (FilterContainer)record.DataItem;

            EditFilterDialog dlg = new EditFilterDialog(filter);

            dlg.ShowDialog();

            if (dlg.m_OK) FilterVM.Refresh();
        }

        private void AddPB_Click(object sender, RoutedEventArgs e)
        {
            EditFilterDialog dlg = new EditFilterDialog(null);

            dlg.ShowDialog();

            if (dlg.m_OK) FilterVM.Refresh();
        }

        private void DeletePB_Click(object sender, RoutedEventArgs e)
        {

            DataRecord record = (DataRecord)filterXamDataGrid.ActiveRecord;
            if (record == null) return;

            if (record != null)
            {
                FilterContainer filter = (FilterContainer)record.DataItem;

                string MsgStr = "Are you sure that you want to DELETE Filter: " + ((FilterChangerEnum)filter.FilterChanger).ToString() + ":" 
                                                                                    + ((FilterPositionEnum)filter.PositionNumber).ToString() + " "
                                                                                    + filter.Description + "?";

                MessageBoxResult result =
                      MessageBox.Show(MsgStr, "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (result == MessageBoxResult.Yes)
                {
                    WaveguideDB wgDB = new WaveguideDB();
                    bool success = wgDB.DeleteFilter(filter.FilterID);

                    if (success) FilterVM.Refresh();
                }
            }


        }

        private void filterXamDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditPB_Click(null, null);
        }






        class FilterViewModel : INotifyPropertyChanged
        {
            WaveguideDB wgDB;

            private BindingList<FilterContainer> _filters;

            public BindingList<FilterContainer> Filters
            {
                get { return _filters; }
                set { _filters = value; NotifyPropertyChanged("Filters"); }
            }


            public FilterViewModel()
            {
                wgDB = new WaveguideDB();

                Filters = new BindingList<FilterContainer>();

                Refresh();
            }
                        

            public void Refresh()
            {
                WaveguideDB wgDB = new WaveguideDB();

                bool success = wgDB.GetAllFilters();

                if (success)
                {
                    Filters.Clear();

                    for (int i = 0; i < wgDB.m_filterList.Count(); i++)
                    {
                        Filters.Add(wgDB.m_filterList[i]);
                    }
                }

            }


            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String info)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
            }

        }

        


        
    }
}
