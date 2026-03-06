using NeoIni;

class NeoIniDemo
{
    private const string TestFile = "demo_config.ini";
    private static bool Encryption = false;
    private static bool CustomPassword = false;
    private static string Password;

    private static async Task Main()
    {
        Console.CursorVisible = false;
        Console.Clear();
        Console.WriteLine("NeoIni Demonstration\n");

        Console.Write("Press Y to enable auto-encryption mode (machine-bound): ");
        Encryption = Console.ReadKey().Key == ConsoleKey.Y;
        Console.WriteLine();

        Console.Write("Press Y to use custom password instead of auto-encryption: ");
        CustomPassword = Console.ReadKey().Key == ConsoleKey.Y;
        Console.WriteLine();

        if (CustomPassword)
        {
            Console.Write("Enter custom encryption password (will NOT be stored in file): ");
            Password = Console.ReadLine();
            Console.WriteLine();
        }

        BasicCreationDemo();
        SectionsDemo();
        KeysValuesDemo();
        ClampAndAutoAddDemo();
        SearchAndRenameDemo();
        OptionsAndPresetsDemo();
        EncryptionPasswordDemo();
        await AsyncOperationsDemo();
        AutoFeaturesDemo();
        FileErrorRecoveryDemo();
        EventsDemo();
        ReadOnlyAndPerformanceDemo();

        Console.Write("Press Y to cleanup file: ");
        if (Console.ReadKey().Key == ConsoleKey.Y)
        {
            Console.WriteLine();
            CleanupDemo();
        }

        Console.WriteLine("\n\nDemonstration completed!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        Console.Clear();
    }

    private static NeoIniReader CreateReaderDefault()
    {
        if (CustomPassword) return new NeoIniReader(TestFile, Password);
        return new NeoIniReader(TestFile, Encryption);
    }

    private static NeoIniReader CreateReaderWithOptions(NeoIniReaderOptions options)
    {
        if (CustomPassword) return new NeoIniReader(TestFile, Password, options);
        return new NeoIniReader(TestFile, Encryption, options);
    }

    private static async Task<NeoIniReader> CreateReaderAsync(NeoIniReaderOptions options = null)
    {
        if (CustomPassword) return await NeoIniReader.CreateAsync(TestFile, Password, options);
        return await NeoIniReader.CreateAsync(TestFile, Encryption, options);
    }

    private static void BasicCreationDemo()
    {
        Console.Clear();
        Console.WriteLine("1. FILE CREATION WITH DEFAULTS");
        using var ini = CreateReaderDefault();

        Console.WriteLine($"File created: {TestFile}");
        Console.WriteLine("All features use default settings:");
        Console.WriteLine($"- AutoAdd: {ini.AutoAdd}");
        Console.WriteLine($"- AutoSave: {ini.AutoSave}");
        Console.WriteLine($"- AutoBackup: {ini.AutoBackup}");
        Console.WriteLine($"- UseChecksum: {ini.UseChecksum}");
        Console.WriteLine($"- AutoSaveInterval: {ini.AutoSaveInterval}");
        Console.WriteLine($"- SaveOnDispose: {ini.SaveOnDispose}");

        Console.WriteLine("\nCurrent raw INI view:");
        Console.WriteLine(ini.ToString());

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void SectionsDemo()
    {
        Console.Clear();
        Console.WriteLine("2. WORKING WITH SECTIONS");
        using var ini = CreateReaderDefault();

        Console.WriteLine($"Section 'User' exists: {ini.SectionExists("User")}");

        ini.AddSection("User");
        ini.AddSection("Database");
        Console.WriteLine("Added sections: User, Database");

        var sections = ini.GetAllSections();
        Console.WriteLine($"All sections ({sections.Length}): {string.Join(", ", sections)}");

        ini.RenameSection("Database", "DbConfig");
        Console.WriteLine("Section 'Database' renamed to 'DbConfig'");

        sections = ini.GetAllSections();
        Console.WriteLine($"Sections after rename ({sections.Length}): {string.Join(", ", sections)}");

        ini.RemoveSection("DbConfig");
        Console.WriteLine("Section 'DbConfig' removed");

        sections = ini.GetAllSections();
        Console.WriteLine($"Remaining sections ({sections.Length}): {string.Join(", ", sections)}");

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void KeysValuesDemo()
    {
        Console.Clear();
        Console.WriteLine("3. KEYS AND VALUES");
        using var ini = CreateReaderDefault();

        ini.AddKeyInSection("User", "Name", "John Doe");
        ini.AddKeyInSection("User", "Age", 30);
        ini.AddKeyInSection("User", "IsAdmin", true);
        ini.AddKeyInSection("User", "Salary", 75000.50);
        ini.AddKeyInSection("User", "Bio", "Line1\r\nLine2\r\nLine3");
        Console.WriteLine("Added keys to User section (including multiline Bio)");

        string name = ini.GetValue("User", "Name", "Unknown");
        int age = ini.GetValue("User", "Age", 0);
        bool isAdmin = ini.GetValue("User", "IsAdmin", false);
        double salary = ini.GetValue("User", "Salary", 0.0);
        string bio = ini.GetValue("User", "Bio", string.Empty);

        Console.WriteLine($"Name: {name}");
        Console.WriteLine($"Age: {age}");
        Console.WriteLine($"Admin: {isAdmin}");
        Console.WriteLine($"Salary: {salary}");
        Console.WriteLine("Bio (with line breaks preserved):");
        Console.WriteLine(bio);

        ini.SetKey("User", "Age", 31);
        Console.WriteLine("Age updated to 31");

        Console.WriteLine($"Key 'Name' exists: {ini.KeyExists("User", "Name")}");
        Console.WriteLine($"Key 'Email' exists: {ini.KeyExists("User", "Email")}");

        string email = ini.GetValue("User", "Email", "no@email.com");
        Console.WriteLine($"Email (auto-added if AutoAdd enabled): {email}");

        var keys = ini.GetAllKeys("User");
        Console.WriteLine($"Keys in User ({keys.Length}): {string.Join(", ", keys)}");

        ini.RenameKey("User", "Name", "FullName");
        Console.WriteLine("Key 'Name' renamed to 'FullName'");

        keys = ini.GetAllKeys("User");
        Console.WriteLine($"Keys in User after rename ({keys.Length}): {string.Join(", ", keys)}");

        ini.RemoveKey("User", "Salary");
        Console.WriteLine("Key 'Salary' removed");

        keys = ini.GetAllKeys("User");
        Console.WriteLine($"Keys in User after removal ({keys.Length}): {string.Join(", ", keys)}");

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void ClampAndAutoAddDemo()
    {
        Console.Clear();
        Console.WriteLine("3.1. CLAMP & AUTO-ADD BEHAVIOR");
        using var ini = CreateReaderDefault();

        Console.WriteLine("Writing values out of allowed range:");
        ini.SetKey("ClampDemo", "Volume", 200);
        ini.SetKey("ClampDemo", "Brightness", -10);

        int clampedVolume = ini.GetValueClamp("ClampDemo", "Volume", 0, 100, 50);
        int clampedBrightness = ini.GetValueClamp("ClampDemo", "Brightness", 0, 100, 50);
        Console.WriteLine($"Clamped Volume [0..100]: {clampedVolume}");
        Console.WriteLine($"Clamped Brightness [0..100]: {clampedBrightness}");

        Console.WriteLine("\nDemonstrating AutoAdd= false (Safe preset):");
        using var safeIni = CreateReaderWithOptions(NeoIniReaderOptions.Safe);
        Console.WriteLine($"Safe.AutoAdd: {safeIni.AutoAdd}");
        int missingValue = safeIni.GetValue("NonExisting", "Key", 123);
        Console.WriteLine($"GetValue on missing key (AutoAdd=false): {missingValue}");
        Console.WriteLine($"SectionExists('NonExisting'): {safeIni.SectionExists("NonExisting")}");

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void SearchAndRenameDemo()
    {
        Console.Clear();
        Console.WriteLine("3.2. SEARCH & RENAME DEMO");
        using var ini = CreateReaderDefault();

        ini.SetKey("SearchDemo", "Path", @"C:\Games\MyGame");
        ini.SetKey("SearchDemo", "Mode", "Debug");
        ini.SetKey("SearchDemo", "Description", "Game config for debug mode");

        var results = ini.Search("game");
        Console.WriteLine($"Search for 'game' found {results.Count} entries:");
        foreach (var (section, key, value) in results)
            Console.WriteLine($"  [{section}] {key} = {value}");

        Console.WriteLine("\nFind key 'Mode' in all sections:");
        var found = ini.FindKeyInAllSections("Mode");
        foreach (var kv in found)
            Console.WriteLine($"  Section [{kv.Key}] -> Mode = {kv.Value}");

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void OptionsAndPresetsDemo()
    {
        Console.Clear();
        Console.WriteLine("4. OPTIONS & PRESETS");

        using (var def = CreateReaderWithOptions(NeoIniReaderOptions.Default))
        {
            Console.WriteLine("Default options:");
            PrintOptions(def);
        }

        using (var safe = CreateReaderWithOptions(NeoIniReaderOptions.Safe))
        {
            Console.WriteLine("\nSafe options:");
            PrintOptions(safe);
        }

        using (var perf = CreateReaderWithOptions(NeoIniReaderOptions.Performance))
        {
            Console.WriteLine("\nPerformance options:");
            PrintOptions(perf);
        }

        using (var buffered = CreateReaderWithOptions(NeoIniReaderOptions.BufferedAutoSave(5)))
        {
            Console.WriteLine("\nBufferedAutoSave(5) options:");
            PrintOptions(buffered);
        }

        using (var readOnly = CreateReaderWithOptions(NeoIniReaderOptions.ReadOnly))
        {
            Console.WriteLine("\nReadOnly options:");
            PrintOptions(readOnly);
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void PrintOptions(NeoIniReader ini)
    {
        Console.WriteLine($"- AutoAdd: {ini.AutoAdd}");
        Console.WriteLine($"- AutoSave: {ini.AutoSave}");
        Console.WriteLine($"- AutoBackup: {ini.AutoBackup}");
        Console.WriteLine($"- UseChecksum: {ini.UseChecksum}");
        Console.WriteLine($"- AutoSaveInterval: {ini.AutoSaveInterval}");
        Console.WriteLine($"- SaveOnDispose: {ini.SaveOnDispose}");
    }

    private static void EncryptionPasswordDemo()
    {
        Console.Clear();
        Console.WriteLine("4.1. ENCRYPTION PASSWORD & MIGRATION");
        using var ini = CreateReaderDefault();

        string status = ini.GetEncryptionPassword();
        Console.WriteLine($"GetEncryptionPassword() => {status}");
        Console.WriteLine("If auto-encryption is enabled, this value can be used on another machine");
        Console.WriteLine("via NeoIniReader(path, password) / CreateAsync(path, password).");

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static async Task AsyncOperationsDemo()
    {
        Console.Clear();
        Console.WriteLine("5. ASYNCHRONOUS OPERATIONS");
        using var ini = await CreateReaderAsync();

        await ini.AddSectionAsync("Settings");
        await ini.SetKeyAsync("Settings", "Theme", "Dark");
        await ini.SetKeyAsync("Settings", "Volume", 80);
        Console.WriteLine("Settings section added asynchronously");

        string theme = await ini.GetValueAsync("Settings", "Theme", "Light");
        int volume = await ini.GetValueAsync("Settings", "Volume", 50);
        Console.WriteLine($"Theme: {theme}, Volume: {volume}%");

        int clampedVolume = await ini.GetValueClampAsync("Settings", "Volume", 0, 100, 50);
        Console.WriteLine($"Clamped Volume [0..100]: {clampedVolume}");

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void AutoFeaturesDemo()
    {
        Console.Clear();
        Console.WriteLine("6. AUTOMATIC FEATURES");
        using var ini = CreateReaderDefault();

        ini.SetKey("Database", "Host", "localhost");
        ini.SetKey("Database", "Port", 5432);
        ini.SaveFile();
        Console.WriteLine("Manual save completed");

        ini.ReloadFromFile();
        Console.WriteLine("Data reloaded from file");
        Console.WriteLine($"Database Host: {ini.GetValue("Database", "Host", "")}");

        ini.AutoSaveInterval = 3;
        Console.WriteLine($"Auto-save every {ini.AutoSaveInterval} operations");

        Console.Write("Adding logs: ");
        ini.OnAutoSave += () => Console.Write("SAVED ");
        for (int i = 1; i <= 6; i++)
        {
            ini.SetKey("Logs", $"Entry{i}", $"Log message {i}");
            if (i % 3 != 0) Console.Write(".");
        }
        Console.WriteLine("Auto-save triggered");

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void FileErrorRecoveryDemo()
    {
        Console.Clear();
        Console.WriteLine("7. FILE ERROR RECOVERY");
        using var ini = CreateReaderDefault();
        ini.OnChecksumMismatch += (expected, actual) =>
        {
            var expectedHex = BitConverter.ToString(expected);
            var actualHex = BitConverter.ToString(actual);
            Console.WriteLine($"Checksum mismatch: expected {expectedHex}, actual {actualHex}");
        };

        Console.WriteLine("\nBEFORE DAMAGE - All sections:");
        ShowContent(ini);

        CorruptFileManually(TestFile);

        Console.WriteLine("\nDAMAGED FILE CONTENT:");
        ShowFileRawContent(TestFile);

        Console.WriteLine("\nAttempting recovery with ini.ReloadFromFile()...");
        ini.ReloadFromFile();

        Console.WriteLine("\nAFTER RECOVERY - All sections:");
        ShowContent(ini);

        Console.WriteLine("\nRecovery demo completed!");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void EventsDemo()
    {
        Console.Clear();
        Console.WriteLine("8. EVENTS AND ACTIONS DEMONSTRATION");
        using var ini = CreateReaderDefault();

        ini.OnKeyAdded += (section, key, value) => Console.WriteLine($"NEW KEY ADDED: [{section}] {key} = '{value}'");
        ini.OnKeyChanged += (section, key, value) => Console.WriteLine($"KEY CHANGED: [{section}] {key} = '{value}'");
        ini.OnKeyRemoved += (section, key) => Console.WriteLine($"KEY REMOVED: [{section}] {key}");

        ini.OnSectionAdded += section => Console.WriteLine($"NEW SECTION: [{section}]");
        ini.OnSectionRemoved += section => Console.WriteLine($"SECTION REMOVED: [{section}]");
        ini.OnSectionRenamed += (oldSection, newSection) => Console.WriteLine($"SECTION RENAMED: [{oldSection}] -> [{newSection}]");
        ini.OnDataCleared += () => Console.WriteLine("ALL DATA CLEARED");
        ini.OnAutoSave += () => Console.WriteLine("AUTO-SAVING FILE...");
        ini.OnSave += () => Console.WriteLine("SAVING FILE...");
        ini.OnLoad += () => Console.WriteLine("FILE LOADED!");
        ini.OnSearchCompleted += (pattern, count) => Console.WriteLine($"SEARCH COMPLETED: '{pattern}' -> {count} matches");

        Console.WriteLine("Demonstrating Events/Actions:");

        Console.WriteLine("1. Added EventsDemo section");
        ini.AddSection("EventsDemo");

        Console.WriteLine("2. Added Counter key");
        ini.AddKeyInSection("EventsDemo", "Counter", 0);

        Console.WriteLine("3. Changed Counter value");
        ini.SetKey("EventsDemo", "Counter", 42);

        Console.WriteLine("4. Added Status key");
        ini.AddKeyInSection("EventsDemo", "Status", "Active");

        Console.WriteLine("5. Removed Status key");
        ini.RemoveKey("EventsDemo", "Status");

        Console.WriteLine("6. Renamed section EventsDemo -> EventsDemoRenamed");
        ini.RenameSection("EventsDemo", "EventsDemoRenamed");

        Console.WriteLine("7. Manual save triggered");
        ini.SaveFile();

        Console.WriteLine("8. Search pattern 'counter'");
        var search = ini.Search("counter");

        Console.WriteLine("\nEvents demonstration completed!");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void ReadOnlyAndPerformanceDemo()
    {
        Console.Clear();
        Console.WriteLine("9. READONLY & PERFORMANCE MODES");

        Console.WriteLine("ReadOnly mode: file is loaded and verified, but never modified.");
        using (var readOnly = CreateReaderWithOptions(NeoIniReaderOptions.ReadOnly))
        {
            Console.WriteLine("- Trying to GetValue with default for missing key:");
            int v = readOnly.GetValue("ROSection", "ROKey", 10);
            Console.WriteLine($"  Value: {v}");
            Console.WriteLine("- After call, section should still not exist (no AutoAdd):");
            Console.WriteLine($"  SectionExists('ROSection'): {readOnly.SectionExists("ROSection")}");
        }

        Console.WriteLine("\nPerformance mode: all safety features disabled, caller manages saves.");
        using (var perf = CreateReaderWithOptions(NeoIniReaderOptions.Performance))
        {
            Console.WriteLine("- Writing several values without autosave/checksum/backup:");
            for (int i = 0; i < 5; i++)
                perf.SetKey("Perf", $"Key{i}", i);

            Console.WriteLine("- Data not on disk until SaveFile() is called explicitly.");
            perf.SaveFile();
            Console.WriteLine("- Manual SaveFile() completed.");
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void CleanupDemo()
    {
        Console.Clear();
        Console.WriteLine("10. COMPLETE CLEANUP");
        using var ini = CreateReaderDefault();

        Console.WriteLine("Final content before cleanup:");
        ShowContent(ini);

        ini.DeleteFileWithData();
        Console.WriteLine("File deleted from disk + memory cleared");
    }

    private static void ShowContent(NeoIniReader ini) => Console.WriteLine(ini.ToString());

    private static void CorruptFileManually(string filePath)
    {
        Console.WriteLine($"\nMANUALLY edit '{filePath}' now!");
        Console.WriteLine("1. Open file in notepad/text editor");
        Console.WriteLine("2. DELETE or EDIT any lines");
        Console.WriteLine("3. SAVE the file");
        Console.WriteLine("4. Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void ShowFileRawContent(string filePath)
    {
        if (File.Exists(filePath))
        {
            Console.WriteLine($"Raw file content ({new FileInfo(filePath).Length} bytes):");
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines) Console.WriteLine(line);
        }
        else Console.WriteLine("File not found!");
    }
}
