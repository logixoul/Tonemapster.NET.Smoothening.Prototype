using OpenCvSharp;
using Sdcb.LibRaw;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Tonemapster.NET.Smoothening.Prototype
{
    internal static class Form1Helpers
    {

        public static Mat EnsureThreeChannels(Mat source)
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

        public static Mat LoadHdrImage(string filePath)
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

        public static Mat LoadRawImage(string filePath)
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
            return RawProcessedImageToMat(image);
        }

        public static Bitmap MatToBitmap(Mat mat)
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

        private static Mat RawProcessedImageToMat(ProcessedImage rgbImage)
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
    }
}