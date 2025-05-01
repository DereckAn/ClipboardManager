using ClipboardManager.Application.Interfaces;
using ClipboardManager.Core.Entities;
using LiteDB;
using Microsoft.Extensions.Options; // Para configuración de path DB

namespace ClipboardManager.Infrastructure.Persistence;

// NOTA: Implementación básica. Añadir manejo de errores, índices, etc.
public class LiteDbClipboardHistoryRepository : IClipboardHistoryRepository, IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<ClipboardItem> _collection;
    private const string CollectionName = "clipboardHistory";

    // Inyectar configuración para la ruta de la DB si es necesario
    public LiteDbClipboardHistoryRepository(/*IOptions<DatabaseSettings> dbSettings*/)
    {
        // string dbPath = dbSettings.Value.ConnectionString ?? "clipboard_history.db";
        string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClipboardManager",
            "clipboard_history.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!); // Asegurarse que el directorio existe

        _db = new LiteDatabase(dbPath);
        _collection = _db.GetCollection<ClipboardItem>(CollectionName);

        // Crear índices para búsquedas eficientes
        _collection.EnsureIndex(x => x.Timestamp);
        _collection.EnsureIndex(x => x.ContentType);
        _collection.EnsureIndex(x => x.IsPinned);
        // Considerar Full-Text Search si LiteDB lo soporta bien o indexar 'Preview'
    }

    public Task AddItemAsync(ClipboardItem item)
    {
        item.Id = Guid.NewGuid(); // Asegurarse de que tenga un ID
        _collection.Insert(item);
        return Task.CompletedTask; // LiteDB es síncrono en su mayoría, envolver si se necesita async real
    }

    public Task<IEnumerable<ClipboardItem>> GetItemsAsync(int limit = 50, int offset = 0, string? searchQuery = null, ContentType? filterType = null)
    {
        var query = _collection.Query();

        if (filterType.HasValue)
        {
            query = query.Where(x => x.ContentType == filterType.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            // Búsqueda simple (mejorar con índices o full-text si es posible)
            query = query.Where(x => (x.Data != null && x.Data.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                                     (x.Preview != null && x.Preview.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)));
        }

        // Ordenar: Anclados primero, luego por fecha descendente
        var results = query.OrderByDescending(x => x.IsPinned)
                           .ThenByDescending(x => x.Timestamp)
                           .Skip(offset)
                           .Limit(limit)
                           .ToEnumerable();

        return Task.FromResult(results);
    }

     public Task UpdateItemAsync(ClipboardItem item)
    {
        _collection.Update(item);
        return Task.CompletedTask;
    }

    public Task PinItemAsync(Guid id, bool isPinned)
    {
        var item = _collection.FindById(id);
        if (item != null)
        {
            item.IsPinned = isPinned;
            _collection.Update(item);
        }
        return Task.CompletedTask;
    }


    public Task DeleteItemAsync(Guid id)
    {
        _collection.Delete(id);
        return Task.CompletedTask;
    }

     public Task ClearHistoryAsync(DateTimeOffset olderThan)
    {
        // Borrar items no anclados más viejos que la fecha especificada
       _collection.DeleteMany(x => !x.IsPinned && x.Timestamp < olderThan);
       return Task.CompletedTask;
    }

    public Task<ClipboardItem?> GetItemByIdAsync(Guid id)
    {
        return Task.FromResult(_collection.FindById(id));
    }

    public void Dispose()
    {
        _db?.Dispose();
        GC.SuppressFinalize(this);
    }
}