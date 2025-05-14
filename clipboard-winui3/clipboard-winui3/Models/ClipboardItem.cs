// Models/ClipboardItem.cs
using System; // Añadido por si acaso

namespace clipboard_winui3.Models // Asegúrate que el namespace es correcto
{
    public class ClipboardItem
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        // 'required' es una característica de C# 11. Si tu proyecto WinUI 3 usa una versión anterior de C#,
        // quita 'required' y asegúrate de inicializar DataType en el constructor o al crear instancias.
        // Por simplicidad, lo quitaré por ahora, asumiendo C# 10 o inferior es más común en plantillas WinUI 3 iniciales.
        public string DataType { get; set; } = string.Empty; // Inicializar para evitar nulls si no es 'required'
        public string? TextContent { get; set; }
        public byte[]? ImageData { get; set; }
    }
}