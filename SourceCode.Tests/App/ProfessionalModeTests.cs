using Habbo_Downloader.App;
using Habbo_Downloader.App.Operations;
using Xunit;

namespace Habbo_Downloader.Tests.App;

public sealed class ProfessionalModeTests
{
    [Fact]
    public void ArgsRecognizeProfessionalMode()
    {
        Args args = Args.Parse(["--professional"]);

        Assert.Equal(RunMode.Professional, args.Mode);
        Assert.True(args.ModeExplicitlySet);
    }

    [Fact]
    public void CatalogContainsEveryExistingOperationOnce()
    {
        IReadOnlyList<OperationDefinition> operations = OperationCatalog.All;

        Assert.Equal(32, operations.Count);
        Assert.Equal(operations.Count, operations.Select(item => item.Id).Distinct().Count());
        Assert.All(operations, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Title));
            Assert.False(string.IsNullOrWhiteSpace(item.Description));
            Assert.NotNull(item.Action);
        });
    }

    [Theory]
    [InlineData(OperationCategory.HabboOriginal, 13)]
    [InlineData(OperationCategory.NitroCustom, 2)]
    [InlineData(OperationCategory.HotelTools, 10)]
    [InlineData(OperationCategory.Database, 5)]
    [InlineData(OperationCategory.General, 2)]
    public void CatalogPreservesAllMenuCategories(OperationCategory category, int expected)
    {
        Assert.Equal(expected, OperationCatalog.ForCategory(category).Count);
    }

    [Fact]
    public void EveryPromptDrivenOperationEnablesNativeInput()
    {
        string[] expected =
        [
            "habbo.clothes",
            "habbo.effects",
            "habbo.all",
            "nitro.furniture",
            "tools.merge-furnidata",
            "tools.merge-productdata",
            "tools.merge-clothes",
            "tools.generate-sql",
            "tools.swf-furniture",
            "tools.swf-clothes"
        ];

        Assert.Equal(expected, OperationCatalog.All
            .Where(operation => operation.RequiresInput)
            .Select(operation => operation.Id));
    }
}
