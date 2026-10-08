using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Habbo_Downloader.Tools;
using static Habbo_Downloader.App.Professional.ProfessionalTheme;

namespace Habbo_Downloader.App.Professional.Views;

/// <summary>The config.ini editor of the Professional window.</summary>
public sealed class SettingsPage
{
    private enum Kind { Text, Url, Password, Port, Choice }

    /// <param name="Default">What the converter uses when the key is missing; not written unless changed.</param>
    private sealed record Field(string Key, string Label, string Hint, Kind Kind = Kind.Text,
        (string Value, string Label)[]? Choices = null, string Default = "");

    private static readonly Field[] DownloadFields =
    [
        new("download_format", "Download format", "Furniture, clothes, effects and pets from Habbo", Kind.Choice,
            [("hab", ".hab only (recommended)"), ("swf", ".swf only"), ("both", ".swf and .hab")], "both"),
        new("spritesheet_format", "Sprite sheets", "Sheet image inside .nitro and .hab bundles", Kind.Choice,
            [("webp", "WebP lossless (recommended)"), ("png", "PNG")], "webp"),
        new("pet_libraries", "Pet libraries", "Comma separated. Empty: the built-in list of 36 pets")
    ];

    private static readonly Field[] NitroFields =
    [
        new("nitro_furnidataJSON", "FurnitureData.json", "renderer-config.json: furnidata.url", Kind.Url),
        new("nitro_furnitureurl", "Furniture folder", "renderer-config.json: furni.asset.url", Kind.Url),
        new("nitro_furniture_icon_url", "Furniture icons", "renderer-config.json: furni.asset.icon.url", Kind.Url),
        new("nitro_clothes_dir", "Clothes folder", "renderer-config.json: avatar.asset.url", Kind.Url),
        new("nitro_figuredata", "FigureData.json", "renderer-config.json: avatar.figuredata.url", Kind.Url),
        new("nitro_figuremap", "FigureMap.json", "renderer-config.json: avatar.figuremap.url", Kind.Url)
    ];

    private static readonly Field[] DatabaseFields =
    [
        new("DATABASESERVER", "Server", "Host name or IP address"),
        new("DATABASEPORT", "Port", "Usually 3306", Kind.Port, Default: "3306"),
        new("DATABASEUSER", "User", "MariaDB / MySQL user"),
        new("DATABASEPASSWORD", "Password", "Stored as plain text in config.ini", Kind.Password),
        new("DATABASENAME", "Database", "Name of the hotel database")
    ];

    private static readonly Field[] CdnFields =
    [
        new("furnitureurl", "Furniture (hof_furni)", "SWF and .hab furniture", Kind.Url),
        new("effecturl", "Gordon", "Clothes, effects and pets", Kind.Url),
        new("catalogiconurl", "Catalogue icons", "Prefix, the icon number is appended", Kind.Url),
        new("catalogurl", "Catalogue images", "c_images/catalogue", Kind.Url),
        new("receptionurl", "Reception", "c_images/reception", Kind.Url),
        new("promosmallurl", "Small promos", "c_images/web_promo_small", Kind.Url),
        new("questsurl", "Quests", "c_images/Quests", Kind.Url),
        new("soundmachineurl", "Sound machine", "Prefix of the MP3 samples", Kind.Url)
    ];

    private const string CustomHotel = "Custom URLs (keep the current ones)";

    private readonly Action<string> _status;
    private readonly Dictionary<Field, (Control Input, TextBlock Error)> _inputs = [];
    private readonly ComboBox _hotel = new() { MinWidth = 380 };
    private readonly StackPanel _hotelUrls = new() { Spacing = 4 };
    private readonly TextBlock _filePath = Text("", 12, "Pro.Muted");
    private readonly TextBlock _result = Text("", 12.5, "Pro.Muted", FontWeight.SemiBold);
    private readonly Button _save = new() { Content = "Save changes", Padding = new Thickness(16, 8), Classes = { "primary" } };
    private ConfigIniFile _file = ConfigIniFile.Load();
    private bool _loading;
    private bool _dirty;

