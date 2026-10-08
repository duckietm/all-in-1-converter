using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;

namespace Habbo_Downloader.App.Professional;

/// <summary>
/// Colours, button styles and icons of the Professional windows. Colours that differ between the light and
/// dark system theme are resources ("Pro.*"), bound with <see cref="Bind"/> so a theme switch repaints them.
/// </summary>
public static class ProfessionalTheme
{
    // Same in both themes.
    public static readonly Color Brand = Color.Parse("#4F5FE6");
    public static readonly Color BrandHover = Color.Parse("#4352D4");
    public static readonly Color BrandPressed = Color.Parse("#3744B8");
    public static readonly Color Sidebar = Color.Parse("#0E1424");
    public static readonly Color SidebarHover = Color.Parse("#1A2338");
    public static readonly Color SidebarSelected = Color.Parse("#232F52");
    public static readonly Color SidebarText = Color.Parse("#C9D2E3");
    public static readonly Color SidebarMuted = Color.Parse("#7283A6");
    public static readonly Color SidebarAccent = Color.Parse("#8B9BFF");
    public static readonly Color ConsoleBackground = Color.Parse("#0B101C");
    public static readonly Color ConsoleText = Color.Parse("#D3DAE6");
    public static readonly Color ConsoleBorder = Color.Parse("#1F293D");

    private static readonly (string Key, string Light, string Dark)[] Tokens =
    [
        ("Pro.Surface", "#F4F6FA", "#0D121C"),
        ("Pro.Card", "#FFFFFF", "#151B28"),
        ("Pro.CardBorder", "#E2E7EF", "#232C3E"),
        ("Pro.CardBorderStrong", "#C9D1DE", "#34405A"),
        ("Pro.Text", "#0F172A", "#E7EBF3"),
        ("Pro.Muted", "#5E6B80", "#97A3B8"),
        ("Pro.Subtle", "#8C97AA", "#66738C"),
        ("Pro.Hover", "#EFF2F7", "#1C2434"),
        ("Pro.Pressed", "#E4E8F0", "#232C3F"),
        ("Pro.Selected", "#ECEFFF", "#1E2550"),
        ("Pro.AccentText", "#4352D4", "#A9B4FF"),
        ("Pro.AccentSoft", "#ECEFFF", "#1E2550"),
        ("Pro.Disabled", "#E9ECF2", "#1B2130"),
        ("Pro.DisabledText", "#9AA4B5", "#5B6680"),
        ("Pro.WarningBack", "#FEF3C7", "#3A2C0C"),
        ("Pro.WarningText", "#8A4B0B", "#F5C451"),
        ("Pro.SuccessBack", "#DCFCE7", "#11301F"),
        ("Pro.SuccessText", "#15803D", "#5BD68A"),
        ("Pro.ErrorBack", "#FEE2E2", "#3A1717"),
        ("Pro.ErrorText", "#B42318", "#F78A80"),
        ("Pro.InfoBack", "#E6F0FF", "#152340"),
        ("Pro.InfoText", "#1D4ED8", "#8AB4FF")
    ];

    private static bool _installed;

    /// <summary>Adds the theme resources and button styles to the application once.</summary>
    public static void Install(Application app)
    {
        if (_installed) return;
        _installed = true;

        var light = new ResourceDictionary();
        var dark = new ResourceDictionary();
        foreach (var (key, lightColor, darkColor) in Tokens)
        {
            light[key] = new SolidColorBrush(Color.Parse(lightColor));
            dark[key] = new SolidColorBrush(Color.Parse(darkColor));
        }
        app.Resources.ThemeDictionaries[ThemeVariant.Light] = light;
        app.Resources.ThemeDictionaries[ThemeVariant.Dark] = dark;

        AddButtonStyles(app.Styles);
    }

