using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

namespace Habbo_Downloader.App.Gui
{
    /// <summary>
    /// Avalonia bootstrap with the Fluent theme.
    /// </summary>
    public sealed class AvaloniaApp : Avalonia.Application
    {
        public override void Initialize()
        {
            Styles.Add(new FluentTheme());
            Habbo_Downloader.App.Professional.ProfessionalTheme.Install(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();
        }
    }
}
