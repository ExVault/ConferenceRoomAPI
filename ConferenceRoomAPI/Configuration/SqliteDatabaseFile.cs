namespace ConferenceRoomAPI.Configuration;

public static class SqliteDatabaseFile
{
    public static void EnsureCreated(string path, string basePath)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("SQLite database path must not be empty.");
        }

        var fullPath = Path.GetFullPath(path, basePath);

        if (string.IsNullOrWhiteSpace(Path.GetFileName(fullPath)))
        {
            throw new InvalidOperationException($"SQLite database path must point to a file: '{path}'.");
        }

        var directoryPath = Path.GetDirectoryName(fullPath);

        if (directoryPath == null)
        {
            throw new InvalidOperationException($"SQLite database directory could not be determined: '{path}'.");
        }

        Directory.CreateDirectory(directoryPath);

        using var file = File.Open(fullPath, FileMode.OpenOrCreate);
    }
}