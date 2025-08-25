using Clipboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Windows.Networking.Proximity;

namespace Clipboard.Views.Controls
{
    public sealed partial class ClipboardListPanel : UserControl
    {
        public ClipboardListPanel()
        {
            this.InitializeComponent();
        }

        // ============ DEPENDENCY PROPERTIES (como props de React) ============

        /// <summary>
        /// ItemsSource - Lista de elementos a mostrar
        /// </summary>
        public ObservableCollection<ClipboardItemViewModel> ItemsSource
        {
            get
            {
                return (ObservableCollection<ClipboardItemViewModel>)GetValue(ItemsSourceProperty);
            }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<ClipboardItemViewModel>), typeof(ClipboardListPanel), new PropertyMetadata(null));

        /// <summary>
        /// SelectedItem - Elemento seleccionado
        /// </summary>
        public ClipboardItemViewModel SelectedItem
        {
            get { return (ClipboardItemViewModel)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem", typeof(ClipboardItemViewModel), typeof(ClipboardListPanel), new PropertyMetadata(null, OnSelectedItemChanged));

        /// <summary>
        /// EmptyStateIcon - Icono cuando no hay selección
        /// </summary>
        public string EmptyStateIcon
        {
            get { return (string)GetValue(EmptyStateIconProperty); }
            set { SetValue(EmptyStateIconProperty, value); }
        }
        public static readonly DependencyProperty EmptyStateIconProperty = DependencyProperty.Register("EmptyStateIcon", typeof(string), typeof(ClipboardListPanel), new PropertyMetadata("📋"));

        /// <summary>
        /// EmptyStateText - Texto cuando no hay selección
        /// </summary>
        public string EmptyStateText
        {
            get { return (string)GetValue(EmptyStateTextProperty); }
            set { SetValue(EmptyStateTextProperty, value); }
        }
        public static readonly DependencyProperty EmptyStateTextProperty = DependencyProperty.Register("EmptyStateText", typeof(string), typeof(ClipboardListPanel), new PropertyMetadata("Selecciona un elemento para ver los detalles"));

          /// <summary>
          /// CounterText - Texto del contador
          /// </summary>
        public string CounterText
        {
            get { return (string)GetValue(CounterTextProperty); }
            set { SetValue(CounterTextProperty, value); }
        }
        public static readonly DependencyProperty CounterTextProperty = DependencyProperty.Register("CounterText", typeof(string), typeof(ClipboardListPanel), new PropertyMetadata("elementos"));

        /// <summary>
        /// CopyToClipboardCommand - Comando para copiar
        /// </summary>
        public ICommand CopyToClipboardCommand
        {
            get { return (ICommand)GetValue(CopyToClipboardCommandProperty); }
            set { SetValue(CopyToClipboardCommandProperty, value); }
        }
        public static readonly DependencyProperty CopyToClipboardCommandProperty = DependencyProperty.Register("CopyToClipboardCommand", typeof(ICommand), typeof(ClipboardListPanel), new PropertyMetadata(null));

        /// <summary>
        /// ToggleFavoriteCommand - Comando para alternar favorito
        /// </summary>
        public ICommand ToggleFavoriteCommand
        {
            get { return (ICommand)GetValue(ToggleFavoriteCommandProperty); }
            set { SetValue(ToggleFavoriteCommandProperty, value); }
        }
        public static readonly DependencyProperty ToggleFavoriteCommandProperty = DependencyProperty.Register("ToggleFavoriteCommand", typeof(ICommand), typeof(ClipboardListPanel), new PropertyMetadata(null));

        /// <summary>
        /// DeleteItemCommand - Comando para eliminar
        /// </summary>
        public ICommand DeleteItemCommand
        {
            get { return (ICommand)GetValue(DeleteItemCommandProperty); }
            set { SetValue(DeleteItemCommandProperty, value); }
        }
        public static readonly DependencyProperty DeleteItemCommandProperty = DependencyProperty.Register("DeleteItemCommand", typeof(ICommand), typeof(ClipboardListPanel), new PropertyMetadata(null));

        // ============ PROPIEDADES COMPUTADAS PARA VISIBILIDAD ============

        /// <summary>
        /// EmptyStateVisibility - Visibilidad del estado vacío
        /// </summary>
        public Visibility EmptyStateVisibility
        {
            get { return SelectedItem == null ? Visibility.Visible : Visibility.Collapsed; }
        }

        /// <summary>
        /// SelectedItemVisibility - Visibilidad del elemento seleccionado
        /// </summary>
        public Visibility SelectedItemVisibility
        {
            get { return SelectedItem != null ? Visibility.Visible : Visibility.Collapsed; }
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ClipboardListPanel panel)
            {
                // Notificar que las propiedades de visibilidad han cambiado
                panel.OnPropertyChanged(nameof(EmptyStateVisibility));
                panel.OnPropertyChanged(nameof(SelectedItemVisibility));
            }
        }

        // Método helper para notificar cambios de propiedades
        private void OnPropertyChanged(string propertyName)
        {
            // Esto fuerza la actualización del binding
            this.Bindings.Update();
        }
    }
}