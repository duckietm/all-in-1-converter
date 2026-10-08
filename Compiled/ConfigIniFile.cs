namespace Habbo_Downloader.Tools;

/// <summary>
/// Edits config.ini in place: only changed values are rewritten, comments, order and unknown keys stay.
/// Keys live in [AppSettings], as <see cref="ConsoleApplication.IniFileParser"/> reads them.
/// </summary>
public sealed class ConfigIniFile
{
    private const string SectionHeader = "[AppSettings]";
    private readonly List<string> _lines;

    public string FilePath { get; }
    public bool Exists { get; }

    private ConfigIniFile(string filePath, List<string> lines, bool exists)
    {
        FilePath = filePath;
        _lines = lines;
        Exists = exists;
    }

    public static string DefaultPath => Path.Combine(Environment.CurrentDirectory, "config.ini");

    public static ConfigIniFile Load(string? filePath = null)
    {
        string path = Path.GetFullPath(filePath ?? DefaultPath);
        bool exists = File.Exists(path);
        return new ConfigIniFile(path, exists ? File.ReadAllLines(path).ToList() : [], exists);
    }

    /// <summary>The value of a key in [AppSettings], or null when the key is not there.</summary>
    public string? Get(string key)
    {
        int index = FindKey(key);
        return index < 0 ? null : ValueOf(_lines[index]);
    }

    /// <summary>Replaces the key's line, or adds it at the end of [AppSettings].</summary>
    public void Set(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key) || key.IndexOfAny(['=', '\r', '\n', '[', ']']) >= 0)
            throw new ArgumentException($"Invalid setting name '{key}'.", nameof(key));
        if (value.IndexOfAny(['\r', '\n']) >= 0)
            throw new ArgumentException($"{key}: a value must be on one line.", nameof(value));

        string line = $"{key}={value.Trim()}";
        int index = FindKey(key);
        if (index >= 0)
        {
            _lines[index] = line;
            return;
        }

        var (start, end) = EnsureSection();
        // After the last setting of the section, so it does not land under the next section's comment block.
        int insertAt = start + 1;
        for (int i = start + 1; i < end; i++)
            if (KeyOf(_lines[i]) is not null) insertAt = i + 1;
        _lines.Insert(insertAt, line);
    }

    /// <summary>Writes the file atomically; the previous version is kept as config.ini.bak.</summary>
    public void Save()
    {
        string? directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".bak", overwrite: true);
        string temporaryPath = FilePath + $".{Guid.NewGuid():N}.tmp";
        File.WriteAllLines(temporaryPath, _lines);
        File.Move(temporaryPath, FilePath, overwrite: true);
    }

    private int FindKey(string key)
    {
        var (start, end) = SectionRange();
        if (start < 0) return -1;
        for (int i = start + 1; i < end; i++)
            if (string.Equals(KeyOf(_lines[i]), key, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    private (int Start, int End) SectionRange()
    {
        int start = _lines.FindIndex(line => string.Equals(line.Trim(), SectionHeader, StringComparison.OrdinalIgnoreCase));
        if (start < 0) return (-1, -1);
        int end = _lines.FindIndex(start + 1, IsSectionHeader);
        return (start, end < 0 ? _lines.Count : end);
    }

    private (int Start, int End) EnsureSection()
    {
        var range = SectionRange();
        if (range.Start >= 0) return range;
        if (_lines.Count > 0 && !string.IsNullOrWhiteSpace(_lines[^1])) _lines.Add(string.Empty);
        _lines.Add(SectionHeader);
        return (_lines.Count - 1, _lines.Count);
    }

    private static bool IsSectionHeader(string line)
    {
        string trimmed = line.Trim();
        return trimmed.StartsWith('[') && trimmed.EndsWith(']');
    }

    private static string? KeyOf(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed[0] is '#' or ';' or '[') return null;
        int separator = trimmed.IndexOf('=');
        return separator > 0 ? trimmed[..separator].Trim() : null;
    }

    private static string ValueOf(string line)
    {
        string trimmed = line.Trim();
        return trimmed[(trimmed.IndexOf('=') + 1)..].Trim();
    }
}
