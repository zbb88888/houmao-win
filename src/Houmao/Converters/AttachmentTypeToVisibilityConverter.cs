using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Houmao.Models;

namespace Houmao.Converters
{
    /// <summary>
    /// 根据附件类型和参数决定可见性
    /// 参数 "Image" 时，图片类型返回 Visible，其他 Collapsed
    /// 参数 "File" 时，音频/文件类型返回 Visible，图片 Collapsed
    /// </summary>
    public class AttachmentTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AttachmentType type && parameter is string param)
            {
                if (param == "Image")
                    return type == AttachmentType.Image ? Visibility.Visible : Visibility.Collapsed;
                
                if (param == "File")
                    return type != AttachmentType.Image ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}