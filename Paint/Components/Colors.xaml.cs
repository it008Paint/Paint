using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Paint.Components
{
    /// <summary>
    /// Interaction logic for Colors.xaml
    /// </summary>
    public partial class Colors : UserControl
    {
        public event Action<SolidColorBrush> Color;
        public Colors()
        {
            InitializeComponent();
        }

        private void black_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Black);
        }
        private void white_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.White);
        }
        private void darkred_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.DarkRed);
        }
        private void red_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Red);
        }
        private void orange_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Orange);
        }
        private void yellow_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Yellow);
        }
        private void green_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Green);
        }
        private void aqua_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Aqua);
        }
        private void blue_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Blue);
        }
        private void pink_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Pink);
        }
    }
}
