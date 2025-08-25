using Clipboard.Services;
using Clipboard.ViewModels;
using Clipboard.Views.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.System;

namespace Clipboard
{
    public sealed partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; }

        // Referencias a los paneles
        private HistoryPanel _historyPanel;
        private FavoritesPanel _favoritesPanel;
        private SettingsPanel _settingsPanel;

        // Win32 API para traer ventana al frente
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // Constantes Win32
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const int SW_SHOW = 5;


        public MainWindow()
        {
            this.InitializeComponent();

            // Configurar tamaño para popup
            ConfigureWindowForPopup();

            ViewModel = App.GetService<MainWindowViewModel>();

            // Inicializar paneles
            InitializePanels();

            // Mostrar historial por defecto
            ShowHistoryPanel();

            // ✨ NUEVO: Interceptar botón X para ocultar (no cerrar)
            this.Closed += OnWindowClosed;

            // Inicializar hotkeys (sin await - fire and forget)
            _ = InitializeHotkeyServiceAsync();

            // Inicializar system tray
            _ = InitializeSystemTrayAsync();
        }

        private void InitializePanels()
        {
            _historyPanel = new HistoryPanel();
            _historyPanel.ViewModel = ViewModel;

            _favoritesPanel = new FavoritesPanel();
            _favoritesPanel.ViewModel = App.GetService<FavoritesViewModel>();

            _settingsPanel = new SettingsPanel();
            _settingsPanel.ViewModel = App.GetService<SettingsViewModel>();
        }

        private void OnHistoryButtonClick(object sender, RoutedEventArgs e)
        {
            ShowHistoryPanel();
        }

        private void OnFavoritesButtonClick(object sender, RoutedEventArgs e)
        {
            ShowFavoritesPanel();
        }

        private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            ShowSettingsPanel();
        }

        private void ShowHistoryPanel()
        {
            CurrentContent.Content = _historyPanel;
            UpdateButtonStates(HistoryButton);
        }

        private void ShowFavoritesPanel()
        {
            CurrentContent.Content = _favoritesPanel;
            UpdateButtonStates(FavoritesButton);
        }

        private void ShowSettingsPanel()
        {
            CurrentContent.Content = _settingsPanel;
            UpdateButtonStates(SettingsButton);
        }

        private void UpdateButtonStates(Button activeButton)
        {
            // Reset all buttons
            HistoryButton.Background = null;
            FavoritesButton.Background = null;
            SettingsButton.Background = null;

            // Highlight active button
            activeButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.White)
            { Opacity = 0.2 };
        }

        private async Task InitializeHotkeyServiceAsync()
        {
            try
            {
                var hotkeyService = App.GetService<IGlobalHotkeyService>();

                // Inicializar con esta ventana
                await hotkeyService.InitializeAsync(this);

                // Suscribirse al evento
                hotkeyService.HotkeyPressed += OnGlobalHotkeyPressed;

                // Registrar hotkey por defecto (Ctrl+Shift+V)
                if (hotkeyService.ParseHotkeyString("Ctrl+Shift+V", out uint modifiers, out VirtualKey virtualKey))
                {
                    var success = await hotkeyService.RegisterHotkeyAsync("main-hotkey", modifiers, virtualKey);
                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Hotkey Ctrl+Shift+V registrado exitosamente");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Error registrando hotkey Ctrl+Shift+V");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inicializando hotkeys: {ex.Message}");
            }
        }

        private void OnGlobalHotkeyPressed(object? sender, string hotkeyId)
        {
            System.Diagnostics.Debug.WriteLine($"🔥 HOTKEY PRESIONADO: {hotkeyId}");

            // Si está visible, ocultarla (toggle behavior)
            if (this.Visible)
            {
                this.AppWindow.Hide();
            }
            else
            {
                // Mostrar y traer AL FRENTE de forma agresiva
                BringWindowToFront();
            }
        }

        private void ConfigureWindowForPopup()
        {
            // Configurar tamaño compacto
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 800, Height = 600 });

            // Centrar en pantalla
            var displayArea = DisplayArea.Primary;
            var centerX = (displayArea.WorkArea.Width - 800) / 2;
            var centerY = (displayArea.WorkArea.Height - 600) / 2;
            this.AppWindow.Move(new Windows.Graphics.PointInt32 { X = centerX, Y = centerY });

            // Configurar barra de título como el portapapeles de Windows
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = this.AppWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = false; // Mantener barra separada

                // Fondo oscuro semitransparente (similar al portapapeles)
                titleBar.BackgroundColor = Microsoft.UI.ColorHelper.FromArgb(128, 32, 32, 32); // #80202020
                titleBar.InactiveBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(128, 32, 32, 32);

                // Ocultar ícono y título
                titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
                this.Title = "";

                // Estilizar botón X (similar al portapapeles)
                titleBar.ButtonBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(128, 32, 32, 32); // Igual que la barra
                titleBar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 80, 80, 80); // Gris claro #FF505050
                titleBar.ButtonPressedBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 60, 60, 60); // Gris más oscuro #FF3C3C3C
                titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White; // Ícono X blanco
                titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                titleBar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 80, 80, 80);
                titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
                titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(128, 32, 32, 32);
                titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;

                // Deshabilitar botones de minimizar y maximizar
                var presenter = this.AppWindow.Presenter as OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.IsMinimizable = false;
                    presenter.IsMaximizable = false;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ Personalización de la barra de título no soportada");
                // Fallback: Configurar ventana sin bordes
                ConfigureBorderlessWindow();
            }
        }

        private void ConfigureBorderlessWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            const int GWL_STYLE = -16;
            const int WS_POPUP = unchecked((int)0x80000000);
            const int WS_VISIBLE = 0x10000000;

            int style = GetWindowLong(hWnd, GWL_STYLE);
            style = (style & ~(0x800000 | 0xC00000)) | WS_POPUP | WS_VISIBLE;
            SetWindowLong(hWnd, GWL_STYLE, style);
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | 0x0020 /* SWP_FRAMECHANGED */);
            System.Diagnostics.Debug.WriteLine("✅ Ventana configurada como sin bordes");
        }

        private async Task InitializeSystemTrayAsync()
        {
            try
            {
                var systemTrayService = App.GetService<ISystemTrayService>();

                // Inicializar con esta ventana
                systemTrayService.Initialize(this);

                // Suscribirse a eventos
                systemTrayService.TrayIconClicked += OnTrayIconClicked;
                systemTrayService.ShowMainWindowRequested += OnShowMainWindowRequested;
                systemTrayService.ExitRequested += OnExitRequested;
                systemTrayService.SettingsRequested += OnSettingsRequested;
                systemTrayService.AutoStartToggleRequested += OnAutoStartToggleRequested;

                // Mostrar icono en tray
                systemTrayService.ShowTrayIcon();

                System.Diagnostics.Debug.WriteLine("✅ System Tray inicializado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inicializando system tray: { ex.Message}");
            }
        }

        private void OnTrayIconClicked(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🔥 TRAY ICON CLICKED!");

            // Toggle ventana (igual que hotkey)
            if (this.Visible)
            {
                this.AppWindow.Hide();
            }
            else
            {
                // Usar el mismo método agresivo
                BringWindowToFront();
            }
        }

        private void OnShowMainWindowRequested(object? sender, EventArgs e)
        {
            // Mostrar ventana desde menú contextual
            this.AppWindow.Show();
            this.Activate();
        }

        private void OnExitRequested(object? sender, EventArgs e)
        {
            // Cerrar aplicación completamente
            Application.Current.Exit();
        }

        private void OnSettingsRequested(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🔥 SETTINGS REQUESTED FROM TRAY!");

            // Mostrar ventana y cambiar a pestaña de configuración
            this.AppWindow.Show();
            this.Activate();
            ShowSettingsPanel(); // Este método ya existe
        }

        private void OnAutoStartToggleRequested(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🔥 AUTO-START TOGGLE REQUESTED!");

            // TODO: Implementar toggle de auto-start
            // Por ahora solo mostrar mensaje
            System.Diagnostics.Debug.WriteLine("Auto-start toggle - pendiente de implementar");
        }

        // Método para traer ventana al frente de forma agresiva
        private void BringWindowToFront()
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // 1. Mostrar la ventana
            this.AppWindow.Show();

            // 2. Activar usando WinUI
            this.Activate();

            // 3. Técnicas Win32 agresivas
            ShowWindow(windowHandle, SW_SHOW);
            SetForegroundWindow(windowHandle);

            // 4. Temporal: Poner como topmost por un momento
            SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            // 5. Inmediatamente quitar topmost (para que no se quede siempre encima)
            SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        private void OnWindowClosed(object sender, WindowEventArgs e)
        {
            // Cancelar el cierre real
            e.Handled = true;

            // Solo ocultar la ventana (como clipboard de Windows)
            this.AppWindow.Hide();

            System.Diagnostics.Debug.WriteLine("🔥 VENTANA OCULTA (X presionado) - App sigue corriendo en tray");
  }
    }
}

