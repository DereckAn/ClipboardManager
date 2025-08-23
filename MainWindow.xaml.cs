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

            // Crear el ViewModel (temporal, después lo haremos con DI)
            ViewModel = new MainWindowViewModel();
        }
    }
}