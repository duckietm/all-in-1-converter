using Habbo_Downloader.App.Operations;
using Xunit;

namespace Habbo_Downloader.Tests.App;

public sealed class OperationRunnerTests
{
    [Fact]
    public async Task RunnerCapturesOutputAndSuppliesNativeInput()
    {
        var definition = new OperationDefinition(
            "test.echo",
            OperationCategory.General,
            "Echo",
            "Echo test",
            async () =>
            {
                Console.Write("Name: ");
                string? name = Console.ReadLine();
                Console.WriteLine($"Hello {name}");
                await Task.CompletedTask;
            },
            RequiresInput: true);
        await using var runner = new OperationRunner();
        var output = new List<string>();
        runner.OutputReceived += output.Add;

        Task<OperationResult> running = runner.RunAsync(definition);
        await WaitUntilAsync(() => output.Any(line => line.Contains("Name:")));
        runner.SubmitInput("Nitro");
        OperationResult result = await running;

        Assert.True(result.Succeeded);
        Assert.Contains(output, line => line.Contains("Hello Nitro"));
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        DateTime timeout = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < timeout)
        {
            await Task.Delay(20);
        }

        Assert.True(condition(), "Timed out waiting for operation output.");
    }
}