/*
 * 🧩 ¿Qué es el archivo .xaml.cs?

  XAML = La "cara" (UI)

  <Button x:Name="MyButton" Content="Click me" Click="OnButtonClick"/>

  XAML.CS = El "cerebro" (Lógica)

  private void OnButtonClick(object sender, RoutedEventArgs e)
  {
      // Aquí va la lógica de qué hacer cuando hacen clic
  }

  🔗 Cómo se conectan:

  1. La clase parcial (partial)

  // MainWindow.xaml.cs
  public sealed partial class MainWindow : Window  // ← "partial"

  ¿Por qué partial?
  - El compilador genera automáticamente otra parte de la clase desde el XAML
  - Tu .xaml.cs se combina con el código generado

  2. InitializeComponent()

  public MainWindow()
  {
      this.InitializeComponent();  // ← Esto conecta XAML con C#
  }

  ¿Qué hace?
  - Lee el XAML y crea todos los controles
  - Conecta los nombres (x:Name="HistoryButton") con variables en C#
  - Registra los event handlers (Click="OnButtonClick")

  🎯 ¿Por qué creamos esos métodos?

  Problema antes (con TabView):

  <TabView>  <!-- WinUI maneja automáticamente el cambio de pestañas -->
      <TabViewItem Header="Historial">...</TabViewItem>
      <TabViewItem Header="Favoritos">...</TabViewItem>
  </TabView>

  Problema ahora (con botones personalizados):

  <Button Click="OnHistoryButtonClick"/>  <!-- NOSOTROS debemos manejar el clic -->

  🔧 Los métodos que creamos:

  1. Event Handlers (Responden a clics)

  private void OnHistoryButtonClick(object sender, RoutedEventArgs e)
  {
      ShowHistoryPanel();  // Cambiar a panel de historial
  }

  2. Lógica de navegación (Cambian el contenido)

  private void ShowHistoryPanel()
  {
      CurrentContent.Content = _historyPanel;  // Mostrar historial
      UpdateButtonStates(HistoryButton);       // Highlight botón activo
  }

  3. Gestión de estado visual (Feedback visual)

  private void UpdateButtonStates(Button activeButton)
  {
      // Todos los botones normales
      HistoryButton.Background = null;

      // El activo se ve diferente
      activeButton.Background = new SolidColorBrush(Colors.White) { Opacity = 0.2 };
  }

  🆚 Comparación con otros paradigmas:

  React (similar):

  function NavBar() {
      const [activeTab, setActiveTab] = useState('history');

      return (
          <button onClick={() => setActiveTab('history')}>📋</button>
      );
  }

  WinUI 3:

  private void OnHistoryButtonClick(object sender, RoutedEventArgs e)
  {
      ShowHistoryPanel();
  }

  🎯 ¿Podríamos evitar .xaml.cs?

  Opción 1: Commands (más MVVM)

  <Button Command="{x:Bind ViewModel.ShowHistoryCommand}"/>

  Opción 2: Code-behind (lo que hacemos ahora)

  <Button Click="OnHistoryButtonClick"/>

  💡 ¿Por qué elegimos Code-behind?

  ✅ Más simple - No necesitas Commands complejos✅ Más directo - Cambio de UI es responsabilidad
   de la ventana✅ Menos acoplamiento - El ViewModel no conoce detalles de navegación✅
  Performance - No necesita binding para navegación
 
 */