using clipboard_winui3.ViewModels;
using Microsoft.UI.Xaml;

namespace clipboard_winui3.Views
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            // No llames a AppWindow configuraciones aqu� directamente.
            // Se har�n desde App.xaml.cs a trav�s del WindowService para que
            // el ViewModel est� listo y el DataContext asignado.
        }

        internal MainViewModel DataContext { get; set; }
    }
}