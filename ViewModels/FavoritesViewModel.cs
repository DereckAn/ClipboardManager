using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Clipboard.ViewModels
{
    public partial class FavoritesViewModel : ObservableObject
    {
        private readonly MainWindowViewModel _mainViewModel;

        // Campo para elemento seleccionado en favoritos
        [ObservableProperty]
        private ClipboardItemViewModel? _selectedItem;

        // Colección filtrada de solo favoritos
        public ObservableCollection<ClipboardItemViewModel> FilteredFavorites { get; }

        // Propiedades delegadas al MainWindowViewModel (estado global)
        public string SearchText
        {
            get => _mainViewModel.SearchText;
            set => _mainViewModel.SearchText = value;
        }

        public string SelectedCategory
        {
            get => _mainViewModel.SelectedCategory;
            set => _mainViewModel.SelectedCategory = value;
        }

        public ObservableCollection<string> Categories => _mainViewModel.Categories;

        // Propiedades de visibilidad
        public Visibility ShowEmptyStateVisibility => SelectedItem == null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowSelectedItemVisibility => SelectedItem != null ?Visibility.Visible : Visibility.Collapsed;

        public FavoritesViewModel(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            FilteredFavorites = new ObservableCollection<ClipboardItemViewModel>();

            // Suscribirse a cambios
            PropertyChanged += OnPropertyChanged;
            _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;

            // Inicializar favoritos
            RefreshFavorites();
        }

        // Refrescar favoritos cuando cambia el filtro global
        private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_mainViewModel.SearchText) ||
                e.PropertyName == nameof(_mainViewModel.SelectedCategory))
            {
                RefreshFavorites();
            }
        }

        // Método para refrescar favoritos desde FilteredItems global
        public void RefreshFavorites()
        {
            FilteredFavorites.Clear();

            // Solo elementos favoritos de la lista ya filtrada globalmente
            var favorites = _mainViewModel.FilteredItems.Where(item => item.IsFavorite);

            foreach (var favorite in favorites)
            {
                FilteredFavorites.Add(favorite);
            }
        }

        // Manejo de cambios de propiedades locales
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedItem))
            {
                OnPropertyChanged(nameof(ShowEmptyStateVisibility));
                OnPropertyChanged(nameof(ShowSelectedItemVisibility));

                // Auto-copiar al seleccionar
                if (SelectedItem != null)
                {
                    _ = _mainViewModel.CopyToClipboardAsync(SelectedItem);
                }
            }
        }

        // Comandos que delegan al ViewModel principal
        [RelayCommand]
        private async Task CopyToClipboardAsync(ClipboardItemViewModel? item)
        {
            if (item != null)
            {
                await _mainViewModel.CopyToClipboardAsync(item);
            }
        }

        [RelayCommand]
        private async Task ToggleFavoriteAsync(ClipboardItemViewModel? item)
        {
            if (item != null)
            {
                await _mainViewModel.ToggleFavoriteAsync(item);
                RefreshFavorites(); // Refrescar después de cambiar favorito
            }
        }

        [RelayCommand]
        private async Task DeleteItemAsync(ClipboardItemViewModel? item)
        {
            if (item != null)
            {
                await _mainViewModel.DeleteItemAsync(item);
                RefreshFavorites(); // Refrescar después de eliminar
            }
        }
    }
}