using System.Text.Json;

public static class JsonFileHandler
{
        
        public static T Read<T>(string filePath)
        {
            string text = File.ReadAllText(filePath);

            T convertedData = JsonSerializer.Deserialize<T>(text);           
            return convertedData;
        }

        public static void Write<T>(T data, string filePath, Boolean writeIndented = true)
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = writeIndented
            };

            string text = JsonSerializer.Serialize(data, options);
            File.WriteAllText(filePath, text);
        }

}

