using ClipboardManager.Core.Entities;
using ClipboardManager.Core.Enums;

namespace ClipboardManager.Application.Interfaces;

public interface IClipboardHistoryRepository
{
    Task AddItemAsync(ClipboardItem item);
    Task<IEnumerable<ClipboardItem>> GetItemsAsync(int limit = 50, int offset = 0, string? searchQuery = null, ContentType? filterType = null);
    Task UpdateItemAsync(ClipboardItem item);
    Task DeleteItemAsync(Guid id);
    Task ClearHistoryAsync(DateTimeOffset olderThan); // Para la retención
    Task<ClipboardItem?> GetItemByIdAsync(Guid id);
    Task PinItemAsync(Guid id, bool isPinned);
}