using System;
using Microsoft.UI.Xaml;
using Clipboard.ViewModels;

namespace Clipboard
{
    public sealed partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();


            // ANTES: Constructor vacío sin servicios
            // ViewModel = new MainWindowViewModel();

            // AHORA: ViewModel con todos los servicios inyectados
            ViewModel = App.GetService<MainWindowViewModel>();

            HistoryControl.ViewModel = ViewModel;
        }
    }
}