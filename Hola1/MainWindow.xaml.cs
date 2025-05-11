using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Forms; // Para NotifyIcon
using System.Drawing;       // Para Icon

namespace BasicClipboardApp // Asegúrate que este namespace coincida con tus otros archivos
{
    public partial class MainWindow : Window
    {
        // --- Constantes y API de Windows ---
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        private IntPtr windowHandle;

        // --- Propiedades y Colección ---
        public ObservableCollection<string> ClipboardHistoryUI { get; set; }
        private const int MaxHistoryItemsInUI = 50;

        // --- NotifyIcon y Control de Salida ---
        private NotifyIcon trayIcon;
        private bool isExiting = false;

        public MainWindow()
        {
            InitializeComponent();

            ClipboardHistoryUI = new ObservableCollection<string>();
            this.DataContext = this; // Para el binding de ClipboardHistoryUI en XAML

            InitializeDatabase();
            LoadHistoryFromDb();
            InitializeTrayIcon();

            // Suscribirse al evento IsVisibleChanged para reposicionar la ventana
            this.IsVisibleChanged += MainWindow_IsVisibleChanged;
        }

        // --- Inicialización ---
        private void InitializeDatabase()
        {
            // Asumiendo que tienes una clase AppDbContext definida en tu proyecto
            // y los paquetes NuGet de Entity Framework Core SQLite instalados.
            using (var dbCtx = new AppDbContext())
            {
                dbCtx.Database.EnsureCreated();
            }
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon();
            try
            {
                // Asegúrate de que "clipboard_icon.ico" está en tu proyecto
                // y su "Build Action" es "Content", "Copy to Output Directory" es "Copy if newer".
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clipboard_icon.ico");
                if (File.Exists(iconPath))
                {
                    trayIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    System.Windows.MessageBox.Show("Icono 'clipboard_icon.ico' no encontrado. Asegúrate de que está en la carpeta de salida y configurado como 'Content' y 'Copy if newer'.", "Error de Icono", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al cargar el icono: {ex.Message}", "Error de Icono", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            trayIcon.Text = "Mi Historial Clipboard";
            trayIcon.Visible = true;
            trayIcon.Click += TrayIcon_Click; // Mostrar/ocultar ventana

            // Crear menú contextual para el icono del tray
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Mostrar/Ocultar Historial", null, TrayIcon_Click);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Salir", null, ExitApplication_Click);
            trayIcon.ContextMenuStrip = contextMenu;
        }

        // --- Lógica de la Ventana (Visibilidad, Posicionamiento, Cierre) ---
        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true) // Si la ventana se está haciendo VISIBLE
            {
                PositionWindowAtBottomAndFullWidth();
            }
        }

        private void TrayIcon_Click(object? sender, EventArgs e)
        {
            if (this.IsVisible)
            {
                this.Hide();
            }
            else
            {
                // La posición se manejará en MainWindow_IsVisibleChanged
                this.Show();
                this.WindowState = WindowState.Normal; // Asegura que no esté minimizada
                this.Activate(); // Traer al frente
            }
        }

        private void PositionWindowAtBottomAndFullWidth()
        {
            // Obtener el área de trabajo de la pantalla principal (excluye la barra de tareas)
            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;

            // Establecer el ancho de la ventana al ancho completo del área de trabajo
            this.Width = screenWidth;

            // Calcular la posición superior (Top)
            // Usar this.ActualHeight para mayor precisión después de que la ventana se haya renderizado.
            // Si this.Height está fijado en XAML, this.Height también funcionaría después de mostrarse.
            this.Top = screenHeight - this.ActualHeight;

            // Alinear la ventana al borde izquierdo del área de trabajo
            this.Left = SystemParameters.WorkArea.Left; // Generalmente 0

            // Asegurar que la ventana no se posicione fuera de los límites superiores del área de trabajo
            if (this.Top < SystemParameters.WorkArea.Top)
            {
                this.Top = SystemParameters.WorkArea.Top;
            }
        }

