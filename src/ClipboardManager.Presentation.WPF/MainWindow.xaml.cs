using System.Windows;
using System.Windows.Input; // Para KeyEventArgs

namespace ClipboardManager.Presentation.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // El DataContext se establece en App.xaml.cs a través de DI
    }

     // Ocultar la ventana si pierde el foco (comportamiento típico)
    private void Window_Deactivated(object sender, EventArgs e)
    {
        this.Hide();
    }

     // Cerrar la ventana con la tecla ESC
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.Hide();
        }
         // Podrías manejar Arriba/Abajo/Enter para navegar y seleccionar items aquí
         // O mejor aún, en el ViewModel con Attached Behaviors o InputBindings
    }

     // Para asegurar que el HwndSource esté listo si el monitor lo necesita
     private void Window_Loaded(object sender, RoutedEventArgs e)
     {
         // El monitor de portapapeles ya debería estar iniciado desde App.xaml.cs
         // pero esto asegura que el handle de la ventana existe.
         // Si el monitor se inicia aquí, habría que mover la lógica de App.xaml.cs
     }
}