using ClipboardManager.Application.Interfaces;
using ClipboardManager.Core.Entities;
using ClipboardManager.Core.Enums;
using System.Runtime.InteropServices;
using System.Windows; // Necesario para Clipboard y formatos
using System.Windows.Interop; // Para HwndSource

namespace ClipboardManager.Infrastructure.Services;

// NOTA: Esta implementación es compleja y requiere manejo cuidadoso de P/Invoke y Threads.
// Esto es un ESQUELETO MUY SIMPLIFICADO.
public class WindowsClipboardMonitorService : IClipboardMonitorService, IDisposable
{
    public event Action<ClipboardItem>? ClipboardChanged;

    private HwndSource? _hwndSource; // Necesario para recibir mensajes de ventana
    private bool _isMonitoring = false;
    private readonly IClipboardHistoryRepository _repository; // Para guardar items

    // P/Invoke firmas (simplificadas)
    private const int WM_CLIPBOARDUPDATE = 0x031D;
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow(); // Para SourceApplication (opcional)
    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count); // Para SourceApplication

    public WindowsClipboardMonitorService(IClipboardHistoryRepository repository)
    {
        _repository = repository;
        // Necesitamos un HWND. La forma más limpia en WPF es crear una ventana invisible
        // o usar el HWND de la ventana principal si ya está creada al iniciar el monitor.
        // Aquí usaremos un HwndSource genérico (requiere que se llame desde un hilo STA).
    }

    public void StartMonitoring()
    {
        if (_isMonitoring) return;

        // Asegurarse de que esto se ejecuta en el hilo de UI o uno con MessageLoop (STA)
        Application.Current.Dispatcher.Invoke(() => {
            if (_hwndSource == null)
            {
                // Necesitamos obtener el HWND de la ventana principal o crear uno invisible
                 // Obtener handle de la ventana principal (si existe)
                Window? mainWindow = Application.Current.MainWindow;
                IntPtr windowHandle = IntPtr.Zero;
                if (mainWindow != null) {
                     windowHandle = new WindowInteropHelper(mainWindow).EnsureHandle();
                }

                // Si no hay ventana principal o handle, crear un HwndSource temporal (menos ideal)
                if (windowHandle == IntPtr.Zero) {
                    _hwndSource = new HwndSource(0, 0, 0, 0, 0, "ClipboardMonitor", IntPtr.Zero);
                     windowHandle = _hwndSource.Handle;
                } else {
                     _hwndSource = HwndSource.FromHwnd(windowHandle);
                }


                if (windowHandle != IntPtr.Zero) {
                     _hwndSource?.AddHook(WndProc);
                     if (!AddClipboardFormatListener(windowHandle)) {
                         // Error al añadir listener
                         Console.WriteLine("Failed to add clipboard listener. Error code: " + Marshal.GetLastWin32Error());
                         _hwndSource?.RemoveHook(WndProc);
                         _hwndSource?.Dispose();
                         _hwndSource = null;
                         return; // No se pudo iniciar
                     }
                      _isMonitoring = true;
                      Console.WriteLine("Clipboard monitoring started.");
                 } else {
                     Console.WriteLine("Failed to get/create window handle for clipboard monitoring.");
                     return; // No se pudo iniciar
                 }

            }

        });


    }

     public void StopMonitoring()
    {
         if (!_isMonitoring || _hwndSource == null || _hwndSource.IsDisposed) return;

         Application.Current.Dispatcher.Invoke(() => {
             if (_hwndSource != null && !_hwndSource.IsDisposed) {
                RemoveClipboardFormatListener(_hwndSource.Handle);
                _hwndSource.RemoveHook(WndProc);
                 // No disponer del HwndSource si pertenece a la ventana principal
                 // _hwndSource.Dispose(); // Sólo si lo creamos nosotros específicamente
                _hwndSource = null; // Permitir recrearlo si se reinicia
             }
            _isMonitoring = false;
             Console.WriteLine("Clipboard monitoring stopped.");
         });
    }


    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            ProcessClipboardChange();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private async void ProcessClipboardChange()
    {
        Console.WriteLine("Clipboard changed detected!");
        // IMPORTANTE: Acceder al portapapeles puede lanzar excepciones si está bloqueado.
        // Usar reintentos o manejo robusto de excepciones.
        try
        {
            // Ejecutar en hilo STA si no estamos seguros del contexto actual
             await Application.Current.Dispatcher.InvokeAsync(async () =>
             {
                 if (Clipboard.ContainsText())
                 {
                     await HandleTextDataAsync();
                 }
                 else if (Clipboard.ContainsImage())
                 {
                     await HandleImageDataAsync();
                 }
                 else if (Clipboard.ContainsFileDropList())
                 {
                     await HandleFileDataAsync();
                 }
                 // Añadir más checks para otros formatos (HTML, etc.)
                 // Detectar colores (requiere analizar el texto)
             });

        }
        catch (Exception ex)
        {
            // Log error (ej. portapapeles ocupado)
            Console.WriteLine($"Error accessing clipboard: {ex.Message}");
        }
    }

    private async Task HandleTextDataAsync()
    {
        string text = Clipboard.GetText();
        if (string.IsNullOrWhiteSpace(text)) return;

        // TODO: Detectar si es Link o Color aquí
        ContentType type = ContentType.Text;
        if (Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
             type = ContentType.Link;
        }
        // else if (IsColorCode(text)) { type = ContentType.Color; } // Implementar IsColorCode

        var newItem = new ClipboardItem
        {
            ContentType = type,
            Data = text,
            Preview = text.Length > 100 ? text.Substring(0, 100) + "..." : text,
            Timestamp = DateTimeOffset.Now,
            SourceApplication = GetSourceApplicationName() // Opcional
        };

        await SaveAndNotifyAsync(newItem);
    }

    private async Task HandleImageDataAsync()
    {
         var imageSource = Clipboard.GetImage();
         if (imageSource == null) return;

         // Convertir BitmapSource a byte[] (PNG es un buen formato sin pérdida)
         byte[]? imageData = null;
         // PngBitmapEncoder encoder = new PngBitmapEncoder();
         // encoder.Frames.Add(BitmapFrame.Create(imageSource));
         // using (MemoryStream ms = new MemoryStream())
         // {
         //     encoder.Save(ms);
         //     imageData = ms.ToArray();
         // }
         // TODO: Implementar conversión real

         if (imageData != null)
         {
             var newItem = new ClipboardItem
             {
                 ContentType = ContentType.Image,
                 BinaryData = imageData,
                 Preview = "Image data", // Podría ser dimensiones o tamaño
                 Timestamp = DateTimeOffset.Now,
                 SourceApplication = GetSourceApplicationName()
             };
             await SaveAndNotifyAsync(newItem);
         }
    }

    private async Task HandleFileDataAsync()
    {
        var fileList = Clipboard.GetFileDropList();
        if (fileList == null || fileList.Count == 0) return;

        string data = string.Join("\n", fileList); // Guardar lista como texto por ahora
        var newItem = new ClipboardItem
        {
            ContentType = ContentType.Files,
            Data = data, // O guardar como JSON o similar si se necesita estructura
            Preview = $"{fileList.Count} file(s)/folder(s): {Path.GetFileName(fileList[0])}" + (fileList.Count > 1 ? "..." : ""),
            Timestamp = DateTimeOffset.Now,
            SourceApplication = GetSourceApplicationName()
        };
        await SaveAndNotifyAsync(newItem);
    }


    private async Task SaveAndNotifyAsync(ClipboardItem item)
    {
        // Opcional: Comprobar si es idéntico al último item para evitar duplicados rápidos
        // var lastItem = await _repository.GetItemsAsync(limit: 1);
        // if (lastItem.FirstOrDefault()?.Data == item.Data && lastItem.FirstOrDefault()?.BinaryData == item.BinaryData) return;

        await _repository.AddItemAsync(item);
        Console.WriteLine($"Saved new {item.ContentType} item.");
        // Notificar a quien esté escuchando (ej. MainViewModel) que un nuevo item FUE GUARDADO
        // Pasar el item completo o solo su ID
        ClipboardChanged?.Invoke(item);
    }

    private string GetSourceApplicationName()
    {
        try
        {
            IntPtr handle = GetForegroundWindow();
            const int nChars = 256;
            System.Text.StringBuilder Buff = new System.Text.StringBuilder(nChars);
            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                // Opcional: Intentar obtener el nombre del proceso ejecutable
                // int processId;
                // GetWindowThreadProcessId(handle, out processId);
                // Process proc = Process.GetProcessById(processId);
                // return proc.MainModule.ModuleName;
                return Buff.ToString(); // Devuelve el título de la ventana
            }
        }
        catch { /* Ignorar errores */ }
        return "Unknown";
    }


    public void Dispose()
    {
        StopMonitoring();
        // Asegurarse que el HwndSource se dispone si lo creamos nosotros
        // _hwndSource?.Dispose(); // Cuidado si es el handle de la ventana principal
        GC.SuppressFinalize(this);
    }
}