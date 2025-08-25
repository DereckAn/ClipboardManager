using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace Clipboard.Services
{
    public interface IGlobalHotkeyService : IDisposable
    {
        // Eventos
        event EventHandler<string> HotkeyPressed;


        // Métodos principales
        Task<bool> RegisterHotkeyAsync(string hotkeyId, uint modifiers, VirtualKey virtualKey);
        Task<bool> UnregisterHotkeyAsync(string hotkeyId);
        void UnregisterAllHotkeys();

        // Helpers
        bool ParseHotkeyString(string hotkeyText, out uint modifiers, out VirtualKey virtualKey);


        // Estado
        bool IsInitialized { get; }

        // Inicialización
        Task InitializeAsync(Window mainWindow);
    }
}