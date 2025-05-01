using ClipboardManager.Application.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClipboardManager.Presentation.WPF.Views;

public partial class ClipboardItemCard : UserControl
{
    public ClipboardItemCard()
    {
        InitializeComponent();
    }

    // Manejar click para activar el item (llamar al comando del MainViewModel)
    private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ClipboardItemViewModel itemVm)
        {
            // Necesitamos acceder al comando ActivateItem del MainViewModel.
            // Esto es un poco complicado desde aquí. Alternativas:
            // 1. Pasar el MainViewModel o su comando Activate al ItemViewModel.
            // 2. Usar un sistema de mensajería (Mediator pattern) para enviar un mensaje "ActivateItemRequested".
            // 3. Buscar el MainViewModel desde el árbol visual (menos ideal).

            // Solución temporal (menos ideal): Buscar el DataContext de la ventana padre
            var mainWindow = Window.GetWindow(this);
            if (mainWindow?.DataContext is MainViewModel mainVm)
            {
                 // Asegurarse que el comando puede ejecutarse
                if (mainVm.ActivateItemCommand.CanExecute(itemVm))
                {
                    mainVm.ActivateItemCommand.Execute(itemVm);
                }
            }
        }
    }
}