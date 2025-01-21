
namespace ConsoleApplication
{
    public static class CompiledHandler
    {
        private static readonly string CompiledFolder = Path.Combine("Compiler", "compiled");

        public static void ListFiles()
        {
            if (!Directory.Exists(CompiledFolder))
            {
                Console.WriteLine("Compiled folder does not exist.");
                return;
            }

            string[] files = Directory.GetFiles(CompiledFolder, "*.*", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Console.WriteLine("No files found in the compiled folder.");
                return;
            }

            Console.WriteLine("Files in compiled folder:");
            foreach (var file in files)
            {
                Console.WriteLine(Path.GetRelativePath(CompiledFolder, file));
            }
        }

        public static async Task ClearFiles()
        {
            if (!Directory.Exists(CompiledFolder))
            {
                Console.WriteLine("Compiled folder does not exist.");
                return;
            }

            string[] files = Directory.GetFiles(CompiledFolder, "*.*", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Console.WriteLine("No files found in the compiled folder.");
                return;
            }

            foreach (var file in files)
            {
                File.Delete(file);
                Console.WriteLine($"Deleted: {file}");
            }

            Console.WriteLine("All files in the compiled folder have been deleted.");
        }
    }
}