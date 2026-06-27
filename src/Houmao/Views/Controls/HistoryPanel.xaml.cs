using System.Windows;
using System.Windows.Controls;

namespace Houmao.Views.Controls
{
    public partial class HistoryPanel : UserControl
    {
        public HistoryPanel()
        {
            InitializeComponent();
        }
        
        private void ListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 检查是否滚动到底部
            var listView = sender as ListView;
            if (listView == null) return;
            
            var scrollViewer = FindVisualChild<ScrollViewer>(listView);
            if (scrollViewer == null) return;
            
            // 如果滚动到底部，触发加载更多
            if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 10)
            {
                var viewModel = DataContext as ViewModels.HistoryViewModel;
                if (viewModel != null && viewModel.HasMore && !viewModel.IsLoading)
                {
                    viewModel.LoadMoreCommand.Execute(null);
                }
            }
        }
        
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }
                
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            
            return null;
        }
    }
}