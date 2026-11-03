using Microsoft.Win32;
using SkiaSharp;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageCropWebP;
public partial class MainWindow : Window
{
    private SKBitmap? source;
    private string? sourcePath;
    private Rect crop;
    private Point anchor;
    private Rect originalCrop;
    private bool dragging, moving;
    private double displayWidth, displayHeight;

    public MainWindow() { InitializeComponent(); Loaded += (_, _) => { if (source is not null) DisplaySource(); }; SizeChanged += (_, _) => { if (source is not null && !dragging) DisplaySource(); }; }

    private double Ratio()
    {
        if (RatioBox?.SelectedItem is not ComboBoxItem item) return 0;
        var tag = item.Tag?.ToString();
        if (tag == "custom")
        {
            if (double.TryParse(RatioW?.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var w) &&
                double.TryParse(RatioH?.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var h) && w > 0 && h > 0) return w / h;
            return 0;
        }
        return double.TryParse(tag, NumberStyles.Float, CultureInfo.InvariantCulture, out var ratio) ? ratio : 0;
    }

    private void LoadImage(string path)
    {
        using var stream = File.OpenRead(path);
        using var codec = SKCodec.Create(stream) ?? throw new InvalidDataException("Nieobsługiwany lub uszkodzony obraz.");
        var bitmap = SKBitmap.Decode(codec) ?? throw new InvalidDataException("Nie udało się odczytać obrazu.");
        source?.Dispose(); source = bitmap; sourcePath = path;
        DisplaySource(); SelectAll(); Info.Text = $"Otwarty: {System.IO.Path.GetFileName(path)} ({source.Width} × {source.Height})";
    }

    private void LoadBytes(byte[] bytes)
    {
        using var data = SKData.CreateCopy(bytes);
        var bitmap = SKBitmap.Decode(data) ?? throw new InvalidDataException("Nie udało się odczytać obrazu ze schowka.");
        source?.Dispose(); source = bitmap; sourcePath = null;
        DisplaySource(); SelectAll(); Info.Text = $"Obraz ze schowka: {source.Width} × {source.Height}";
    }

    private void DisplaySource()
    {
        if (source is null) return;
        double availableW = Math.Max(250, ActualWidth - 390), availableH = Math.Max(250, ActualHeight - 95);
        double factor = Math.Min(1, Math.Min(availableW / source.Width, availableH / source.Height));
        displayWidth = Math.Max(1, Math.Round(source.Width * factor));
        displayHeight = Math.Max(1, Math.Round(source.Height * factor));
        using var image = SKImage.FromBitmap(source);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream(encoded.ToArray());
        var bi = new BitmapImage(); bi.BeginInit(); bi.CacheOption = BitmapCacheOption.OnLoad; bi.StreamSource = ms; bi.EndInit(); bi.Freeze();
        Photo.Source = bi;
        Photo.Width = displayWidth; Photo.Height = displayHeight;
        ImageSurface.Width = displayWidth; ImageSurface.Height = displayHeight;
        Overlay.Width = displayWidth; Overlay.Height = displayHeight;
        EmptyHint.Visibility = Visibility.Collapsed;
        crop = new Rect(crop.X, crop.Y, Math.Min(crop.Width, source.Width), Math.Min(crop.Height, source.Height));
        DrawCrop();
    }

    private void SelectAll()
    {
        if (source is null) return;
        var ratio = Ratio();
        double w = source.Width, h = source.Height;
        if (ratio > 0) { if (w / h > ratio) w = h * ratio; else h = w / ratio; }
        crop = new Rect((source.Width - w) / 2, (source.Height - h) / 2, w, h);
        DrawCrop();
    }

    private Point ToPixels(Point p) => new(Math.Clamp(p.X / displayWidth * source!.Width, 0, source.Width), Math.Clamp(p.Y / displayHeight * source.Height, 0, source.Height));

