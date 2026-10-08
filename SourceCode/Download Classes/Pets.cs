using Habbo_Downloader.Tools;
using static ConsoleApplication.HabboAssetDownloader;

namespace ConsoleApplication
{
    /// <summary>Downloads Habbo's pet libraries (gordon/&lt;release&gt;/&lt;pet&gt;) into Habbo_Default/pets/{swf,hab}.</summary>
    internal static class PetsDownloader
    {
        /// <summary>The pet libraries to download; config.ini pet_libraries overrides it.</summary>
        private static readonly string[] DefaultPets =
        {
            "bear", "bearbaby", "bunnydepressed", "bunnyeaster", "bunnyevil", "bunnylove", "cat", "chicken",
            "cow", "croco", "demonmonkey", "dog", "dragon", "dragondog", "fools", "frog", "gnome", "haloompa",
            "horse", "kittenbaby", "lion", "monkey", "monster", "monsterplant", "pig", "pigeonevil", "pigeongood",
            "pigletbaby", "pterosaur", "puppybaby", "rhino", "spider", "terrier", "terrierbaby", "turtle", "velociraptor"
        };

        internal static async Task DownloadPetsAsync()
        {
            var config = IniFileParser.Parse("config.ini");

            string externalVarsUrl = config["AppSettings:externalvarsurl"];
            string gordonUrl = config["AppSettings:effecturl"].TrimEnd('/');

            string? release;
            try
            {
                release = await GetReleaseAsync(externalVarsUrl);
            }
            catch (Exception ex)
            {
                Log(ConsoleColor.Red, "Error downloading external_variables: " + ex.Message);
                return;
            }

            if (string.IsNullOrEmpty(release))
            {
                Log(ConsoleColor.Red, "Error: Could not determine the release version.");
                return;
            }

            string[] pets = config.TryGetValue("AppSettings:pet_libraries", out string? custom) && !string.IsNullOrWhiteSpace(custom)
                ? custom.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : DefaultPets;

            Console.WriteLine($"Downloading {pets.Length} pet libraries from release {release}; download format: {ConverterSettings.DownloadFormat} (config.ini download_format)");

            PrepareFolder(HabboAssetFolder.Pets);

            Counts counts = await DownloadLibrariesAsync(HabboAssetFolder.Pets,
                pets.Distinct(StringComparer.OrdinalIgnoreCase).Select(pet => (pet, $"{gordonUrl}/{release}/{pet}")));

            Log(ConsoleColor.Green, Summary("pet files", counts));
        }
    }
}
