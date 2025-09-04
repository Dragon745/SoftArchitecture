using System.Text.Json;
using SoftArchitecture.Models;
using System.IO;
using System.Windows;

namespace SoftArchitecture.Services
{
    public class ProjectDataService
    {
        public static ProjectData? LoadProjectData(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                string jsonContent = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<ProjectData>(jsonContent, options);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading project data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}
