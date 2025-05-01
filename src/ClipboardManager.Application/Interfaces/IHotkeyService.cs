// src/ClipboardManager.Application/Interfaces/IHotkeyService.cs
using System;

namespace ClipboardManager.Application.Interfaces
{
    public interface IHotkeyService : IDisposable // Implementar IDisposable para liberar recursos (hotkeys)
    {
        /// <summary>
        /// Evento que se dispara cuando se presiona el hotkey registrado.
        /// </summary>
        event Action? HotkeyActivated;

        /// <summary>
        /// Registra un hotkey global basado en una cadena (ej. "Control+Alt+V").
        /// </summary>
        /// <param name="hotkeyCombination">La combinación de teclas.</param>
        /// <returns>True si el registro fue exitoso, False en caso contrario.</returns>
        bool RegisterHotkey(string hotkeyCombination);

        /// <summary>
        /// Desregistra el hotkey previamente registrado.
        /// </summary>
        void UnregisterHotkey();
    }
}