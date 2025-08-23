
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Clipboard.Converters
{
    /// <summary>
    /// Convierte un objeto a Visibility. 
    /// null o false = Collapsed, cualquier otro valor = Visible
    /// </summary>
    public class ObjectToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return Visibility.Collapsed;

            if (value is bool boolValue)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string
language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convierte un objeto a Visibility invertido.
    /// null o false = Visible, cualquier otro valor = Collapsed
    /// </summary>
    public class InvertedObjectToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return Visibility.Visible;

            if (value is bool boolValue)
                return boolValue ? Visibility.Collapsed : Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string
language)
        {
            throw new NotImplementedException();
        }
    }
}