using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HyZaap.Converters
{
    public class StepVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int currentStep && parameter is string stepParam)
            {
                int targetStep = int.Parse(stepParam);
                return currentStep == targetStep ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

