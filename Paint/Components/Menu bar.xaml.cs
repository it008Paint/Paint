using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Paint.Components
{
    /// <summary>
    /// Interaction logic for Menu_bar.xaml
    /// </summary>
    public partial class Menu_bar : System.Windows.Controls.UserControl
    {
        public MainWindow MainWindowRef { get; set; }
        public Menu_bar()
        {
            InitializeComponent();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindowRef.CurrentFilePath != null)
            {
                if (MainWindowRef.Undo.Count != 0)
                {
                    MessageBoxResult result = System.Windows.MessageBox.Show(
                        "Do you want to save?","",MessageBoxButton.YesNo   
                    );
                    if (result == MessageBoxResult.Yes)
                    {
                        FileManager.SaveCanvasToJson(MainWindowRef, MainWindowRef.CurrentFilePath);
                    }
                }
            }
                
            MainWindowRef.PaintSurface.Children.Clear();
            MainWindowRef.CurrentFilePath = null;
            MainWindowRef.Undo.Clear();
            MainWindowRef.Redo.Clear();
        }
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Paint project (*.json)|*.json" };
            if (dlg.ShowDialog() == true)
            {
                FileManager.LoadCanvasFromJson(MainWindowRef, dlg.FileName);
                MainWindowRef.CurrentFilePath = dlg.FileName;
                MainWindowRef.LayersListBox.SelectedIndex = 0;
                MainWindowRef.Undo.Clear();
                MainWindowRef.Redo.Clear();
            }
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MainWindowRef.CurrentFilePath))
            {
                SaveAs_Click(sender, e);
                return;
            }
            FileManager.SaveCanvasToJson(MainWindowRef, MainWindowRef.CurrentFilePath);
        }
        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Paint project (*.json)|*.json" };
            if (dlg.ShowDialog() == true)
            {
                FileManager.SaveCanvasToJson(MainWindowRef, dlg.FileName);
                MainWindowRef.CurrentFilePath = dlg.FileName;
            }
        }
        private void Import_Click(object sender, RoutedEventArgs e)
        {
            FileManager.ImportImageToCanvas(MainWindowRef);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            MainWindowRef.CanvasScrollViewer.ScrollToHorizontalOffset(0);
            MainWindowRef.CanvasScrollViewer.ScrollToVerticalOffset(0);
            MainWindowRef.ZoomPreset.SelectedIndex = 1;
            MainWindowRef.ZoomPreset.SelectedIndex = 3;
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "PNG image (*.png)|*.png|JPEG image (*.jpeg)|*.jpeg" };
            if (dlg.ShowDialog() == true)
            {
                FileManager.ExportCanvasToPng(MainWindowRef.PaintSurface, dlg.FileName);
            }
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (!MainWindowRef.CurrentLayer.IsVisible) return;
            Action action;
            if (MainWindowRef.CurrentLayer.Undo.TryPop(out action) == false) return;
            action.Invoke();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (!MainWindowRef.CurrentLayer.IsVisible) return;
            Action action;
            if (MainWindowRef.CurrentLayer.Redo.TryPop(out action) == false) return;
            action.Invoke();
        }

        private void ClearCanvas_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindowRef == null) return;
            if (!MainWindowRef.CurrentLayer.IsVisible) return;

            foreach (var element in MainWindowRef.CurrentLayer.Elements)
            {
                MainWindowRef.PaintSurface.Children.Remove(element);
            }
            MainWindowRef.CurrentLayer.Elements.Clear();
            MainWindowRef.CurrentLayer.Undo.Clear();
            MainWindowRef.CurrentLayer.Redo.Clear();
        }
    }
}
