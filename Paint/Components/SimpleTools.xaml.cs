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
    /// Interaction logic for SimpleTools.xaml
    /// </summary>

   

    public partial class SimpleTools : UserControl
    {
        public event Action<string>? ToolSelected;
        public bool Selected = false;
        public SimpleTools()
        {
            InitializeComponent();
        }
        private void SelectionButton_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            ToolSelected?.Invoke("Selection");

            Deselect();
            Button button = sender as Button;
            button.BorderBrush = Brushes.Blue;
            button.BorderThickness = new Thickness(3);
        }

        private void PencilButton_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            ToolSelected?.Invoke("Pencil");
            Deselect();
            Button button = sender as Button;
            button.BorderBrush = Brushes.Blue;
            button.BorderThickness = new Thickness(3);
        }

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            ToolSelected?.Invoke("Fill");
            Deselect();
            Button button = sender as Button;
            button.BorderBrush = Brushes.Blue;
            button.BorderThickness = new Thickness(3);
        }

        private void TextButton_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            ToolSelected?.Invoke("Text");
            Deselect();

            Button button = sender as Button;
            button.BorderBrush = Brushes.Blue;
            button.BorderThickness = new Thickness(3);
        }

        private void Eraser_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            ToolSelected?.Invoke("Eraser");
            Deselect();
            Button button = sender as Button;
            button.BorderBrush = Brushes.Blue;
            button.BorderThickness = new Thickness(3);
        }
        public void Deselect()
        {
            foreach (var child in tool.Children)
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
                // Ensure the Shapes control in MainWindow.xaml has x:Name="currentshape"
                if (main.FindName("currentshape") is Shapes shapes)
                {
                    if (shapes.Selected)
                    {
                        shapes.Selected = false;
                        shapes.Deselect();
                    }
                }
            }
        }
    }
}
