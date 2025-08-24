using Clipboard.Models;
using Clipboard.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
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

        [ObservableProperty]
        private string _selectedCategory = "Todos";

        // Campo para el elemento seleccionado
        private ClipboardItemViewModel? _selectedItem;

        public ClipboardItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    // Notificar cambios de visibilidad
                    OnPropertyChanged(nameof(ShowEmptyStateVisibility));
                    OnPropertyChanged(nameof(ShowSelectedItemVisibility));

                    // auto copiar cuando se selecciona
                    if (value != null)
                    {
                        _ = CopyToClipboardAsync(value); // fire-and-forget
                    }
                }
            }
        }

        // Coleccion observable de elementos del portapapeles
        public ObservableCollection<ClipboardItemViewModel> ClipboardItems { get; }

        // Opciones para dropdown de categorias 
        public ObservableCollection<string> Categories { get; }

        // Coleccion filtrada (la que se muestra en la UI
        public ObservableCollection<ClipboardItemViewModel> FilteredItems { get; }

        // Registry para mantener una sola instancia por elemento
        private readonly Dictionary<int, ClipboardItemViewModel> _viewModelRegistry;

        // Control de cache
        private const int MAX_CACHED_VIEWMODELS = 1000;
        private readonly Queue<int> _cacheOrder;

        // Propiedades computadas para la visibilidad dinamica
        public Visibility ShowEmptyStateVisibility => SelectedItem == null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowSelectedItemVisibility => SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;

        public MainWindowViewModel(IClipboardService clipboardService, ClipboardDbContext dbContext)
        {
            _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            //_clipboardService = clipboardService; // Permitir null temporalmente
            //_dbContext = dbContext; // Permitir null temporalmente

            // Inicializar el registry
            _viewModelRegistry = new Dictionary<int, ClipboardItemViewModel>();

            // Inicializar control de cache
            _cacheOrder = new Queue<int>();

            ClipboardItems = new ObservableCollection<ClipboardItemViewModel>();
            FilteredItems = new ObservableCollection<ClipboardItemViewModel>();

            // Suscribirse a cambios en el texto de busqueda
            PropertyChanged += OnPropertyChanged;

            Categories = new ObservableCollection<string>
            {
                "Todos",
                "Texto",
                "Código",
                "Links",
                "Colores",
                "Imágenes"
            };

            // Nuevo Inicializar automaticamente al crear el viewmodel
            _ = InitializeAsync();
        }

        // Método para obtener o crear ViewModels únicos
        private ClipboardItemViewModel GetOrCreateViewModel(ClipboardItem item)
        {
            if (_viewModelRegistry.TryGetValue(item.Id, out var existingViewModel))
            {
                // Ya existe, moverlo al frente del cache
                MoveToFront(item.Id);
                return existingViewModel;
            }

            // No existe, crear nuevo
            var newViewModel = new ClipboardItemViewModel(item);

            // Agregar al registry
            _viewModelRegistry[item.Id] = newViewModel;
            _cacheOrder.Enqueue(item.Id);

            // Limpiar cache si es necesario
            CleanupCacheIfNeeded();

            return newViewModel;
        }

        // Limpiar cache cuando excede el límite
        private void CleanupCacheIfNeeded()
        {
            while (_viewModelRegistry.Count > MAX_CACHED_VIEWMODELS && _cacheOrder.Count > 0)
            {
                // Remover el ViewModel más antiguo
                var oldestId = _cacheOrder.Dequeue();

                // Solo remover si no es el elemento seleccionado actualmente
                if (SelectedItem?.Id != oldestId)
                {
                    _viewModelRegistry.Remove(oldestId);
                }
                else
                {
                    // Si es el seleccionado, mantenerlo pero moverlo al final
                    _cacheOrder.Enqueue(oldestId);
                    break; // Evitar loop infinito
                }
            }
        }

        // Mover un elemento al frente del cache sin reasignar la cola
        private void MoveToFront(int itemId)
        {
            // Crear lista temporal con todos los elementos excepto el que queremos mover
            var tempList = new List<int>();

            // Vaciar la cola original guardando elementos en lista temporal
            while (_cacheOrder.Count > 0)
            {
                var id = _cacheOrder.Dequeue();
                if (id != itemId) // Solo guardar los que NO son el que queremos mover
                {
                    tempList.Add(id);
                }
            }

            // Volver a llenar la cola: primero los elementos guardados, luego el movido
            foreach (var id in tempList)
            {
                _cacheOrder.Enqueue(id);
            }

            // Finalmente, agregar el elemento movido al final (más reciente)
            _cacheOrder.Enqueue(itemId);
        }

        // Metodo que se ejecuta cuando cambia cualquier propiedad
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchText) || e.PropertyName == nameof(SelectedCategory))
            {
                FilterItems();
            }

            if (e.PropertyName == nameof(SelectedItem))
            {
                OnPropertyChanged(nameof(ShowEmptyStateVisibility));
                OnPropertyChanged(nameof(ShowSelectedItemVisibility));

                // Auto copiar al portapapeles cuando se selecciona un item
                if (SelectedItem != null)
                {
                    _ = CopyToClipboardAsync(SelectedItem); // fire-and-forget
                }
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
                //ClipboardItems.Add(new ClipboardItemViewModel(item));
                ClipboardItems.Add(GetOrCreateViewModel(item));
            }

            FilterItems();
        }

        // Metodo que maneja cambios en el portapapeles 
        private async void OnClipboardChanged(object? sender, ClipboardItem newItem)
        {
            // Create el ViewModel del nuevo elemento
            //var viewModel = new ClipboardItemViewModel(newItem);
            var viewModel = GetOrCreateViewModel(newItem);



            // Agregar al inicio de la coleccion (mas reciente primero)
            ClipboardItems.Insert(0, viewModel);

            // Aplicar filtro
            FilterItems();
        }

        // Metodo para filtrar elementos
        private void FilterItems()
        {
            FilteredItems.Clear();

            var items = ClipboardItems.AsEnumerable();

            // Filtrar por categoría si no es "Todos"
            if (SelectedCategory != "Todos")
            {
                items = SelectedCategory switch
                {
                    "Texto" => items.Where(item => item.ClipboardType.Name == "Text"),
                    "Código" => items.Where(item => item.ClipboardType.Name == "Code"),
                    "Links" => items.Where(item => item.ClipboardType.Name == "Url"),
                    "Colores" => items.Where(item => item.ClipboardType.Name == "Color"),
                    "Imágenes" => items.Where(item => item.ClipboardType.Name == "Image"),
                    _ => items
                };
            }

            // Filtrar por texto de búsqueda
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                items = items.Where(item =>
                    item.Content.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (item.Preview?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            foreach (var item in items)
            {
                FilteredItems.Add(item);
            }
        }

        // Comando para copiar un elemento de vuelta al portapapeles
        [RelayCommand]
        public async Task CopyToClipboardAsync(ClipboardItemViewModel? item)
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
        public async Task ToggleFavoriteAsync(ClipboardItemViewModel? item)
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
        public async Task DeleteItemAsync(ClipboardItemViewModel? item)
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