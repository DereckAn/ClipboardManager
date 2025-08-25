using Clipboard.Services;
using Clipboard.ViewModels;
using Clipboard.Views.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
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
                // Mostrar y traer al frente
                this.AppWindow.Show();
                this.Activate();
            }
        }

        private void ConfigureWindowForPopup()
        {
            // Tamaño compacto para popup
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 800, Height = 600 });

            // Centrar en pantalla
            var displayArea = Microsoft.UI.Windowing.DisplayArea.Primary;
            var centerX = (displayArea.WorkArea.Width - 800) / 2;
            var centerY = (displayArea.WorkArea.Height - 600) / 2;

            this.AppWindow.Move(new Windows.Graphics.PointInt32 { X = centerX, Y = centerY });
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
                this.AppWindow.Show();
                this.Activate();
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