using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics; 
using System.Numerics; 
using System.Windows.Ink; 
using System.Windows.Media.Animation; 

namespace Paint
{
    /// <summary>
   
    /// </summary>
    public partial class MainWindow : Window
    {
        // --- Logic Vẽ Hình (Của bạn) ---
        Point startpoint;
        string? selectedshape = null;
        Shape? drawshape = null;

        private Polyline? currentPolyline = null;
        private bool isDrawingPencil = false;
        private Brush currentColor = Brushes.Black;
        private double currentThickness = 2;
        public string CurrentFilePath { get; set; }

        // --- Logic Zoom ---
        private ScaleTransform _zoomTransform;
        private double _minZoom = 0.25;
        private double _maxZoom = 2.0;

        // --- Logic Panning (Kéo chuột) ---
        private bool _isPanning = false;
        private Point _panStartPoint;
        public MainWindow()
        {
            InitializeComponent();

            // Khởi tạo logic Zoom
            _zoomTransform = new ScaleTransform(1.0, 1.0);
            PaintSurface.LayoutTransform = _zoomTransform;

            // Gắn sự kiện Zoom
            ZoomSlider.ValueChanged += ZoomSlider_ValueChanged;
            ZoomPreset.SelectionChanged += ZoomPreset_SelectionChanged;
            ZoomInButton.Click += ZoomInButton_Click;
            ZoomOutButton.Click += ZoomOutButton_Click;

            // Khởi tạo logic Vẽ hình (Của bạn)
            MainMenu.MainWindowRef = this;
            currentshape.Shape += (text) =>
            {
                selectedshape = text;
            };

            SimpleTools.ToolSelected += (tool) =>
            {
                selectedshape = tool;
            };
        }

        // --- Logic Xử lý Zoom ---

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _zoomTransform.ScaleX = e.NewValue;
            _zoomTransform.ScaleY = e.NewValue;
        }

        private void ZoomPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ZoomPreset.SelectedItem is ComboBoxItem item)
            {
                string text = item.Content.ToString().TrimEnd('%');
                if (double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out double percent))
                {
                    double scale = percent / 100.0;
                    scale = Math.Min(Math.Max(scale, _minZoom), _maxZoom);

                    _zoomTransform.ScaleX = scale;
                    _zoomTransform.ScaleY = scale;
                    ZoomSlider.Value = scale;
                }
            }
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            double step = ZoomSlider.Value * 0.1;
            double newValue = Math.Min(_maxZoom, ZoomSlider.Value + step);
            ZoomSlider.Value = newValue;
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            double step = ZoomSlider.Value * 0.1;
            double newValue = Math.Max(_minZoom, ZoomSlider.Value - step);
            ZoomSlider.Value = newValue;
        }

        // --- Logic Xử lý Panning (Kéo chuột phải) ---
        private void CanvasScrollViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _panStartPoint = e.GetPosition(CanvasScrollViewer);
                CanvasScrollViewer.CaptureMouse();
                CanvasScrollViewer.Cursor = Cursors.ScrollAll;
                e.Handled = true;
            }
        }

        private void CanvasScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                Point currentPoint = e.GetPosition(CanvasScrollViewer);
                double offsetX = _panStartPoint.X - currentPoint.X;
                double offsetY = _panStartPoint.Y - currentPoint.Y;

                CanvasScrollViewer.ScrollToHorizontalOffset(CanvasScrollViewer.HorizontalOffset + offsetX);
                CanvasScrollViewer.ScrollToVerticalOffset(CanvasScrollViewer.VerticalOffset + offsetY);

                _panStartPoint = currentPoint;
                e.Handled = true;
            }
        }

        private void CanvasScrollViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                CanvasScrollViewer.ReleaseMouseCapture();
                CanvasScrollViewer.Cursor = Cursors.Arrow;
                e.Handled = true;
            }
        }

        // --- Logic Xử lý Vẽ hình (Của bạn) ---

        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startpoint = e.GetPosition(PaintSurface); // Đã sửa lỗi khai báo biến cục bộ
            if (selectedshape == "Pencil")
            {
                isDrawingPencil = true;
                currentPolyline = new Polyline
                {
                    Stroke = currentColor,
                    StrokeThickness = currentThickness,
                    Points = new PointCollection { startpoint }
                };
                PaintSurface.Children.Add(currentPolyline);
            }
            else
            {
                createshape(startpoint);
            }
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPoint = e.GetPosition(PaintSurface);

            if (isDrawingPencil && e.LeftButton == MouseButtonState.Pressed)
            {
                currentPolyline?.Points.Add(currentPoint);
            }
            else if (drawshape != null && e.LeftButton == MouseButtonState.Pressed)
            {
                setposition(currentPoint);
            }
        }

        private void canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrawingPencil)
            {
                isDrawingPencil = false;
                currentPolyline = null;
            }
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
                    ((Polygon)drawshape).Points.Add(new Point(startpoint.X, startpoint.Y));
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
                PaintSurface.Children.Add(drawshape);
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
