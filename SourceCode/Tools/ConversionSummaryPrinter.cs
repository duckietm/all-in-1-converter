using System;
using System.IO;

namespace Habbo_Downloader.Tools
{
    /// <summary>
    /// Utility for rendering clear, professional summary reports with file sizes, throughput,
    /// and disk space savings at the conclusion of every conversion and compilation process.
    /// </summary>
    public static class ConversionSummaryPrinter
    {
        public static void PrintSummary(
            string processTitle,
            int totalFiles,
            int convertedFiles,
            int skippedFiles,
            int failedFiles,
            long totalOriginalBytes,
            long totalOutputBytes,
            TimeSpan elapsed,
            string outputDirectory,
            string formatName = "WebP Lossless")
        {
            double origMb = totalOriginalBytes / (1024.0 * 1024.0);
            double outMb = totalOutputBytes / (1024.0 * 1024.0);
            long savedBytes = totalOriginalBytes - totalOutputBytes;
            double savedMb = savedBytes / (1024.0 * 1024.0);
            double savingPercent = totalOriginalBytes > 0
                ? ((double)savedBytes / totalOriginalBytes) * 100.0
                : 0.0;
            double throughput = elapsed.TotalSeconds > 0
                ? (convertedFiles + skippedFiles + failedFiles) / Math.Max(0.001, elapsed.TotalSeconds)
                : 0.0;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("================================================================================");
            Console.WriteLine($"                    PROCESS SUMMARY: {processTitle.ToUpperInvariant()}");
            Console.WriteLine("================================================================================");
            Console.ResetColor();

            Console.WriteLine($"  Format:              {formatName}");
            Console.WriteLine($"  Time Elapsed:        {elapsed.TotalMinutes:F1} min ({elapsed.TotalSeconds:F1} sec)");
            Console.WriteLine($"  Throughput:          {throughput:F1} files/sec");
            Console.WriteLine($"  Files Converted:     {convertedFiles:N0}");
            if (skippedFiles > 0)
            {
                Console.WriteLine($"  Files Skipped:       {skippedFiles:N0} (already compiled/present)");
            }
            if (failedFiles > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Files Failed:        {failedFiles:N0}");
                Console.ResetColor();
            }
            Console.WriteLine($"  Total Files Scanned: {totalFiles:N0}");

            Console.WriteLine("--------------------------------------------------------------------------------");

            if (totalOriginalBytes > 0 && totalOutputBytes > 0)
            {
                Console.WriteLine($"  Original Size:       {origMb:F2} MB ({totalOriginalBytes:N0} bytes)");
                Console.WriteLine($"  Output Size:         {outMb:F2} MB ({totalOutputBytes:N0} bytes)");

                if (savedBytes > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  Disk Space Saved:    {savedMb:F2} MB ({savingPercent:F1}% overall reduction)");
                    Console.ResetColor();
                }
                else if (savedBytes < 0)
                {
                    Console.WriteLine($"  Size Difference:     +{Math.Abs(savedMb):F2} MB (+{Math.Abs(savingPercent):F1}%)");
                }
                else
                {
                    Console.WriteLine("  Disk Space Saved:    0 MB (identical size)");
                }
            }
            else if (totalOutputBytes > 0)
            {
                Console.WriteLine($"  Output Size:         {outMb:F2} MB ({totalOutputBytes:N0} bytes)");
            }

            Console.WriteLine($"  Output Directory:    {Path.GetFullPath(outputDirectory)}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("================================================================================");
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
