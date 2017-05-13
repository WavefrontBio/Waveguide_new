using System;
using System.Windows.Controls;

namespace Waveguide
{


    public class PlateBarcodeValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            // check to see if Barcode is ok
            var str = value as string;
            // check to see if it is empty
            if (String.IsNullOrEmpty(str))
            {
                return new ValidationResult(false, "Must provide a Barcode for the plate");
            }

            // require exact length of 8 characters
            if (str.Length != 8)
            {
                return new ValidationResult(false, "Barcode must be exactly 8 characters");
            }

            // Barcode look ok
            return new ValidationResult(true, null);
        }
    }

     


}