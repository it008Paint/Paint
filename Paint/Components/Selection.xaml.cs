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
    /// Interaction logic for Selection.xaml
    /// </summary>
    public partial class Selection : UserControl
    {
        public event Action<string>? ToolSelected;
        public bool Selected = false;

        public Selection()
        {
            InitializeComponent();
        }

        
        private void SelectCursorButton_Click(object sender, RoutedEventArgs e)
        {
            Selected = true;
            ToolSelected?.Invoke("SelectTool"); 

            Deselect(); 

            
            Button button = sender as Button;
            if (button != null)
            {
                button.BorderBrush = Brushes.Blue;
                button.BorderThickness = new Thickness(3);
            }
        }

       
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            
            Selected = true;
            ToolSelected?.Invoke("DeleteTool"); 
            Deselect();

            Button button = sender as Button;
            if (button != null)
            {
                button.BorderBrush = Brushes.Red;
                button.BorderThickness = new Thickness(3);
            }
        }
        public void Deselect()
        {
            
            foreach (var child in selectionTools.Children)
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
                
                if (main.FindName("simpleTools") is SimpleTools simpleTools)
                {
                    if (simpleTools.Selected)
                    {
                        simpleTools.Selected = false;
                        simpleTools.Deselect();
                    }
                }

                
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
