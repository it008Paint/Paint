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
using Microsoft.Win32;

namespace Paint.Components
{
    /// <summary>
    /// Interaction logic for Menu_bar.xaml
    /// </summary>
    public partial class Menu_bar : UserControl
    {
        public MainWindow MainWindowRef { get; set; }
        public Menu_bar()
        {
            InitializeComponent();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindowRef.CurrentFilePath != null)
                FileManager.SaveCanvasToJson(MainWindowRef.PaintSurface, MainWindowRef.CurrentFilePath);
            MainWindowRef.PaintSurface.Children.Clear();
            MainWindowRef.CurrentFilePath = null;
        }
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Paint project (*.json)|*.json" };
            if (dlg.ShowDialog() == true)
            {
                FileManager.LoadCanvasFromJson(MainWindowRef.PaintSurface, dlg.FileName);
                MainWindowRef.CurrentFilePath = dlg.FileName;
            }
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MainWindowRef.CurrentFilePath))
            {
                SaveAs_Click(sender, e);
                return;
            }
            FileManager.SaveCanvasToJson(MainWindowRef.PaintSurface, MainWindowRef.CurrentFilePath);
        }
        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "Paint project (*.json)|*.json" };
            if (dlg.ShowDialog() == true)
            {
                FileManager.SaveCanvasToJson(MainWindowRef.PaintSurface, dlg.FileName);
                MainWindowRef.CurrentFilePath = dlg.FileName;
            }
        }
        private void Import_Click(object sender, RoutedEventArgs e)
        {
            FileManager.ImportImageToCanvas(MainWindowRef.PaintSurface);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "PNG image (*.png)|*.png|JPEG image (*.jpeg)|*.jpeg" };
            if (dlg.ShowDialog() == true)
            {
                FileManager.ExportCanvasToPng(MainWindowRef.PaintSurface, dlg.FileName);
            }
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            Action action;
            if (MainWindowRef.Undo.TryPop(out action) == false) return;
            action.Invoke();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            Action action;
            if (MainWindowRef.Redo.TryPop(out action) == false) return;
            action.Invoke();
        }
    }
}
