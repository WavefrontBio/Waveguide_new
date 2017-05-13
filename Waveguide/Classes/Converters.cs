using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Waveguide;

namespace Waveguide
{
    public class BooleanToStringValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (System.Convert.ToString(value).Equals(System.Convert.ToString(parameter)))
            {
                return true;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (System.Convert.ToBoolean(value))
            {
                return parameter;
            }
            return null;
        }
    }



    public class BoolInverterConverter : IValueConverter
    {
        // used when binding 2 radio buttons to a single boolean property

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                return !(bool)value;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                return !(bool)value;
            }
            return value;
        }

        #endregion
    }



    public class EnumBindingSourceExtension : MarkupExtension
    {
        private Type _enumType;
        public Type EnumType
        {
            get { return this._enumType; }
            set
            {
                if (value != this._enumType)
                {
                    if (null != value)
                    {
                        Type enumType = Nullable.GetUnderlyingType(value) ?? value;
                        if (!enumType.IsEnum)
                            throw new ArgumentException("Type must be for an Enum.");
                    }

                    this._enumType = value;
                }
            }
        }

        public EnumBindingSourceExtension() { }

        public EnumBindingSourceExtension(Type enumType)
        {
            this.EnumType = enumType;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (null == this._enumType)
                throw new InvalidOperationException("The EnumType must be specified.");

            Type actualEnumType = Nullable.GetUnderlyingType(this._enumType) ?? this._enumType;
            Array enumValues = Enum.GetValues(actualEnumType);

            if (actualEnumType == this._enumType)
                return enumValues;

            Array tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
            enumValues.CopyTo(tempArray, 1);
            return tempArray;
        }
    }



    public class EnumDescriptionTypeConverter : EnumConverter
    {
        public EnumDescriptionTypeConverter(Type type)
            : base(type)
        {
        }
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value != null)
                {
                    FieldInfo fi = value.GetType().GetField(value.ToString());
                    if (fi != null)
                    {
                        var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                        return ((attributes.Length > 0) && (!String.IsNullOrEmpty(attributes[0].Description))) ? attributes[0].Description : value.ToString();
                    }
                }

                return string.Empty;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }


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


    public class RadioBoolToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int integer = (int)value;
            if (integer == int.Parse(parameter.ToString()))
                return true;
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter;
        }
    }



    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isTrue = (bool)value;

            return (isTrue ? Visibility.Visible : Visibility.Collapsed);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter;
        }
    }


    public class BoolToInvertVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isTrue = (bool)value;

            return (isTrue ? Visibility.Collapsed : Visibility.Visible);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter;
        }
    }



    public class UsePixelMaskToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool usePixelMask = (bool)value;

            return (usePixelMask ? "Yes" : "No");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class ValidationErrorsToStringConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new ValidationErrorsToStringConverter();
        }

        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            ReadOnlyObservableCollection<ValidationError> errors =
                value as ReadOnlyObservableCollection<ValidationError>;

            if (errors == null)
            {
                return string.Empty;
            }

            return string.Join("\n", (from e in errors
                                      select e.ErrorContent as string).ToArray());
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }




}
