using Habbo_Downloader.App.Workspaces;

/// <summary>
/// Writes a converted asset as .nitro or .hab (picked by the Hotel Tools option) into a nitro/ or hab/ folder
/// under the tool's output folder. Every bundle is json + a lossless WebP sheet; a PNG sheet is converted on
/// the way (pixel-checked, see SheetWebp). An Asset Workspace keeps its own layout, so no subfolder there.
/// </summary>
public static class AssetBundleWriter
{
    /// <summary>".nitro" or ".hab"; set by the Hotel Tools option before a conversion starts.</summary>
    public static string Extension { get; set; } = ".hab";

    public static string Label => $"{Extension.TrimStart('.').ToUpperInvariant()} (json + WebP Lossless)";

    /// <summary>The nitro/ or hab/ folder of an output folder.</summary>
    public static string Folder(string outputDirectory) => Folder(outputDirectory, Extension);

    public static string Folder(string outputDirectory, string extension)
    {
        AssetWorkspacePaths? paths = AssetWorkspaceRuntime.Router.Paths;
        if (paths is not null && IsInside(paths.Root, outputDirectory)) return outputDirectory;

        return Path.Combine(outputDirectory, extension.TrimStart('.'));
    }

    public static string OutputPath(string outputDirectory, string name) =>
        Path.Combine(Folder(outputDirectory), name + Extension);

    /// <summary>True when this asset is already converted.</summary>
    public static bool Exists(string outputDirectory, string name) => File.Exists(OutputPath(outputDirectory, name));

    /// <summary>The bundle bytes; PNG sheets become lossless WebP first.</summary>
    public static async Task<byte[]> BuildAsync(string extension, string name, IReadOnlyList<KeyValuePair<string, byte[]>> files)
    {
        var webpFiles = files.Select(file => SheetWebp.ToWebpEntry(file, name)).ToList();

        if (extension == ".hab") return HabBundle.Write(name, webpFiles);

        var bundler = new NitroBundler();
        foreach (var (fileName, data) in webpFiles) bundler.AddFile(fileName, data);
        return await bundler.ToBufferAsync();
    }

    /// <summary>Writes the bundle into the nitro/ or hab/ folder of outputDirectory; returns its size.</summary>
    public static async Task<long> WriteAsync(string outputDirectory, string name, IReadOnlyList<KeyValuePair<string, byte[]>> files,
        Func<string, byte[], Task>? write = null)
    {
        string folder = Folder(outputDirectory);
        Directory.CreateDirectory(folder);

        byte[] data = await BuildAsync(Extension, name, files);
        string path = Path.Combine(folder, name + Extension);

        if (write != null) await write(path, data);
        else await File.WriteAllBytesAsync(path, data);

        Console.WriteLine($"📦 Generated {name}{Extension} -> {folder}");
        return data.Length;
    }

    /// <summary>The json and sheet of a converted asset, skipping the ones that are missing.</summary>
    public static async Task<List<KeyValuePair<string, byte[]>>> ReadFilesAsync(params string[] paths)
    {
        var files = new List<KeyValuePair<string, byte[]>>();
        foreach (string path in paths)
        {
            if (File.Exists(path)) files.Add(new KeyValuePair<string, byte[]>(Path.GetFileName(path), await File.ReadAllBytesAsync(path)));
        }
        return files;
    }

    private static bool IsInside(string root, string path)
    {
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullRoot, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}
