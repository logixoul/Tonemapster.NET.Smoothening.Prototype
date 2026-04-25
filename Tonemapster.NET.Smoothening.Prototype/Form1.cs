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

            using Mat preview = CreatePreviewImage(loadedImage, trackBarSmoothing.Value);
            using Mat displayImage = CreateDisplayImage(preview, IsHdrFile(loadedImagePath ?? string.Empty));
            Bitmap bitmap = MatToBitmap(displayImage);

            Image? previousImage = pictureBoxPreview.Image;
            pictureBoxPreview.Image = bitmap;
            previousImage?.Dispose();
            Text = $"{Path.GetFileName(loadedImagePath)} - Preview";
        }

        private static Mat CreatePreviewImage(Mat image, int strength)
        {
            if (strength <= 0)
            {
                return image.Clone();
            }

            Mat filtered = new();
            double sigmaSpatial = 10 + (strength * 2);
            double sigmaColor = Math.Max(0.01, strength / 20.0);

            Debug.WriteLine($"Applying DTFilter with sigmaSpatial={sigmaSpatial} and sigmaColor={sigmaColor}");
            CvXImgProc.DTFilter(image, image, filtered, sigmaSpatial, sigmaColor, EdgeAwareFiltersList.DTF_RF, 3);
            Debug.WriteLine($"Applied DTFilter with sigmaSpatial={sigmaSpatial} and sigmaColor={sigmaColor}");
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
                using Mat hdrWithEpsilon = new();
                using Mat logImage = new();
                using Mat normalized = new();
                Mat display = new();

                Cv2.Add(image, Scalar.All(1e-6), hdrWithEpsilon);
                Cv2.Log(hdrWithEpsilon, logImage);
                Cv2.Normalize(logImage, normalized, 0, 255, NormTypes.MinMax);
                normalized.ConvertTo(display, MatType.CV_8UC3);
                return display;
            }

            Mat rawDisplay = new();
            image.ConvertTo(rawDisplay, MatType.CV_8UC3, 255.0);
            return rawDisplay;
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
            rgbImage.SwapRGB();

            if (rgbImage.Bits == 16)
            {
                using Mat mat16 = Mat.FromPixelData(rgbImage.Height, rgbImage.Width, MatType.CV_16UC3, rgbImage.DataPointer, rgbImage.Width * rgbImage.Channels * sizeof(ushort));
                Mat floatMat16 = new();
                mat16.ConvertTo(floatMat16, MatType.CV_32FC3, 1.0 / ushort.MaxValue);
                return floatMat16;
            }

            using Mat mat8 = Mat.FromPixelData(rgbImage.Height, rgbImage.Width, MatType.CV_8UC3, rgbImage.DataPointer, rgbImage.Width * rgbImage.Channels);
            Mat floatMat8 = new();
            mat8.ConvertTo(floatMat8, MatType.CV_32FC3, 1.0 / byte.MaxValue);
            return floatMat8;
        }

        private void DisposeLoadedImage()
        {
            loadedImage?.Dispose();
            loadedImage = null;
            loadedImagePath = null;
        }
    }
}
