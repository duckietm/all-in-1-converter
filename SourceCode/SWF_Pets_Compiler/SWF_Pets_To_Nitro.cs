using ConsoleApplication;
using System.Diagnostics;

namespace Habbo_Downloader.Compiler
{
    public static class SWF_Pets_To_Nitro
    {
        private static readonly string DecoderPath = Path.Combine("Tools", "Decoder");
        private static readonly string NodeModulesPath = Path.Combine(DecoderPath, "node_modules");

        public static async Task RunDecoderPipelineAsync()
        {
            try
            {
                Console.WriteLine("🚀 Starting SWF to Nitro conversion...");

                if (!Directory.Exists(NodeModulesPath))
                {
                    Console.WriteLine("📦 Installing dependencies...");

                    if (!await RunCommandAsync("yarn install", DecoderPath))
                    {
                        Console.WriteLine("❌ Failed to install dependencies.");
                        return;
                    }

                    Console.WriteLine("🔨 Building the project...");
                    if (!await RunCommandAsync("yarn build", DecoderPath))
                    {
                        Console.WriteLine("❌ Build process failed.");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("✅ node_modules found. Skipping installation.");
                }

                Console.WriteLine("📡 Downloading variables...");
                await VariablesDownloader.DownloadVariablesAsync();

                Console.WriteLine("▶️ Starting Decoder...");
                if (!await RunCommandAsync("yarn start", DecoderPath))
                {
                    Console.WriteLine("❌ Failed to start the Decoder.");
                    return;
                }

                Console.WriteLine("🎉 Pets to Nitro conversion completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in conversion process: {ex.Message}");
            }
        }

        private static async Task<bool> RunCommandAsync(string command, string workingDirectory)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                string output = await outputTask;
                string error = await errorTask;

                Console.WriteLine("\n✅ Process completed!");

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to run command '{command}': {ex.Message}");
                return false;
            }
        }
    }
}
