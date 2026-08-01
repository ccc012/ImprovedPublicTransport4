using System.Linq;
using UnityEngine;
using ImprovedPublicTransport;
using CSLModsCommon.Extension;

namespace AutoLineColor.Coloring
{
    internal static class ColorSelector
    {
        public static IColorSelector DifferenceThreshold { get; } = new DifferenceThresholdSelector();
        public static IColorSelector LeastUsed { get; } = new LeastUsedSelector();

        private class DifferenceThresholdSelector : IColorSelector
        {
            public Color32 SelectColor(in TransportLine transportLine, IColorSet colorSet, IUsedColors usedColors, IColorDistanceMetric metric)
            {
                var colors = colorSet.GetColors();
                var threshold = ModSetting.Instance.AutoLineColorMinColorDiffPercentage / 100f;

                for (var i = 0; i < ModSetting.Instance.AutoLineColorMaxDiffColorPickAttempt; i++)
                {
                    var candidate = colors[Random.Range(0, colors.Count)];

                    if (usedColors.MeasureNovelty(candidate, metric) >= threshold)
                    {
                        return candidate;
                    }
                }

                // nothing was above the threshold
                return colors.DefaultIfEmpty(Color.black)
                    .MaxBy(candidate => usedColors.MeasureNovelty(candidate, metric));
            }
        }

        private class LeastUsedSelector : IColorSelector
        {
            public Color32 SelectColor(in TransportLine transportLine, IColorSet colorSet, IUsedColors usedColors,
                IColorDistanceMetric metric)
            {
                var colors = colorSet.GetColors();
                return colors.DefaultIfEmpty(Color.black)
                    .MinBy(usedColors.CountPreviousUses);
            }
        }
    }
}
