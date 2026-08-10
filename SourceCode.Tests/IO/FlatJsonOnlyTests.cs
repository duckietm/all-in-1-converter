using Habbo_Downloader.IO;
using Xunit;

namespace Habbo_Downloader.Tests.IO;

public sealed class FlatJsonOnlyTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), $"habbo-downloader-flat-json-{Guid.NewGuid():N}");

    public FlatJsonOnlyTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task GamedataLoadersRejectJson5Files()
    {
        string path = Path.Combine(_root, "data.json5");
        await File.WriteAllTextAsync(path, "{}");
        Func<string, Task>[] loaders =
        [
            async value => await FurnidataIO.LoadAsync(value),
            async value => await ProductDataIO.LoadAsync(value),
            async value => await FigureDataIO.LoadAsync(value),
            async value => await FigureMapIO.LoadAsync(value)
        ];

        foreach (Func<string, Task> load in loaders)
        {
            await Assert.ThrowsAsync<NotSupportedException>(() => load(path));
        }
    }

    [Fact]
    public async Task GamedataLoadersRejectDirectoryInputs()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "FurnitureData.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(_root, "ProductData.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(_root, "FigureData.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(_root, "FigureMap.json"), "{}");
        Func<string, Task>[] loaders =
        [
            async value => await FurnidataIO.LoadAsync(value),
            async value => await ProductDataIO.LoadAsync(value),
            async value => await FigureDataIO.LoadAsync(value),
            async value => await FigureMapIO.LoadAsync(value)
        ];

        foreach (Func<string, Task> load in loaders)
        {
            await Assert.ThrowsAsync<NotSupportedException>(() => load(_root));
        }
    }

    [Fact]
    public async Task JsonReaderRejectsComments()
    {
        string path = Path.Combine(_root, "commented.json");
        await File.WriteAllTextAsync(path, "{ /* JSON5 comment */ \"value\": 1 }");

        await Assert.ThrowsAsync<InvalidDataException>(() => JsonReadHelper.LoadJObjectAsync(path));
    }

    [Fact]
    public async Task JsonReaderRejectsTrailingCommas()
    {
        string path = Path.Combine(_root, "trailing-comma.json");
        await File.WriteAllTextAsync(path, "{ \"value\": 1, }");

        await Assert.ThrowsAsync<InvalidDataException>(() => JsonReadHelper.LoadJObjectAsync(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
