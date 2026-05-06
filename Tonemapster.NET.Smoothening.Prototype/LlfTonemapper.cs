using System.Diagnostics;
using OpenCvSharp;
using OpenCvSharp.XImgProc;
using UMapx.Transform;

namespace Tonemapster.NET.Smoothening.Prototype
{
    internal static class LlfTonemapper
    {
        private const double LuminanceEpsilon = 1e-6;
        private static readonly LocalLaplacianFilter llf = new LocalLaplacianFilter();

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
            using Mat inputNormalized = new();
            Cv2.Normalize(input, inputNormalized, 0, 1, NormTypes.MinMax);
            var inputArr = Helpers.ToFloatArray2DFast(inputNormalized);
            //llf.Factor = (float)detailBoost*1000;
            //llf.Radius = strength;
            llf.Sigma = (float)sigmaColor;

            //llf.Radius = 2;
            llf.Radius = strength*2;
            //llf.Sigma = 0.05f;
            llf.N = 20;
            llf.Levels = 2000;
            llf.Factor = (float)detailBoost*30.0f;
            llf.Apply(inputArr);
            Debug.WriteLine("llf.Radius = " + llf.Radius + ";");
            Debug.WriteLine("llf.Sigma = " + llf.Sigma + ";");
            Debug.WriteLine("llf.N = " + llf.N + ";");
            Debug.WriteLine("llf.Levels = " + llf.Levels + ";");
            Debug.WriteLine("llf.Factor = " + llf.Factor + ";");
            return Helpers.ToMatFast(inputArr);
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
