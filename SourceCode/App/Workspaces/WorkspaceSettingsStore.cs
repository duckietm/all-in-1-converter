namespace Habbo_Downloader.App.Workspaces;

public sealed class WorkspaceSettingsStore
{
    private const string Section = "[AppSettings]";
    private const string Key = "nitro_assets_root";
    private readonly string _configPath;

    public WorkspaceSettingsStore(string configPath) => _configPath = Path.GetFullPath(configPath);

    public string? LoadRoot()
    {
        if (!File.Exists(_configPath)) return null;
        string? section = null;
        foreach (string line in File.ReadLines(_configPath))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                section = trimmed;
                continue;
            }

            if (!string.Equals(section, Section, StringComparison.OrdinalIgnoreCase)) continue;
            int separator = trimmed.IndexOf('=');
            if (separator <= 0 || !string.Equals(trimmed[..separator].Trim(), Key, StringComparison.OrdinalIgnoreCase)) continue;
            string value = trimmed[(separator + 1)..].Trim();
            return string.IsNullOrWhiteSpace(value) ? null : Path.GetFullPath(value);
        }
        return null;
    }

    public void SaveRoot(string root)
    {
        string normalized = AssetWorkspacePaths.FromRoot(root).Root;
        var lines = File.Exists(_configPath) ? File.ReadAllLines(_configPath).ToList() : [];
        int sectionIndex = lines.FindIndex(line => string.Equals(line.Trim(), Section, StringComparison.OrdinalIgnoreCase));
        if (sectionIndex < 0)
        {
            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1])) lines.Add(string.Empty);
            sectionIndex = lines.Count;
            lines.Add(Section);
        }

        int nextSection = lines.FindIndex(sectionIndex + 1, line => line.TrimStart().StartsWith('['));
        if (nextSection < 0) nextSection = lines.Count;
        int keyIndex = lines.FindIndex(sectionIndex + 1, nextSection - sectionIndex - 1, line =>
        {
            string trimmed = line.Trim();
            int separator = trimmed.IndexOf('=');
            return separator > 0 && string.Equals(trimmed[..separator].Trim(), Key, StringComparison.OrdinalIgnoreCase);
        });

        string setting = $"{Key}={normalized}";
        if (keyIndex >= 0) lines[keyIndex] = setting;
        else lines.Insert(nextSection, setting);

        string? directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        string temporaryPath = _configPath + $".{Guid.NewGuid():N}.tmp";
        File.WriteAllLines(temporaryPath, lines);
        File.Move(temporaryPath, _configPath, overwrite: true);
    }
}
