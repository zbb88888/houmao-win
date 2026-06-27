using System;
using System.Globalization;
using System.Windows.Data;

namespace Houmao.Converters
{
    /// <summary>
    /// 将字符串首字母大写，并格式化为标题
    /// </summary>
    public class StringTitleCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrEmpty(str))
            {
                var format = parameter as string ?? "{0} Settings";
                var title = char.ToUpper(str[0]) + str[1..].ToLower();
                return string.Format(format, title);
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}