using ClipboardManager.Core.Enums;

namespace ClipboardManager.Core.Entities;

public class ClipboardItem
{
    public Guid Id { get; set; }
    public ContentType ContentType { get; set; }
    public string? Data { get; set; } // Para texto, links, colores HEX/RGB
    public byte[]? BinaryData { get; set; } // Para imágenes u otros datos binarios
    public string? Preview { get; set; } // Texto corto o path/info para preview
    public DateTimeOffset Timestamp { get; set; }
    public bool IsPinned { get; set; }
    public string? SourceApplication { get; set; } // Opcional: De dónde se copió
}