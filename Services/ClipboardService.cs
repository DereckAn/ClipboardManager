using Clipboard.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.UI.Xaml;

namespace Clipboard.Services
{
    public class ClipboardService : IClipboardService, IDisposable
    {

        private readonly ClipboardDbContext _context;
        private bool _isMonitoring;
        private string _lastContent = string.Empty;
        private IntPtr _windowHandle;

        // Patrones regex para detectar diferentes formatos de colores
        private readonly Dictionary<string, Regex> _colorPatterns = new()
        {
            ["HEX"] = new Regex(@"^#?([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", RegexOptions.Compiled),
            ["RGB"] = new Regex(@"^rgb\s*\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["RGBA"] = new Regex(@"^rgba\s*\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*([01]?\.?\d*)\s*\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["HSL"] = new Regex(@"^hsl\s*\(\s*(\d{1,3})\s*,\s*(\d{1,3})%\s*,\s*(\d{1,3})%\s*\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["HSLA"] = new Regex(@"^hsla\s*\(\s*(\d{1,3})\s*,\s*(\d{1,3})%\s*,\s*(\d{1,3})%\s*,\s*([01]?\.?\d*)\s*\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase)
        };

        // Constante para el mensaje de Windows de cambio de clipboard
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        public bool IsMonitoring => _isMonitoring;
        public event EventHandler<ClipboardItem>? ClipboardChanged;
        public event EventHandler<Exception>? ErrorOccurred;

        // Importaciones Win32 API para monitoreo global del clipboard
        [DllImport("user32.dll", SetLastError = true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public ClipboardService(ClipboardDbContext context)
        {
            _context = context;
        }



        public void Dispose()
        {
            _ = StopMonitoringAsync();
        }

        // Inicia el monitoreo global del portapapeles usando Win32 API
        public async Task StartMonitoringAsync(Window mainWindow)
        {
            if (_isMonitoring) return;

            try
            {
                // Obtener el handle nativo de la ventana WinUI 3
                _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

                if (_windowHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("No se pudo obtener el handle de la ventana");
                }

                // Registrar listener Win32 para monitoreo global
                if (AddClipboardFormatListener(_windowHandle))
                {
                    _isMonitoring = true;

                    // También suscribirse al evento nativo como respaldo
                    Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged +=
        OnClipboardContentChanged;
                }
                else
                {
                    throw new InvalidOperationException("No se pudo registrar el listener del clipboard");
                }
            }
            catch (Exception ex)
            {
                await NotifyErrorAsync(ex);
            }
        }

        // Detiene el monitoreo global del portapapeles
        public async Task StopMonitoringAsync()
        {
            if(!_isMonitoring) return;
            try
            {
                _isMonitoring = false;

                // Remover listener Win32
                if (_windowHandle != IntPtr.Zero)
                {
                    RemoveClipboardFormatListener(_windowHandle);
                }

                // Desuscribirse del evento nativo de respaldo
                Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged -= OnClipboardContentChanged;
            }
            catch (Exception ex)
            {
                await NotifyErrorAsync(ex);
            }
        }

        // Manejador del evento -  usa fire-and-forget controlado
        private void OnClipboardContentChanged(object? sender, object e)
        {
            // Fire-and-forget controlado para no bloquear el evento
            _ = ProcessClipboardChangeAsync().ConfigureAwait(false);
        }

        // Procesamiento real del cambio -  async Task para mejor control de errores
        private async Task ProcessClipboardChangeAsync()
        {
            if (!_isMonitoring) return;

            try
            {
                var dataPackage = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();

                // Verificar si hay texto 
                if (dataPackage.Contains(StandardDataFormats.Text))
                {
                    var text = await dataPackage.GetTextAsync();
                    if (!string.IsNullOrEmpty(text) && text != _lastContent)
                    {
                        _lastContent = text;

                        // Verificar si es un color antes de procesarlo como texto
                        if (IsColorFormat(text, out var colorType))
                        {
                            await ProcessColorContent(text, colorType);
                        }
                        else if (IsCodeSnippet(text))
                        {
                            await ProcessCodeContent(text);
                        }

                        else if (IsUrl(text))
                        {
                            await ProcessLinkContent(text);
                        }
                        else
                        {
                            await ProcessTextContent(text);
                        }
                    }      
                }

                // Verificar si hay imagenes
                else if (dataPackage.Contains(StandardDataFormats.Bitmap))
                {
                    await ProcessImageContent(dataPackage);
                }

                // Verificar si hay archivos
                else if (dataPackage.Contains(StandardDataFormats.StorageItems))
                {
                    await ProcessFileContent(dataPackage);
                }
            }
            catch (Exception ex)
            {
                await NotifyErrorAsync(ex);
            }

        }

        // Verifica si el texto es un formato de color reconocido
        private bool IsColorFormat(string text, out string colorType)
        {
            colorType = string.Empty;
            text = text.Trim();

            foreach (var pattern in _colorPatterns)
            {
                if (pattern.Value.IsMatch(text))
                {
                    colorType = pattern.Key;
                    return true;
                }
            }

            return false;
        }

        // Verifica si el texto parece ser un snippet de código
        private bool IsCodeSnippet(string text)
        {
            // Buscar patrones tipicos de codigo
            var codePatterns = new[]
            {

                  @"\b(function|class|interface|import|export|const|let|var)\b", // JavaScript/TypeScript
                  @"\b(public|private|protected|class|interface|namespace)\b", // C#/Java
                  @"<\w+[^>]*>.*</\w+>", // HTML/XML
                  @"\b(def|import|class|if __name__)\b", // Python
                  @"\{[^}]*\}", // JSON-like o bloques de código
            };

            return codePatterns.Any(pattern => Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase));
        }

        // Verifica si el texto es una URL
        private bool IsUrl(string text)
        {
            return Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        // Processa contenido de color del protapapeles
        private async Task ProcessColorContent(string colorValue, string colorType)
        {
            var clipboardType = await _context.ClipboardTypes
                .FirstOrDefaultAsync(ct => ct.Name == "Color");

            if (clipboardType == null) return;

            var clipboardItem = new ClipboardItem
            {
                Content = colorValue.Trim(),
                ClipboardTypeId = clipboardType.Id,
                CreatedAt = DateTime.UtcNow,
                Preview = $"{colorType} Color: {colorValue.Trim()}",
                Size = System.Text.Encoding.UTF8.GetByteCount(colorValue),
                Format = $"color/{colorType.ToLower()}"
            };

            _context.ClipboardItems.Add(clipboardItem);
            await _context.SaveChangesAsync();

            ClipboardChanged?.Invoke(this, clipboardItem);
        }

        // Procesa contenido de codigo del portapapeles
        private async Task ProcessCodeContent(string code)
        {
            var clipboardType = await _context.ClipboardTypes
                .FirstOrDefaultAsync(ct => ct.Name == "Code");

            if(clipboardType == null) return;

            var clipboardItem = new ClipboardItem
            {
                Content = code,
                ClipboardTypeId = clipboardType.Id,
                CreatedAt = DateTime.UtcNow,
                Preview = code.Length > 100 ? code.Substring(0, 100) + "..." : code,
                Size = System.Text.Encoding.UTF8.GetByteCount(code),
                Format = "text/code"
            };

            _context.ClipboardItems.Add(clipboardItem);
            await _context.SaveChangesAsync();
            ClipboardChanged?.Invoke(this, clipboardItem);
        }

        // Procesa contenido de link del portapapeles
        private async Task ProcessLinkContent(string url)
        {
            var clipboardType = await _context.ClipboardTypes
                .FirstOrDefaultAsync(ct => ct.Name == "Link");

            if (clipboardType == null) return;

            var clipboardItem = new ClipboardItem
            {
                Content = url,
                ClipboardTypeId = clipboardType.Id,
                CreatedAt = DateTime.UtcNow,
                Preview = $"Link: {url}",
                Size = System.Text.Encoding.UTF8.GetByteCount(url),
                Format = "text/link"
            };

            _context.ClipboardItems.Add(clipboardItem);
            await _context.SaveChangesAsync();

            ClipboardChanged?.Invoke(this, clipboardItem);
        }

        // Procesa contyenido de texto del portapapeles
        private async Task ProcessTextContent(string content)
        {
            var clipboardType = await _context.ClipboardTypes
                .FirstOrDefaultAsync(ct => ct.Name == "Text");

            if (clipboardType == null) return;

            var clipboardItem = new ClipboardItem
            {
                Content = content,
                ClipboardTypeId = clipboardType.Id,
                CreatedAt = DateTime.UtcNow,
                Preview = content.Length > 100 ? content.Substring(0, 100) + "..." : content,
                Size = System.Text.Encoding.UTF8.GetByteCount(content),
                Format = "text/plain"
            };

            _context.ClipboardItems.Add(clipboardItem);
            await _context.SaveChangesAsync();

            ClipboardChanged?.Invoke(this, clipboardItem);
        }

        // Procesa contenido de imagen del portapapeles
        private async Task ProcessImageContent(DataPackageView dataPackage)
        {
           try
            {
                var bitmap = await dataPackage.GetBitmapAsync();
                var stream = await bitmap.OpenReadAsync();

                var bytes = new byte[stream.Size];
                await stream.AsStreamForRead().ReadAsync(bytes, 0, (int)stream.Size);

                var clipboardType = await _context.ClipboardTypes
                    .FirstOrDefaultAsync(ct => ct.Name == "Image");

                if (clipboardType == null) return;

                var clipboardItem = new ClipboardItem
                {
                    Content = "[Image]",
                    BinaryData = bytes,
                    ClipboardTypeId = clipboardType.Id,
                    CreatedAt = DateTime.UtcNow,
                    Preview = $"Image ({FormatFileSize(bytes.Length)})",
                    Size = bytes.Length,
                    Format = "image/bmp",
                };

                _context.ClipboardItems.Add(clipboardItem);
                await _context.SaveChangesAsync();

                ClipboardChanged?.Invoke(this, clipboardItem);
            }
            catch (Exception ex)
            {
                await NotifyErrorAsync(ex);
            }
        }

        // Procesa contenido de archivo del portapapeles
        private async Task ProcessFileContent(DataPackageView dataPackage)
        {
            try
            {
                var storageItems = await dataPackage.GetStorageItemsAsync();

                if (storageItems.Any())
                {
                    var fileNames = string.Join(", ", storageItems.Select(item => item.Name));

                    var clipboardType = await _context.ClipboardTypes
                        .FirstOrDefaultAsync(ct => ct.Name == "File");

                    if (clipboardType == null) return;

                    var clipboardItem = new ClipboardItem
                    {
                        Content = fileNames,
                        ClipboardTypeId = clipboardType.Id,
                        CreatedAt = DateTime.UtcNow,
                        Preview = $"Files ({storageItems.Count}): {fileNames}",
                        Size = fileNames.Length,
                        Format = "files"
                    };

                    _context.ClipboardItems.Add(clipboardItem);
                    await _context.SaveChangesAsync();

                    ClipboardChanged?.Invoke(this, clipboardItem);
                }
            }
            catch (Exception ex)
            {
                await NotifyErrorAsync(ex);
            }
        }

        // Notifica errores al UI thread de manera segura 
        private async Task NotifyErrorAsync(Exception ex)
        {
            try
            {
                // Usar el DispatcherQueue de la ventana si está disponible
                if (_windowHandle != IntPtr.Zero)
                {
                    // Intentar obtener el DispatcherQueue desde cualquier ventana activa
                    var dispatcherQueue =
        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

                    if (dispatcherQueue != null)
                    {
                        dispatcherQueue.TryEnqueue(() =>
                        {
                            ErrorOccurred?.Invoke(this, ex);
                        });
                        return;
                    }
                }

                // Fallback: disparar el evento directamente
                ErrorOccurred?.Invoke(this, ex);
            }
            catch
            {
                // Último recurso: log del sistema
                System.Diagnostics.Debug.WriteLine($"Critical error in clipboard service:{ ex.Message}");
            }
        }


        // Formatea tamaños de archivo en una cadena legible
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1}{suffixes[counter]}";
        }

        // Método para establecer contenido en el portapapeles
        public async Task SetClipboardContentAsync(string content)
        {
            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(content);

                // Temporalmente deshabilitar monitoreo para evitar bucle infinito
                var wasMonitoring = _isMonitoring;
                _isMonitoring = false;

                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

                // Pequeña pausa para asegurar que el contenido se establezca
                await Task.Delay(100);

                // Restaurar monitoreo
                _isMonitoring = wasMonitoring;
            }
            catch (Exception ex)
            {
                await NotifyErrorAsync(ex);
            }
        }
    }
}


/*
 *  ¿Qué hace ClipboardService.cs?

  El ClipboardService es el cerebro de tu aplicación - es quien "vigila" y procesa todo lo que
  copias en Windows. Te explico sus funciones principales:

  🔍 1. Monitoreo Global del Portapapeles

  // Usa Win32 API para "escuchar" cambios globalmente
  AddClipboardFormatListener(_windowHandle)
  ¿Qué hace? Se registra con Windows para recibir notificaciones cada vez que cualquier 
  aplicación cambia el portapapeles (Ctrl+C, copiar, etc.).

  🤖 2. Detección Inteligente de Tipos

  Cuando detecta algo nuevo en el portapapeles, automáticamente identifica qué tipo es:

  - 🎨 Color: #FF5733, rgb(255,87,51), hsl(120,50%,75%)
  - 💻 Código: function test() {}, public class MyClass
  - 🔗 Link: https://example.com
  - 📝 Texto: Cualquier texto normal
  - 🖼️ Imagen: Capturas de pantalla, imágenes copiadas
  - 📁 Archivos: Cuando copias archivos en explorador

  💾 3. Almacenamiento Automático

  Para cada elemento:
  - Lo guarda en la base de datos SQLite
  - Crea un preview (resumen corto)
  - Calcula el tamaño
  - Asigna el tipo correcto
  - Guarda metadatos (formato, fecha, etc.)

  📢 4. Notificaciones

  ClipboardChanged?.Invoke(this, clipboardItem);
  ¿Qué hace? Notifica a tu MainWindowViewModel que hay un nuevo elemento para que actualice la
  interfaz inmediatamente.

  ↩️ 5. Restauración al Portapapeles

  SetClipboardContentAsync(content)
  ¿Qué hace? Permite "pegar de vuelta" cualquier elemento del historial al portapapeles actual.

  ---
  🔧 Conceptos Técnicos Importantes:

  Win32 API Integration

  [DllImport("user32.dll")]
  AddClipboardFormatListener(IntPtr hwnd)
  ¿Por qué? WinUI 3 solo detecta cambios cuando tu app está activa. Con Win32 API detecta cambios
   globalmente (incluso cuando tu app está minimizada).

  Fire-and-Forget Pattern

  _ = ProcessClipboardChangeAsync().ConfigureAwait(false);
  ¿Qué significa? Procesa los cambios sin bloquear la UI. El _ significa "ignora el resultado",
  es decir, no esperamos a que termine.

  Regex Patterns

  ["HEX"] = new Regex(@"^#?([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
  ¿Para qué? Detecta automáticamente formatos de colores usando expresiones regulares (patrones
  de texto).
 */