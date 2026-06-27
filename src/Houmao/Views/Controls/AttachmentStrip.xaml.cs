using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Houmao.Views.Controls
{
    public partial class AttachmentStrip : UserControl
    {
        public AttachmentStrip()
        {
            InitializeComponent();
        }

        private void Attachment_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                var deleteButton = FindChild<Button>(border, "DeleteButton");
                if (deleteButton != null)
                {
                    deleteButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void Attachment_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                var deleteButton = FindChild<Button>(border, "DeleteButton");
                if (deleteButton != null)
                {
                    deleteButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private static T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {
                        return typedChild;
                    }
                }

                var result = FindChild<T>(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}