        private void ExitApplication_Click(object? sender, EventArgs e)
        {
            isExiting = true; // Marcar que estamos saliendo realmente
            trayIcon.Visible = false;
            trayIcon.Dispose(); // Liberar recursos del NotifyIcon
            System.Windows.Application.Current.Shutdown(); // Cerrar la aplicación WPF
        }

        // Sobrescribir OnClosing para controlar el comportamiento del botón 'X' de la ventana
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e); // Buena práctica llamar al método base
            if (!isExiting) // Si no se está saliendo a través de la opción "Salir" del tray
            {
                e.Cancel = true; // Cancela el cierre de la ventana
                this.Hide();     // Solo oculta la ventana
            }
            // Si isExiting es true, se permite el cierre y la limpieza se hará en Window_Closing
        }

        // Manejador para el evento Closing definido en XAML (Window_Closing)
        // Este se llama tanto al ocultar (si OnClosing lo cancela) como al cerrar de verdad.
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Limpiar listeners del clipboard solo cuando la aplicación realmente se cierra
            // o si quieres detener el monitoreo cuando está oculta (generalmente no es lo deseado).
            // Si isExiting es true, entonces es un cierre real.
            if (isExiting)
            {
                if (windowHandle != IntPtr.Zero)
                {
                    RemoveClipboardFormatListener(windowHandle);
                    HwndSource source = HwndSource.FromHwnd(windowHandle);
                    source?.RemoveHook(WndProc); // Asegúrate de quitar el hook
                    windowHandle = IntPtr.Zero; // Marcar como limpiado
                }
            }
            // No es necesario disponer el trayIcon aquí si se hace en ExitApplication_Click
        }


        // Manejador para el evento StateChanged definido en XAML
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized && this.IsVisible)
            {
                // Opcional: Ocultar la ventana al minimizarla si se prefiere que solo viva en el tray
                // this.Hide();
            }
        }

        // Manejador para poder mover la ventana cuando WindowStyle="None"
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // --- Lógica del Portapapeles y Base de Datos ---
        private void LoadHistoryFromDb()
        {
            ClipboardHistoryUI.Clear();
            try
            {
                using (var dbCtx = new AppDbContext()) // Asumiendo que tienes AppDbContext.cs
                {
                    var itemsFromDb = dbCtx.ClipboardHistoryItems
                                         .OrderByDescending(item => item.Timestamp)
                                         .Take(MaxHistoryItemsInUI)
                                         .ToList();

                    // Añadir a la UI. Si quieres el más reciente arriba en la UI, invierte la lógica
                    // o la consulta. Por ahora, añadimos tal cual, el más reciente estará arriba si la lista
                    // se muestra en orden de inserción.
                    foreach (var dbItem in itemsFromDb.AsEnumerable().Reverse()) // Reverse para que el más antiguo se añada primero y el más nuevo al final de la carga inicial
                    {
                        if (dbItem.DataType == "Text" && !string.IsNullOrWhiteSpace(dbItem.TextContent))
                        {
                            ClipboardHistoryUI.Add(dbItem.TextContent!); // '!' para indicar que no esperamos null aquí
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al cargar el historial desde la base de datos: {ex.Message}", "Error de Base de Datos", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Se llama cuando la ventana está lista para interactuar con Win32 (tiene un HWND)
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            windowHandle = new WindowInteropHelper(this).Handle;
            if (windowHandle == IntPtr.Zero)
            {
                // Manejar error, no se pudo obtener el handle
                return;
            }

            if (!AddClipboardFormatListener(windowHandle))
            {
                // Manejar error, no se pudo registrar el listener
            }

            HwndSource source = HwndSource.FromHwnd(windowHandle);
            if (source != null)
            {
                source.AddHook(WndProc); // Añadir hook para procesar mensajes de Windows
            }
            else
            {
                // Manejar error, no se pudo crear HwndSource
            }
        }

        // Procesa mensajes de Windows (como la actualización del portapapeles)
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                // Ejecutar en el hilo de la UI si es necesario (Dispatcher.Invoke)
                // para operaciones que modifican la UI o colecciones enlazadas.
                // Para este caso, UpdateClipboardHistoryToDb ya maneja la UI.
                UpdateClipboardHistoryToDb();
                handled = true; // Indicamos que hemos manejado este mensaje
            }
            return IntPtr.Zero; // Valor de retorno estándar
        }

        private void UpdateClipboardHistoryToDb()
        {
            try
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    string currentText = System.Windows.Clipboard.GetText();
                    if (string.IsNullOrWhiteSpace(currentText)) return;

                    // Evitar duplicados rápidos en la UI (y potencialmente en BD si no hay otra comprobación)
                    if (ClipboardHistoryUI.Count > 0 && ClipboardHistoryUI[0] == currentText) return;

                    var newItem = new ClipboardItem // Asumiendo que tienes ClipboardItem.cs
                    {
                        Timestamp = DateTime.UtcNow, // Usar UTC para consistencia
                        DataType = "Text",
                        TextContent = currentText
                    };

                    using (var dbCtx = new AppDbContext()) // Asumiendo que tienes AppDbContext.cs
                    {
                        // Opcional: podrías añadir una comprobación para evitar duplicados exactos en la BD aquí si es necesario
                        dbCtx.ClipboardHistoryItems.Add(newItem);
                        dbCtx.SaveChanges();
                    }

                    // Actualizar la UI (añadir al principio para que el más reciente esté arriba)
                    ClipboardHistoryUI.Insert(0, currentText);
                    if (ClipboardHistoryUI.Count > MaxHistoryItemsInUI)
                    {
                        ClipboardHistoryUI.RemoveAt(ClipboardHistoryUI.Count - 1); // Quitar el más antiguo de la UI
                    }
                }
                // Aquí añadirías lógica para otros tipos de datos (imágenes, archivos, etc.) en el futuro
            }
            catch (COMException comEx)
            {
                // Error común si el portapapeles está ocupado
                Console.WriteLine($"Advertencia al acceder al portapapeles: {comEx.Message}");
            }
            catch (Exception ex)
            {
                // Otros errores al actualizar el historial
                Console.WriteLine($"Error al actualizar el historial en la base de datos: {ex.Message}");
                // Considera mostrar un MessageBox si es un error crítico para el usuario.
            }
        }

        // Manejador para cuando se selecciona un ítem en el ListBox
        private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryListBox.SelectedItem is string selectedText) // Asegurarse que es un string
            {
                try
                {
                    System.Windows.Clipboard.SetText(selectedText);
                    // Feedback visual opcional
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    System.Threading.Tasks.Task.Delay(100).ContinueWith(_ => Dispatcher.Invoke(() => Mouse.OverrideCursor = null));
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error al copiar al portapapeles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // Opcional: Método para limpiar ítems antiguos de la base de datos
        private void CleanupOldDbItems(TimeSpan retentionPeriod)
        {
            try
            {
                using (var dbCtx = new AppDbContext()) // Asumiendo que tienes AppDbContext.cs
                {
                    var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);
                    // En SQLite, puede ser más eficiente ejecutar SQL crudo para borrados masivos
                    // dbCtx.Database.ExecuteSqlRaw("DELETE FROM ClipboardHistoryItems WHERE Timestamp < {0}", cutoffDate);
                    // O usar LINQ to Entities:
                    var itemsToDelete = dbCtx.ClipboardHistoryItems.Where(item => item.Timestamp < cutoffDate);
                    if (itemsToDelete.Any())
                    {
                        dbCtx.ClipboardHistoryItems.RemoveRange(itemsToDelete);
                        dbCtx.SaveChanges();
                        Console.WriteLine($"Limpiados {itemsToDelete.Count()} ítems antiguos de la BD.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al limpiar ítems antiguos de la BD: {ex.Message}");
            }
        }
    }
}