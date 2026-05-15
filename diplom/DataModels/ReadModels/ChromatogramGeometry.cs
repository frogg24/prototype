namespace DataModels.ReadModels
{
    public static class ChromatogramGeometry
    {
        public static int[] ReversePeakLocations(IReadOnlyList<int>? peakLocations, int traceLength)
        {
            if (peakLocations == null || peakLocations.Count == 0 || traceLength <= 0)
            {
                return Array.Empty<int>();
            }

            var result = new int[peakLocations.Count];
            for (var i = 0; i < peakLocations.Count; i++)
            {
                result[i] = traceLength - 1 - peakLocations[peakLocations.Count - 1 - i];
            }

            return result;
        }

        public static ChromatogramWindow SliceByPeakLocations(
            IReadOnlyList<int>? peakLocations,
            int traceLength,
            int totalBases,
            int fromBaseInclusive,
            int toBaseExclusive)
        {
            if (traceLength <= 0 || totalBases <= 0)
            {
                return ChromatogramWindow.Empty;
            }

            var safeFrom = Math.Clamp(fromBaseInclusive, 0, totalBases);
            var safeTo = Math.Clamp(toBaseExclusive, safeFrom, totalBases);
            if (safeFrom >= safeTo)
            {
                return ChromatogramWindow.Empty;
            }

            if (peakLocations == null || peakLocations.Count < totalBases)
            {
                var fallbackStart = (int)Math.Floor(traceLength * (safeFrom / (double)totalBases));
                var fallbackEnd = (int)Math.Ceiling(traceLength * (safeTo / (double)totalBases));
                fallbackStart = Math.Clamp(fallbackStart, 0, traceLength);
                fallbackEnd = Math.Clamp(fallbackEnd, fallbackStart, traceLength);

                return new ChromatogramWindow(
                    fallbackStart,
                    fallbackEnd,
                    Array.Empty<int>(),
                    false);
            }

            var medianSpacing = GetMedianPeakSpacing(peakLocations, totalBases);
            var firstPeak = peakLocations[safeFrom];
            var lastPeak = peakLocations[safeTo - 1];

            var startHalfSpacing = safeFrom > 0
                ? Math.Max(1, (firstPeak - peakLocations[safeFrom - 1]) / 2.0)
                : medianSpacing / 2.0;

            var endHalfSpacing = safeTo < totalBases
                ? Math.Max(1, (peakLocations[safeTo] - lastPeak) / 2.0)
                : medianSpacing / 2.0;

            var startSample = (int)Math.Floor(firstPeak - startHalfSpacing);
            var endSample = (int)Math.Ceiling(lastPeak + endHalfSpacing);

            startSample = Math.Clamp(startSample, 0, traceLength);
            endSample = Math.Clamp(endSample, startSample, traceLength);

            var localPeaks = peakLocations
                .Skip(safeFrom)
                .Take(safeTo - safeFrom)
                .Select(p => p - startSample)
                .ToArray();

            return new ChromatogramWindow(startSample, endSample, localPeaks, true);
        }

        public static double MapSampleToX(
            double sampleIndex,
            IReadOnlyList<int>? localPeakLocations,
            double baseWidth,
            int visibleBaseCount,
            int fallbackTraceLength = 0)
        {
            if (baseWidth <= 0 || visibleBaseCount <= 0)
            {
                return 0;
            }

            if (localPeakLocations == null || localPeakLocations.Count < visibleBaseCount)
            {
                var maxSample = Math.Max(1.0, fallbackTraceLength - 1.0);
                return Math.Clamp(sampleIndex / maxSample, 0.0, 1.0) * visibleBaseCount * baseWidth;
            }

            double CenterOfBase(int baseIndex) => (baseIndex * baseWidth) + (baseWidth / 2.0);

            if (visibleBaseCount == 1)
            {
                var spacing = baseWidth;
                return CenterOfBase(0) + ((sampleIndex - localPeakLocations[0]) / Math.Max(1.0, spacing)) * baseWidth;
            }

            if (sampleIndex <= localPeakLocations[0])
            {
                var peakSpacing = Math.Max(1, localPeakLocations[1] - localPeakLocations[0]);
                return CenterOfBase(0) + ((sampleIndex - localPeakLocations[0]) / peakSpacing) * baseWidth;
            }

            for (var i = 0; i < visibleBaseCount - 1; i++)
            {
                var leftPeak = localPeakLocations[i];
                var rightPeak = localPeakLocations[i + 1];
                if (sampleIndex <= rightPeak)
                {
                    var ratio = (sampleIndex - leftPeak) / Math.Max(1.0, rightPeak - leftPeak);
                    return CenterOfBase(i) + ratio * baseWidth;
                }
            }

            var last = visibleBaseCount - 1;
            var lastSpacing = Math.Max(1, localPeakLocations[last] - localPeakLocations[last - 1]);
            return CenterOfBase(last) + ((sampleIndex - localPeakLocations[last]) / lastSpacing) * baseWidth;
        }

        private static double GetMedianPeakSpacing(IReadOnlyList<int> peakLocations, int totalBases)
        {
            var spacings = new List<int>();
            for (var i = 1; i < totalBases && i < peakLocations.Count; i++)
            {
                var spacing = peakLocations[i] - peakLocations[i - 1];
                if (spacing > 0)
                {
                    spacings.Add(spacing);
                }
            }

            if (spacings.Count == 0)
            {
                return 1.0;
            }

            spacings.Sort();
            var middle = spacings.Count / 2;
            return spacings.Count % 2 == 1
                ? spacings[middle]
                : (spacings[middle - 1] + spacings[middle]) / 2.0;
        }
    }

    public sealed record ChromatogramWindow(
        int StartSample,
        int EndSample,
        int[] LocalPeakLocations,
        bool UsesPeakLocations)
    {
        public static ChromatogramWindow Empty { get; } = new(0, 0, Array.Empty<int>(), false);
    }
}