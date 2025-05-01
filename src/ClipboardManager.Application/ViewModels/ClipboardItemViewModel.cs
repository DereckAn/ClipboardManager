using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipboardManager.Core.Entities;
using ClipboardManager.Application.Interfaces; // Para IClipboardHistoryRepository

namespace ClipboardManager.Application.ViewModels;

public partial class ClipboardItemViewModel : ObservableObject
{
    private readonly IClipboardHistoryRepository _repository; // Para acciones como Pin/Delete

    [ObservableProperty]
    private ClipboardItem _item;

    // Propiedades para Binding en la UI (ej. icono basado en ContentType)
    public string IconGlyph => GetIconForContentType(Item.ContentType);
    public string DisplayTextPreview => GetPreviewText(Item);
    // public ImageSource? ImagePreview => GetImagePreview(Item); // Necesita converter o carga async

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PinButtonText))] // Actualiza texto del botón
    private bool _isPinned;

    public string PinButtonText => IsPinned ? "Unpin" : "Pin";

    public ClipboardItemViewModel(ClipboardItem item, IClipboardHistoryRepository repository)
    {
        _item = item;
        _repository = repository;
        _isPinned = item.IsPinned; // Inicializa desde la entidad
    }

     partial void OnIsPinnedChanged(bool value)
    {
        // Cuando la propiedad cambia (por UI o código), actualiza la entidad y persiste
        if (Item.IsPinned != value)
        {
            Item.IsPinned = value;
            // No esperar, pero manejar errores potenciales
            _ = PinItemAsync();
        }
    }

    [RelayCommand]
    private async Task PinItemAsync()
    {
       // Alterna el estado actual y lo guarda
       bool newPinnedState = !this.IsPinned; // Calcula el nuevo estado ANTES de cambiar la propiedad
       await _repository.PinItemAsync(Item.Id, newPinnedState);
       // Si la operación fue exitosa, actualiza la propiedad observable
       // Esto también actualizará el texto del botón a través de [NotifyPropertyChangedFor]
       IsPinned = newPinnedState;
    }


    [RelayCommand]
    private async Task DeleteItemAsync()
    {
        await _repository.DeleteItemAsync(Item.Id);
        // Aquí deberías notificar al MainViewModel para que elimine este item de su colección Observable
        // Esto se puede hacer con un evento, un mediador, o pasando una acción al constructor
        RequestRemove?.Invoke(this);
    }

    public event Action<ClipboardItemViewModel>? RequestRemove;


    // --- Helpers para la UI ---
    private string GetIconForContentType(Core.Enums.ContentType type)
    {
        // Devuelve un código de FontIcon (ej. Segoe MDL2 Assets) o path a un icono
        return type switch
        {
            Core.Enums.ContentType.Text => "\uE735", // TextDocument
            Core.Enums.ContentType.Image => "\uEB9F", // Picture
            Core.Enums.ContentType.Link => "\uE71B", // Link
            Core.Enums.ContentType.Color => "\uE790", // Color
            Core.Enums.ContentType.Files => "\uE8A5", // Folder
            _ => "\uE78B", // Unknown
        };
    }

     private string GetPreviewText(ClipboardItem item)
    {
        if (!string.IsNullOrEmpty(item.Preview)) return item.Preview;
        if (item.ContentType == Core.Enums.ContentType.Text && item.Data != null)
        {
            return item.Data.Length > 100 ? item.Data.Substring(0, 100) + "..." : item.Data;
        }
         if (item.ContentType == Core.Enums.ContentType.Image) return "Image";
         if (item.ContentType == Core.Enums.ContentType.Files) return "Files/Folders";
         if (item.ContentType == Core.Enums.ContentType.Link) return item.Data ?? "Link";
         if (item.ContentType == Core.Enums.ContentType.Color) return item.Data ?? "Color";
        return "Clipboard Item";
    }

    // Implementar GetImagePreview si es necesario (requiere más lógica para cargar/convertir)
}