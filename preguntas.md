¿Por qué crear un Host?

  El Host (usando Microsoft.Extensions.Hosting) te proporciona:

  1. Contenedor de Dependency Injection: Para resolver automáticamente dependencias
  2. Gestión del ciclo de vida: Inicialización y limpieza ordenada de servicios
  3. Configuración centralizada: Manejo de settings y opciones
  4. Logging integrado: Sistema de logs consistente

  ¿Qué servicios vas a manejar?

  En tu clipboard manager necesitarás estos servicios:

  - ClipboardService: Monitoreo del portapapeles del sistema
  - DatabaseService: Acceso a SQLite con Entity Framework
  - HotkeyService: Registro de teclas globales (Ctrl+Shift+V)
  - SettingsService: Configuraciones de usuario
  - SearchService: Filtrado y búsqueda de elementos

  ¿Por qué obtener servicios desde cualquier parte?
	
  Te permite:
  - Desacoplamiento: Las clases no dependen directamente unas de otras
  - Testabilidad: Puedes inyectar mocks para testing
  - Flexibilidad: Cambiar implementaciones sin modificar código dependiente
  - Singleton management: Servicios únicos (como DatabaseContext) se comparten correctamente

  ¿Es necesario registrar ViewModels y Services?

  Sí, es altamente recomendado porque:

  - ViewModels: El DI container los crea con sus dependencias automáticamente
  - Services: Garantiza una sola instancia y gestión correcta del ciclo de vida
  - Performance: Evita crear múltiples instancias innecesarias
  - Mantenibilidad: Código más limpio y predecible

  Esta es la base sólida para una aplicación profesional escalable.

---

# 🎯 RESUMEN SESIÓN: Completamos Fase 1 - Configuración Base MVVM y Base de Datos

## ¿Qué logramos hoy?

Establecimos toda la **arquitectura fundamental** del Clipboard Manager profesional siguiendo el patrón **MVVM** y configurando una base de datos **SQLite** robusta.

## 📁 Estructura MVVM Creada

```
Models/          ← Datos y lógica de negocio
Views/           ← Interfaces de usuario (XAML)
ViewModels/      ← Lógica de presentación y binding
Services/        ← Servicios de la aplicación
Converters/      ← Conversores de datos para UI
Helpers/         ← Utilidades y métodos auxiliares
```

**¿Por qué MVVM?**
- **Separación clara**: UI, lógica y datos están separados
- **Testabilidad**: Puedes probar ViewModels sin UI
- **Reutilización**: Los ViewModels pueden usarse en diferentes Views
- **Mantenibilidad**: Cambios en UI no afectan la lógica de negocio

## 🗄️ Modelos de Base de Datos

### ClipboardType.cs
```csharp
public class ClipboardType
{
    public int Id { get; set; }                    // Clave primaria
    public string Name { get; set; }               // "Text", "Image", etc.
    public string? Description { get; set; }       // Descripción del tipo
    public ICollection<ClipboardItem> Items { get; set; }  // Relación 1 a muchos
}
```

**¿Por qué separar tipos?**
- **Organización**: Agrupa elementos similares
- **Rendimiento**: Filtrado más eficiente por tipo
- **Extensibilidad**: Fácil agregar nuevos tipos
- **UI especializada**: Cada tipo puede tener su propia visualización

### ClipboardItem.cs  
```csharp
public class ClipboardItem
{
    public int Id { get; set; }                    // Clave primaria
    public string Content { get; set; }            // Contenido principal
    public DateTime CreatedAt { get; set; }        // Timestamp de creación
    public string? Preview { get; set; }           // Vista previa corta
    public long Size { get; set; }                 // Tamaño en bytes
    public int ClipboardTypeId { get; set; }       // Clave foránea
    public ClipboardType ClipboardType { get; set; } // Navegación al tipo
    public string? Format { get; set; }            // Formato específico (MIME type)
    public bool IsFavorite { get; set; }           // Marcado como favorito
    public byte[]? BinaryData { get; set; }        // Datos binarios (imágenes)
}
```

