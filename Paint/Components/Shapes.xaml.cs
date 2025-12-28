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
        public bool Selected = false;
        public Shapes()
        {
            InitializeComponent();
        }

        private void Line_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            Shape?.Invoke("Line");
            Deselect();
            Button button = sender as Button;
            button.BorderBrush = Brushes.Blue;
            button.BorderThickness = new Thickness(3);
        }

        private void Wavesquare_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            Shape?.Invoke("Wavesquare");
            Deselect();
            Button button = sender as Button;
            button.BorderBrush = Brushes.Blue;
            button.BorderThickness = new Thickness(3);
        }

        private void Circle_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            Shape?.Invoke("Circle");
            Deselect();
            Button button = sender as Button;
            button.BorderBrush = Brushes.Blue;
            button.BorderThickness = new Thickness(3);
        }

        private void Square_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            Shape?.Invoke("Square");
            Deselect();
            Button button = sender as Button;
            button.BorderBrush = Brushes.Blue;
            button.BorderThickness = new Thickness(3);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            Shape?.Invoke("Play");
            Deselect();
            Button button = sender as Button;
            button.BorderBrush = Brushes.Blue;
            button.BorderThickness = new Thickness(3);
        }

        private void Diamond_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            Shape?.Invoke("Diamond");
            Deselect();
            Button button = sender as Button;
            button.BorderBrush = Brushes.Blue;
            button.BorderThickness = new Thickness(3);
        }

        private void Star_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            Shape?.Invoke("Star");
            Deselect();
            Button button = sender as Button;
            button.BorderBrush = Brushes.Blue;
            button.BorderThickness = new Thickness(3);
        }

        private void ArrowRight_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            Shape?.Invoke("ArrowRight");
            Deselect();
            Button button = sender as Button;
            button.BorderBrush = Brushes.Blue;
            button.BorderThickness = new Thickness(3);
        }

        public void Deselect()
        {
            foreach (var child in shape.Children)
            {
                if (child is Button button)
                {
                    button.BorderBrush = Brushes.Black;
                    button.BorderThickness = new Thickness(1);
                }
            }
            var main = Window.GetWindow(this) as global::Paint.MainWindow;
            if (main != null)
            {
                if (main.FindName("SimpleToolsRef") is SimpleTools tool)
                {
                    if (Selected && tool.Selected)
                    {
                        tool.Selected = false;
                        tool.Deselect();
                    }   
                }
            }
        }
    }
}
