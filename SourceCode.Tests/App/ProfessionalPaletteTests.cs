using Avalonia.Media;
using Habbo_Downloader.App.Professional;
using Xunit;

namespace Habbo_Downloader.Tests.App;

public sealed class ProfessionalPaletteTests
{
    [Fact]
    public void SidebarTextAndCallToActionMeetAccessibleContrast()
    {
        Assert.True(ContrastRatio(ProfessionalPalette.SidebarText, ProfessionalPalette.SidebarBackground) >= 7.0);
        Assert.True(ContrastRatio(ProfessionalPalette.SidebarMutedText, ProfessionalPalette.SidebarBackground) >= 4.5);
        Assert.True(ContrastRatio(ProfessionalPalette.CallToActionText, ProfessionalPalette.CallToAction) >= 4.5);
    }

    [Fact]
    public void NavigationStatesAreVisuallyDistinct()
    {
        Assert.NotEqual(ProfessionalPalette.SidebarBackground, ProfessionalPalette.NavigationHover);
        Assert.NotEqual(ProfessionalPalette.NavigationHover, ProfessionalPalette.NavigationSelected);
        Assert.NotEqual(ProfessionalPalette.NavigationSelected, ProfessionalPalette.SelectionIndicator);
    }

    private static double ContrastRatio(Color foreground, Color background)
    {
        double lighter = Math.Max(Luminance(foreground), Luminance(background));
        double darker = Math.Min(Luminance(foreground), Luminance(background));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double Luminance(Color color) =>
        (0.2126 * Linear(color.R)) + (0.7152 * Linear(color.G)) + (0.0722 * Linear(color.B));

    private static double Linear(byte component)
    {
        double channel = component / 255.0;
        return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
