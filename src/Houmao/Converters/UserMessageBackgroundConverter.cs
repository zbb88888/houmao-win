using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Houmao.Converters
{
    public class UserMessageBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUser)
            {
                return isUser 
                    ? Application.Current.TryFindResource("UserMessageBackground") as Brush 
                    : Application.Current.TryFindResource("AssistantMessageBackground") as Brush;
            }
            
            return Application.Current.TryFindResource("AssistantMessageBackground") as Brush;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class UserMessageAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUser)
            {
                return isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            
            return HorizontalAlignment.Left;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}