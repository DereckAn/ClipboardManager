using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Clipboard.Services
{
    public class SystemTrayService : ISystemTrayService
    {
        // Constantes
        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;

        private const uint WM_USER = 0x0400;
        private const uint WM_TRAYICON = WM_USER + 1;

        private readonly ILogger<SystemTrayService> _logger;
        private IntPtr _windowHandle;
        private NOTIFYICONDATA _notifyIconData;
        private bool _isInitialized;

        public event EventHandler? TrayIconClicked;
        public event EventHandler? ShowMainWindowRequested;
        public event EventHandler? ExitRequested;
        public event EventHandler? SettingsRequested;
        public event EventHandler? AutoStartToggleRequested;

        // Constantes para menús
        private const uint MF_STRING = 0x0000;
        private const uint MF_SEPARATOR = 0x0800;
        private const uint MF_CHECKED = 0x0008;
        private const uint MF_UNCHECKED = 0x0000;
        private const uint TPM_LEFTALIGN = 0x0000;
        private const uint TPM_RETURNCMD = 0x0100;

        // IDs para los elementos del menú
        private const uint MENU_SHOW_WINDOW = 1000;
        private const uint MENU_SETTINGS = 1001;
        private const uint MENU_AUTOSTART = 1002;
        private const uint MENU_EXIT = 1003;

        // Constantes para LoadImage
        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x00000010;
        private const uint LR_DEFAULTSIZE = 0x00000040;

        // Win32 API para System Tray
        [DllImport("shell32.dll")]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA pnid);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        // Estructuras Win32
        [StructLayout(LayoutKind.Sequential)]
        private struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        // Win32 API para menús contextuales
        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll")]
        private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string
        lpNewItem);

        [DllImport("user32.dll")]
        private static extern uint TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int
        nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // Win32 API para cargar iconos
        [DllImport("user32.dll")]
        private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public SystemTrayService(ILogger<SystemTrayService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Initialize(Window mainWindow)
        {
            _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
            CreateTrayIcon();
            _isInitialized = true;
            _logger.LogInformation("SystemTrayService initialized");
        }

        private void CreateTrayIcon()
        {
            _notifyIconData = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _windowHandle,
                uID = 1,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = LoadApplicationIcon(), // TODO: Implementar
                szTip = "Clipboard Manager"
            };
        }

        private IntPtr LoadApplicationIcon()
        {
            _logger.LogInformation("🦆 Intentando cargar icono personalizado...");

            try
            {
                var iconPath = ExtractEmbeddedIcon();

                if (!string.IsNullOrEmpty(iconPath) && System.IO.File.Exists(iconPath))
                {
                    _logger.LogInformation("🦆 Cargando icono desde archivo temporal...");

                    var icon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);

                    if (icon != IntPtr.Zero)
                    {
                        _logger.LogInformation("🦆 ✅ Icono personalizado cargado exitosamente!");
                        return icon;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"🦆 ❌ Error cargando icono personalizado: {ex.Message}");
            }

            _logger.LogInformation("🦆 Usando icono por defecto del sistema");
            return IntPtr.Zero;
        }

        private string ExtractEmbeddedIcon()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                _logger.LogInformation($"🦆 Assembly: {assembly.FullName}");

                var resourceName = "Clipboard.Assets.pato.ico";
                _logger.LogInformation($"🦆 Buscando recurso: {resourceName}");

                // Listar todos los recursos disponibles
                var availableResources = assembly.GetManifestResourceNames();
                _logger.LogInformation($"🦆 Recursos disponibles: {string.Join(", ", availableResources)}");

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    _logger.LogInformation($"🦆 Recurso encontrado, tamaño: {stream.Length} bytes");

                    // ARREGLO: Crear archivo temporal con extensión .ico desde el inicio
                    var tempPath = System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(),
                        $"clipboard_icon_{Guid.NewGuid()}.ico"
                    );

                    _logger.LogInformation($"🦆 Creando archivo temporal: {tempPath}");

                    using (var fileStream = System.IO.File.Create(tempPath))
                    {
                        stream.CopyTo(fileStream);
                        fileStream.Flush(); // IMPORTANTE: Forzar escritura al disco
                    }

                    // VERIFICACIÓN: Confirmar que el archivo fue escrito correctamente
                    var fileInfo = new System.IO.FileInfo(tempPath);
                    _logger.LogInformation($"🦆 Archivo temporal creado: {tempPath}");
                    _logger.LogInformation($"🦆 Tamaño del archivo en disco: {fileInfo.Length} bytes");
                    _logger.LogInformation($"🦆 Archivo existe: {fileInfo.Exists}");

                    return tempPath;
                }
                else
                {
                    _logger.LogWarning($"🦆 ❌ Recurso no encontrado: {resourceName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"🦆 ❌ Error extrayendo icono embebido: {ex.Message}");
            }

            return string.Empty;
        }

        public void ShowTrayIcon()
        {
            if (_isInitialized)
            {
                Shell_NotifyIcon(NIM_ADD, ref _notifyIconData);
            }
        }

        public void HideTrayIcon()
        {
            if (_isInitialized)
            {
                Shell_NotifyIcon(NIM_DELETE, ref _notifyIconData);
            }
        }

        public void Dispose()
        {
            if (_isInitialized)
            {
                HideTrayIcon();
                _isInitialized = false;
                _logger.LogInformation("SystemTrayService disposed");
            }
        }

        public void TriggerTrayIconClicked()
        {
            TrayIconClicked?.Invoke(this, EventArgs.Empty);
        }

        public void ShowContextMenu()
        {
            // Obtener posición del cursor
            if (GetCursorPos(out POINT cursorPos))
            {
                // Crear menú
                var menu = CreatePopupMenu();

                // Agregar elementos del menú
                AppendMenu(menu, MF_STRING, MENU_SHOW_WINDOW, "Mostrar Ventana Principal");
                AppendMenu(menu, MF_STRING, MENU_SETTINGS, "Configuración");
                AppendMenu(menu, MF_SEPARATOR, 0, "");
                AppendMenu(menu, MF_STRING | MF_UNCHECKED, MENU_AUTOSTART, "Iniciar con Windows");
                AppendMenu(menu, MF_STRING, MENU_EXIT, "Salir");

                // Necesario para que el menú funcione correctamente
                SetForegroundWindow(_windowHandle);

                // Mostrar menú y obtener selección
                var selectedId = TrackPopupMenu(menu, TPM_LEFTALIGN | TPM_RETURNCMD, cursorPos.X, cursorPos.Y, 0, _windowHandle, IntPtr.Zero);

                // Procesar selección
                HandleMenuSelection(selectedId);

                // Limpiar recursos
                DestroyMenu(menu);
            }
        }

        private void HandleMenuSelection(uint menuId)
        {
            switch (menuId)
            {
                case MENU_SHOW_WINDOW:
                    ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case MENU_SETTINGS:
                    SettingsRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case MENU_AUTOSTART:
                    AutoStartToggleRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case MENU_EXIT:
                    ExitRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}