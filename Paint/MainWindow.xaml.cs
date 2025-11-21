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
using System.Windows.Controls.Primitives;

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

        private TextBox? currentTextBox = null;
        public string CurrentFilePath { get; set; }
        public Stack<Action>? Undo { get; set; }
        public Stack<Action>? Redo { get; set; }

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
            Undo = new Stack<Action>();
            Redo = new Stack<Action>();
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
            
            if (selectedshape == "Fill")
            {
                Point p = e.GetPosition(PaintSurface);
                FillColorAtPoint((int)p.X, (int)p.Y);
                return;
            }

            if (selectedshape == "Text")
            {
                AddTextBoxAtPoint(startpoint);
                return;
            }

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
                Shape shape = CloneShape(currentPolyline);
                PaintSurface.Children.Remove(currentPolyline);
                PaintSurface.Children.Add(shape);
                UndoPush(shape);
                Redo.Clear();
                isDrawingPencil = false;
                currentPolyline = null;
            }

            if (drawshape is Path && clickcountbezier == 2 || drawshape is not Path)
            {
                if (drawshape == null) return;
                Shape shape = CloneShape(drawshape);
                double left = Canvas.GetLeft(drawshape);
                double top = Canvas.GetTop(drawshape);
                PaintSurface.Children.Remove(drawshape);
                PaintSurface.Children.Add(shape);
                Canvas.SetLeft(shape, left);
                Canvas.SetTop(shape, top);
                UndoPush(shape);
                Redo.Clear();
                drawshape = null;
            }
        }

        private void FillColorAtPoint(int x, int y)
        {
            int width = (int)PaintSurface.ActualWidth;
            int height = (int)PaintSurface.ActualHeight;

            if (width == 0 || height == 0)
                return;

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(PaintSurface);

            WriteableBitmap wb = new WriteableBitmap(rtb);

            Color newColor;
            if (currentColor is SolidColorBrush brush)
                newColor = brush.Color;
            else
                newColor = Colors.Black;

            int stride = wb.PixelWidth * (wb.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[wb.PixelHeight * stride];
            wb.CopyPixels(pixels, stride, 0);

            int index = (y * stride) + (x * 4);
            if (index < 0 || index >= pixels.Length)
                return;

            Color oldColor = Color.FromArgb(
                pixels[index + 3],
                pixels[index + 2],
                pixels[index + 1],
                pixels[index + 0]
            );

            if (oldColor == newColor)
                return;

            Queue<Point> q = new Queue<Point>();
            q.Enqueue(new Point(x, y));

            while (q.Count > 0)
            {
                Point pt = q.Dequeue();
                int px = (int)pt.X;
                int py = (int)pt.Y;

                if (px < 0 || py < 0 || px >= width || py >= height)
                    continue;

                int idx = (py * stride) + (px * 4);
                if (idx < 0 || idx >= pixels.Length)
                    continue;

                Color colorHere = Color.FromArgb(
                    pixels[idx + 3],
                    pixels[idx + 2],
                    pixels[idx + 1],
                    pixels[idx + 0]
                );

                if (colorHere != oldColor)
                    continue;

                pixels[idx + 0] = newColor.B;
                pixels[idx + 1] = newColor.G;
                pixels[idx + 2] = newColor.R;
                pixels[idx + 3] = newColor.A;

                q.Enqueue(new Point(px + 1, py));
                q.Enqueue(new Point(px - 1, py));
                q.Enqueue(new Point(px, py + 1));
                q.Enqueue(new Point(px, py - 1));
            }

            wb.WritePixels(
                new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight),
                pixels, stride, 0);

            System.Windows.Controls.Image img = new System.Windows.Controls.Image();
            img.Source = wb;
            PaintSurface.Children.Add(img);

            UndoPushImage(img);
        }

        private void UndoPushImage(System.Windows.Controls.Image img)
        {
            Undo.Push(() =>
            {
                PaintSurface.Children.Remove(img);
                Redo.Push(() =>
                {
                    PaintSurface.Children.Add(img);
                    UndoPushImage(img);
                });
            });
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
        private Shape CloneShape(Shape x)
        {
            if (x is Line line)
            {
                Shape shape = new Line
                {
                    X1 = line.X1,
                    Y1 = line.Y1,
                    X2 = line.X2,
                    Y2 = line.Y2,
                    Stroke = line.Stroke,
                    StrokeThickness = line.StrokeThickness
                };
                return shape;
            }
            else if(x is Ellipse ellipse)
            {
                Shape shape = new Ellipse
                {
                    Width = ellipse.Width,
                    Height = ellipse.Height,
                    Stroke = ellipse.Stroke,
                    StrokeThickness = ellipse.StrokeThickness,
                    Fill = ellipse.Fill
                };
                return shape;
            }
            else if (x is Rectangle rectangle)
            {
                Shape shape = new Rectangle
                {
                    Width = rectangle.Width,
                    Height = rectangle.Height,
                    Stroke = rectangle.Stroke,
                    StrokeThickness = rectangle.StrokeThickness,
                    Fill = rectangle.Fill
                };
                return shape;
            }
            else if (x is Polygon polygon)
            {
                Shape shape = new Polygon
                {
                    Points = polygon.Points,
                    Stroke = polygon.Stroke,
                    StrokeThickness = polygon.StrokeThickness,
                    Fill = polygon.Fill
                };
                return shape;
            }
            else if (x is Polyline polyline)
            {
                Shape shape = new Polyline
                {
                    Points = polyline.Points,
                    Stroke = polyline.Stroke,
                    StrokeThickness = polyline.StrokeThickness
                };
                return shape;
            }
            else if (x is Path path)
            {
                Shape shape = new Path
                {
                    Data = path.Data,
                    Stroke = path.Stroke,
                    StrokeThickness = path.StrokeThickness
                };
                return shape;
            }
            return x;
        }

        private void AddTextBoxAtPoint(Point position)
        {
            if (currentTextBox != null)
                return;

            currentTextBox = new TextBox
            {
                Width = 150,
                Height = 30,
                FontSize = 16,
                Background = Brushes.Transparent,
                Foreground = currentColor,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                AcceptsReturn = true
            };

            Canvas.SetLeft(currentTextBox, position.X);
            Canvas.SetTop(currentTextBox, position.Y);

            PaintSurface.Children.Add(currentTextBox);
            currentTextBox.Focus();

            // Tạo Thumb để resize
            Thumb resizeThumb = new Thumb
            {
                Width = 10,
                Height = 10,
                Background = Brushes.Gray,
                Cursor = Cursors.SizeNWSE
            };
            Canvas.SetLeft(resizeThumb, position.X + currentTextBox.Width - 5);
            Canvas.SetTop(resizeThumb, position.Y + currentTextBox.Height - 5);
            PaintSurface.Children.Add(resizeThumb);

            resizeThumb.DragDelta += (s, e) =>
            {
                currentTextBox.Width = Math.Max(30, currentTextBox.Width + e.HorizontalChange);
                currentTextBox.Height = Math.Max(20, currentTextBox.Height + e.VerticalChange);

                Canvas.SetLeft(resizeThumb, Canvas.GetLeft(currentTextBox) + currentTextBox.Width - resizeThumb.Width / 2);
                Canvas.SetTop(resizeThumb, Canvas.GetTop(currentTextBox) + currentTextBox.Height - resizeThumb.Height / 2);
            };

            currentTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    FinalizeTextBox();
                    PaintSurface.Children.Remove(resizeThumb);
                    e.Handled = true;
                }
            };
        }

        private void FinalizeTextBox()
        {
            if (currentTextBox == null)
                return;

            string text = currentTextBox.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                TextBlock tb = new TextBlock
                {
                    Text = text,
                    FontSize = currentTextBox.FontSize,
                    Foreground = currentColor
                };

                double left = Canvas.GetLeft(currentTextBox);
                double top = Canvas.GetTop(currentTextBox);

                PaintSurface.Children.Remove(currentTextBox);
                PaintSurface.Children.Add(tb);
                Canvas.SetLeft(tb, left);
                Canvas.SetTop(tb, top);

                UndoPush(tb);
            }
            else
            {
                PaintSurface.Children.Remove(currentTextBox);
            }

            currentTextBox = null;
        }

        private void UndoPush(UIElement element)
        {
            double left = Canvas.GetLeft(element);
            double top = Canvas.GetTop(element);

            Undo.Push(() =>
            {
                PaintSurface.Children.Remove(element);
                Redo.Push(() =>
                {
                    PaintSurface.Children.Add(element);
                    Canvas.SetLeft(element, left);
                    Canvas.SetTop(element, top);
                    UndoPush(element);
                });
            });
        }
      

private void CanvasScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
{
    if (Keyboard.Modifiers == ModifierKeys.Control)
    {
        e.Handled = true;
        Point mousePos = e.GetPosition(PaintSurface);
        Point mouseView = e.GetPosition(CanvasScrollViewer);
        double zoomFactor = 0.1; 
        if (e.Delta < 0)
        {
            zoomFactor = -0.1;
        }
        double newScale = ZoomSlider.Value + zoomFactor;
        newScale = Math.Max(_minZoom, Math.Min(_maxZoom, newScale));
        if (newScale == ZoomSlider.Value) return;
        ZoomSlider.Value = newScale;
        double newOffsetX = (mousePos.X * newScale) - mouseView.X;
        double newOffsetY = (mousePos.Y * newScale) - mouseView.Y;

        CanvasScrollViewer.ScrollToHorizontalOffset(newOffsetX);
        CanvasScrollViewer.ScrollToVerticalOffset(newOffsetY);
    }
}
    }
}
