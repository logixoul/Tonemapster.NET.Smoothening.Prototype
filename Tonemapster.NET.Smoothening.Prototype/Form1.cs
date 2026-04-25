using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.XImgProc;
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
                ? LoadHdrImage(filePath)
                : LoadRawImage(filePath);

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

        private void ApplySmoothing()
        {
            if (loadedImage is null)
            {
                return;
            }

            bool isHdrImage = IsHdrFile(loadedImagePath ?? string.Empty);
            using Mat preview = CreatePreviewImage(loadedImage, trackBarSmoothing.Value, GetDetailBoost(), GetSigmaColor());
            using Mat displayImage = CreateDisplayImage(preview, isHdrImage);
            Bitmap bitmap = MatToBitmap(displayImage);

            Image? previousImage = pictureBoxPreview.Image;
            pictureBoxPreview.Image = bitmap;
            previousImage?.Dispose();
            Text = $"{Path.GetFileName(loadedImagePath)} - Preview";
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

        private static Mat CreatePreviewImage(Mat image, int strength, double detailBoost, double sigmaColor)
        {
            using Mat originalLuminance = ComputeLuminance(image);
            using Mat logLuminance = CreateLogLuminance(originalLuminance);
            using Mat enhancedLogLuminance = strength <= 0
                ? logLuminance.Clone()
                : EnhanceLogLuminance(logLuminance, strength, detailBoost, sigmaColor);
            using Mat enhancedLuminance = new();

            Cv2.Exp(enhancedLogLuminance, enhancedLuminance);
            return ReapplyChroma(image, originalLuminance, enhancedLuminance);
        }

        private static Mat EnhanceLogLuminance(Mat logLuminance, int strength, double detailBoost, double sigmaColor)
        {
            Mat filtered = new();
            using Mat details = new();
            double sigmaSpatial = 10 + (strength * 2);

            Debug.WriteLine($"Applying edge-aware filter with sigmaSpatial={sigmaSpatial}, sigmaColor={sigmaColor}");
            CvXImgProc.DTFilter(logLuminance, logLuminance, filtered, sigmaSpatial, sigmaColor, EdgeAwareFiltersList.DTF_RF, 3);
            Cv2.Subtract(logLuminance, filtered, details);
            Cv2.Multiply(details, detailBoost, details);
            Cv2.Add(logLuminance, details, filtered);
            return filtered;
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

        private static Mat LoadRawImage(string filePath)
        {
            using RawContext raw = RawContext.OpenFile(filePath);
            raw.Unpack();
            raw.DcrawProcess(options =>
            {
                options.UseCameraWb = true;
                options.OutputBps = 16;
                options.OutputTiff = false;
            });

            using ProcessedImage image = raw.MakeDcrawMemoryImage();
            return ProcessedImageToMat(image);
        }

        private static Mat LoadHdrImage(string filePath)
        {
            using Mat hdr = Cv2.ImRead(filePath, ImreadModes.AnyColor | ImreadModes.AnyDepth);

            if (hdr.Empty())
            {
                throw new InvalidOperationException("Unable to load HDR image.");
            }

            using Mat colorHdr = EnsureThreeChannels(hdr);
            Mat floatHdr = new();
            colorHdr.ConvertTo(floatHdr, MatType.CV_32FC3);
            return floatHdr;
        }

        private static Mat CreateDisplayImage(Mat image, bool isHdrImage)
        {
            if (isHdrImage)
            {
                using Mat originalLuminance = ComputeLuminance(image);
                using Mat logLuminance = CreateLogLuminance(originalLuminance);
                using Mat normalizedLogLuminance = new();
                using Mat displayLuminance = new();
                using Mat normalizedDisplayLuminance = new();
                Mat display = new();

                Cv2.Normalize(logLuminance, normalizedLogLuminance, 0, 1, NormTypes.MinMax);
                Cv2.Exp(normalizedLogLuminance, displayLuminance);
                Cv2.Normalize(displayLuminance, normalizedDisplayLuminance, 0, 1, NormTypes.MinMax);
                using Mat chromaPreservedDisplay = ReapplyChroma(image, originalLuminance, normalizedDisplayLuminance);
                chromaPreservedDisplay.ConvertTo(display, MatType.CV_8UC3, 255.0);
                return display;
            }

            Mat rawDisplay = new();
            image.ConvertTo(rawDisplay, MatType.CV_8UC3, 255.0);
            return rawDisplay;
        }

        private static Mat CreateLogLuminance(Mat luminance)
        {
            using Mat safeLuminance = new();
            Mat logImage = new();

            Cv2.Add(luminance, Scalar.All(LuminanceEpsilon), safeLuminance);
            Cv2.Log(safeLuminance, logImage);
            return logImage;
        }

        private static Mat ComputeLuminance(Mat image)
        {
            Mat[] channels = Cv2.Split(image);

            try
            {
                using Mat blueWeighted = new();
                using Mat greenWeighted = new();
                Mat luminance = new();

                Cv2.Multiply(channels[0], Scalar.All(0.0722), blueWeighted);
                Cv2.Multiply(channels[1], Scalar.All(0.7152), greenWeighted);
                Cv2.Add(blueWeighted, greenWeighted, luminance);
                Cv2.AddWeighted(luminance, 1.0, channels[2], 0.2126, 0.0, luminance);
                return luminance;
            }
            finally
            {
                foreach (Mat channel in channels)
                {
                    channel.Dispose();
                }
            }
        }

        private static Mat ReapplyChroma(Mat input, Mat originalLuminance, Mat targetLuminance)
        {
            using Mat safeOriginalLuminance = new();
            using Mat luminanceRatio = new();
            using Mat luminanceRatio3 = new();
            Mat output = new();

            Cv2.Add(originalLuminance, Scalar.All(LuminanceEpsilon), safeOriginalLuminance);
            Cv2.Divide(targetLuminance, safeOriginalLuminance, luminanceRatio);
            Cv2.Merge([luminanceRatio, luminanceRatio, luminanceRatio], luminanceRatio3);
            Cv2.Multiply(input, luminanceRatio3, output);
            return output;
        }

        private static Mat EnsureThreeChannels(Mat source)
        {
            Mat destination = new();

            switch (source.Channels())
            {
                case 1:
                    Cv2.CvtColor(source, destination, ColorConversionCodes.GRAY2BGR);
                    break;
                case 3:
                    source.CopyTo(destination);
                    break;
                case 4:
                    Cv2.CvtColor(source, destination, ColorConversionCodes.BGRA2BGR);
                    break;
                default:
                    throw new NotSupportedException("Unsupported image channel count.");
            }

            return destination;
        }

        private static Bitmap MatToBitmap(Mat mat)
        {
            Bitmap bitmap = new(mat.Width, mat.Height, PixelFormat.Format24bppRgb);
            Rectangle bounds = new(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            try
            {
                int srcStride = (int)mat.Step();
                int rowLength = mat.Width * mat.ElemSize();
                byte[] rowBuffer = new byte[rowLength];

                for (int y = 0; y < mat.Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(mat.Data, y * srcStride), rowBuffer, 0, rowLength);
                    Marshal.Copy(rowBuffer, 0, IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride), rowLength);
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        private static Mat ProcessedImageToMat(ProcessedImage rgbImage)
        {
            if (rgbImage.Bits != 16)
            {
                throw new NotSupportedException($"Expected 16-bit RAW output, but got {rgbImage.Bits}-bit.");
            }

            using Mat rawMat = Mat.FromPixelData(
                rgbImage.Height,
                rgbImage.Width,
                MatType.CV_16UC3,
                rgbImage.DataPointer,
                rgbImage.Width * rgbImage.Channels * sizeof(ushort));

            Mat floatMat = new();
            Mat bgrFloatMat = new();
            rawMat.ConvertTo(floatMat, MatType.CV_32FC3, 1.0 / ushort.MaxValue);
            Cv2.CvtColor(floatMat, bgrFloatMat, ColorConversionCodes.RGB2BGR);
            floatMat.Dispose();
            return bgrFloatMat;
        }

        private void DisposeLoadedImage()
        {
            loadedImage?.Dispose();
            loadedImage = null;
            loadedImagePath = null;
        }
    }
}
