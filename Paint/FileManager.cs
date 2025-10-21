using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup.Localizer;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

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
        public class ElementData
        {
            public string Type { get; set; } = "";
        }
        public class LineElement : ElementData
        {
            public double X1, Y1, X2, Y2;
            public string? Stroke;
            public double StrokeThickness;
        }
        public class CurveElement : ElementData
        {
            public List<System.Drawing.Point>? Point;
            public string? Stroke;
            public double StrokeThickness;
        }
        public class EllipseElement : ElementData
        {
            public double Left, Top, Width, Height;
            public string? Stroke;
            public string? Fill;
            public double StrokeThickness;
        }
        public class RectangleElement : ElementData
        {
            public double Left, Top, Width, Height;
            public string? Stroke;
            public string? Fill;
            public double StrokeThickness;
        }
        public class TriangleElement : ElementData
        {
            public List<System.Drawing.Point>? Point;
            public string? Stroke;
            public string? Fill;
            public double StrokeThickness;
        }
        public class DiamondElement : ElementData
        {
            public List<System.Drawing.Point>? Point;
            public string? Stroke;
            public string? Fill;
            public double StrokeThickness;
        }
        public class StarElement : ElementData
        {
            public List<System.Drawing.Point>? Point;
            public string? Stroke;
            public string? Fill;
            public double StrokeThickness;
        }
        public class ArrowElement : ElementData
        {
            public double StartX,StartY,EndX,EndY;
            public string? Stroke;
            public string? Fill;
            public double StrokeThickness;
        }
        public class ImageElement : ElementData
        {
            public double Left, Top, Width, Height;
            public string? ImageBase64;
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
                switch (child)
                {
                    case Line line:
                        data.Elements.Add(new LineElement
                        {
                            Type = "Line",
                            X1 = line.X1,
                            Y1 = line.Y1,
                            X2 = line.X2,
                            Y2 = line.Y2,
                            Stroke = (line.Stroke as SolidColorBrush)?.Color.ToString() ?? "",
                            StrokeThickness = line.StrokeThickness
                        });
                        break;

                    case Ellipse ellipse:
                        data.Elements.Add(new EllipseElement
                        {
                            Type = "Ellipse",
                            Left = Canvas.GetLeft(ellipse),
                            Top = Canvas.GetTop(ellipse),
                            Width = ellipse.Width,
                            Height = ellipse.Height,
                            Stroke = (ellipse.Stroke as SolidColorBrush)?.Color.ToString() ?? "",
                            Fill = (ellipse.Fill as SolidColorBrush)?.Color.ToString() ?? "",
                            StrokeThickness = ellipse.StrokeThickness
                        });
                        break;

                    case System.Windows.Shapes.Rectangle rectangle:
                        data.Elements.Add(new RectangleElement
                        {
                            Type = "Rectangle",
                            //list point
                            Stroke = (rectangle.Stroke as SolidColorBrush)?.Color.ToString() ?? "",
                            Fill = (rectangle.Fill as SolidColorBrush)?.Color.ToString() ?? "",
                            StrokeThickness = rectangle.StrokeThickness
                        });
                        break;

                    case System.Windows.Controls.Image image when image.Source is BitmapSource bitmap:
                        data.Elements.Add(new ImageElement
                        {
                            Type = "Image",
                            Left = Canvas.GetLeft(image),
                            Top = Canvas.GetTop(image),
                            Width = image.Width,
                            Height = image.Height,
                            ImageBase64 = EncodeBitmapToBase64(bitmap)
                        });
                        break;
                        //triangle,diamond,star,curve,arrow
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
            canvas.Background = (SolidColorBrush)(new BrushConverter().ConvertFromString(data.BackgroundColor) ?? "White");

            var root = JObject.Parse(json);
            var elements = (JArray)root["Elements"];

            foreach (var e in elements)
            {
                string type = e.Value<string>("Type") ?? string.Empty;
                switch (type)
                {
                    case "Line":
                        var lineData = e.ToObject<LineElement>();
                        var newline = new System.Windows.Shapes.Line
                        {
                            X1 = lineData.X1,
                            X2 = lineData.X2,
                            Y1 = lineData.Y1,
                            Y2 = lineData.Y2,
                            Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(lineData.Stroke) ?? "Black"),
                            StrokeThickness = lineData.StrokeThickness
                        };
                        canvas.Children.Add(newline);
                        break;
                    
                    case "Image":
                        var imageData = e.ToObject<ImageElement>();
                        var newimage = new System.Windows.Controls.Image
                        {
                            Source = DecodeBase64ToBitmap(imageData.ImageBase64),
                            Width = imageData.Width,
                            Height = imageData.Height
                        };
                        Canvas.SetLeft(newimage, imageData.Left);
                        Canvas.SetTop(newimage, imageData.Top);
                        canvas.Children.Add(newimage);
                        break;

                    case "Ellipse":
                        var elData = e.ToObject<EllipseElement>();
                        var newellipse = new Ellipse
                        {
                            Width = elData.Width,
                            Height = elData.Height,
                            Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(elData.Stroke) ?? "Black"),
                            Fill = (SolidColorBrush)(new BrushConverter().ConvertFromString(elData.Fill) ?? ""),
                            StrokeThickness = elData.StrokeThickness
                        };
                        Canvas.SetLeft(newellipse, elData.Left);
                        Canvas.SetTop(newellipse, elData.Top);
                        canvas.Children.Add(newellipse);
                        break;

                    //////
                    default:
                        MessageBox.Show("nah");
                        break;
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
                var img = new System.Windows.Controls.Image
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
