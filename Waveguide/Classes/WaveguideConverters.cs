using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Waveguide
{


    public class FilterChangerIntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FilterChangerEnum fce = (FilterChangerEnum)value;
            string name = fce.ToString();
            return fce.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class FilterPositionIntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FilterPositionEnum fpe = (FilterPositionEnum)value;
            return fpe.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class FilterPositionIntToDescriptionStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string filterDesc = "No Filter";
            WaveguideDB wgDB = new WaveguideDB();
            bool success = wgDB.GetAllFilters();
            foreach (FilterContainer filter in wgDB.m_filterList)
            {
                if (filter.PositionNumber == (int)value &&
                    filter.FilterChanger == (int)parameter)
                {
                    filterDesc = filter.Description;
                    break;
                }
            }

            return filterDesc;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
