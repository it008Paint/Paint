using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Paint.Components
{
    public partial class Selection : UserControl
    {
        public static bool IsSelectionMode { get; private set; } = false;

        public event Action<bool>? SelectionChanged;

        public Selection()
        {
            InitializeComponent();
        }

        private void SelectBtn_Click(object sender, RoutedEventArgs e)
        {
            IsSelectionMode = !IsSelectionMode;

            Root.Background = IsSelectionMode
                ? new SolidColorBrush(Color.FromRgb(180, 220, 255))
                : new SolidColorBrush(Color.FromRgb(240, 240, 240));

            SelectionChanged?.Invoke(IsSelectionMode);
        }
    }
}