    public Control View { get; }
    public bool IsDirty => _dirty;

    /// <param name="status">Shows a message in the window's status bar.</param>
    public SettingsPage(Action<string> status)
    {
        _status = status;
        var page = new StackPanel { Spacing = 16 };
        page.Children.Add(BuildToolbar());
        page.Children.Add(BuildHotelCard());
        page.Children.Add(Section("Downloads", "How Habbo assets are saved", DownloadFields));
        page.Children.Add(Section("Nitro retro", "Custom downloads from a Nitro hotel; copy the values from its renderer-config.json", NitroFields));
        page.Children.Add(Section("Database", "Used by the Database tools", DatabaseFields));
        page.Children.Add(Section("Habbo CDN", "Image and asset hosts; these are the same for every hotel", CdnFields, collapsed: true));
        View = page;
        Reload();
    }

    // ---------------------------------------------------------------- layout

    private Control BuildToolbar()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        var text = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(Text("config.ini", 14.5, weight: FontWeight.SemiBold));
        text.Children.Add(_filePath);
        text.Children.Add(_result);
        grid.Children.Add(text);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        var open = new Button { Content = "Open file", Padding = new Thickness(16, 8), Classes = { "secondary" } };
        open.Click += (_, _) => OpenFile();
        var reload = new Button { Content = "Reload", Padding = new Thickness(16, 8), Classes = { "secondary" } };
        reload.Click += (_, _) =>
        {
            Reload();
            Show("Reloaded from disk.", "Pro.Muted");
        };
        _save.Click += (_, _) => Save();
        buttons.Children.Add(open);
        buttons.Children.Add(reload);
        buttons.Children.Add(_save);
        Grid.SetColumn(buttons, 1);
        grid.Children.Add(buttons);
        return Card(grid, new Thickness(20, 16));
    }

    private Control BuildHotelCard()
    {
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(Heading("Habbo hotel", "Where Habbo Original downloads gamedata (furnidata, productdata, texts and variables) from"));

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("240,*"), ColumnSpacing = 16 };
        row.Children.Add(Label("Hotel", "Sets the five gamedata URLs below"));
        _hotel.HorizontalAlignment = HorizontalAlignment.Left;
        _hotel.ItemTemplate = new FuncDataTemplate<object>((item, _) =>
        {
            var line = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            line.Children.Add(Flags.For(item as HabboHotel, 24));
            line.Children.Add(new TextBlock { Text = item?.ToString(), VerticalAlignment = VerticalAlignment.Center });
            return line;
        });
        _hotel.SelectionChanged += (_, _) =>
        {
            ShowHotelUrls();
            Changed();
        };
        Grid.SetColumn(_hotel, 1);
        row.Children.Add(_hotel);
        panel.Children.Add(row);

        var urls = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10), Child = _hotelUrls }
            .Bind(Border.BackgroundProperty, "Pro.Hover");
        panel.Children.Add(urls);

        var note = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        note.Children.Add(Chip("NOTE", "Pro.WarningBack", "Pro.WarningText"));
        note.Children.Add(Text("Item IDs and offer_id values are not the same across hotels.", 12.5, "Pro.Muted"));
        panel.Children.Add(note);
        return Card(panel);
    }

    private Control Section(string title, string subtitle, Field[] fields, bool collapsed = false)
    {
        var panel = new StackPanel { Spacing = 14 };
        var rows = new StackPanel { Spacing = 14, IsVisible = !collapsed };
        foreach (Field field in fields) rows.Children.Add(FieldRow(field));

        if (!collapsed)
        {
            panel.Children.Add(Heading(title, subtitle));
        }
        else
        {
            var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
            header.Children.Add(Heading(title, subtitle));
            var toggle = new Button { Content = "Show", Padding = new Thickness(16, 7), VerticalAlignment = VerticalAlignment.Center, Classes = { "secondary" } };
            toggle.Click += (_, _) =>
            {
                rows.IsVisible = !rows.IsVisible;
                toggle.Content = rows.IsVisible ? "Hide" : "Show";
            };
            Grid.SetColumn(toggle, 1);
            header.Children.Add(toggle);
            panel.Children.Add(header);
        }
        panel.Children.Add(rows);
        return Card(panel);
    }

    private Control FieldRow(Field field)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("240,*"), ColumnSpacing = 16 };
        row.Children.Add(Label(field.Label, field.Hint));

        Control input;
        if (field.Kind == Kind.Choice)
        {
            var combo = new ComboBox { MinWidth = 300, HorizontalAlignment = HorizontalAlignment.Left, ItemsSource = field.Choices!.Select(c => c.Label).ToList() };
            combo.SelectionChanged += (_, _) => Changed();
            input = combo;
        }
        else
        {
            var box = new TextBox();
            if (field.Kind == Kind.Password) box.PasswordChar = '•';
            if (field.Kind == Kind.Port) { box.MaxWidth = 140; box.HorizontalAlignment = HorizontalAlignment.Left; }
            box.TextChanged += (_, _) => Changed();
            input = box;
        }

        var error = Text("", 12, "Pro.ErrorText");
        error.IsVisible = false;
        var right = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        right.Children.Add(input);
        right.Children.Add(error);
        Grid.SetColumn(right, 1);
        row.Children.Add(right);
        _inputs[field] = (input, error);
        return row;
    }

    private static Control Heading(string title, string subtitle)
    {
        var panel = new StackPanel { Spacing = 2 };
        panel.Children.Add(Text(title, 15, weight: FontWeight.SemiBold));
        panel.Children.Add(Text(subtitle, 12.5, "Pro.Muted"));
        return panel;
    }

    private static Control Label(string label, string hint)
    {
        var panel = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(Text(label, 13, weight: FontWeight.SemiBold));
        panel.Children.Add(Text(hint, 11.5, "Pro.Muted"));
        return panel;
    }

    // ---------------------------------------------------------------- data

    /// <summary>Reads config.ini again and drops unsaved edits.</summary>
    public void Reload()
    {
        _loading = true;
        try
        {
            _file = ConfigIniFile.Load();
            _filePath.Text = _file.Exists ? _file.FilePath : $"{_file.FilePath} (not found; saving creates it)";

            foreach (var (field, (input, error)) in _inputs)
            {
                string value = _file.Get(field.Key) ?? field.Default;
                if (input is ComboBox combo)
                    combo.SelectedIndex = Math.Max(0, Array.FindIndex(field.Choices!, c => string.Equals(c.Value, value, StringComparison.OrdinalIgnoreCase)));
                else if (input is TextBox box)
                    box.Text = value;
                error.IsVisible = false;
            }

            HabboHotel? hotel = HabboHotels.Detect(_file.Get);
            var items = new List<object>();
            if (hotel is null) items.Add(CustomHotel);
            items.AddRange(HabboHotels.All);
            _hotel.ItemsSource = items;
            _hotel.SelectedItem = (object?)hotel ?? CustomHotel;
            ShowHotelUrls();
        }
        finally
        {
            _loading = false;
        }
        SetDirty(false);
    }

    /// <summary>Validates and writes only the changed values. Returns false when nothing could be saved.</summary>
    public bool Save()
    {
        if (!Validate())
        {
            Show("Fix the highlighted fields first.", "Pro.ErrorText");
            return false;
        }

        try
        {
            var changed = new List<string>();
            foreach (var (field, (input, _)) in _inputs)
            {
                string value = ValueOf(field, input);
                string? current = _file.Get(field.Key);
                if (current is null && (value.Length == 0 || value.Equals(field.Default, StringComparison.OrdinalIgnoreCase))) continue;
                if (string.Equals(current, value, StringComparison.Ordinal)) continue;
                _file.Set(field.Key, value);
                changed.Add(field.Key);
            }

            if (_hotel.SelectedItem is HabboHotel hotel && !Equals(HabboHotels.Detect(_file.Get), hotel))
            {
                foreach (var (key, url) in HabboHotels.Urls(hotel))
                {
                    _file.Set(key, url);
                    changed.Add(key);
                }
            }

            if (changed.Count == 0)
            {
                Show("Nothing to save.", "Pro.Muted");
                SetDirty(false);
                return true;
            }

            _file.Save();
            ConverterSettings.Reload();
            Reload();
            string message = $"Saved {changed.Count} setting{(changed.Count == 1 ? "" : "s")}. The previous file is kept as config.ini.bak.";
            Show(message, "Pro.SuccessText");
            _status(message);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Show($"Could not save config.ini: {ex.Message}", "Pro.ErrorText");
            return false;
        }
    }

    private bool Validate()
    {
        bool valid = true;
        foreach (var (field, (input, error)) in _inputs)
        {
            string value = ValueOf(field, input);
            string? message = field.Kind switch
            {
                _ when value.IndexOfAny(['\r', '\n']) >= 0 => "Must be on one line.",
                Kind.Url when value.Length > 0 && !IsHttpUrl(value) => "Enter a full http:// or https:// address.",
                Kind.Port when !int.TryParse(value, out int port) || port is < 1 or > 65535 => "Enter a port between 1 and 65535.",
                _ => null
            };
            error.Text = message ?? string.Empty;
            error.IsVisible = message is not null;
            valid &= message is null;
        }
        return valid;
    }

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string ValueOf(Field field, Control input) => input switch
    {
        ComboBox combo => field.Choices![Math.Max(0, combo.SelectedIndex)].Value,
        TextBox box => (box.Text ?? string.Empty).Trim(),
        _ => string.Empty
    };

    private void ShowHotelUrls()
    {
        _hotelUrls.Children.Clear();
        IEnumerable<(string Key, string Url)> urls = _hotel.SelectedItem is HabboHotel hotel
            ? HabboHotels.Urls(hotel)
            : HabboHotels.Urls(HabboHotels.All[0]).Select(entry => (entry.Key, _file.Get(entry.Key) ?? "(not set)"));
        foreach (var (key, url) in urls)
        {
            var line = new Grid { ColumnDefinitions = new ColumnDefinitions("150,*"), ColumnSpacing = 10 };
            line.Children.Add(Mono(key, "Pro.Muted"));
            var value = Mono(url, "Pro.Text");
            Grid.SetColumn(value, 1);
            line.Children.Add(value);
            _hotelUrls.Children.Add(line);
        }
    }

    private static TextBlock Mono(string text, string color)
    {
        var block = Text(text, 12, color);
        block.FontFamily = new FontFamily("Cascadia Mono, Consolas, DejaVu Sans Mono, monospace");
        block.TextWrapping = TextWrapping.NoWrap;
        block.TextTrimming = TextTrimming.CharacterEllipsis;
        return block;
    }

    private void Changed()
    {
        if (_loading) return;
        SetDirty(true);
        Show("Unsaved changes.", "Pro.WarningText");
    }

    private void SetDirty(bool dirty)
    {
        _dirty = dirty;
        _save.IsEnabled = dirty;
        if (!dirty && _result.Text == "Unsaved changes.") _result.Text = string.Empty;
    }

    private void Show(string message, string color)
    {
        _result.Text = message;
        _result.Bind(TextBlock.ForegroundProperty, color);
    }

    private void OpenFile()
    {
        if (!File.Exists(_file.FilePath))
        {
            Show("config.ini does not exist yet; save first.", "Pro.ErrorText");
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo { FileName = _file.FilePath, UseShellExecute = true })?.Dispose();
        }
        catch (Exception ex)
        {
            Show($"Could not open the file: {ex.Message}", "Pro.ErrorText");
        }
    }
}
