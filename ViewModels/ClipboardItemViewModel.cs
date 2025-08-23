using Clipboard.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipboard.ViewModels
{
    public partial class ClipboardItemViewModel : ObservableObject
    {
        private readonly ClipboardItem _model;

        public ClipboardItemViewModel(ClipboardItem model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        // Propiedades que exponen los datos del modelo
        public int Id => _model.Id;
        public string Content => _model.Content;
        public DateTime CreatedAt => _model.CreatedAt;
        public string? Preview => _model.Preview;
        public long Size => _model.Size;
        public string? Format => _model.Format;
        public byte[]? BinaryData => _model.BinaryData;
        public ClipboardType ClipboardType => _model.ClipboardType;


        // Propiedad observable para IsFavorite
        public bool IsFavorite
        {
            get => _model.IsFavorite;
            set
            {
                if (SetProperty(_model.IsFavorite, value, _model, (model, val) => model.IsFavorite = val))
                {
                    // ✨ NUEVO: Notificar que FavoriteIcon también cambió
                    OnPropertyChanged(nameof(FavoriteIcon));
                }
            }
        }

        // Propiedad computadas para la UI 
        public string FormattedDate => CreatedAt.ToString("dd/MM/yyyy HH:mm");
        public string FormattedSize => FormatBytes(Size);
        public string DisplayContent => string.IsNullOrEmpty(Preview) ? Content : Preview;
        public string FavoriteIcon => IsFavorite ? "⭐" : "☆";


        // Metodo auxiliar para formatear bytes
        private static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
          
            return $"{len:0.##} {sizes[order]}";
        }
    }
}

/*
 * 1. ¿Para qué sirve este archivo?

  El ClipboardItemViewModel es como un "traductor" entre tu modelo de datos (ClipboardItem) y tu
  interfaz de usuario. En MVVM:

  - Modelo = Los datos puros (ClipboardItem)
  - ViewModel = El "traductor" que prepara los datos para la UI
  - Vista = La interfaz que el usuario ve

  Este ViewModel toma un ClipboardItem de la base de datos y lo "viste" con funcionalidades
  adicionales que la UI necesita, como formatear fechas, mostrar el tamaño en KB/MB, etc.

  2. El símbolo _ (underscore)

  private readonly ClipboardItem _model;

  El _ es una convención de nomenclatura en C# para campos privados:
  - _model = campo privado (solo esta clase puede accederlo)
  - model = parámetro o variable local
  - Model = propiedad pública

  ¿Por qué usarlo?
  - Te ayuda a distinguir visualmente entre campos privados y otras variables
  - Es un estándar que todos los programadores C# entienden
  - Evita confusiones cuando tienes _model (campo) y model (parámetro)

  3. La palabra partial

  public partial class ClipboardItemViewModel : ObservableObject

  partial significa que esta clase puede estar dividida en múltiples archivos:

  // Archivo 1: ClipboardItemViewModel.cs
  public partial class ClipboardItemViewModel
  {
      // Parte de la clase aquí
  }

  // Archivo 2: ClipboardItemViewModel.Generated.cs
  public partial class ClipboardItemViewModel
  {
      // Otra parte de la clase aquí
  }

  ¿Por qué la usamos aquí?
  Porque el paquete Microsoft.Toolkit.Mvvm usa generadores de código que automáticamente crean
  comandos y propiedades en otro archivo. Es como tener un asistente que escribe código por ti.

  4. Los dos puntos : (herencia)

  public partial class ClipboardItemViewModel : ObservableObject

  Los : significan herencia. Es como decir:
  - "ClipboardItemViewModel ES UN ObservableObject"
  - "ClipboardItemViewModel hereda todas las funcionalidades de ObservableObject"

  ¿Qué nos da ObservableObject?
  - Implementa INotifyPropertyChanged automáticamente
  - Nos da el método SetProperty()
  - Hace que la UI se actualice cuando cambian las propiedades

  5. El constructor y el operador ??

  public ClipboardItemViewModel(ClipboardItem model)
  {
      _model = model ?? throw new ArgumentNullException(nameof(model));
  }

  Línea por línea:

  Constructor:

  public ClipboardItemViewModel(ClipboardItem model)
  Es como el "nacimiento" del ViewModel. Cuando creas uno nuevo, DEBES darle un ClipboardItem.

  El operador ?? (null-coalescing):

  _model = model ?? throw new ArgumentNullException(nameof(model));

  Se lee como: "Si model NO es null, asígnalo a _model. Si ES null, lanza una excepción".

  Equivalente más largo:
  if (model == null)
  {
      throw new ArgumentNullException(nameof(model));
  }
  _model = model;

  ¿Por qué hacer esto?
  - Previene errores: Si alguien pasa null, el programa falla inmediatamente con un mensaje claro
  - Es más seguro que permitir que _model sea null y que falle después

  nameof(model):

  Obtiene el nombre de la variable como string ("model"). Si cambias el nombre del parámetro,
  también se actualiza automáticamente.
 */