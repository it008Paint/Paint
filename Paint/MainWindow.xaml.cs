using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace Paint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Point startpoint;
        string? selectedshape = null;
        Shape? drawshape = null;
        public MainWindow()
        {
            InitializeComponent();
            currentshape.Shape += (text) =>
            {
                selectedshape = text;
            };
        }
        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point startpoint = e.GetPosition(canvas);
            createshape(startpoint);
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (drawshape == null) return;
            Point secondpoint = e.GetPosition(canvas);
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
            {
                setposition(secondpoint);
            }
        }

        private void canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            drawshape = null;
        }
        private void createshape(Point startpoint)
        {
            switch (selectedshape)
            {
                case "Line":
                    drawshape = new Line();
                    ((Line)drawshape).X1 = startpoint.X;
                    ((Line)drawshape).Y1 = startpoint.Y;
                    break;
                case "Wavesquare":

                    break;
                case "Circle":
                    
                    break;
                case "Square":
                    drawshape = new Polygon();
                    ((Polygon)drawshape).Points = new PointCollection();
                    ((Polygon)drawshape).Points.Add(new Point(startpoint.X,startpoint.Y));
                    ((Polygon)drawshape).Points.Add(new Point(startpoint.X, startpoint.Y));
                    ((Polygon)drawshape).Points.Add(new Point(startpoint.X, startpoint.Y));
                    ((Polygon)drawshape).Points.Add(new Point(startpoint.X, startpoint.Y));
                    break;
                case "Play":

                    break;
                case "Diamond":

                    break;
                case "Star":

                    break;
                case "ArrowRight":

                    break;
            }
            if (drawshape != null)
            {
                drawshape.StrokeThickness = 5;
                drawshape.Stroke = Brushes.Black;
                canvas.Children.Add(drawshape);
            }
        }
        private void setposition(Point secondpoint)
        {
            switch (selectedshape)
            {
                case "Line":
                    ((Line)drawshape).X2 = secondpoint.X;
                    ((Line)drawshape).Y2 = secondpoint.Y;
                    break;
                case "Wavesquare":

                    break;
                case "Circle":

                    break;
                case "Square":
                    
                    break;
                case "Play":

                    break;
                case "Diamond":

                    break;
                case "Star":

                    break;
                case "ArrowRight":

                    break;
            }
        }
        void UpdatePoint(Polygon poly, int index, double x, double y)
        {
            Point p = poly.Points[index];
            p.X = x;
            p.Y = y;
            poly.Points[index] = p;
        }
    }
}