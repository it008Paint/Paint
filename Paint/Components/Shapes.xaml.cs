using System;
using System.Collections.Generic;
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
    /// Interaction logic for Shapes.xaml
    /// </summary>
    public partial class Shapes : UserControl
    {
        public event Action<string> Shape;
        public Shapes()
        {
            InitializeComponent();
        }

        private void Line_Click(object sender, RoutedEventArgs e)
        {
            Shape?.Invoke("Line");
        }

        private void Wavesquare_Click(object sender, RoutedEventArgs e)
        {
            Shape?.Invoke("Wavesquare");
        }

        private void Circle_Click(object sender, RoutedEventArgs e)
        {
            Shape?.Invoke("Circle");
        }

        private void Square_Click(object sender, RoutedEventArgs e)
        {
            Shape?.Invoke("Square");
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            Shape?.Invoke("Play");
        }

        private void Diamond_Click(object sender, RoutedEventArgs e)
        {
            Shape?.Invoke("Diamond");
        }

        private void Star_Click(object sender, RoutedEventArgs e)
        {
            Shape?.Invoke("Star");
        }

        private void ArrowRight_Click(object sender, RoutedEventArgs e)
        {
            Shape?.Invoke("ArrowRight");
        }
    }
}
