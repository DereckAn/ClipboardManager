using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardManager.Application.Interfaces
{
    public interface IClipboardMonitorService
    {
        // Eventos y métodos definidos aquí...
        event Action<Core.Entities.ClipboardItem>? ClipboardChanged;
        void StartMonitoring();
        void StopMonitoring();
        // Añadir IDisposable si es necesario
    }
}
