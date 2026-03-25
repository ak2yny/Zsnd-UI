using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Zsnd_UI.Controls
{
    public sealed partial class BorderedTextBlock : UserControl
    {
        public BorderedTextBlock()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register( nameof(Text), typeof(string),
                typeof(BorderedTextBlock), new PropertyMetadata(string.Empty));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
    }
}
