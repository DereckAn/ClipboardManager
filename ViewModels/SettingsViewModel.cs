using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipboard.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {

        // ==================== General ====================
        [ObservableProperty]
        private int _maxItemsInDatabase = 10000;

        [ObservableProperty]
        private int _maxCacheSize = 1000;

        [ObservableProperty]
        private int _retentionDays = 30;

        [ObservableProperty]
        private bool _autoStartWithWindows = false;

        // ==================== Hotkey ====================
        [ObservableProperty]
        private string _globalHotkey = "Ctrl + Shift + V";

        [ObservableProperty]
        private string _historyHotkey = "Ctrl + 1";

        [ObservableProperty]
        private string _favoritesHotkey = "Ctrl + 2";

        // ==================== Theme ====================

        [ObservableProperty]
        private string _selectedTheme = "Sistema";

        public ObservableCollection<string> ThemeOptions { get; } = new()
        {
            "Claro",
            "Oscuro",
            "Sistema"
        };

        [ObservableProperty]
        private int _fontSize = 14;

        [ObservableProperty]
        private bool _compactMode = false;


        // ==================== Content ====================
        [ObservableProperty]
        private bool _captureText = true;

        [ObservableProperty]
        private bool _captureImages = true;

        [ObservableProperty]
        private bool _captureColors = true;

        [ObservableProperty]
        private bool _captureUrls = true;

        [ObservableProperty]
        private bool _captureCode = true;

        [ObservableProperty]
        private string _excludedApplications = string.Empty;

        // ==================== Sync ====================

        [ObservableProperty]
        private bool _enableAutoBackup = false;
        
        [ObservableProperty]
        private string _backupLocation = "C:\\Users\\[tu-usuario]\\Documents\\ClipboardBackups";

        [ObservableProperty]
        private bool _enableCloudSync = false;

        [ObservableProperty]
        private bool enableEncryption = false;

        // ==================== Construction ====================

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            try
            {
                // TODO: Implementar guardado en archivo de configuración
                // Por ahora solo mostramos mensaje de éxito
                System.Diagnostics.Debug.WriteLine("Configuraciones guardadas exitosamente");

                // Aplicar cambios inmediatos
                ApplySettings();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando configuración: { ex.Message}");
              }
        }

        [RelayCommand]
        private void RestoreDefaultSettings()
        {
            MaxItemsInDatabase = 10000;
            MaxCacheSize = 1000;
            RetentionDays = 30;
            GlobalHotkey = "Ctrl+Shift+V";
            SelectedTheme = "Sistema";
            FontSize = 14;
            CaptureText = true;
            CaptureImages = true;
            CaptureColors = true;
            CaptureUrls = true;
            CaptureCode = true;
            ExcludedApplications = "notepad.exe, cmd.exe";

            System.Diagnostics.Debug.WriteLine("Configuraciones restauradas a valores por defecto");
          }

        [RelayCommand]
        private async Task TestHotkeyAsync()
        {
            System.Diagnostics.Debug.WriteLine($"Probando hotkey: {GlobalHotkey}");
            // TODO: Implementar test de hotkey
        }

        [RelayCommand]
        private async Task SelectBackupLocationAsync()
        {
            // TODO: Implementar selector de carpeta
            System.Diagnostics.Debug.WriteLine("Selector de carpeta para backup");
        }

        // ============ MÉTODOS PRIVADOS ============

        private void LoadSettings()
        {
            // TODO: Cargar desde archivo de configuración
            // Por ahora usar valores por defecto
            System.Diagnostics.Debug.WriteLine("Cargando configuraciones...");
        }

        private void ApplySettings()
        {
            // TODO: Aplicar cambios a la aplicación
            // - Actualizar límites en MainWindowViewModel
            // - Cambiar tema
            // - Registrar hotkeys
            System.Diagnostics.Debug.WriteLine("Aplicando configuraciones...");
        }

    }
}
