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
using System.Windows.Shapes;

namespace Waveguide
{

    public enum FilterChangerEnum
    {
        Emission,
        Excitation
    }

    public enum FilterPositionEnum
    {
        Position_0,
        Position_1,
        Position_2,
        Position_3,
        Position_4,
        Position_5,
        Position_6,
        Position_7,
        Position_8,
        Position_9
    }



    /// <summary>
    /// Interaction logic for EditFilterDialog.xaml
    /// </summary>
    public partial class EditFilterDialog : Window
    {
        

        EditFilterViewModel FilterVM;
        bool m_addingNew;
        public bool m_OK;

        public EditFilterDialog(FilterContainer filter)
        {
            InitializeComponent();
                       
            FilterVM = new EditFilterViewModel(filter);

            this.DataContext = FilterVM;

            MainGrid.DataContext = FilterVM;

            m_OK = false;

            if (filter == null)
                m_addingNew = true;
            else
                m_addingNew = false;
        }

        private void SavePB_Click(object sender, RoutedEventArgs e)
        {
            WaveguideDB wgDB = new WaveguideDB();

            bool success = wgDB.GetAllFilters();
            if (success)
            {
                bool goodData = true;
                string errStr = "No Error";

                // check to make sure that the FilterChanger/FilterPosition combination is not already occupied
                for (int i = 0; i < wgDB.m_filterList.Count(); i++)
                {
                    if (FilterVM.Filter.FilterChanger == wgDB.m_filterList[i].FilterChanger && FilterVM.Filter.PositionNumber == wgDB.m_filterList[i].PositionNumber)
                    {
                        goodData = false;
                        errStr = ((FilterPositionEnum)FilterVM.Filter.PositionNumber).ToString() + 
                                  " already taken in " + ((FilterChangerEnum)FilterVM.Filter.FilterChanger).ToString() + 
                                  " Filter Changer";
                    }
                }

                if (goodData)
                {
                    if (m_addingNew)
                    {
                        // adding new filter, so call insert
                        FilterContainer fc = FilterVM.Filter;
                        success = wgDB.InsertFilter(ref fc);                        
                    }
                    else
                    {
                        // updating existing filter, so call update
                        FilterContainer fc = FilterVM.Filter;
                        success = wgDB.UpdateFilter(fc);
                    }

                    if (success)
                    {
                        m_OK = true;
                        Close();
                    }
                    else
                    {
                        errStr = wgDB.GetLastErrorMsg();
                        MessageBox.Show(errStr, "Database Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                if (!goodData)
                    MessageBox.Show(errStr, "Error in Filter Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }            
        }

        private void CancelPB_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        class EditFilterViewModel : INotifyPropertyChanged
        {
            private FilterContainer _filter;

            public FilterContainer Filter
            {
                get { return _filter; }
                set
                {
                    _filter = value;
                    NotifyPropertyChanged("Filter");
                }
            }


            public EditFilterViewModel(FilterContainer filter)
            {                  
                if (filter == null)
                {
                    // add new filter
                    Filter = new FilterContainer();
                    Filter.FilterChanger = (int)FilterChangerEnum.Emission;
                    Filter.PositionNumber = (int)FilterPositionEnum.Position_0;
                    Filter.Description = "";
                    Filter.Manufacturer = "";
                    Filter.PartNumber = "";
                }
                else
                {
                    Filter = filter;                    
                }
            }

            
            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String info)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
            }







            public FilterChangerEnum SelectedFilterChanger
            {
                get { return (FilterChangerEnum)Filter.FilterChanger; }
                set
                {
                    Filter.FilterChanger = (int)value;
                    NotifyPropertyChanged("SelectedFilterChanger");
                }
            }

            public IEnumerable<FilterChangerEnum> FilterChangerEnumTypeValues
            {
                get
                {
                    return Enum.GetValues(typeof(FilterChangerEnum))
                        .Cast<FilterChangerEnum>();
                }
            }


            public FilterPositionEnum SelectedFilterPosition
            {
                get { return (FilterPositionEnum)Filter.PositionNumber; }
                set
                {
                    Filter.PositionNumber = (int)value;
                    NotifyPropertyChanged("SelectedFilterPosition");
                }
            }

            public IEnumerable<FilterPositionEnum> FilterPositionEnumTypeValues
            {
                get
                {
                    return Enum.GetValues(typeof(FilterPositionEnum))
                        .Cast<FilterPositionEnum>();
                }
            }


        }


    }
}
