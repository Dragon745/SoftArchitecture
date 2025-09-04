using System;
using System.IO;
using System.Text.Json;
using SoftArchitecture.Models;

namespace SoftArchitecture.Services
{
    public static class FirebaseDatabaseService
    {
        public static FirebaseDatabaseData? LoadFirebaseDatabaseData(string jsonPath)
        {
            try
            {
                if (!File.Exists(jsonPath))
                {
                    return null;
                }

                string jsonContent = File.ReadAllText(jsonPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<FirebaseDatabaseData>(jsonContent, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading Firebase database data: {ex.Message}");
                return null;
            }
        }
    }
}
