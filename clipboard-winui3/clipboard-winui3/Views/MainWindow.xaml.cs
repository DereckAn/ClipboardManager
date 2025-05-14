using clipboard_winui3.ViewModels;
using Microsoft.UI.Xaml;

namespace clipboard_winui3.Views
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            // No llames a AppWindow configuraciones aquí directamente.
            // Se harán desde App.xaml.cs a través del WindowService para que
            // el ViewModel esté listo y el DataContext asignado.
        }

        internal MainViewModel DataContext { get; set; }
    }
}