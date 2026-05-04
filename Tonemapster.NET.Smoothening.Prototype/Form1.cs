using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenCvSharp;
using Sdcb.LibRaw;

namespace Tonemapster.NET.Smoothening.Prototype
{
    public partial class Form1 : Form
    {
        private static readonly string[] HdrExtensions = [".hdr"];
        private const int TargetPixelCount = 700_000;
        private const double LuminanceEpsilon = 1e-6;
        private const double SigmaColorMin = 0.01;
        private const double SigmaColorMax = 10.0;
        private Mat? loadedImage;
        private string? loadedImagePath;

        public Form1()
        {
            InitializeComponent();

            string initialImagePath = "D:\\Tonemapster.Vibe.TS.Gauss\\public\\assets\\Columns.hdr";
            if (File.Exists(initialImagePath))
            {
                LoadImage(initialImagePath);
            }
        }

        private void HandleDragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void HandleFileDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            {
                return;
            }

            try
            {
                LoadImage(files[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Failed to load image", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadImage(string filePath)
        {
            using Mat sourceImage = IsHdrFile(filePath)
                ? Form1Helpers.LoadHdrImage(filePath)
                : Form1Helpers.LoadRawImage(filePath);

            Mat image = DownscaleToTargetPixelCount(sourceImage, TargetPixelCount);

            loadedImage?.Dispose();
            loadedImage = image;
            loadedImagePath = filePath;

            ApplySmoothing();
        }

        private static bool IsHdrFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            return HdrExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        private void HandleSmoothingStrengthScroll(object? sender, EventArgs e)
        {
            labelSmoothing.Text = $"Smoothing: {trackBarSmoothing.Value}";
            ApplySmoothing();
        }

        private void HandleDetailBoostScroll(object? sender, EventArgs e)
        {
            labelDetailBoost.Text = $"Detail Boost: {GetDetailBoost():0.0}";
            ApplySmoothing();
        }

        private void HandleSigmaColorScroll(object? sender, EventArgs e)
        {
            labelSigmaColor.Text = $"Sigma Color: {GetSigmaColor():0.00}";
            ApplySmoothing();
        }

        private void HandleSmoothingStrengthMouseUp(object? sender, MouseEventArgs e)
        {
            ApplySmoothing();
        }

        private void HandleSmoothingStrengthMouseCaptureChanged(object? sender, EventArgs e)
        {
            ApplySmoothing();
        }

        private double GetDetailBoost()
        {
            return trackBarDetailBoost.Value / 10.0;
        }

        private double GetSigmaColor()
        {
            double position = (double)trackBarSigmaColor.Value / trackBarSigmaColor.Maximum;
            return SigmaColorMin * Math.Pow(SigmaColorMax / SigmaColorMin, position);
        }

        private void ApplySmoothing()
        {
            if (loadedImage is null)
            {
                return;
            }

            using Mat preview = MultiscaleTonemapper.CreateTonemappedImage(loadedImage, trackBarSmoothing.Value, GetDetailBoost(), GetSigmaColor());
            using Mat displayImage = CreateDisplayImage(preview);
            Bitmap bitmap = Form1Helpers.MatToBitmap(displayImage);

            Image? previousImage = pictureBoxPreview.Image;
            pictureBoxPreview.Image = bitmap;
            previousImage?.Dispose();
            Text = $"{Path.GetFileName(loadedImagePath)} - Preview";
        }

        private static Mat DownscaleToTargetPixelCount(Mat image, int targetPixelCount)
        {
            long pixelCount = (long)image.Width * image.Height;
            if (pixelCount <= targetPixelCount)
            {
                return image.Clone();
            }

            double scale = Math.Sqrt((double)targetPixelCount / pixelCount);
            OpenCvSharp.Size targetSize = new(
                Math.Max(1, (int)Math.Round(image.Width * scale)),
                Math.Max(1, (int)Math.Round(image.Height * scale)));

            Mat resized = new();
            Cv2.Resize(image, resized, targetSize, 0, 0, InterpolationFlags.Area);
            return resized;
        }

        private static Mat CreateDisplayImage(Mat image)
        {
            Mat rawDisplay = new();
            image.ConvertTo(rawDisplay, MatType.CV_8UC3, 255.0);
            return rawDisplay;
        }

        private void DisposeLoadedImage()
        {
            loadedImage?.Dispose();
            loadedImage = null;
            loadedImagePath = null;
        }
    }
}
