using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardManager.Application.Interfaces
{
    public interface ISettingsService
    {
        Task<AppSettings> GetSettingsAsync(); // AppSettings sería otra clase o record
        Task SaveSettingsAsync(AppSettings settings);
        // Podrías tener métodos más específicos:
        // TimeSpan GetHistoryRetentionPeriod();
        // Hotkey GetActivationHotkey();
    }

    // Ejemplo de clase para las configuraciones
    public record AppSettings
    {
        public TimeSpan HistoryRetention { get; init; } = TimeSpan.FromDays(90); // Default 3 meses
        public string ActivationHotkey { get; init; } = "Control+Alt+V"; // Default hotkey
        public bool ShowNotifications { get; init; } = true;
        // ... otras configuraciones
    }
}
