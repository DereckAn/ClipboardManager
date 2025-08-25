using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipboard.Services
{
    public interface ISystemTrayService : IDisposable
    {
        void Initialize(Window mainWindow);
        void ShowTrayIcon();
        void HideTrayIcon();
        event EventHandler? TrayIconClicked;
        event EventHandler? ShowMainWindowRequested;
        event EventHandler? ExitRequested;
    }
}
