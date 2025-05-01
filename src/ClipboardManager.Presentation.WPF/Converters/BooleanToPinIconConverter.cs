// src/ClipboardManager.Presentation.WPF/Converters/BooleanToPinIconConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace ClipboardManager.Presentation.WPF.Converters // Asegúrate que el namespace es correcto
{
    public class BooleanToPinIconConverter : IValueConverter
    {
        // Devuelve el glifo de icono Segoe MDL2 Assets para Pin/Unpin
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPinned && isPinned)
            {
                return "\uE718"; // Pin icon glyph
            }
            return "\uE840"; // Placeholder or Unpin glyph (e.g., \uE77A but E840 is less common)
                             // Choose a different unpin glyph if needed
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // No se necesita conversión inversa
        }
    }
}