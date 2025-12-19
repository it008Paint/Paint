using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Paint.Components
{
    public partial class Selection : UserControl
    {
        private Point startPoint;
        private bool isSelecting;

        private Rect selectedRegion;
        private RenderTargetBitmap copiedBitmap;

        public Canvas? TargetCanvas { get; set; }

        public Selection()
        {
            InitializeComponent();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TargetCanvas == null) return;

            startPoint = e.GetPosition(TargetCanvas);
            isSelecting = true;

            SelectionRect.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRect, startPoint.X);
            Canvas.SetTop(SelectionRect, startPoint.Y);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;

            CaptureMouse();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isSelecting || TargetCanvas == null) return;

            Point pos = e.GetPosition(TargetCanvas);

            double x = Math.Min(pos.X, startPoint.X);
            double y = Math.Min(pos.Y, startPoint.Y);
            double w = Math.Abs(pos.X - startPoint.X);
            double h = Math.Abs(pos.Y - startPoint.Y);

            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width = w;
            SelectionRect.Height = h;
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isSelecting) return;

            isSelecting = false;
            ReleaseMouseCapture();

            selectedRegion = new Rect(
                Canvas.GetLeft(SelectionRect),
                Canvas.GetTop(SelectionRect),
                SelectionRect.Width,
                SelectionRect.Height);
        }

        public void Copy()
        {
            if (TargetCanvas == null) return;

            copiedBitmap = CaptureRegion();
            if (copiedBitmap != null)
                Clipboard.SetImage(copiedBitmap);
        }

        public void Delete()
        {
            if (TargetCanvas == null) return;

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Transparent, null, selectedRegion);
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)TargetCanvas.ActualWidth,
                (int)TargetCanvas.ActualHeight,
                96, 96, PixelFormats.Pbgra32);

            rtb.Render(TargetCanvas);

            TargetCanvas.Children.Clear();
            TargetCanvas.Children.Add(new Image { Source = rtb });
        }

        private RenderTargetBitmap? CaptureRegion()
        {
            if (TargetCanvas == null ||
                selectedRegion.Width <= 0 ||
                selectedRegion.Height <= 0)
                return null;

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)TargetCanvas.ActualWidth,
                (int)TargetCanvas.ActualHeight,
                96, 96, PixelFormats.Pbgra32);

            rtb.Render(TargetCanvas);

            CroppedBitmap crop = new CroppedBitmap(
                rtb,
                new Int32Rect(
                    (int)selectedRegion.X,
                    (int)selectedRegion.Y,
                    (int)selectedRegion.Width,
                    (int)selectedRegion.Height));

            RenderTargetBitmap result = new RenderTargetBitmap(
                crop.PixelWidth, crop.PixelHeight,
                96, 96, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawImage(crop, new Rect(0, 0, crop.PixelWidth, crop.PixelHeight));
            }

            result.Render(dv);
            return result;
        }
    }
}

