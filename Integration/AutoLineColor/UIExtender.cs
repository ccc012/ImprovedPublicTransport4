using ICities;
using JetBrains.Annotations;

namespace AutoLineColor
{
    /// <summary>
    /// Historically created its own "RefreshNameAndColor" button on the crowded LinesOverview
    /// row. That duplicated the reliable IPT button PanelExtenderLine.CreateAutoColorRefreshButton
    /// now places next to the colour field (see the class comment there), so both showed up at
    /// once, cramped and easy to click by mistake. Kept as an empty LoadingExtensionBase - the
    /// game discovers this class directly (see README "Adding an integration") - in case a
    /// future strategy needs an AutoLineColor-specific level-load hook again.
    /// </summary>
    [UsedImplicitly]
    public class UIExtender : LoadingExtensionBase
    {
    }
}
