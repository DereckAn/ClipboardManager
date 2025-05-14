using clipboard_winui3.Services;
using clipboard_winui3.ViewModels;
using clipboard_winui3.Views;
using Microsoft.UI.Xaml;

namespace clipboard_winui3
{
    public partial class App : Application
    {
        private Window? m_window;
        private MainViewModel? _mainViewModel;
        private WindowService? _windowService;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // 1. Crear Servicios (si aún no existen como singletons)
            _windowService = new WindowService();

            // 2. Crear ViewModel principal
            _mainViewModel = new MainViewModel();

            // 3. Crear Ventana Principal
            m_window = new MainWindow();

            // 4. Asignar DataContext
            if (m_window is MainWindow mainWin) // Hacemos un cast seguro
            {
                mainWin.DataContext = _mainViewModel;
            }

            // 5. Inicializar y configurar la ventana a través del servicio
            // Es importante hacerlo ANTES de Activate() para que los cambios de tamaño/posición
            // y estilo se apliquen antes de que la ventana sea visible por primera vez.
            _windowService.InitializeMainWindow(m_window);

            // 6. Activar la ventana (esto la muestra)
            m_window.Activate();
            // _windowService.ShowWindow(); // Alternativamente, si centralizas todo en el servicio
        }
    }
}