    private void Surface_Down(object sender, MouseButtonEventArgs e)
    {
        if (source is null) return;
        anchor = ToPixels(e.GetPosition(ImageSurface));
        moving = crop.Contains(anchor);
        originalCrop = crop;
        if (!moving) crop = new Rect(anchor, anchor);
        dragging = true; ImageSurface.CaptureMouse(); DrawCrop();
    }
    private void Surface_Move(object sender, MouseEventArgs e)
    {
        if (!dragging || source is null) return;
        Point p = ToPixels(e.GetPosition(ImageSurface));
        if (moving)
        {
            double x = Math.Clamp(originalCrop.X + p.X - anchor.X, 0, source.Width - originalCrop.Width);
            double y = Math.Clamp(originalCrop.Y + p.Y - anchor.Y, 0, source.Height - originalCrop.Height);
            crop = new Rect(x, y, originalCrop.Width, originalCrop.Height);
        }
        else
        {
            double dx = p.X - anchor.X, dy = p.Y - anchor.Y, ratio = Ratio();
            if (ratio > 0)
            {
                double sx = Math.Sign(dx == 0 ? 1 : dx), sy = Math.Sign(dy == 0 ? 1 : dy);
                double width = Math.Min(Math.Abs(dx), Math.Abs(dy) * ratio);
                if (Math.Abs(dy) < 1) width = Math.Abs(dx);
                double height = width / ratio;
                width = Math.Min(width, sx > 0 ? source.Width - anchor.X : anchor.X);
                height = Math.Min(height, sy > 0 ? source.Height - anchor.Y : anchor.Y);
                width = Math.Min(width, height * ratio); height = width / ratio;
                dx = sx * width; dy = sy * height;
            }
            crop = new Rect(new Point(Math.Min(anchor.X, anchor.X + dx), Math.Min(anchor.Y, anchor.Y + dy)), new Point(Math.Max(anchor.X, anchor.X + dx), Math.Max(anchor.Y, anchor.Y + dy)));
        }
        DrawCrop();
    }
    private void Surface_Up(object sender, MouseButtonEventArgs e) { if (!dragging) return; dragging = false; ImageSurface.ReleaseMouseCapture(); if (crop.Width < 2 || crop.Height < 2) SelectAll(); }
    private void Surface_Leave(object sender, MouseEventArgs e) { /* Capture remains active while dragging outside image. */ }
    private void DrawCrop()
    {
        if (source is null || displayWidth <= 0) return;
        var x = crop.X / source.Width * displayWidth; var y = crop.Y / source.Height * displayHeight;
        var w = crop.Width / source.Width * displayWidth; var h = crop.Height / source.Height * displayHeight;
        foreach (Rectangle r in new[] { CropBox, CropOutline }) { Canvas.SetLeft(r, x); Canvas.SetTop(r, y); r.Width = w; r.Height = h; r.Visibility = w > 0 && h > 0 ? Visibility.Visible : Visibility.Collapsed; }
        UpdateOutput();
    }
    private void UpdateOutput()
    {
        if (OutputSize is null) return;
        if (crop.Width <= 0 || crop.Height <= 0 || !int.TryParse(OutputWidth?.Text, out int w) || w < 1 || w > 20000) { OutputSize.Text = "Podaj szerokość 1–20000 px"; return; }
        int h = (int)Math.Round(w * crop.Height / crop.Width);
        OutputSize.Text = $"Wynik: {w} × {h} px";
        FileEstimate?.Text = "Rozmiar: — (kliknij Sprawdź)";
    }
    private void Ratio_Changed(object sender, SelectionChangedEventArgs e) { CustomRatio?.Visibility = (RatioBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "custom" ? Visibility.Visible : Visibility.Collapsed; SelectAll(); }
    private void RatioCustom_Changed(object sender, TextChangedEventArgs e) { if (source is not null) SelectAll(); }
    private void Settings_Changed(object sender, RoutedEventArgs e) { if (QualityText is not null && QualitySlider is not null) QualityText.Text = $"{QualitySlider.Value:0}%"; UpdateOutput(); }
    private void SelectAll_Click(object sender, RoutedEventArgs e) => SelectAll();
    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.tif;*.tiff;*.gif|Wszystkie pliki|*.*" };
        if (dlg.ShowDialog() == true) Safe(() => LoadImage(dlg.FileName));
    }
    private void Paste_Click(object sender, RoutedEventArgs e) => Safe(() =>
    {
        if (!Clipboard.ContainsImage()) throw new InvalidOperationException("Schowek nie zawiera obrazu.");
        var bitmap = Clipboard.GetImage()!;
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var ms = new MemoryStream(); encoder.Save(ms); LoadBytes(ms.ToArray());
    });
    private void Window_DragOver(object sender, DragEventArgs e) { e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; }
    private void Window_Drop(object sender, DragEventArgs e) { if (e.Data.GetData(DataFormats.FileDrop) is string[] paths && paths.Length > 0) Safe(() => LoadImage(paths[0])); }
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        if (e.Key == Key.O) Open_Click(sender, new RoutedEventArgs());
        else if (e.Key == Key.S) Save_Click(sender, new RoutedEventArgs());
        else if (e.Key == Key.V && !((Keyboard.FocusedElement is TextBox))) Paste_Click(sender, new RoutedEventArgs());
        else return;
        e.Handled = true;
    }
    private byte[] Encode()
    {
        if (source is null) throw new InvalidOperationException("Najpierw otwórz obraz.");
        if (!int.TryParse(OutputWidth.Text, out var width) || width < 1 || width > 20000) throw new InvalidOperationException("Szerokość musi być z zakresu 1–20000 px.");
        if (crop.Width < 2 || crop.Height < 2) throw new InvalidOperationException("Zaznacz kadr.");
        int height = (int)Math.Round(width * crop.Height / crop.Width);
        if (height < 1 || height > 20000 || (long)width * height > 100_000_000) throw new InvalidOperationException("Wymiary wynikowe są zbyt duże.");
        var rect = new SKRectI(Math.Clamp((int)Math.Round(crop.Left), 0, source.Width - 1), Math.Clamp((int)Math.Round(crop.Top), 0, source.Height - 1), Math.Clamp((int)Math.Round(crop.Right), 1, source.Width), Math.Clamp((int)Math.Round(crop.Bottom), 1, source.Height));
        if (rect.Width <= 0 || rect.Height <= 0) throw new InvalidOperationException("Nieprawidłowy kadr.");
        using var output = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(output))
        using (var paint = new SKPaint { IsAntialias = true})
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(source, rect, new SKRect(0, 0, width, height), paint);
            canvas.Flush();
        }
        using var image = SKImage.FromBitmap(output);
        // SkiaSharp exposes lossless WebP through SKWebpEncoderOptions; quality slider is for lossy mode.
        using var data = image.Encode(    SKEncodedImageFormat.Webp,    (int)Math.Round(QualitySlider.Value)
);
        return data?.ToArray() ?? throw new InvalidOperationException("Kodowanie WebP nie powiodło się.");
    }
    private void Estimate_Click(object sender, RoutedEventArgs e) => Safe(() => { var bytes = Encode(); FileEstimate.Text = $"Szacowany rozmiar: {bytes.Length / 1024d:N1} KiB"; });
    private void Save_Click(object sender, RoutedEventArgs e) => Safe(() =>
    {
        var bytes = Encode();
        var suggested = sourcePath is null ? "obraz.webp" : System.IO.Path.GetFileNameWithoutExtension(sourcePath) + "-web.webp";
        var dlg = new SaveFileDialog { Filter = "WebP (*.webp)|*.webp", FileName = suggested, DefaultExt = ".webp", AddExtension = true };
        if (dlg.ShowDialog() != true) return;
        File.WriteAllBytes(dlg.FileName, bytes);
        FileEstimate.Text = $"Rozmiar: {bytes.Length / 1024d:N1} KiB";
        Info.Text = $"Zapisano: {dlg.FileName}";
    });
    private void Safe(Action action) { try { action(); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "ImageCrop WebP", MessageBoxButton.OK, MessageBoxImage.Warning); } }
    protected override void OnClosed(EventArgs e) { source?.Dispose(); base.OnClosed(e); }
}
