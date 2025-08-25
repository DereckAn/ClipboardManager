using Clipboard.Models;
using Clipboard.Services;
using Clipboard.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using Path = System.IO.Path; // ✅ Excelente solución al conflicto de nombres

namespace Clipboard
{
    public partial class App : Application
    {
        // HOST: Contenedor de servicios de la aplicación (como una caja de herramientas global)
        private static IHost? _host;
        private IClipboardService? _clipboardService;
        private Window? m_window;

        /// <summary>
        /// Propiedad que da acceso al contenedor de servicios
        /// Lazy loading: solo se crea cuando se necesita por primera vez
        /// </summary>
        public static IHost Host
        {
            get
            {
                // ?? = null coalescing: si _host es null, ejecuta CreateHost()
                _host ??= CreateHost();
                return _host;
            }
        }

        /// <summary>
        /// FÁBRICA: Crea y configura el contenedor de servicios
        /// </summary>
        private static IHost CreateHost()
        {
            return Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()  // Configuración básica de .NET
                .ConfigureLogging(logging => // Aquí configuramos el logging
                {
                    logging.ClearProviders(); // Limpiamos proveedores por defecto
                    logging.AddDebug();       // Agregamos logging a la salida de debug
                })
                .ConfigureServices(services =>  // Aquí registramos nuestros servicios
                {
                    // 📁 CONFIGURACIÓN DE LA BASE DE DATOS
                    // Path: C:\Users\[tu-usuario]\AppData\Local\ClipboardManager\clipboard.db
                    var dbPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), // %LocalAppData%
                        "ClipboardManager",     // Carpeta de nuestra app
                        "clipboard.db"          // Archivo de base de datos
                    );

                    // 🛠️ CREAR CARPETA SI NO EXISTE
                    var directory = Path.GetDirectoryName(dbPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory!); // ! = garantiza que no es null
                    }

                    // 🗄️ REGISTRAR ENTITY FRAMEWORK CON SQLITE
                    services.AddSingleton<ClipboardDbContext>(provider =>
                    {
                        var options = new DbContextOptionsBuilder<ClipboardDbContext>()
                            .UseSqlite($"Data Source={dbPath}")
                            .Options;
                        return new ClipboardDbContext(options);
                    });

                    // 📋 REGISTRAR CLIPBOARD SERVICE (NUEVO)
                    services.AddSingleton<IClipboardService, ClipboardService>();
                    services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();

                    // Registrar VIEWMODELS
                    services.AddTransient<MainWindowViewModel>();
                    services.AddTransient<ClipboardItemViewModel>();
                    services.AddTransient<FavoritesViewModel>();
                    services.AddTransient<SettingsViewModel>();
                })
                .Build(); // Construye el contenedor
        }

        /// <summary>
        /// HELPER: Método para obtener servicios desde cualquier parte de la app
        /// Ejemplo: var dbContext = App.GetService<ClipboardDbContext>();
        /// </summary>
        public static T GetService<T>() where T : class
        {
            return Host.Services.GetRequiredService<T>();
        }

        /// <summary>
        /// Constructor de la aplicación - se ejecuta al iniciar
        /// </summary>
        public App()
        {
            this.InitializeComponent(); // Carga recursos XAML
        }

        /// <summary>
        /// Se ejecuta cuando Windows lanza la aplicación
        /// </summary>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // 🚀 INICIALIZAR EL CONTENEDOR DE SERVICIOS (ya lo tienes)
            _ = Host;

            // 🗄️ ASEGURAR QUE LA BASE DE DATOS ESTÉ ACTUALIZADA (NUEVO)
            await EnsureDatabaseAsync();

            // 🪟 CREAR Y MOSTRAR VENTANA PRINCIPAL (ya lo tienes)
            m_window = new MainWindow();
            m_window.Activate();

            // 📋 INICIAR MONITOREO DEL CLIPBOARD (NUEVO)
            await StartClipboardMonitoringAsync();

            // 🧪 TESTING TEMPORAL (remover después)
            #if DEBUG
            TestClipboardService();
            #endif
        }

        private async Task StartClipboardMonitoringAsync()
        {
            try
            {
                if (m_window != null)
                {
                    var clipboardService = GetService<IClipboardService>();
                    await clipboardService.StartMonitoringAsync(m_window);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting clipboard monitoring: { ex.Message}");
            }
        }

        /// Asegura que la base de datos esté creada y actualizada
        private async Task EnsureDatabaseAsync()
        {
            try
            {
                var context = GetService<ClipboardDbContext>();
                await context.Database.MigrateAsync(); // Aplica migraciones pendientes

                System.Diagnostics.Debug.WriteLine("Base de datos inicializada correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inicializando base de datos: {ex.Message}");
            }
        }


        /// <summary>
        /// MÉTODO TEMPORAL: Probar el servicio de clipboard (remover después)
        /// </summary>
        private void TestClipboardService()
        {
            try
            {
                var clipboardService = GetService<IClipboardService>();

                // Suscribirse a eventos para testing
                clipboardService.ClipboardChanged += (sender, item) =>
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Clipboard detectado: { item.ClipboardType.Name}- { item.Preview}");
                };

                clipboardService.ErrorOccurred += (sender, ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error en clipboard: {ex.Message}");
                };

                System.Diagnostics.Debug.WriteLine("🧪 ClipboardService configurado para testing");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error configurando testing: {ex.Message}");
            }
        }


    }
}