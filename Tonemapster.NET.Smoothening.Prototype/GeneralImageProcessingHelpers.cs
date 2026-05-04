using OpenCvSharp;

namespace Tonemapster.NET.Smoothening.Prototype
{
    internal static class GeneralImageProcessingHelpers
    {
        private const int PercentileHistogramBinCount = 4096;
        public static Mat ClampToPercentileRangeAndNormalize(Mat image)
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

        public static Mat ComputeLuminance(Mat image)
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
    }
}