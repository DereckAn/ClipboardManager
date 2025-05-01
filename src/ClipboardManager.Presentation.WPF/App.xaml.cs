using ClipboardManager.Application.Interfaces;
using ClipboardManager.Application.ViewModels;
using ClipboardManager.Infrastructure.Persistence;
using ClipboardManager.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification; // Para Tray Icon


namespace ClipboardManager.Presentation.WPF;

public partial class App : System.Windows.Application
{
    private ServiceProvider _serviceProvider = null!;
    private TaskbarIcon? _notifyIcon;
    private IClipboardMonitorService? _monitorService; // Para detener al salir
    private IHotkeyService? _hotkeyService;         // Para desregistrar al salir

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configurar DI
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        _serviceProvider = serviceCollection.BuildServiceProvider();

        // Crear y mostrar ventana principal
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
        // No mostrarla inmediatamente, manejar visibilidad con Hotkey/TrayIcon
        // mainWindow.Show();

        // Iniciar servicios de fondo
        _monitorService = _serviceProvider.GetRequiredService<IClipboardMonitorService>();
        _monitorService.ClipboardChanged += OnClipboardContentChanged; // Suscribirse al evento
        _monitorService.StartMonitoring();

        _hotkeyService = _serviceProvider.GetRequiredService<IHotkeyService>();
        // Registrar hotkey desde configuración (ej. Ctrl+Alt+V)
        // var settings = _serviceProvider.GetRequiredService<ISettingsService>();
        // var hotkey = settings.GetActivationHotkey();
        // _hotkeyService.RegisterHotkey(hotkey);
        _hotkeyService.HotkeyActivated += OnHotkeyActivated;
        // TODO: Registrar hotkey real (requiere implementación en WindowsHotkeyService)
        Console.WriteLine("Hotkey service initialized (registration pending implementation).");


        // Configurar Icono de Bandeja
        ConfigureTrayIcon();

        // Opcional: Hacer limpieza periódica del historial según settings
        // StartHistoryCleanupTimer();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // --- Singleton Services ---
        services.AddSingleton<IClipboardHistoryRepository, LiteDbClipboardHistoryRepository>();
        services.AddSingleton<IClipboardMonitorService, WindowsClipboardMonitorService>();
        services.AddSingleton<ISettingsService, JsonSettingsService>(); // O una implementación diferente
        services.AddSingleton<IHotkeyService, WindowsHotkeyService>(); // Implementación P/Invoke

        // --- ViewModels ---
        // Transient si necesitas una instancia nueva cada vez, Singleton si debe persistir estado
        services.AddTransient<MainViewModel>();
        // services.AddTransient<SettingsViewModel>();

        // --- Views ---
        services.AddTransient<MainWindow>();
        // services.AddTransient<SettingsWindow>();

        // --- Configuración (Opcional con IOptions) ---
        // services.Configure<DatabaseSettings>(Configuration.GetSection("Database"));
        // services.Configure<AppSettings>(Configuration.GetSection("App"));
    }

     // Callback cuando el monitor detecta un cambio y GUARDA un item
     private void OnClipboardContentChanged(Core.Entities.ClipboardItem newItem)
     {
         // Necesitamos notificar al MainViewModel para que actualice la UI.
         // Usar Dispatcher si la notificación viene de otro hilo.
         Current.Dispatcher.InvokeAsync(async () => {
             var mainViewModel = _serviceProvider.GetService<MainViewModel>();
              // Podríamos pasar el ID o el item mismo.
              // Aquí asumimos que el VM tiene un método para manejarlo.
              await mainViewModel?.HandleNewClipboardItemAsync(newItem.Id);

             // Opcional: Mostrar notificación de sistema
             // _notifyIcon?.ShowBalloonTip("Clipboard Manager", $"Copied: {newItem.Preview}", BalloonIcon.Info);
         });
     }

     private void OnHotkeyActivated()
     {
         // Mostrar/Ocultar la ventana principal
         Current.Dispatcher.Invoke(() => {
              var mainWindow = _serviceProvider.GetService<MainWindow>();
              if (mainWindow != null)
              {
                  if (mainWindow.IsVisible)
                  {
                      mainWindow.Hide();
                  }
                  else
                  {
                      mainWindow.Show();
                      mainWindow.Activate(); // Traer al frente
                      // Opcional: Cargar historial reciente al mostrar
                       var vm = mainWindow.DataContext as MainViewModel;
                       vm?.LoadHistoryCommand.Execute(null);
                  }
              }
         });
     }


     private void ConfigureTrayIcon()
    {
        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        if (_notifyIcon != null)
        {
            _notifyIcon.DataContext = this; // O un ViewModel específico para el tray
            _notifyIcon.TrayMouseDoubleClick += (s, e) => OnHotkeyActivated(); // Doble click = hotkey
            // TODO: Añadir ContextMenu con opciones (Show/Hide, Settings, Exit)
            var showHideMenuItem = new System.Windows.Controls.MenuItem { Header = "Show/Hide" };
            showHideMenuItem.Click += (s, e) => OnHotkeyActivated();

            var exitMenuItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
            exitMenuItem.Click += (s, e) => Shutdown();

            _notifyIcon.ContextMenu = new System.Windows.Controls.ContextMenu();
            _notifyIcon.ContextMenu.Items.Add(showHideMenuItem);
            _notifyIcon.ContextMenu.Items.Add(new System.Windows.Controls.Separator());
            _notifyIcon.ContextMenu.Items.Add(exitMenuItem);

        } else {
             Console.WriteLine("Error: NotifyIcon resource not found.");
        }

    }


    protected override void OnExit(ExitEventArgs e)
    {
        _monitorService?.StopMonitoring();
        _hotkeyService?.Dispose(); // Asegurarse de desregistrar hotkeys
        _notifyIcon?.Dispose(); // Limpiar icono de bandeja
        (_serviceProvider as IDisposable)?.Dispose(); // Liberar contenedor DI
        base.OnExit(e);
    }
}