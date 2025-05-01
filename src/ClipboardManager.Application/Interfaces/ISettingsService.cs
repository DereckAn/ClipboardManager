// src/ClipboardManager.Application/Interfaces/ISettingsService.cs
using System;
using System.Threading.Tasks; // Para los métodos async

namespace ClipboardManager.Application.Interfaces
{
    /// <summary>
    /// Define las configuraciones de la aplicación.
    /// Usar 'record' permite inmutabilidad fácil.
    /// </summary>
    public record AppSettings
    {
        // Tiempo que se guarda el historial. TimeSpan.Zero o MaxValue podría significar indefinido.
        public TimeSpan HistoryRetentionPeriod { get; init; } = TimeSpan.FromDays(90); // Default 90 días
        // Atajo de teclado para activar la ventana principal.
        public string ActivationHotkey { get; init; } = "Control+Alt+V"; // Default Ctrl+Alt+V
        // Mostrar notificaciones al copiar.
        public bool ShowCopyNotifications { get; init; } = false; // Default desactivado
        // Número máximo de items a mostrar en la UI (no necesariamente el límite guardado).
        public int MaxItemsInView { get; init; } = 100;
        // Tema de la aplicación (Light, Dark, System)
        public string Theme { get; init; } = "System"; // O un enum
        // ... otras configuraciones futuras (ej. sync, etc.)
    }

    /// <summary>
    /// Interfaz para gestionar la carga y guardado de las configuraciones de la aplicación.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Carga las configuraciones de la aplicación de forma asíncrona.
        /// Si no existen, devuelve las configuraciones por defecto.
        /// </summary>
        /// <returns>Una tarea que representa la operación de carga, con las configuraciones como resultado.</returns>
        Task<AppSettings> GetSettingsAsync();

        /// <summary>
        /// Guarda las configuraciones de la aplicación proporcionadas de forma asíncrona.
        /// </summary>
        /// <param name="settings">Las configuraciones a guardar.</param>
        /// <returns>Una tarea que representa la operación de guardado.</returns>
        Task SaveSettingsAsync(AppSettings settings);
    }
}