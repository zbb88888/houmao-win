using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Houmao.Views.Controls
{
    public partial class MarkdownViewer : UserControl
    {
        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register(
                nameof(Markdown),
                typeof(string),
                typeof(MarkdownViewer),
                new PropertyMetadata(null, OnMarkdownChanged));
        
        public string? Markdown
        {
            get => (string?)GetValue(MarkdownProperty);
            set => SetValue(MarkdownProperty, value);
        }
        
        public MarkdownViewer()
        {
            InitializeComponent();
        }
        
        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownViewer viewer && e.NewValue is string markdown)
            {
                viewer.RenderMarkdown(markdown);
            }
        }
        
        private void RenderMarkdown(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                Document.Blocks.Clear();
                return;
            }
            
            // 解析 Markdown
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
            
            var document = Markdig.Markdown.Parse(markdown, pipeline);
            
            // 清空现有内容
            Document.Blocks.Clear();
            
            // 渲染每个块
            foreach (var block in document)
            {
                RenderBlock(block);
            }
        }
        
        private void RenderBlock(Markdig.Syntax.Block block)
        {
            switch (block)
            {
                case ParagraphBlock paragraph:
                    RenderParagraph(paragraph);
                    break;
                
                case HeadingBlock heading:
                    RenderHeading(heading);
                    break;
                
                case CodeBlock code:
                    RenderCodeBlock(code);
                    break;
                
                case ListBlock list:
                    RenderList(list);
                    break;
                
                case ThematicBreakBlock:
                    RenderHorizontalRule();
                    break;
                
                case QuoteBlock quote:
                    RenderQuote(quote);
                    break;
            }
        }
        
        private void RenderParagraph(ParagraphBlock paragraph)
        {
            var paragraphElement = new System.Windows.Documents.Paragraph();
            paragraphElement.Margin = new Thickness(0, 0, 0, 12);
            
            foreach (var inline in paragraph.Inline!)
            {
                RenderInline(inline, paragraphElement);
            }
            
            Document.Blocks.Add(paragraphElement);
        }
        
        private void RenderHeading(HeadingBlock heading)
        {
            var paragraph = new System.Windows.Documents.Paragraph();
            paragraph.Margin = new Thickness(0, 12, 0, 6);
            
            // 根据级别设置字体大小
            paragraph.FontSize = heading.Level switch
            {
                1 => 24,
                2 => 20,
                3 => 18,
                4 => 16,
                _ => 14
            };
            
            paragraph.FontWeight = FontWeights.SemiBold;
            
            foreach (var inline in heading.Inline!)
            {
                RenderInline(inline, paragraph);
            }
            
            Document.Blocks.Add(paragraph);
        }
        
        private void RenderCodeBlock(CodeBlock code)
        {
            // 创建代码块容器
            var section = new System.Windows.Documents.Section();
            section.Margin = new Thickness(0, 0, 0, 12);
            
            // 代码块背景
            var border = new Border();
            border.Background = (Brush)Application.Current.TryFindResource("Surface");
            border.BorderBrush = (Brush)Application.Current.TryFindResource("Border");
            border.BorderThickness = new Thickness(0.5);
            border.CornerRadius = new CornerRadius(6);
            border.Padding = new Thickness(12, 8, 12, 8);
            
            // 代码文本
            var codeText = new System.Windows.Documents.Run();
            codeText.FontFamily = new FontFamily("Consolas");
            codeText.FontSize = 13;
            codeText.Foreground = (Brush)Application.Current.TryFindResource("Text");
            
            // 获取代码内容
            if (code is FencedCodeBlock fencedCode)
            {
                codeText.Text = string.Join("\n", fencedCode.Lines);
            }
            else
            {
                codeText.Text = string.Join("\n", code.Lines);
            }
            
            var paragraph = new System.Windows.Documents.Paragraph(codeText);
            paragraph.Margin = new Thickness(0);
            
            var flowDoc = new System.Windows.Documents.FlowDocument(paragraph);
            var docViewer = new FlowDocumentScrollViewer();
            docViewer.Document = flowDoc;
            border.Child = docViewer;
            
            // 复制按钮（悬停显示）
            var copyButton = new Button();
            copyButton.Content = "Copy";
            copyButton.Style = (Style)Application.Current.TryFindResource("WindowButtonStyle");
            copyButton.HorizontalAlignment = HorizontalAlignment.Right;
            copyButton.VerticalAlignment = VerticalAlignment.Top;
            copyButton.Margin = new Thickness(0, 4, 4, 0);
            copyButton.Visibility = Visibility.Collapsed;
            copyButton.Click += (s, e) =>
            {
                Clipboard.SetText(codeText.Text);
                copyButton.Content = "Copied!";
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(2);
                timer.Tick += (s2, e2) =>
                {
                    copyButton.Content = "Copy";
                    timer.Stop();
                };
                timer.Start();
            };
            
            var grid = new Grid();
            grid.Children.Add(border);
            grid.Children.Add(copyButton);
            
            // 鼠标悬停显示 Copy 按钮
            grid.MouseEnter += (s, e) => copyButton.Visibility = Visibility.Visible;
            grid.MouseLeave += (s, e) => copyButton.Visibility = Visibility.Collapsed;
            
            section.Blocks.Add(new System.Windows.Documents.BlockUIContainer(grid));
            Document.Blocks.Add(section);
        }
        
        private void RenderList(ListBlock list)
        {
            var listElement = new System.Windows.Documents.List();
            listElement.Margin = new Thickness(0, 0, 0, 12);
            
            if (list.IsOrdered)
            {
                listElement.MarkerStyle = System.Windows.TextMarkerStyle.Decimal;
            }
            else
            {
                listElement.MarkerStyle = System.Windows.TextMarkerStyle.Disc;
            }
            
            foreach (var item in list)
            {
                if (item is ListItemBlock listItem)
                {
                    var listItemElement = new System.Windows.Documents.ListItem();
                    
                    foreach (var block in listItem)
                    {
                        if (block is ParagraphBlock paragraph)
                        {
                            var paragraphElement = new System.Windows.Documents.Paragraph();
                            paragraphElement.Margin = new Thickness(0, 0, 0, 4);
                            
                            foreach (var inline in paragraph.Inline!)
                            {
                                RenderInline(inline, paragraphElement);
                            }
                            
                            listItemElement.Blocks.Add(paragraphElement);
                        }
                    }
                    
                    listElement.ListItems.Add(listItemElement);
                }
            }
            
            Document.Blocks.Add(listElement);
        }
        
        private void RenderHorizontalRule()
        {
            var paragraph = new System.Windows.Documents.Paragraph();
            paragraph.Margin = new Thickness(0, 12, 0, 12);
            
            var line = new System.Windows.Documents.Run("────────────────────────────────────────");
            line.Foreground = (Brush)Application.Current.TryFindResource("Border");
            
            paragraph.Inlines.Add(line);
            Document.Blocks.Add(paragraph);
        }
        
        private void RenderQuote(QuoteBlock quote)
        {
            var section = new System.Windows.Documents.Section();
            section.Margin = new Thickness(0, 0, 0, 12);
            
            var border = new Border();
            border.Background = (Brush)Application.Current.TryFindResource("Surface");
            border.BorderBrush = (Brush)Application.Current.TryFindResource("Accent");
            border.BorderThickness = new Thickness(4, 0, 0, 0);
            border.Padding = new Thickness(12, 8, 12, 8);
            
            var flowDocument = new System.Windows.Documents.FlowDocument();
            flowDocument.Background = Brushes.Transparent;
            
            foreach (var block in quote)
            {
                if (block is ParagraphBlock paragraph)
                {
                    var paragraphElement = new System.Windows.Documents.Paragraph();
                    paragraphElement.Margin = new Thickness(0, 0, 0, 4);
                    
                    foreach (var inline in paragraph.Inline!)
                    {
                        RenderInline(inline, paragraphElement);
                    }
                    
                    flowDocument.Blocks.Add(paragraphElement);
                }
            }
            
            var docViewer = new FlowDocumentScrollViewer();
            docViewer.Document = flowDocument;
            border.Child = docViewer;
            
            section.Blocks.Add(new System.Windows.Documents.BlockUIContainer(border));
            Document.Blocks.Add(section);
        }
        
        private void RenderInline(Markdig.Syntax.Inlines.Inline inline, System.Windows.Documents.Paragraph paragraph)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    var run = new System.Windows.Documents.Run(literal.Content.ToString());
                    paragraph.Inlines.Add(run);
                    break;
                
                case EmphasisInline emphasis:
                    var emphasisRun = new System.Windows.Documents.Run();
                    foreach (var child in emphasis)
                    {
                        if (child is LiteralInline childLiteral)
                        {
                            emphasisRun.Text += childLiteral.Content.ToString();
                        }
                    }
                    
                    if (emphasis.DelimiterCount == 2)
                    {
                        emphasisRun.FontWeight = FontWeights.Bold;
                    }
                    else if (emphasis.DelimiterCount == 1)
                        emphasisRun.FontStyle = FontStyles.Italic;
                    
                    paragraph.Inlines.Add(emphasisRun);
                    break;
                
                case CodeInline code:
                    var codeRun = new System.Windows.Documents.Run(code.Content);
                    codeRun.FontFamily = new FontFamily("Consolas");
                    codeRun.Background = (Brush)Application.Current.TryFindResource("Surface");
                    codeRun.Foreground = (Brush)Application.Current.TryFindResource("Text");
                    paragraph.Inlines.Add(codeRun);
                    break;
                
                case LinkInline link:
                    var linkRun = new System.Windows.Documents.Run();
                    foreach (var child in link)
                    {
                        if (child is LiteralInline childLiteral)
                        {
                            linkRun.Text += childLiteral.Content.ToString();
                        }
                    }
                    
                    linkRun.Foreground = (Brush)Application.Current.TryFindResource("Accent");
                    linkRun.TextDecorations = TextDecorations.Underline;
                    linkRun.Cursor = System.Windows.Input.Cursors.Hand;
                    
                    // 点击链接
                    linkRun.MouseDown += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(link.Url))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = link.Url,
                                UseShellExecute = true
                            });
                        }
                    };
                    
                    paragraph.Inlines.Add(linkRun);
                    break;
            }
        }
    }
}
