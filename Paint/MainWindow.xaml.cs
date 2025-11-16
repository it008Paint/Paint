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
//using System.Drawing;

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
        SolidColorBrush currcolor = Brushes.Black;
        int clickcountbezier = 2;

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
                clickcountbezier = 2;
            };
            currentcolor.Color += (color) =>
            {
                currcolor = color;
                currentColor = color;
            };

            SimpleToolsRef.ToolSelected += (tool) =>
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
            startpoint = e.GetPosition(PaintSurface);

            if (selectedshape == "Pencil")
            {
                isDrawingPencil = true;
                currentPolyline = new Polyline
                {
                    Stroke = currentColor,
                    StrokeThickness = thicknessslider.Value,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeLineJoin = PenLineJoin.Round
                };
                currentPolyline.Points = new PointCollection { startpoint };
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

            // --- Nếu đang vẽ Pencil ---
            if (isDrawingPencil && e.LeftButton == MouseButtonState.Pressed)
            {
                currentPolyline?.Points.Add(currentPoint);
                return;
            }

            // --- Nếu đang vẽ hình khác ---
            if (drawshape != null && e.LeftButton == MouseButtonState.Pressed)
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

            if (drawshape is Path && clickcountbezier == 2 || drawshape is not Path)
            {
                drawshape = null;
            }
        }

        private void createshape(Point startpoint)
        {
            // Sử dụng using System.Windows.Shapes;
            // Sử dụng using System.Windows.Media;

            switch (selectedshape)
            {
                case "Line":
                    drawshape = new Line();
                    break;

                case "Square":
                    drawshape = new Rectangle();
                    break;

                case "Circle":
                    drawshape = new Ellipse();
                    break;

                case "Wavesquare":
                    if (clickcountbezier == 2)
                    {
                        clickcountbezier = 0;
                        drawshape = new Path();
                    }
                    else clickcountbezier++;
                    break;

                case "Play":
                    drawshape = new Polygon();
                    ((Polygon)drawshape).Points = new PointCollection();
                    break;

                case "Diamond": 
                    drawshape = new Polygon();
                    ((Polygon)drawshape).Points = new PointCollection();
                    break;

                case "Star":
                    drawshape = new Polygon();
                    ((Polygon)drawshape).Points = new PointCollection();
                    break;

                case "ArrowRight":
                    drawshape = new Polygon();
                    ((Polygon)drawshape).Points = new PointCollection();
                    break;
            }

            if (drawshape != null&&((drawshape is Path && clickcountbezier==0)||drawshape is not Path))
            {
                drawshape.Stroke = currcolor;
                drawshape.StrokeThickness = thicknessslider.Value;
                drawshape.StrokeThickness = 3;
                drawshape.Fill = Brushes.Transparent;
                PaintSurface.Children.Add(drawshape);
            }
        }
        private void setposition(Point secondpoint)
        {
            double x, y, w, h;
            x = Math.Min(startpoint.X, secondpoint.X);
            y = Math.Min(startpoint.Y, secondpoint.Y);
            w = Math.Abs(startpoint.X - secondpoint.X);
            h = Math.Abs(startpoint.Y - secondpoint.Y);
            switch (selectedshape)
            {
                case "Line":
                    ((Line)drawshape).X1 = startpoint.X;
                    ((Line)drawshape).Y1 = startpoint.Y;
                    ((Line)drawshape).X2 = secondpoint.X;
                    ((Line)drawshape).Y2 = secondpoint.Y;
                    break;

                case "Square":
                    ((Rectangle)drawshape).Width = w;
                    ((Rectangle)drawshape).Height = h;
                    Canvas.SetTop(drawshape, y);
                    Canvas.SetLeft(drawshape, x);
                    break;

                case "Circle":
                    ((Ellipse)drawshape).Width = w;
                    ((Ellipse)drawshape).Height = h;
                    Canvas.SetTop(drawshape, y);
                    Canvas.SetLeft(drawshape, x);
                    break;

                case "Wavesquare":
                    if (clickcountbezier == 0)
                    {
                        PathGeometry geo = new PathGeometry();
                        PathFigure fig = new PathFigure();
                        BezierSegment bez = new BezierSegment();
                        fig.StartPoint = startpoint;
                        bez.Point1 = startpoint;
                        bez.Point2 = secondpoint;
                        bez.Point3 = secondpoint;
                        fig.Segments.Add(bez);
                        geo.Figures.Add(fig);
                        ((Path)drawshape).Data = geo;
                    }else
                    {
                        bezierpoint(secondpoint, clickcountbezier);
                    }
                    break;

                case "Play":
                    PointCollection point = new PointCollection();
                    point.Add(new Point(x, y));
                    point.Add(new Point(x+w,y+h/2));
                    point.Add(new Point(x, y + h));
                    ((Polygon)drawshape).Points = point;
                    break;

                case "Diamond":
                    PointCollection point1 = new PointCollection();
                    point1.Add(new Point(x, y));
                    point1.Add(new Point(x + w, y + h / 2));
                    point1.Add(new Point(x, y + h));
                    point1.Add(new Point(x - w, y + h/2));
                    ((Polygon)drawshape).Points = point1;
                    break;

                case "Star":
                    PointCollection point2 = new PointCollection();
                    point2.Add(new Point(x, y));
                    point2.Add(new Point(x - 0.25 * w, y + 0.3 * h));
                    point2.Add(new Point(x - 0.6 * w, y + 0.3 * h));
                    point2.Add(new Point(x - 0.3 * w, y + 0.6 * h));
                    point2.Add(new Point(x - 0.45 * w, y + h));
                    point2.Add(new Point(x, y + 0.8 * h));
                    point2.Add(new Point(x + 0.45 * w, y + h));
                    point2.Add(new Point(x + 0.3 * w, y + 0.6 * h));
                    point2.Add(new Point(x + 0.6 * w, y + 0.3 * h));
                    point2.Add(new Point(x + 0.25 * w, y + 0.3 * h));
                    ((Polygon)drawshape).Points = point2;
                    break;

                case "ArrowRight":
                    PointCollection point3 = new PointCollection();
                    point3.Add(new Point(x, y));
                    point3.Add(new Point(x + w, y));
                    point3.Add(new Point(x + 0.8 * w, y - 0.5 * h));
                    point3.Add(new Point(x + w, y));
                    point3.Add(new Point(x + 0.8 * w, y + 0.5 * h));
                    point3.Add(new Point(x + w, y));
                    ((Polygon)drawshape).Points = point3;
                    break;
            }
        }
        private void bezierpoint(Point point,int clicknum)
        {
            PathGeometry geo = ((Path)drawshape).Data as PathGeometry;
            PathFigure fig = geo.Figures[0];
            BezierSegment bez = fig.Segments[0] as BezierSegment;
            if (clicknum == 1)
            {
                bez.Point1 = point;
            }
            else
            {
                bez.Point2 = point;
            }
        }
    }
}
