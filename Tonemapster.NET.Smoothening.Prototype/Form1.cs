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
        private const int PercentileHistogramBinCount = 4096;
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

        private void ApplySmoothing()
        {
            if (loadedImage is null)
            {
                return;
            }

            using Mat preview = CreatePreviewImage(loadedImage, trackBarSmoothing.Value, GetDetailBoost(), GetSigmaColor());
            using Mat displayImage = CreateDisplayImage(preview);
            Bitmap bitmap = Form1Helpers.MatToBitmap(displayImage);

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
            using Mat enhancedLogLuminance = EnhanceLogLuminance(logLuminance, strength, detailBoost, sigmaColor);
            using Mat enhancedLuminance = new();
            Cv2.Normalize(enhancedLogLuminance, enhancedLogLuminance, 0, 1, NormTypes.MinMax);
            Cv2.Exp(enhancedLogLuminance, enhancedLuminance);
            ClampToPercentileRangeAndNormalize(enhancedLuminance).CopyTo(enhancedLuminance);
            
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

        private static Mat CreateDisplayImage(Mat image)
        {
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

        private static Mat ClampToPercentileRangeAndNormalize(Mat image)
        {
            if (image.Empty())
            {
                throw new ArgumentException("Image must not be empty.", nameof(image));
            }

            if (image.Type() != MatType.CV_32FC1)
            {
                throw new ArgumentException("Expected a single-channel CV_32FC1 image.", nameof(image));
            }

            Cv2.MinMaxLoc(image, out double minValue, out double maxValue);

            if (maxValue <= minValue)
            {
                return new Mat(image.Size(), MatType.CV_32FC1, Scalar.All(0));
            }

            float histogramMin = (float)minValue;
            float histogramMax = (float)maxValue;
            if (histogramMax <= histogramMin)
            {
                histogramMax = histogramMin + 1e-6f;
            }

            using Mat histogram = new();
            Cv2.CalcHist(
                [image],
                [0],
                null,
                histogram,
                1,
                [PercentileHistogramBinCount],
                [new Rangef(histogramMin, histogramMax)],
                true,
                false);

            double totalPixelCount = image.Total();
            double lowerTargetCount = totalPixelCount * 0.01;
            double upperTargetCount = totalPixelCount * 0.99;
            double binWidth = (histogramMax - histogramMin) / PercentileHistogramBinCount;

            double GetPercentileValue(double targetCount)
            {
                double cumulativeCount = 0;

                for (int i = 0; i < PercentileHistogramBinCount; i++)
                {
                    float binCount = histogram.Get<float>(i);
                    double nextCumulativeCount = cumulativeCount + binCount;

                    if (nextCumulativeCount >= targetCount)
                    {
                        double fraction = binCount > 0
                            ? (targetCount - cumulativeCount) / binCount
                            : 0;
                        fraction = Math.Clamp(fraction, 0, 1);
                        return histogramMin + ((i + fraction) * binWidth);
                    }

                    cumulativeCount = nextCumulativeCount;
                }

                return histogramMax;
            }

            double lowerValue = GetPercentileValue(lowerTargetCount);
            double upperValue = GetPercentileValue(upperTargetCount);

            if (upperValue <= lowerValue)
            {
                return new Mat(image.Size(), MatType.CV_32FC1, Scalar.All(0));
            }

            Mat clamped = image.Clone();
            using Mat lowerBound = new(image.Size(), MatType.CV_32FC1, Scalar.All(lowerValue));
            using Mat upperBound = new(image.Size(), MatType.CV_32FC1, Scalar.All(upperValue));

            Cv2.Max(clamped, lowerBound, clamped);
            Cv2.Min(clamped, upperBound, clamped);

            Cv2.Subtract(clamped, Scalar.All(lowerValue), clamped);
            Cv2.Multiply(clamped, Scalar.All(1.0 / (upperValue - lowerValue)), clamped);
            return clamped;
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

        

        private void DisposeLoadedImage()
        {
            loadedImage?.Dispose();
            loadedImage = null;
            loadedImagePath = null;
        }
    }
}
