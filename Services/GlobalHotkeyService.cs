using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using System.Threading.Tasks;
using Windows.System;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Clipboard.Services
{
    public class GlobalHotkeyService : IGlobalHotkeyService
    {
        private readonly ILogger<GlobalHotkeyService> _logger;
        private readonly Dictionary<string, int> _registeredHotkeys = new();
        private bool _isInitialized = false;
        private int _currentHotkeyId = 9000;
        private IntPtr _windowHandle = IntPtr.Zero;

        // Win32 API Constants
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint WM_HOTKEY = 0x0312;

        // Delegate para el hook de ventana
        private delegate IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WindowProc _windowProc;
        private IntPtr _originalWindowProc;

        // Win32 API para subclassing
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const int GWL_WNDPROC = -4;
        private DispatcherQueue? _dispatcherQueue;

        // Win32 API
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Eventos
        public event EventHandler<string>? HotkeyPressed;

        public bool IsInitialized => _isInitialized;

        private readonly ISystemTrayService _systemTrayService;

        public GlobalHotkeyService(ILogger<GlobalHotkeyService> logger, ISystemTrayService systemTrayService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _systemTrayService = systemTrayService ?? throw new ArgumentNullException(nameof(systemTrayService));
        }

        public async Task InitializeAsync(Window mainWindow)
        {
            try
            {
                // Obtener handle de la ventana principal
                _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

                // Obtener dispatcher para UI thread
                _dispatcherQueue = mainWindow.DispatcherQueue;

                // Instalar el hook de ventana
                _windowProc = new WindowProc(WindowProcHook);
                _originalWindowProc = SetWindowLongPtr(_windowHandle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_windowProc));

                _isInitialized = true;
                _logger.LogInformation("GlobalHotkeyService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize GlobalHotkeyService");
                _isInitialized = false;
            }
        }

        public async Task<bool> RegisterHotkeyAsync(string hotkeyId, uint modifiers, VirtualKey virtualKey)
        {
            if (!_isInitialized)
            {
                _logger.LogWarning("Service not initialized");
                return false;
            }

            try
            {
                var hotkeyIdInt = ++_currentHotkeyId;
                var success = RegisterHotKey(_windowHandle, hotkeyIdInt, modifiers, (uint)virtualKey);

                if (success)
                {
                    _registeredHotkeys[hotkeyId] = hotkeyIdInt;
                    _logger.LogInformation($"Registered hotkey: {hotkeyId} (ID: { hotkeyIdInt})");
                      return true;
                }
                else
                {
                    _logger.LogWarning($"Failed to register hotkey: {hotkeyId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception registering hotkey: {hotkeyId}");
                return false;
            }
        }

        public async Task<bool> UnregisterHotkeyAsync(string hotkeyId)
        {
            if (!_registeredHotkeys.TryGetValue(hotkeyId, out var hotkeyIdInt))
            {
                _logger.LogWarning($"Hotkey not found: {hotkeyId}");
                return false;
            }

            try
            {
                var success = UnregisterHotKey(_windowHandle, hotkeyIdInt);
                if (success)
                {
                    _registeredHotkeys.Remove(hotkeyId);
                    _logger.LogInformation($"Unregistered hotkey: {hotkeyId}");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception unregistering hotkey: {hotkeyId}");
                return false;
            }
        }

        public void UnregisterAllHotkeys()
        {
            foreach (var kvp in _registeredHotkeys.ToList())
            {
                UnregisterHotKey(_windowHandle, kvp.Value);
                _logger.LogInformation($"Unregistered hotkey: {kvp.Key}");
            }
            _registeredHotkeys.Clear();
        }

        public bool ParseHotkeyString(string hotkeyText, out uint modifiers, out VirtualKey virtualKey)
        {
            modifiers = 0;
            virtualKey = VirtualKey.None;

            if (string.IsNullOrWhiteSpace(hotkeyText))
                return false;

            var parts = hotkeyText.Split('+');
            if (parts.Length < 2) return false;

            // Procesar modificadores
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var part = parts[i].Trim().ToLower();
                switch (part)
                {
                    case "ctrl":
                    case "control":
                        modifiers |= MOD_CONTROL;
                        break;
                    case "shift":
                        modifiers |= MOD_SHIFT;
                        break;
                    case "alt":
                        modifiers |= MOD_ALT;
                        break;
                    case "win":
                    case "windows":
                        modifiers |= MOD_WIN;
                        break;
                    default:
                        return false;
                }
            }

            // Procesar tecla principal
            var keyPart = parts[^1].Trim().ToUpper();

            // Mapear teclas comunes
            virtualKey = keyPart switch
            {
                "V" => VirtualKey.V,
                "C" => VirtualKey.C,
                "X" => VirtualKey.X,
                "Z" => VirtualKey.Z,
                "Y" => VirtualKey.Y,
                "SPACE" => VirtualKey.Space,
                "ENTER" => VirtualKey.Enter,
                "F1" => VirtualKey.F1,
                "F2" => VirtualKey.F2,
                "F3" => VirtualKey.F3,
                "F4" => VirtualKey.F4,
                "F5" => VirtualKey.F5,
                "F6" => VirtualKey.F6,
                "F7" => VirtualKey.F7,
                "F8" => VirtualKey.F8,
                "F9" => VirtualKey.F9,
                "F10" => VirtualKey.F10,
                "F11" => VirtualKey.F11,
                "F12" => VirtualKey.F12,
                _ => VirtualKey.None
            };

            return virtualKey != VirtualKey.None;
        }

        public void Dispose()
        {
            if (_isInitialized)
            {
                UnregisterAllHotkeys();
                _isInitialized = false;
                _logger.LogInformation("GlobalHotkeyService disposed");
            }
        }

        private IntPtr WindowProcHook(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            // Interceptar WM_HOTKEY
            if (msg == WM_HOTKEY)
            {
                ProcessHotkeyMessage(wParam.ToInt32());
            }
            // Interceptar mensajes del tray icon
            else if (msg == 0x0401) // WM_TRAYICON
            {
                ProcessTrayIconMessage(wParam, lParam);
            }

            // Llamar al procesador original
            return CallWindowProc(_originalWindowProc, hWnd, msg, wParam, lParam);
        }

        private void ProcessHotkeyMessage(int hotkeyId)
        {
            // Buscar qué hotkey string corresponde a este ID
            var hotkeyString = _registeredHotkeys.FirstOrDefault(kvp => kvp.Value == hotkeyId).Key;

            if (!string.IsNullOrEmpty(hotkeyString))
            {
                _logger.LogInformation($"Hotkey received: {hotkeyString}");

                // Disparar evento en UI thread
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    HotkeyPressed?.Invoke(this, hotkeyString);
                });
            }
        }

        private void ProcessTrayIconMessage(IntPtr wParam, IntPtr lParam)
        {
            uint message = (uint)(lParam.ToInt32() & 0xFFFF);

            switch (message)
            {
                case 0x0202: // WM_LBUTTONUP (click izquierdo)
                    _logger.LogInformation("Tray icon left clicked");

                    // Disparar evento usando dispatcher
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        // Disparar eventos del SystemTrayService
                        ((SystemTrayService)_systemTrayService).TriggerTrayIconClicked();
                    });
                    break;

                case 0x0205: // WM_RBUTTONUP (click derecho)
                    _logger.LogInformation("Tray icon right clicked");
                    // TODO: Mostrar menu contextual
                    break;
            }
        }
    }
}