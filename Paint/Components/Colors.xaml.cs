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
using System.Windows.Forms;

namespace Paint.Components
{
    /// <summary>
    /// Interaction logic for Colors.xaml
    /// </summary>
    public partial class Colors : System.Windows.Controls.UserControl
    {
        public event Action<SolidColorBrush> Color;
        public Colors()
        {
            InitializeComponent();
        }

        private void black_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Black);
            crcolor.Fill = Brushes.Black;
        }
        private void white_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.White);
            crcolor.Fill = Brushes.White;
        }
        private void darkred_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.DarkRed);
            crcolor.Fill = Brushes.DarkRed;
        }
        private void red_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Red);
            crcolor.Fill = Brushes.Red;
        }
        private void orange_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Orange);
            crcolor.Fill = Brushes.Orange;
        }
        private void yellow_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Yellow);
            crcolor.Fill = Brushes.Yellow;
        }
        private void green_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Green);
            crcolor.Fill = Brushes.Green;
        }
        private void aqua_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Aqua);
            crcolor.Fill = Brushes.Aqua;
        }
        private void blue_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Blue);
            crcolor.Fill = Brushes.Blue;
        }
        private void pink_Click(object sender, RoutedEventArgs e)
        {
            Color?.Invoke(Brushes.Pink);
            crcolor.Fill = Brushes.Pink;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog color = new ColorDialog();
            if (color.ShowDialog() == DialogResult.OK)
            {
                System.Windows.Media.Color co = System.Windows.Media.Color.FromArgb(color.Color.A, color.Color.R, color.Color.G, color.Color.B);
                crcolor.Fill = new SolidColorBrush(co);
                Color?.Invoke(new SolidColorBrush(co));
            }
        }
    }
}
