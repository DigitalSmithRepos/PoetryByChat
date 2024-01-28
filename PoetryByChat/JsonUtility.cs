using System.Text.Json;

public static class JsonUtility
{
    public static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,

    };

    public static T? DeserializeFile<T>(string filePath)
    {
        FileStream fileStream = null!;
        try
        {
            fileStream = new FileStream(filePath, FileMode.Open);
            return JsonSerializer.Deserialize<T>(fileStream, jsonSerializerOptions);
        }
        finally
        {
            fileStream.Close();
            fileStream.Dispose();
        }
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, jsonSerializerOptions);
    }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, jsonSerializerOptions);
    }
}