    private static void AddButtonStyles(Styles styles)
    {
        IBrush brand = new SolidColorBrush(Brand);

        // primary: the one main action of a view
        ButtonClass(styles, "primary",
            normal: (brand, Brushes.White, Brushes.Transparent),
            hover: (new SolidColorBrush(BrandHover), Brushes.White, Brushes.Transparent),
            pressed: (new SolidColorBrush(BrandPressed), Brushes.White, Brushes.Transparent),
            disabled: (Res("Pro.Disabled"), Res("Pro.DisabledText"), Brushes.Transparent));

        // secondary: bordered neutral button
        ButtonClass(styles, "secondary",
            normal: (Res("Pro.Card"), Res("Pro.Text"), Res("Pro.CardBorderStrong")),
            hover: (Res("Pro.Hover"), Res("Pro.Text"), Res("Pro.CardBorderStrong")),
            pressed: (Res("Pro.Pressed"), Res("Pro.Text"), Res("Pro.CardBorderStrong")),
            disabled: (Res("Pro.Card"), Res("Pro.DisabledText"), Res("Pro.CardBorder")));

        // row: an entry of the operation list
        ButtonClass(styles, "row",
            normal: (Brushes.Transparent, Res("Pro.Text"), Brushes.Transparent),
            hover: (Res("Pro.Hover"), Res("Pro.Text"), Brushes.Transparent),
            pressed: (Res("Pro.Pressed"), Res("Pro.Text"), Brushes.Transparent),
            disabled: (Brushes.Transparent, Res("Pro.Text"), Brushes.Transparent));
        ButtonClass(styles, "row.selected",
            normal: (Res("Pro.Selected"), Res("Pro.Text"), Brushes.Transparent),
            hover: (Res("Pro.Selected"), Res("Pro.Text"), Brushes.Transparent),
            pressed: (Res("Pro.Selected"), Res("Pro.Text"), Brushes.Transparent),
            disabled: (Res("Pro.Selected"), Res("Pro.Text"), Brushes.Transparent));

        // tile: a clickable card
        ButtonClass(styles, "tile",
            normal: (Res("Pro.Card"), Res("Pro.Text"), Res("Pro.CardBorder")),
            hover: (Res("Pro.Card"), Res("Pro.Text"), brand),
            pressed: (Res("Pro.Hover"), Res("Pro.Text"), brand),
            disabled: (Res("Pro.Card"), Res("Pro.Text"), Res("Pro.CardBorder")));

        // nav: sidebar entries
        IBrush sidebarSelected = new SolidColorBrush(SidebarSelected);
        ButtonClass(styles, "nav",
            normal: (Brushes.Transparent, new SolidColorBrush(SidebarText), Brushes.Transparent),
            hover: (new SolidColorBrush(SidebarHover), Brushes.White, Brushes.Transparent),
            pressed: (sidebarSelected, Brushes.White, Brushes.Transparent),
            disabled: (Brushes.Transparent, new SolidColorBrush(SidebarMuted), Brushes.Transparent));
        ButtonClass(styles, "nav.selected",
            normal: (sidebarSelected, Brushes.White, Brushes.Transparent),
            hover: (sidebarSelected, Brushes.White, Brushes.Transparent),
            pressed: (sidebarSelected, Brushes.White, Brushes.Transparent),
            disabled: (sidebarSelected, new SolidColorBrush(SidebarText), Brushes.Transparent));
    }

