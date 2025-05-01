// src/ClipboardManager.Infrastructure/Services/JsonSettingsService.cs
using ClipboardManager.Application.Interfaces;
using System;
using System.IO;
using System.Text.Json; // Necesario para la serialización JSON
using System.Threading.Tasks;

namespace ClipboardManager.Infrastructure.Services
{
    public class JsonSettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            // Opciones para que el JSON sea más legible (opcional)
            WriteIndented = true,
            // Podrías añadir converters si tienes tipos complejos
        };

        public JsonSettingsService()
        {
            // Define dónde se guardará el archivo de configuración
            // Usar LocalApplicationData es una buena práctica
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataFolder, "ClipboardManager"); // Nombre de tu app
            Directory.CreateDirectory(appFolder); // Asegurarse que el directorio exista
            _settingsFilePath = Path.Combine(appFolder, "settings.json"); // Nombre del archivo

            Console.WriteLine($"Settings file path: {_settingsFilePath}");
        }

        public async Task<AppSettings> GetSettingsAsync()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    Console.WriteLine("Settings file not found. Returning default settings.");
                    // Si el archivo no existe, devuelve los valores por defecto definidos en AppSettings
                    return new AppSettings();
                }

                Console.WriteLine("Reading settings file...");
                string json = await File.ReadAllTextAsync(_settingsFilePath);

                // Intenta deserializar. Si falla o está vacío, devuelve defaults.
                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine("Settings file is empty. Returning default settings.");
                    return new AppSettings();
                }

                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

                if (settings == null)
                {
                    Console.WriteLine("Failed to deserialize settings. Returning default settings.");
                    return new AppSettings();
                }

                Console.WriteLine("Settings loaded successfully.");
                return settings;

            }
            catch (Exception ex)
            {
                // Log el error (idealmente con un logger real)
                Console.WriteLine($"Error reading settings file: {ex.Message}. Returning default settings.");
                // En caso de error al leer/parsear, devuelve los defaults
                return new AppSettings();
            }
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                Console.WriteLine("Saving settings...");
                string json = JsonSerializer.Serialize(settings, _jsonOptions);
                await File.WriteAllTextAsync(_settingsFilePath, json);
                Console.WriteLine("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                // Log el error
                Console.WriteLine($"Error saving settings file: {ex.Message}");
                // Podrías lanzar la excepción o manejarla de otra forma
            }
        }
    }
}