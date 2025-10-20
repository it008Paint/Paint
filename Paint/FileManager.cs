using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Paint
{
    public static class FileManager
    {
        // ==== Dữ liệu tổng thể của Canvas ====
        private class CanvasData
        {
            public string BackgroundColor { get; set; } = "White";
            public List<ElementData> Elements { get; set; } = new();
        }

        // ==== Một phần tử trên Canvas (Shape hoặc Image) ====
        private class ElementData
        {
            public string Type { get; set; } = "";
            public double X { get; set; }
            public double Y { get; set; }

            // Dành cho Shape
            public double Width { get; set; }
            public double Height { get; set; }
            public string Fill { get; set; } = "";
            public string Stroke { get; set; } = "";
            public double StrokeThickness { get; set; }

            // Dành cho Image
            public string? ImageBase64 { get; set; }
        }

        // ==== SAVE TO JSON ====
        public static void SaveCanvasToJson(Canvas canvas, string filePath)
        {
            var data = new CanvasData
            {
                BackgroundColor = (canvas.Background as SolidColorBrush)?.Color.ToString() ?? "White"
            };

            foreach (var child in canvas.Children)
            {
                if (child is System.Windows.Shapes.Shape shape)
                {
                    data.Elements.Add(new ElementData
                    {
                        Type = shape.GetType().Name,
                        X = Canvas.GetLeft(shape),
                        Y = Canvas.GetTop(shape),
                        Width = shape.Width,
                        Height = shape.Height,
                        Fill = (shape.Fill as SolidColorBrush)?.Color.ToString() ?? "",
                        Stroke = (shape.Stroke as SolidColorBrush)?.Color.ToString() ?? "",
                        StrokeThickness = shape.StrokeThickness
                    });
                }
                else if (child is Image image && image.Source is BitmapSource bitmap)
                {
                    data.Elements.Add(new ElementData
                    {
                        Type = "Image",
                        X = Canvas.GetLeft(image),
                        Y = Canvas.GetTop(image),
                        Width = image.Width,
                        Height = image.Height,
                        ImageBase64 = EncodeBitmapToBase64(bitmap)
                    });
                }
            }

            string json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        // ==== LOAD FROM JSON ====
        public static void LoadCanvasFromJson(Canvas canvas, string filePath)
        {
            string json = File.ReadAllText(filePath);
            var data = JsonConvert.DeserializeObject<CanvasData>(json);

            canvas.Children.Clear();
            canvas.Background = (SolidColorBrush)(new BrushConverter().ConvertFromString(data.BackgroundColor));

            foreach (var e in data.Elements)
            {
                if (e.Type == "Image" && e.ImageBase64 != null)
                {
                    var img = new Image
                    {
                        Source = DecodeBase64ToBitmap(e.ImageBase64),
                        Width = e.Width,
                        Height = e.Height
                    };
                    Canvas.SetLeft(img, e.X);
                    Canvas.SetTop(img, e.Y);
                    canvas.Children.Add(img);
                }
                else
                {
                    System.Windows.Shapes.Shape shape = e.Type switch
                    {
                        "Rectangle" => new System.Windows.Shapes.Rectangle(),
                        "Ellipse" => new System.Windows.Shapes.Ellipse(),
                        "Line" => new System.Windows.Shapes.Line(),
                        _ => null
                    };
                    if (shape == null) continue;

                    shape.Width = e.Width;
                    shape.Height = e.Height;
                    shape.Fill = (SolidColorBrush)(new BrushConverter().ConvertFromString(e.Fill));
                    shape.Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(e.Stroke));
                    shape.StrokeThickness = e.StrokeThickness;

                    Canvas.SetLeft(shape, e.X);
                    Canvas.SetTop(shape, e.Y);
                    canvas.Children.Add(shape);
                }
            }
        }

        // ==== EXPORT TO PNG ====
        public static void ExportCanvasToPng(Canvas canvas, string filePath)
        {
            var rect = new System.Windows.Rect(canvas.RenderSize);
            var rtb = new RenderTargetBitmap((int)rect.Width, (int)rect.Height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(canvas);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fs);
            }
        }

        // ==== IMPORT IMAGE ====
        public static void ImportImageToCanvas(Canvas canvas)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg"
            };
            if (dlg.ShowDialog() == true)
            {
                var bitmap = new BitmapImage(new Uri(dlg.FileName));
                var img = new Image
                {
                    Source = bitmap,
                    Width = 1100,
                    Height = 500
                };
                Canvas.SetLeft(img, 0);
                Canvas.SetTop(img, 0);
                canvas.Children.Insert(0, img);
            }
        }

        // ==== Utility ====
        private static string EncodeBitmapToBase64(BitmapSource bitmap)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private static BitmapImage DecodeBase64ToBitmap(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            using (var ms = new MemoryStream(bytes))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
        }
    }
}