    /// <summary>One style per state; Fluent paints hover/pressed/disabled on the template's ContentPresenter.</summary>
    private static void ButtonClass(Styles styles, string classes,
        (object Back, object Fore, object Border) normal,
        (object Back, object Fore, object Border) hover,
        (object Back, object Fore, object Border) pressed,
        (object Back, object Fore, object Border) disabled)
    {
        string[] names = classes.Split('.');
        Selector Base(Selector? x)
        {
            Selector s = x.OfType<Button>();
            foreach (string name in names) s = s.Class(name);
            return s;
        }

        var root = new Style(x => Base(x));
        root.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, normal.Back));
        root.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, normal.Fore));
        root.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, normal.Border));
        styles.Add(root);

        foreach (var (pseudo, colors) in new[] { (":pointerover", hover), (":pressed", pressed), (":disabled", disabled) })
        {
            var state = new Style(x => Base(x).Class(pseudo).Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"));
            state.Setters.Add(new Setter(ContentPresenter.BackgroundProperty, colors.Back));
            state.Setters.Add(new Setter(ContentPresenter.ForegroundProperty, colors.Fore));
            state.Setters.Add(new Setter(ContentPresenter.BorderBrushProperty, colors.Border));
            styles.Add(state);
        }
    }

    private static DynamicResourceExtension Res(string key) => new(key);

    /// <summary>Binds a property to a theme resource, so it follows the light/dark switch.</summary>
    public static T Bind<T>(this T control, AvaloniaProperty property, string key) where T : Control
    {
        control.Bind(property, control.GetResourceObservable(key));
        return control;
    }

    public static IBrush Solid(Color color) => new SolidColorBrush(color);

    /// <summary>A text block in a theme colour.</summary>
    public static TextBlock Text(string text, double size = 13, string color = "Pro.Text", FontWeight? weight = null) =>
        new TextBlock { Text = text, FontSize = size, FontWeight = weight ?? FontWeight.Normal, TextWrapping = TextWrapping.Wrap }
            .Bind(TextBlock.ForegroundProperty, color);

    /// <summary>A rounded card on the page background.</summary>
    public static Border Card(Control? child = null, Thickness? padding = null) =>
        new Border
        {
            Child = child,
            Padding = padding ?? new Thickness(20),
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1)
        }
        .Bind(Border.BackgroundProperty, "Pro.Card")
        .Bind(Border.BorderBrushProperty, "Pro.CardBorder");

    /// <summary>A small rounded label, e.g. CAUTION or INPUT.</summary>
    public static Border Chip(string text, string back, string fore) =>
        new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(7, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = text, FontSize = 10, FontWeight = FontWeight.SemiBold, LetterSpacing = 0.4 }
                .Bind(TextBlock.ForegroundProperty, fore)
        }
        .Bind(Border.BackgroundProperty, back);

    public static Ellipse Dot(double size = 8) => new() { Width = size, Height = size, VerticalAlignment = VerticalAlignment.Center };

    public static PathIcon Icon(string data, double size = 18) =>
        new() { Data = Geometry.Parse(data), Width = size, Height = size };

    /// <summary>Material Design icon paths (Apache 2.0), 24x24.</summary>
    public static class Icons
    {
        public const string Home = "M10,20V14H14V20H19V12H22L12,3L2,12H5V20H10Z";
        public const string Folder = "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z";
        public const string Download = "M5,20H19V18H5M19,9H15V3H9V9H5L12,16L19,9Z";
        public const string Star = "M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z";
        public const string Wrench = "M22.7,19L13.6,9.9C14.5,7.6 14,4.9 12.1,3C10.1,1 7.1,0.6 4.7,1.7L9,6L6,9L1.6,4.7C0.4,7.1 0.9,10.1 2.9,12.1C4.8,14 7.5,14.5 9.8,13.6L18.9,22.7C19.3,23.1 19.9,23.1 20.3,22.7L22.6,20.4C23.1,20 23.1,19.3 22.7,19Z";
        public const string Database = "M12,3C7.58,3 4,4.79 4,7C4,9.21 7.58,11 12,11C16.42,11 20,9.21 20,7C20,4.79 16.42,3 12,3M4,9V12C4,14.21 7.58,16 12,16C16.42,16 20,14.21 20,12V9C20,11.21 16.42,13 12,13C7.58,13 4,11.21 4,9M4,14V17C4,19.21 7.58,21 12,21C16.42,21 20,19.21 20,17V14C20,16.21 16.42,18 12,18C7.58,18 4,16.21 4,14Z";
        public const string Info = "M13,9H11V7H13M13,17H11V11H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        public const string Play = "M8,5.14V19.14L19,12.14L8,5.14Z";
        public const string Swap = "M21,9L17,5V8H10V10H17V13M7,11L3,15L7,19V16H14V14H7V11Z";
        public const string Check = "M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z";
        public const string Alert = "M13,14H11V10H13M13,18H11V16H13M1,21H23L12,2L1,21Z";
        public const string Cube = "M21,16.5C21,16.88 20.79,17.21 20.47,17.38L12.57,21.82C12.41,21.94 12.21,22 12,22C11.79,22 11.59,21.94 11.43,21.82L3.53,17.38C3.21,17.21 3,16.88 3,16.5V7.5C3,7.12 3.21,6.79 3.53,6.62L11.43,2.18C11.59,2.06 11.79,2 12,2C12.21,2 12.41,2.06 12.57,2.18L20.47,6.62C20.79,6.79 21,7.12 21,7.5V16.5Z";
        public const string ChevronRight = "M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z";
        public const string Cog = "M12,15.5A3.5,3.5 0 0,1 8.5,12A3.5,3.5 0 0,1 12,8.5A3.5,3.5 0 0,1 15.5,12A3.5,3.5 0 0,1 12,15.5M19.43,12.97C19.47,12.65 19.5,12.33 19.5,12C19.5,11.67 19.47,11.34 19.43,11L21.54,9.37C21.73,9.22 21.78,8.95 21.66,8.73L19.66,5.27C19.54,5.05 19.27,4.96 19.05,5.05L16.56,6.05C16.04,5.66 15.5,5.32 14.87,5.07L14.5,2.42C14.46,2.18 14.25,2 14,2H10C9.75,2 9.54,2.18 9.5,2.42L9.13,5.07C8.5,5.32 7.96,5.66 7.44,6.05L4.95,5.05C4.73,4.96 4.46,5.05 4.34,5.27L2.34,8.73C2.21,8.95 2.27,9.22 2.46,9.37L4.57,11C4.53,11.34 4.5,11.67 4.5,12C4.5,12.33 4.53,12.65 4.57,12.97L2.46,14.63C2.27,14.78 2.21,15.05 2.34,15.27L4.34,18.73C4.46,18.95 4.73,19.03 4.95,18.95L7.44,17.94C7.96,18.34 8.5,18.68 9.13,18.93L9.5,21.58C9.54,21.82 9.75,22 10,22H14C14.25,22 14.46,21.82 14.5,21.58L14.87,18.93C15.5,18.67 16.04,18.34 16.56,17.94L19.05,18.95C19.27,19.03 19.54,18.95 19.66,18.73L21.66,15.27C21.78,15.05 21.73,14.78 21.54,14.63L19.43,12.97Z";
        public const string History = "M13.5,8H12V13L16.28,15.54L17,14.33L13.5,12.25V8M13,3A9,9 0 0,0 4,12H1L4.96,16.03L9,12H6A7,7 0 0,1 13,5A7,7 0 0,1 20,12A7,7 0 0,1 13,19C11.07,19 9.32,18.21 8.06,16.94L6.64,18.36C8.27,20 10.5,21 13,21A9,9 0 0,0 22,12A9,9 0 0,0 13,3Z";
    }
}
