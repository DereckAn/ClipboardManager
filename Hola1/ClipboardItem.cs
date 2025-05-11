// En un nuevo archivo, ej. ClipboardItem.cs
public class ClipboardItem
{
    public int Id { get; set; } // Clave primaria
    public DateTime Timestamp { get; set; }
    public required string DataType { get; set; } // "Text", "Image", "Link", etc.
    public string? TextContent { get; set; } // Para texto, link, color
    public byte[]? ImageData { get; set; } // Para datos de imagen
    // ... otros campos que necesites ...
}