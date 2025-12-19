using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics; 
using System.Globalization;
using System.Numerics; 
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
//using System.Drawing;

namespace Paint
{
    /// <summary>

    /// </summary>

   

    public partial class MainWindow : Window
    {
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && selectedshape == "Selection")
            {
                DeleteSelection();
                e.Handled = true;
            }
        }

        private void DeleteSelection()
        {
            if (_selectedElements.Count == 0) return;

            foreach (UIElement el in _selectedElements)
            {
                PaintSurface.Children.Remove(el);
            }

            _selectedElements.Clear();

            // Xóa khung selection
            if (_selectionRect != null)
            {
                PaintSurface.Children.Remove(_selectionRect);
                _selectionRect = null;
            }
        }


        private void MoveElement(UIElement el, double dx, double dy)
        {
            if (el.RenderTransform is not TranslateTransform tt)
            {
                tt = new TranslateTransform();
                el.RenderTransform = tt;
            }

            tt.X += dx;
            tt.Y += dy;
        }


        private bool IsPointInsideSelection(Point p)
        {
            if (_selectionRect == null) return false;

            Rect rect = new Rect(
                Canvas.GetLeft(_selectionRect),
                Canvas.GetTop(_selectionRect),
                _selectionRect.Width,
                _selectionRect.Height
            );

            return rect.Contains(p);
        }

        private void SelectElementsInRectangle()
        {
            if (_selectionRect == null) return;
            foreach (UIElement el in _selectedElements)
            {
                if (el is Shape s)
                    s.Opacity = 1.0;
            }

            _selectedElements.Clear();

            Rect selectionArea = new Rect(
                Canvas.GetLeft(_selectionRect),
                Canvas.GetTop(_selectionRect),
                _selectionRect.Width,
                _selectionRect.Height
            );

            foreach (UIElement el in PaintSurface.Children)
            {
                if (el == _selectionRect) continue;

                Rect bounds = el.RenderTransform.TransformBounds(
                    VisualTreeHelper.GetDescendantBounds(el)
                );

                if (selectionArea.IntersectsWith(bounds))
                {
                    _selectedElements.Add(el);

                    if (el is Shape s)
                        s.Opacity = 0.6;
                }
            }

            Debug.WriteLine($"SELECTED = {_selectedElements.Count}");
        }


        private void HighlightElement(UIElement element)
        {
            if (element is Shape shape)
            {
                shape.StrokeThickness += 1;
                shape.Stroke = Brushes.Blue;
            }
        }

        private void RemoveSelectionRectangle()
        {
            if (_selectionRect != null)
            {
                PaintSurface.Children.Remove(_selectionRect);
                _selectionRect = null;
            }
        }

        private void ClearSelectedElements()
        {
            foreach (var el in _selectedElements)
            {
                if (el is Shape s)
                {
                    s.Stroke = currcolor;
                }
            }
            _selectedElements.Clear();
        }



        // ===== SELECTION (PAINT STYLE) =====
        private Rectangle? _selectionRect;
        private Point _selectionStart;
        private bool _isSelecting = false;
        private bool _isMovingSelection = false;
        private List<UIElement> _selectedElements = new();
        private Point _lastMousePosition;

        private void RemoveSelectionBorder()
        {
            if (_selectionBorder != null)
            {
                PaintSurface.Children.Remove(_selectionBorder);
                _selectionBorder = null;
            }
        }


        private void StartSelection(MouseButtonEventArgs e)
        {
            _selectionStartPoint = e.GetPosition(PaintSurface);

            _selectedElement = GetElementAtPoint(_selectionStartPoint);

            if (_selectedElement != null)
            {
                _isDragging = true;
                _originalElementPosition = new Point(
                    Canvas.GetLeft(_selectedElement),
                    Canvas.GetTop(_selectedElement)
                );

                DrawSelectionBorder(_selectedElement);
            }
        }
        private UIElement? GetElementAtPoint(Point point)
        {
            HitTestResult result = VisualTreeHelper.HitTest(PaintSurface, point);
            return result?.VisualHit as UIElement;
        }
        private void DrawSelectionBorder(UIElement element)
        {
            RemoveSelectionBorder();

            Rect bounds = VisualTreeHelper.GetDescendantBounds(element);

            _selectionBorder = new Border
            {
                BorderBrush = Brushes.Blue,
                BorderThickness = new Thickness(1),
                Width = bounds.Width,
                Height = bounds.Height
            };

            Canvas.SetLeft(_selectionBorder, Canvas.GetLeft(element));
            Canvas.SetTop(_selectionBorder, Canvas.GetTop(element));

            PaintSurface.Children.Add(_selectionBorder);
        }
        // --- Logic Vẽ Hình (Của bạn) ---
        Point startpoint;
        string? selectedshape = null;
        Shape? drawshape = null;
        SolidColorBrush currcolor = Brushes.Black;
        int clickcountbezier = 2;
        int iserasing = 0;
        private GeometryGroup? group = null;


        private Polyline? currentPolyline = null;
        private bool isDrawingPencil = false;
        private Brush currentColor = Brushes.Black;
        private double currentThickness = 2;
        private UIElement? _selectedElement = null;     
        private Point _selectionStartPoint;             
        private bool _isDragging = false;              
        private Point _originalElementPosition;         
        private Border? _selectionBorder = null;

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

        public ObservableCollection<Layer> Layers { get; set; } = new ObservableCollection<Layer>();
        private Layer CurrentLayer;
        public MainWindow()
        {
            InitializeComponent();
            this.Focus();




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
                      
            Layer defaultLayer = new Layer("Layer 1");
            Layers.Add(defaultLayer);
            CurrentLayer = defaultLayer;
            LayersListBox.ItemsSource = Layers;
            LayersListBox.SelectedIndex = 0;
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
            Point pos = e.GetPosition(PaintSurface);
            Debug.WriteLine($"SelectionRect null? {_selectionRect == null}");
            Debug.WriteLine($"SelectedElements count: {_selectedElements.Count}");


            if (selectedshape == "Selection")
            {

                Point p = e.GetPosition(PaintSurface);

                if (_selectionRect != null && IsPointInsideSelection(pos))
                {
                    _isMovingSelection = true;
                    _lastMousePosition = pos;
                    PaintSurface.CaptureMouse();
                    return;
                }
                if (IsPointInsideSelection(pos) && _selectedElements.Count > 0)
                {
                    _isMovingSelection = true;
                    _lastMousePosition = pos;
                    return;
                }
            }


            if (selectedshape == "Selection")
            {
                _selectionStart = e.GetPosition(PaintSurface);
                _isSelecting = true;

                // Xóa selection cũ
                RemoveSelectionRectangle();
                _selectedElements.Clear();

                _selectionRect = new Rectangle
                {
                    Stroke = Brushes.Blue,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                    Fill = Brushes.Transparent
                };

                Canvas.SetLeft(_selectionRect, _selectionStart.X);
                Canvas.SetTop(_selectionRect, _selectionStart.Y);
                PaintSurface.Children.Add(_selectionRect);

                return;
            }


            ///////////////////////////////////////////////////////////////////// /////////////////////////////////////////////////////////////////////
            if (selectedshape == "Selection")
            {
                StartSelection(e);
                return;
            }
            //// /////////////////////////////////////////////////////////////// /////////////////////////////////////////////////////////////////////

            if (selectedshape == "Fill")
            {
                Point p = e.GetPosition(PaintSurface);
                FillColorAtPoint((int)p.X, (int)p.Y);
                return;
            }
            if (selectedshape == "Eraser")
            {
                iserasing = 1;
                group = new GeometryGroup();
                drawshape = new Path
                {
                    Data = group,
                    Stroke = Brushes.White,
                    StrokeThickness = thicknessslider.Value
                };
                PaintSurface.Children.Add(drawshape);

                if (CurrentLayer != null)
                {
                    CurrentLayer.Elements.Add(drawshape);
                    int zIndex = Layers.Count - Layers.IndexOf(CurrentLayer);
                    Canvas.SetZIndex(drawshape, zIndex);
                }

                draweraser(startpoint);
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
                if (CurrentLayer != null)
                {
                    CurrentLayer.Elements.Add(currentPolyline);
                    int zIndex = Layers.Count - Layers.IndexOf(CurrentLayer);
                    Canvas.SetZIndex(currentPolyline, zIndex);
                }

            }
            else
            {
                createshape(startpoint);
            }
        }
       


        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPoint = e.GetPosition(PaintSurface);

            if (_isMovingSelection && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPos = e.GetPosition(PaintSurface);
                System.Windows.Vector delta = currentPos - _lastMousePosition;

                foreach (UIElement el in _selectedElements)
                {
                    MoveElement(el, delta.X, delta.Y);
                }

                foreach (UIElement el in _selectedElements)
                {
                    double left = Canvas.GetLeft(el);
                    double top = Canvas.GetTop(el);

                    Canvas.SetLeft(el, left + delta.X);
                    Canvas.SetTop(el, top + delta.Y);
                }

                // Di chuyển luôn khung selection
                Canvas.SetLeft(_selectionRect, Canvas.GetLeft(_selectionRect) + delta.X);
                Canvas.SetTop(_selectionRect, Canvas.GetTop(_selectionRect) + delta.Y);

                _lastMousePosition = currentPos;
                return;
            }

            if (_isSelecting && _selectionRect != null)
            {
                Point pos = e.GetPosition(PaintSurface);

                double x = Math.Min(pos.X, _selectionStart.X);
                double y = Math.Min(pos.Y, _selectionStart.Y);
                double w = Math.Abs(pos.X - _selectionStart.X);
                double h = Math.Abs(pos.Y - _selectionStart.Y);

                Canvas.SetLeft(_selectionRect, x);
                Canvas.SetTop(_selectionRect, y);
                _selectionRect.Width = w;
                _selectionRect.Height = h;
                return;
            }

            if (selectedshape == "Selection" && _isSelecting && _selectionRect != null)
            {
                Point current = e.GetPosition(PaintSurface);

                double x = Math.Min(current.X, _selectionStart.X);
                double y = Math.Min(current.Y, _selectionStart.Y);
                double w = Math.Abs(current.X - _selectionStart.X);
                double h = Math.Abs(current.Y - _selectionStart.Y);

                Canvas.SetLeft(_selectionRect, x);
                Canvas.SetTop(_selectionRect, y);

                _selectionRect.Width = w;
                _selectionRect.Height = h;
                return;
            }

            if (selectedshape == "Selection" && _isDragging && _selectedElement != null)
            {
                Point current = e.GetPosition(PaintSurface);
                System.Windows.Vector delta = current - _selectionStartPoint;

                Canvas.SetLeft(_selectedElement, _originalElementPosition.X + delta.X);
                Canvas.SetTop(_selectedElement, _originalElementPosition.Y + delta.Y);

                if (_selectionBorder != null)
                {
                    Canvas.SetLeft(_selectionBorder, Canvas.GetLeft(_selectedElement));
                    Canvas.SetTop(_selectionBorder, Canvas.GetTop(_selectedElement));
                }
                return;
            }

            // --- Nếu đang vẽ Pencil ---
            if (isDrawingPencil && e.LeftButton == MouseButtonState.Pressed)
            {
                currentPolyline?.Points.Add(currentPoint);
                return;
            }
            if (iserasing == 1 && e.LeftButton == MouseButtonState.Pressed)
            {
                draweraser(currentPoint);
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
            if (_isSelecting)
            {
                _isSelecting = false;
                SelectElementsInRectangle();
                return;
            }
            if (selectedshape == "Selection")
            {
                SelectElementsInRectangle();
            }

            if (selectedshape == "Selection" && _isSelecting)
            {
                _isSelecting = false;
            }

            if (isDrawingPencil)
            {
                Shape shape = CloneShape(currentPolyline);
                PaintSurface.Children.Remove(currentPolyline);
                PaintSurface.Children.Add(shape);

                if (CurrentLayer != null)
                {
                    CurrentLayer.Elements.Add(shape);
                    int zIndex = Layers.Count - Layers.IndexOf(CurrentLayer);
                    Canvas.SetZIndex(shape, zIndex);
                }

                UndoPush(shape);
                Redo.Clear();
                isDrawingPencil = false;
                currentPolyline = null;
            }
            if (iserasing == 1)
            {
                if (drawshape == null) return;
                Shape shape = CloneShape(drawshape);
                double left = Canvas.GetLeft(drawshape);
                double top = Canvas.GetTop(drawshape);
                PaintSurface.Children.Remove(drawshape);
                PaintSurface.Children.Add(shape);
                Canvas.SetLeft(shape, left);
                Canvas.SetTop(shape, top);

                if (CurrentLayer != null)
                {
                    CurrentLayer.Elements.Add(shape);
                    int zIndex = Layers.Count - Layers.IndexOf(CurrentLayer);
                    Canvas.SetZIndex(shape, zIndex);
                }

                UndoPush(shape);
                Redo.Clear();
                iserasing = 0;
                drawshape = null;
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

                if (CurrentLayer != null)
                {
                    CurrentLayer.Elements.Add(shape);
                    int zIndex = Layers.Count - Layers.IndexOf(CurrentLayer);
                    Canvas.SetZIndex(shape, zIndex);
                }

                UndoPush(shape);
                Redo.Clear();
                drawshape = null;
                _isDragging = false;
                _isMovingSelection = false;
                PaintSurface.ReleaseMouseCapture();

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

            if (CurrentLayer != null)
            {
                CurrentLayer.Elements.Add(img);
                int zIndex = Layers.Count - Layers.IndexOf(CurrentLayer);
                Canvas.SetZIndex(img, zIndex);
            }

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
                    drawshape = new Rectangle { Width = 0, Height = 0 };
                    Canvas.SetLeft(drawshape, startpoint.X);
                    Canvas.SetTop(drawshape, startpoint.Y);
                    break;

                case "Circle":
                    drawshape = new Ellipse { Width = 0, Height = 0 };
                    Canvas.SetLeft(drawshape, startpoint.X);
                    Canvas.SetTop(drawshape, startpoint.Y);
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

            if (drawshape != null && ((drawshape is Path && clickcountbezier == 0) || drawshape is not Path))
            {
                drawshape.Stroke = currcolor;
                drawshape.StrokeThickness = thicknessslider.Value;
                drawshape.Fill = Brushes.Transparent;
                PaintSurface.Children.Add(drawshape);

                if (CurrentLayer != null)
                {
                    CurrentLayer.Elements.Add(drawshape);
                    int zIndex = Layers.Count - Layers.IndexOf(CurrentLayer);
                    Canvas.SetZIndex(drawshape, zIndex);
                }
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
                    } else
                    {
                        bezierpoint(secondpoint, clickcountbezier);
                    }
                    break;

                case "Play":
                    PointCollection point = new PointCollection();
                    point.Add(new Point(x, y));
                    point.Add(new Point(x + w, y + h / 2));
                    point.Add(new Point(x, y + h));
                    ((Polygon)drawshape).Points = point;
                    break;

                case "Diamond":
                    PointCollection point1 = new PointCollection();
                    point1.Add(new Point(x, y));
                    point1.Add(new Point(x + w, y + h / 2));
                    point1.Add(new Point(x, y + h));
                    point1.Add(new Point(x - w, y + h / 2));
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
        private void bezierpoint(Point point, int clicknum)
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
            else if (x is Ellipse ellipse)
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
                    StrokeThickness = polyline.StrokeThickness,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeLineJoin = PenLineJoin.Round
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
                Foreground = currentColor, BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                AcceptsReturn = true
            };

            Canvas.SetLeft(currentTextBox, position.X);
            Canvas.SetTop(currentTextBox, position.Y);

            PaintSurface.Children.Add(currentTextBox);

            if (CurrentLayer != null)
            {
                CurrentLayer.Elements.Add(currentTextBox);
                int zIndex = Layers.Count - Layers.IndexOf(CurrentLayer);
                Canvas.SetZIndex(currentTextBox, zIndex);
            }

            
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

            if (CurrentLayer != null)
            {
                CurrentLayer.Elements.Add(resizeThumb);
                int zIndex = Layers.Count - Layers.IndexOf(CurrentLayer);
                Canvas.SetZIndex(resizeThumb, zIndex);
            }

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

                if (CurrentLayer != null)
                {
                    CurrentLayer.Elements.Add(tb);
                    int zIndex = Layers.Count - Layers.IndexOf(CurrentLayer);
                    Canvas.SetZIndex(tb, zIndex);
                }

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
        void draweraser(Point p)
        {
            double size = thicknessslider.Value;
            var eraserGeom = new RectangleGeometry(
                new Rect(p.X - size / 2, p.Y - size / 2, size, size)
            );
            group.Children.Add(eraserGeom);
        }




        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            string newName = $"Layer {Layers.Count + 1}";
            Layer newLayer = new Layer(newName);
            Layers.Insert(0, newLayer);
            CurrentLayer = newLayer;
            LayersListBox.SelectedItem = newLayer;
        }

        private void DeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            if (Layers.Count <= 1)
            {
                MessageBox.Show("Cannot delete the last layer!");
                return;
            }
            Button btn = sender as Button;
            Layer layerToDelete = btn.Tag as Layer;


            if (layerToDelete != null)
            {
                foreach (var element in layerToDelete.Elements)
                {
                    PaintSurface.Children.Remove(element);
                }
                Layers.Remove(layerToDelete);
                int temp = 1;
                for (int i = Layers.Count - 1; i >=0 ;i--)
                {
                    Layers[i].Name = $"Layer {temp++}";
                }
                if (CurrentLayer == layerToDelete)
                {
                    LayersListBox.SelectedIndex = 0;
                    CurrentLayer = Layers[0];
                }
            }
        }
        private void LayersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LayersListBox.SelectedItem is Layer selected)
            {
                CurrentLayer = selected;
            }
        }
    }
}

public class Layer : INotifyPropertyChanged
{
    private string _name;
    private bool _isVisible;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            OnPropertyChanged();
            UpdateVisibility();
        }
    }

    public List<UIElement> Elements { get; set; } = new List<UIElement>();

    public Layer(string name)
    {
        Name = name;
        IsVisible = true;
    }

    private void UpdateVisibility()
    {
        Visibility v = IsVisible ? Visibility.Visible : Visibility.Hidden;
        foreach (var element in Elements)
        {
            element.Visibility = v;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
