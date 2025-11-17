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
        public class PolygonElement : ElementData
        {
            public PointCollection? Points;
            public string? Stroke;
            public string? Fill;
            public double StrokeThickness;
        }
        public class ImageElement : ElementData
        {
            public double Left, Top, Width, Height;
            public string? ImageBase64;
        }
        public class PolylineElement : ElementData
        {
            public PointCollection? Points;
            public string? Stroke;
            public double StrokeThickness;
        }
        public class CurveElement : ElementData
        {
            public System.Windows.Point StartPoint, Point1, Point2, Point3;
            public string? Stroke;
            public double StrokeThickness;
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
                            Fill = (ellipse.Fill as SolidColorBrush)?.Color.ToString() ?? "1",
                            StrokeThickness = ellipse.StrokeThickness
                        });
                        break;

                    case System.Windows.Shapes.Rectangle rectangle:
                        data.Elements.Add(new RectangleElement
                        {
                            Type = "Rectangle",
                            Width = rectangle.Width,
                            Height = rectangle.Height,
                            Top = Canvas.GetTop(rectangle),
                            Left = Canvas.GetLeft(rectangle),
                            Stroke = (rectangle.Stroke as SolidColorBrush)?.Color.ToString() ?? "",
                            Fill = (rectangle.Fill as SolidColorBrush)?.Color.ToString() ?? "",
                            StrokeThickness = rectangle.StrokeThickness
                        });
                        break;

                    case Polygon polygon:
                        data.Elements.Add(new PolygonElement
                        {
                            Type = "Polygon",
                            Points = polygon.Points,
                            Stroke = (polygon.Stroke as SolidColorBrush)?.Color.ToString() ?? "",
                            Fill = (polygon.Fill as SolidColorBrush)?.Color.ToString() ?? "",
                            StrokeThickness = polygon.StrokeThickness
                        });
                        break;

                    case Polyline polyline:
                        data.Elements.Add(new PolylineElement
                        {
                            Type = "Polyline",
                            Points = polyline.Points,
                            Stroke = (polyline.Stroke as SolidColorBrush)?.Color.ToString() ?? "",
                            StrokeThickness = polyline.StrokeThickness
                        });
                        break;

                    case System.Windows.Shapes.Path path:
                        var geo = path.Data as PathGeometry;
                        var fig = geo.Figures.FirstOrDefault();
                        var bez = fig.Segments.OfType<BezierSegment>().FirstOrDefault();
                        data.Elements.Add(new CurveElement
                        {
                            Type = "Curve",
                            StartPoint = fig.StartPoint,
                            Point1 = bez.Point1,
                            Point2 = bez.Point2,
                            Point3 = bez.Point3,
                            Stroke = (path.Stroke as SolidColorBrush)?.Color.ToString() ?? "",
                            StrokeThickness = path.StrokeThickness
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
                            Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(lineData.Stroke) ?? ""),
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
                            Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(elData.Stroke) ?? ""),
                            Fill = (SolidColorBrush)(new BrushConverter().ConvertFromString(elData.Fill) ?? ""),
                            StrokeThickness = elData.StrokeThickness
                        };
                        Canvas.SetLeft(newellipse, elData.Left);
                        Canvas.SetTop(newellipse, elData.Top);
                        canvas.Children.Add(newellipse);
                        break;

                    case "Rectangle":
                        var recData = e.ToObject<RectangleElement>();
                        var newRect = new System.Windows.Shapes.Rectangle
                        {
                            Width = recData.Width,
                            Height = recData.Height,
                            Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(recData.Stroke) ?? ""),
                            Fill = (SolidColorBrush)(new BrushConverter().ConvertFromString(recData.Fill) ?? ""),
                            StrokeThickness = recData.StrokeThickness
                        };
                        Canvas.SetLeft(newRect, recData.Left);
                        Canvas.SetTop(newRect, recData.Top);
                        canvas.Children.Add(newRect);
                        break;

                    case "Polygon":
                        var polygonData = e.ToObject<PolygonElement>();
                        var newPolygon = new Polygon
                        {
                            Points = polygonData.Points,
                            Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(polygonData.Stroke) ?? ""),
                            Fill = (SolidColorBrush)(new BrushConverter().ConvertFromString(polygonData.Fill) ?? ""),
                            StrokeThickness = polygonData.StrokeThickness
                        };
                        canvas.Children.Add(newPolygon);
                        break;

                    case "Polyline":
                        var polylineData = e.ToObject<PolylineElement>();
                        var newPolyline = new Polyline
                        {
                            Points = polylineData.Points,
                            Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(polylineData.Stroke) ?? ""),
                            StrokeThickness = polylineData.StrokeThickness
                        };
                        canvas.Children.Add(newPolyline);
                        break;

                    case "Curve":
                        var curveData = e.ToObject<CurveElement>();
                        PathGeometry geo = new PathGeometry();
                        PathFigure fig = new PathFigure();
                        BezierSegment bez = new BezierSegment();
                        fig.StartPoint = curveData.StartPoint;
                        bez.Point1 = curveData.Point1;
                        bez.Point2 = curveData.Point2;
                        bez.Point3 = curveData.Point3;
                        fig.Segments.Add(bez);
                        geo.Figures.Add(fig);
                        var newCurve = new System.Windows.Shapes.Path
                        {
                            Data = geo,
                            Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(curveData.Stroke) ?? ""),
                            StrokeThickness = curveData.StrokeThickness
                        };
                        canvas.Children.Add(newCurve);
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
                    Width = canvas.ActualWidth,
                    Height = canvas.ActualHeight
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
