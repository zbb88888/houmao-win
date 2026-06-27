using System;
using System.Globalization;
using System.Windows.Data;
using Houmao.Models;

namespace Houmao.Converters
{
    public class AttachmentTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AttachmentType type)
            {
                return type switch
                {
                    AttachmentType.Image => "🖼️",
                    AttachmentType.Audio => "🎵",
                    AttachmentType.File => "📄",
                    _ => "📎"
                };
            }
            
            return "📎";
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}