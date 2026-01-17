using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup.Localizer;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Paint
{
    public static class FileManager
    {
        // ==== Dữ liệu tổng thể của Canvas ====
        private class CanvasData
        {
            public string BackgroundColor { get; set; } = "White";
            public List<LayerData> Elements { get; set; } = new();
        }
        public class LayerData
        {
            public string LayerName { get; set; } = "";
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
        public class EraserElement : ElementData
        {
            public List<RectData> rectDatas;
            public string? Stroke;
            public double StrokeThickness;
        }
        public class TextBoxElement : ElementData
        {
            public double Left, Top, Width, Height;
            public string? Text;
            public double FontSize;
            public string? Foreground;
            public string? Background;
            public double Padding;
            public bool AcceptsReturn;
            public bool TextWrapping;
            public string? VerticalContentAlignment;
        }
        public class RectData
        {
            public double x, y, width, height;
        }
        // ==== SAVE TO JSON ====
        public static void SaveCanvasToJson(MainWindow window, string filePath)
        {
            var data = new CanvasData
            {
                BackgroundColor = (window.PaintSurface.Background as SolidColorBrush)?.Color.ToString() ?? "White"
            };
            foreach (var layer in window.Layers.Reverse())
            {
                LayerData layerData = new LayerData
                {
                    LayerName = layer.Name
                };
                foreach (var child in layer.Elements)
                {
                    switch (child)
                    {
                        case Line line:
                            layerData.Elements.Add(new LineElement
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
                            layerData.Elements.Add(new EllipseElement
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
                            layerData.Elements.Add(new RectangleElement
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
                            layerData.Elements.Add(new PolygonElement
                            {
                                Type = "Polygon",
                                Points = polygon.Points,
                                Stroke = (polygon.Stroke as SolidColorBrush)?.Color.ToString() ?? "",
                                Fill = (polygon.Fill as SolidColorBrush)?.Color.ToString() ?? "",
                                StrokeThickness = polygon.StrokeThickness
                            });
                            break;

                        case Polyline polyline:
                            layerData.Elements.Add(new PolylineElement
                            {
                                Type = "Polyline",
                                Points = polyline.Points,
                                Stroke = (polyline.Stroke as SolidColorBrush)?.Color.ToString() ?? "",
                                StrokeThickness = polyline.StrokeThickness
                            }); 
                            break;

                        case System.Windows.Shapes.Path path:
                            var geo = path.Data as PathGeometry;
                            if (geo == null)
                            {
                                var groupdata = path.Data as GeometryGroup;
                                List<RectData> rects = new List<RectData>();
                                foreach (var g in groupdata.Children)
                                {
                                    if (g is RectangleGeometry rect)
                                    {
                                        rects.Add(new RectData
                                        {
                                            x = rect.Rect.X,
                                            y = rect.Rect.Y,
                                            width = rect.Rect.Width,
                                            height = rect.Rect.Height
                                        });
                                    }
                                }
                                layerData.Elements.Add(new EraserElement
                                {
                                    Type = "Eraser",
                                    rectDatas = rects,
                                    Stroke = (path.Stroke as SolidColorBrush)?.Color.ToString() ?? "",
                                    StrokeThickness = path.StrokeThickness
                                });
                                break;
                            }
                            var fig = geo.Figures.FirstOrDefault();
                            var bez = fig.Segments.OfType<BezierSegment>().FirstOrDefault();
                            layerData.Elements.Add(new CurveElement
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
                            layerData.Elements.Add(new ImageElement
                            {
                                Type = "Image",
                                Left = Canvas.GetLeft(image),
                                Top = Canvas.GetTop(image),
                                Width = image.Width,
                                Height = image.Height,
                                ImageBase64 = EncodeBitmapToBase64(bitmap)
                            });
                            break;

                        case Canvas textCanvas:
                            {
                                var border = textCanvas.Children.OfType<Border>().FirstOrDefault();
                                TextBox? tb = null;
                                if (border?.Child is TextBox bTB)
                                    tb = bTB;
                                else
                                    tb = textCanvas.Children.OfType<TextBox>().FirstOrDefault();

                                if (tb != null)
                                {
                                    layerData.Elements.Add(new TextBoxElement
                                    {
                                        Type = "TextBox",
                                        Left = Canvas.GetLeft(textCanvas),
                                        Top = Canvas.GetTop(textCanvas),
                                        Width = textCanvas.Width,
                                        Height = textCanvas.Height,
                                        Text = tb.Text,
                                        FontSize = tb.FontSize,
                                        Foreground = (tb.Foreground as SolidColorBrush)?.Color.ToString() ?? "",
                                        Background = (tb.Background as SolidColorBrush)?.Color.ToString() ?? "",
                                        Padding = tb.Padding.Left,
                                        AcceptsReturn = tb.AcceptsReturn,
                                        TextWrapping = tb.TextWrapping == TextWrapping.Wrap,
                                        VerticalContentAlignment = tb.VerticalContentAlignment.ToString()
                                    });
                                }
                            }
                            break;
                    }
                }
                data.Elements.Add(layerData);
            }
            string json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        // ==== LOAD FROM JSON ====
        public static void LoadCanvasFromJson(MainWindow window, string filePath)
        {
            string json = File.ReadAllText(filePath);
            var data = JsonConvert.DeserializeObject<CanvasData>(json);

            window.PaintSurface.Children.Clear();
            window.PaintSurface.Background = (SolidColorBrush)(new BrushConverter().ConvertFromString(data.BackgroundColor) ?? "White");

            var root = JObject.Parse(json);
            var elements = (JArray)root["Elements"];

            window.Layers.Clear();
            foreach (var l in elements)
            {
                string layerName = l.Value<string>("LayerName") ?? string.Empty;
                Layer layer = new Layer(layerName);

                var layerElements = (JArray)l["Elements"] ?? new JArray();
                foreach (var e in layerElements)
                {
                    var type = e.Value<string>("Type") ?? string.Empty;
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
                            window.PaintSurface.Children.Add(newline);
                            layer.Elements.Add(newline);
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
                            window.PaintSurface.Children.Add(newimage);
                            layer.Elements.Add(newimage);
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
                            window.PaintSurface.Children.Add(newellipse);
                            layer.Elements.Add(newellipse);
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
                            window.PaintSurface.Children.Add(newRect);
                            layer.Elements.Add(newRect);
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
                            window.PaintSurface.Children.Add(newPolygon);
                            layer.Elements.Add(newPolygon);
                            break;

                        case "Polyline":
                            var polylineData = e.ToObject<PolylineElement>();
                            var newPolyline = new Polyline
                            {
                                Points = polylineData.Points,
                                Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(polylineData.Stroke) ?? ""),
                                StrokeThickness = polylineData.StrokeThickness,
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round
                            };
                            window.PaintSurface.Children.Add(newPolyline);
                            layer.Elements.Add(newPolyline);
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
                            window.PaintSurface.Children.Add(newCurve);
                            layer.Elements.Add(newCurve);
                            break;

                        case "Eraser":
                            var eraserData = e.ToObject<EraserElement>();
                            GeometryGroup geometryGroup = new GeometryGroup();
                            foreach (var g in eraserData.rectDatas)
                            {
                                geometryGroup.Children.Add(new RectangleGeometry(new Rect(g.x, g.y, g.width, g.height)));
                            }
                            var newEraser = new System.Windows.Shapes.Path
                            {
                                Data = geometryGroup,
                                Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(eraserData.Stroke) ?? ""),
                                StrokeThickness = eraserData.StrokeThickness
                            };
                            window.PaintSurface.Children.Add(newEraser);
                            layer.Elements.Add(newEraser);
                            break;

                        case "TextBox":
                            var tbData = e.ToObject<TextBoxElement>();

                            // Recreate the structure used in CreateTextBox: Canvas -> Border -> TextBox + Thumb
                            var textCanvas = new Canvas
                            {
                                Width = tbData.Width,
                                Height = tbData.Height,
                                Background = System.Windows.Media.Brushes.Transparent
                            };

                            var border = new Border
                            {
                                Width = tbData.Width,
                                Height = tbData.Height,
                                BorderBrush = System.Windows.Media.Brushes.Blue,
                                BorderThickness = new Thickness(1),
                                Background = System.Windows.Media.Brushes.Transparent
                            };

                            var textBox = new TextBox
                            {
                                AcceptsReturn = tbData.AcceptsReturn,
                                TextWrapping = tbData.TextWrapping ? TextWrapping.Wrap : TextWrapping.NoWrap,
                                BorderThickness = new Thickness(0),
                                Background = (SolidColorBrush)(new BrushConverter().ConvertFromString(tbData.Background) ?? System.Windows.Media.Brushes.Transparent),
                                FontSize = tbData.FontSize > 0 ? tbData.FontSize : 16,
                                Padding = new Thickness(tbData.Padding),
                                VerticalContentAlignment = Enum.TryParse<VerticalAlignment>(tbData.VerticalContentAlignment, out var vca) ? vca : VerticalAlignment.Top,
                                Foreground = (SolidColorBrush)(new BrushConverter().ConvertFromString(tbData.Foreground) ?? System.Windows.Media.Brushes.Black),
                                Text = tbData.Text ?? string.Empty
                            };

                            border.Child = textBox;
                            textCanvas.Children.Add(border);

                            // Resize Thumb
                            Thumb resizeThumb = new Thumb
                            {
                                Width = 10,
                                Height = 10,
                                Background = System.Windows.Media.Brushes.Blue,
                                Cursor = Cursors.SizeNWSE
                            };
                            Canvas.SetRight(resizeThumb, 0);
                            Canvas.SetBottom(resizeThumb, 0);
                            textCanvas.Children.Add(resizeThumb);

                            resizeThumb.DragDelta += (s, ev) =>
                            {
                                double newW = Math.Max(60, textCanvas.Width + ev.HorizontalChange);
                                double newH = Math.Max(30, textCanvas.Height + ev.VerticalChange);

                                textCanvas.Width = newW;
                                textCanvas.Height = newH;

                                border.Width = newW;
                                border.Height = newH;
                            };

                            Canvas.SetLeft(textCanvas, tbData.Left);
                            Canvas.SetTop(textCanvas, tbData.Top);

                            window.PaintSurface.Children.Add(textCanvas);
                            layer.Elements.Add(textCanvas);
                            layer.Elements.Add(resizeThumb);
                            break;

                        //////
                        default:
                            MessageBox.Show("nah");
                            break;
                    }
                }
                window.Layers.Add(layer);          
            }
            var reversedLayers = window.Layers.Reverse().ToList();
            window.Layers.Clear();
            foreach (var layer in reversedLayers)
            {
                window.Layers.Add(layer);
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
        public static void ImportImageToCanvas(MainWindow window)
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
                    Width = window.PaintSurface.ActualWidth,
                    Height = window.PaintSurface.ActualHeight
                };
                Canvas.SetLeft(img, 0);
                Canvas.SetTop(img, 0);
                window.PaintSurface.Children.Insert(0, img);
                Layer layer = new Layer("*imported layer");
                layer.Elements.Add(img);
                window.Layers.Add(layer);
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
