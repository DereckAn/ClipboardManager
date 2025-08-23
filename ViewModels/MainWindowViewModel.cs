using Clipboard.Models;
using Clipboard.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace Clipboard.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IClipboardService _clipboardService;
        private readonly ClipboardDbContext _dbContext;

        // Campo para el texto de busqueda
        [ObservableProperty]
        private string _searchText = string.Empty;

        // Campo para el elemento seleccionado
        [ObservableProperty]
        private ClipboardItemViewModel? _selectedItem;

        // Coleccion observable de elementos del portapapeles
        public ObservableCollection<ClipboardItemViewModel> ClipboardItems { get; }

        // Coleccion filtrada (la que se muestra en la UI
        public ObservableCollection<ClipboardItemViewModel> FilteredItems { get; }

        public MainWindowViewModel(IClipboardService clipboardService, ClipboardDbContext dbContext)
        {
            _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            //_clipboardService = clipboardService; // Permitir null temporalmente
            //_dbContext = dbContext; // Permitir null temporalmente

            ClipboardItems = new ObservableCollection<ClipboardItemViewModel>();
            FilteredItems = new ObservableCollection<ClipboardItemViewModel>();

            // Suscribirse a cambios en el texto de busqueda
            PropertyChanged += OnPropertyChanged;

            // Nuevo Inicializar automaticamente al crear el viewmodel
            _ = InitializeAsync();
        }

        // Metodo que se ejecuta cuando cambia cualquier propiedad
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchText))
            {
                FilterItems();
            }
        }

        // Comando para inicializar la aplicación 
        [RelayCommand]
        private async Task InitializeAsync()
        {
            await LoadClipboardItemAsync();
            _clipboardService.ClipboardChanged += OnClipboardChanged;
        }

        // Comando para cargar elementos desde la base de datos
        private async Task LoadClipboardItemAsync()
        {
            var items = await _dbContext.ClipboardItems
                .Include(x => x.ClipboardType)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            ClipboardItems.Clear();

            foreach (var item in items)
            {
                ClipboardItems.Add(new ClipboardItemViewModel(item));
            }

            FilterItems();
        }

        // Metodo que maneja cambios en el portapapeles 
        private async void OnClipboardChanged(object? sender, ClipboardItem newItem)
        {
            // Create el ViewModel del nuevo elemento
            var viewModel = new ClipboardItemViewModel(newItem);

            // Agregar al inicio de la coleccion (mas reciente primero)
            ClipboardItems.Insert(0, viewModel);

            // Aplicar filtro
            FilterItems();
        }

        // Metodo para filtrar elementos
        private void FilterItems()
        {
            FilteredItems.Clear();
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? ClipboardItems
                : ClipboardItems.Where(item =>
                    item.Content.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (item.Preview?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));

            foreach (var item in filtered)
            {
                FilteredItems.Add(item);
            }
        }

        // Comando para copiar un elemento de vuelta al portapapeles
        [RelayCommand]
        private async Task CopyToClipboardAsync(ClipboardItemViewModel? item)
        {
            if (item == null) return;

            try
            {
                await _clipboardService.SetClipboardContentAsync(item.Content);
            }
            catch (Exception ex)
            {
                // TODO: Manejar error (mostrar mensaje al usuario, log, etc.)
                System.Diagnostics.Debug.WriteLine($"Error copiando al portapapeles: {ex.Message}");
            }
        }

        // Comando para alternar favorito
        [RelayCommand]
        private async Task ToggleFavoriteAsync(ClipboardItemViewModel? item)
        {
            if (item == null) return;

            item.IsFavorite = !item.IsFavorite;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Revertir el cambio si fallo
                item.IsFavorite = !item.IsFavorite;
                System.Diagnostics.Debug.WriteLine($"Error actualizando favorito: {ex.Message}");
            }
        }


        // Comando para eliminar un elemento
        [RelayCommand]
        private async Task DeleteItemAsync(ClipboardItemViewModel? item)
        {
            if (item == null) return;

            try
            {
                var modelToDelete = await _dbContext.ClipboardItems.FindAsync(item.Id);
                if (modelToDelete != null)
                {
                    _dbContext.ClipboardItems.Remove(modelToDelete);
                    await _dbContext.SaveChangesAsync();

                    ClipboardItems.Remove(item);
                    FilterItems();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting item: {ex.Message}");
            }
        }

    }
}


/*
 * [ObservableProperty] (líneas 24, 28)

  [ObservableProperty]
  private string _searchText = string.Empty;

  ¿Qué hace? Es un atributo mágico que automáticamente genera:

  // El generador crea esto por ti:
  public string SearchText
  {
      get => _searchText;
      set => SetProperty(ref _searchText, value);
  }

  ¿Por qué es útil? Te ahorra escribir código repetitivo para propiedades que notifican cambios.

  [RelayCommand] (líneas 59, 109, 126, 147)

  [RelayCommand]
  private async Task InitializeAsync()

  ¿Qué hace? Automáticamente crea un ICommand llamado InitializeCommand que la UI puede usar.

  Equivale a escribir manualmente:
  public ICommand InitializeCommand => new AsyncRelayCommand(InitializeAsync);

  ObservableCollection (líneas 32, 35)

  public ObservableCollection<ClipboardItemViewModel> ClipboardItems { get; }

  ¿Qué es? Una lista que automáticamente notifica a la UI cuando:
  - Agregas elementos (Add)
  - Quitas elementos (Remove)
  - Cambias elementos (Insert, Clear)

  ¿Por qué no List<T>? Porque List<T> no notifica cambios - la UI no se actualizaría.

  Dependency Injection en el constructor

  public MainWindowViewModel(IClipboardService clipboardService, ClipboardDbContext dbContext)

  ¿Qué significa? El sistema automáticamente "inyecta" las dependencias cuando creas el
  ViewModel. No tienes que crear manualmente los servicios.

  Event Subscription (línea 46)

  PropertyChanged += OnPropertyChanged;

  ¿Qué hace? Se "suscribe" al evento PropertyChanged. Cada vez que cambia una propiedad (como
  SearchText), se ejecuta OnPropertyChanged.
 */