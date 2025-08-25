using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace Clipboard.Services
{
    public class SystemTrayService : ISystemTrayService
    {
        // Win32 API para System Tray
        [DllImport("shell32.dll")]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA pnid);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        // Constantes
        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;

        private const uint WM_USER = 0x0400;
        private const uint WM_TRAYICON = WM_USER + 1;

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

        private readonly ILogger<SystemTrayService> _logger;
        private IntPtr _windowHandle;
        private NOTIFYICONDATA _notifyIconData;
        private bool _isInitialized;

        public event EventHandler? TrayIconClicked;
        public event EventHandler? ShowMainWindowRequested;
        public event EventHandler? ExitRequested;

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
            // TODO: Cargar icono de la aplicación
            // Por ahora retornamos IntPtr.Zero (icono por defecto del sistema)
            return IntPtr.Zero;
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
    }
}