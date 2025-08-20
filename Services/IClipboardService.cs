using Clipboard.Models;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipboard.Services
{
    /// Interfaz para el servicio de monitoreo del portapapeles
    public interface IClipboardService
    {
        // Evento que se dispara cuando cambia el contenido del portapapeles
        event EventHandler<ClipboardItem> ClipboardChanged;

        // Evento que se dispara cuando ocurre un error
        event EventHandler<Exception>? ErrorOccurred;

        // Inicia el monitoreo del portapapeles pasando la ventana principal
        Task StartMonitoringAsync(Window mainWindow);

        // Detiene el monitoreo del portapapeles
        Task StopMonitoringAsync();

        // Indica si el servicio está monitoreando el portapapeles
        bool IsMonitoring { get; }
    }
}
