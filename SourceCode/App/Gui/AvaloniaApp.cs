using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

namespace Habbo_Downloader.App.Gui
{
    /// <summary>
    /// Avalonia bootstrap. Uses Fluent base theme but every window applies
    /// the mainframe color palette via inline Brush properties (see
    /// MainframePalette).
    /// </summary>
    public sealed class AvaloniaApp : Avalonia.Application
    {
        public override void Initialize()
        {
            Styles.Add(new FluentTheme());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();
        }
    }
}
