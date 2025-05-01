using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipboardManager.Application.Interfaces;
using System.Collections.ObjectModel;
using System.Windows; // Para Clipboard.SetDataObject - Considerar mover a un servicio de UI

namespace ClipboardManager.Application.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IClipboardHistoryRepository _repository;
    private readonly IClipboardMonitorService _monitorService; // Para iniciar/detener
    private readonly ISettingsService _settingsService;       // Para obtener settings
    // Inyectar IHotkeyService si el VM maneja la visibilidad de la ventana

    [ObservableProperty]
    private ObservableCollection<ClipboardItemViewModel> _clipboardItems = new();

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private bool _isLoading = false;

    public MainViewModel(
        IClipboardHistoryRepository repository,
        IClipboardMonitorService monitorService,
        ISettingsService settingsService)
    {
        _repository = repository;
        _monitorService = monitorService;
        _settingsService = settingsService;

        // Escuchar cambios detectados por el monitor service
        // Esto debería hacerse de forma más robusta, quizás con un mediador o eventos específicos
        // _monitorService.ClipboardChanged += OnClipboardChangedDetected;

        LoadHistoryCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        IsLoading = true;
        try
        {
            // Cargar configuración (ej. límite de items)
            // var settings = await _settingsService.GetSettingsAsync();
            var items = await _repository.GetItemsAsync(limit: 100, searchQuery: SearchText /*, filterType: currentFilter*/);
            ClipboardItems.Clear();
            foreach (var item in items)
            {
                ClipboardItems.Add(new ClipboardItemViewModel(item, _repository)); // Pasar repo para acciones
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Este método debería ser llamado por el servicio de monitorización
    // cuando detecta un nuevo ítem y lo guarda en el repo.
    // El VM sólo necesita refrescar su vista.
    public async Task HandleNewClipboardItemAsync(Guid newItemId)
    {
        var newItem = await _repository.GetItemByIdAsync(newItemId);
        if (newItem != null)
        {
            // Añadir al principio de la lista en la UI
            ClipboardItems.Insert(0, new ClipboardItemViewModel(newItem, _repository));
            // Opcional: Limitar el tamaño de la colección en memoria
            // if (ClipboardItems.Count > MAX_DISPLAY_ITEMS) ClipboardItems.RemoveAt(ClipboardItems.Count - 1);
        }
    }

    partial void OnSearchTextChanged(string? value)
    {
        // Idealmente, esto debería disparar una recarga con filtro debounce
        // Simplificado: Recarga inmediata
        LoadHistoryCommand.Execute(null);
    }

    [RelayCommand]
    private void ActivateItem(ClipboardItemViewModel itemViewModel)
    {
        if (itemViewModel?.Item == null) return;

        // Lógica para poner el item de vuelta en el portapapeles del sistema
        // Esto requiere acceso al Clipboard de WPF/Windows.
        // Podría estar en un servicio de UI o aquí temporalmente.
        try
        {
            // Crear DataObject basado en itemViewModel.Item.ContentType
            var dataObject = new DataObject();
            switch (itemViewModel.Item.ContentType)
            {
                case Core.Enums.ContentType.Text:
                case Core.Enums.ContentType.Link:
                case Core.Enums.ContentType.Color:
                    if (itemViewModel.Item.Data != null)
                    {
                        dataObject.SetText(itemViewModel.Item.Data);
                    }
                    break;
                case Core.Enums.ContentType.Image:
                    if (itemViewModel.Item.BinaryData != null)
                    {
                        // Convertir byte[] a BitmapSource
                        // var image = ByteArrayToBitmapSource(itemViewModel.Item.BinaryData);
                        // if (image != null) dataObject.SetImage(image);
                        // TODO: Implementar conversión y manejo de errores
                    }
                    break;
                 // case Core.Enums.ContentType.Files:
                    // TODO: Manejar lista de archivos (StringCollection con FileDrop)
                 //   break;
            }

            if (dataObject.GetFormats().Length > 0)
            {
                Clipboard.SetDataObject(dataObject, true);
                // Opcional: Cerrar la ventana del gestor
                // RequestClose?.Invoke();
            }
        }
        catch (Exception ex)
        {
            // Log error
            Console.WriteLine($"Error activating clipboard item: {ex.Message}");
        }
    }

    // TODO: Añadir comandos para Filtrar, Anclar (delegar a ClipboardItemViewModel), Borrar, Abrir Settings, etc.
}