**¿Por qué estas propiedades?**
- **Content**: Texto principal o referencia al contenido
- **Preview**: Para mostrar en listas sin cargar todo el contenido
- **Size**: Para mostrar tamaño y optimizar memoria
- **BinaryData**: Para imágenes y archivos
- **IsFavorite**: Funcionalidad de favoritos
- **CreatedAt**: Ordenamiento cronológico

## 🔧 DbContext - El Administrador de Base de Datos

### ClipboardDbContext.cs
```csharp
public class ClipboardDbContext : DbContext
{
    public DbSet<ClipboardItem> ClipboardItems { get; set; }    // Tabla items
    public DbSet<ClipboardType> ClipboardTypes { get; set; }    // Tabla tipos
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configura relación 1 a muchos
        // Si borras un tipo, se borran todos sus items (Cascade)
        
        // Datos iniciales: Text, Image, File, Color, Code, Link
    }
}
```

**¿Por qué Entity Framework?**
- **ORM potente**: Convierte objetos C# a SQL automáticamente
- **LINQ**: Consultas tipo-seguras en C#
- **Migraciones**: Control de versiones de la base de datos
- **Change tracking**: Detecta cambios automáticamente
- **Lazy loading**: Carga datos solo cuando se necesitan

## 🏭 Design-Time Factory

### ClipboardDbContextFactory.cs
```csharp
public class ClipboardDbContextFactory : IDesignTimeDbContextFactory<ClipboardDbContext>
{
    public ClipboardDbContext CreateDbContext(string[] args)
    {
        // Crea DbContext para herramientas de EF (migraciones)
    }
}
```

**¿Por qué necesaria?**
- **WinUI 3 Limitation**: EF Tools no pueden acceder al DI container de WinUI
- **Migraciones**: `Add-Migration` necesita crear DbContext independientemente
- **Diseño**: Permite que herramientas funcionen en tiempo de diseño

## ⚙️ Dependency Injection en App.xaml.cs

```csharp
private static IHost CreateHost()
{
    return Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            // Configura path: %LocalAppData%\ClipboardManager\clipboard.db
            var dbPath = Path.Combine(...);
            
            // Registra DbContext con SQLite
            services.AddDbContext<ClipboardDbContext>(...);
        })
        .Build();
}
```

**¿Por qué Dependency Injection?**
- **Desacoplamiento**: Las clases no se crean directamente unas a otras
- **Testabilidad**: Puedes inyectar mocks para testing
- **Gestión automática**: El container maneja ciclos de vida
- **Configuración centralizada**: Un solo lugar para configurar servicios

**¿Por qué `_ = Host;` en OnLaunched?**
```csharp
_ = Host;  // Fuerza la ejecución del getter
```
- **Lazy initialization**: Host solo se crea cuando se necesita
- **Timing**: Garantiza que la BD esté configurada antes de usar la app
- **_ =**: Sintaxis que dice "ejecuta pero no guardes el resultado"

## 📊 Resultado Final

**Base de datos creada en:**
`C:\Users\[usuario]\AppData\Local\ClipboardManager\clipboard.db`

**Tablas creadas:**
- `ClipboardTypes` (con 6 tipos predefinidos)
- `ClipboardItems` (vacía, lista para datos)

**Relación:** 1 ClipboardType → Muchos ClipboardItems

## 🎯 ¿Qué sigue? (Fase 2)

Ya tienes la **arquitectura sólida**. Ahora viene:
1. **ClipboardService**: Para monitorear el portapapeles del sistema
2. **ViewModels**: Para manejar la lógica de la UI
3. **Comandos MVVM**: Para las acciones del usuario

**Esta base te permitirá:**
- Guardar automáticamente todo lo que copies
- Buscar en el historial
- Organizar por tipos
- Implementar favoritos
- Agregar hotkeys globales

¡La fundación está lista para construir el clipboard manager profesional! 🚀