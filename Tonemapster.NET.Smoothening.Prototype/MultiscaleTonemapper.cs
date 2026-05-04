using System.Diagnostics;
using OpenCvSharp;
using OpenCvSharp.XImgProc;

namespace Tonemapster.NET.Smoothening.Prototype
{
    internal static class MultiscaleTonemapper
    {
        private const double LuminanceEpsilon = 1e-6;

        public static Mat CreateTonemappedImage(Mat image, int strength, double detailBoost, double sigmaColor)
        {
            using Mat originalLuminance = GeneralImageProcessingHelpers.ComputeLuminance(image);
            using Mat logLuminance = CreateLogLuminance(originalLuminance);
            using Mat enhancedLogLuminance = EnhanceDetails(logLuminance, strength, detailBoost, sigmaColor);
            using Mat enhancedLuminance = new();
            Cv2.Normalize(enhancedLogLuminance, enhancedLogLuminance, 0, 1, NormTypes.MinMax);
            Cv2.Exp(enhancedLogLuminance, enhancedLuminance);
            GeneralImageProcessingHelpers.ClampToPercentileRangeAndNormalize(enhancedLuminance).CopyTo(enhancedLuminance);

            return ReapplyChroma(image, originalLuminance, enhancedLuminance);
        }

        public static Mat EnhanceDetails(Mat input, int strength, double detailBoost, double sigmaColor)
        {
            const int layerCount = 5;
            double baseSigmaSpatial = 1;
            List<Mat> detailLayers = new(layerCount);
            Mat currentBase = input.Clone();

            try
            {
                for (int i = 0; i < layerCount; i++)
                {
                    double sigmaSpatial = baseSigmaSpatial * Math.Pow(2, i);
                    Mat nextBase = new();
                    Mat detailLayer = new();

                    Debug.WriteLine($"Applying edge-aware filter layer {i + 1}/{layerCount} with sigmaSpatial={sigmaSpatial}, sigmaColor={sigmaColor}");
                    CvXImgProc.DTFilter(currentBase, currentBase, nextBase, sigmaSpatial, sigmaColor, EdgeAwareFiltersList.DTF_RF, 3);
                    Cv2.Subtract(currentBase, nextBase, detailLayer);
                    detailLayers.Add(detailLayer);

                    currentBase.Dispose();
                    currentBase = nextBase;
                }

                Mat recomposed = currentBase.Clone();

                float layerIndex = 0;
                foreach (Mat detailLayer in detailLayers)
                {
                    layerIndex++;
                    using Mat boostedDetailLayer = new();
                    Cv2.Multiply(detailLayer, Math.Pow(detailBoost, layerIndex), boostedDetailLayer);
                    Cv2.Add(recomposed, boostedDetailLayer, recomposed);
                }

                return recomposed;
            }
            finally
            {
                currentBase.Dispose();

                foreach (Mat detailLayer in detailLayers)
                {
                    detailLayer.Dispose();
                }
            }
        }

        public static Mat CreateLogLuminance(Mat luminance)
        {
            using Mat safeLuminance = new();
            Mat logImage = new();

            Cv2.Add(luminance, Scalar.All(LuminanceEpsilon), safeLuminance);
            Cv2.Log(safeLuminance, logImage);
            return logImage;
        }

        public static Mat ReapplyChroma(Mat input, Mat originalLuminance, Mat targetLuminance)
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
    }
}
