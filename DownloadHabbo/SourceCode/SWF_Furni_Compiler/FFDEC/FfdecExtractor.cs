using System.Diagnostics;

namespace Habbo_Downloader.Tools
{
    public static class FfdecExtractor
    {
        private const string ToolsDirectory = @"Tools\ffdec\ffdec.jar";

        public static async Task ExtractSWFAsync(string swfFilePath, string outputDirectory)
        {
            string command = $"-export binaryData,image \"{outputDirectory}\" \"{swfFilePath}\"";
            await RunFfdecCommandAsync(command);
        }

        private static async Task RunFfdecCommandAsync(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar {ToolsDirectory} {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFDEC Error: {error.Trim()}\nOutput: {output.Trim()}");
            }
        }
    }
}
