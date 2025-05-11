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

namespace BasicClipboardApp
{
    public partial class MainWindow : Window
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        private IntPtr windowHandle;

        public ObservableCollection<string> ClipboardHistoryUI { get; set; }
        private const int MaxHistoryItemsInUI = 50;

        private NotifyIcon trayIcon;
        private bool isExiting = false;
        private bool blockNextClipboardUpdateProcessing = false; // Bandera para controlar el procesamiento

        public MainWindow()
        {
            InitializeComponent();
            ClipboardHistoryUI = new ObservableCollection<string>();
            this.DataContext = this;
            InitializeDatabase();
            LoadHistoryFromDb();
            InitializeTrayIcon();
            this.IsVisibleChanged += MainWindow_IsVisibleChanged;
        }

        private void InitializeDatabase()
        {
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
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clipboard_icon.ico");
                if (File.Exists(iconPath)) { trayIcon.Icon = new System.Drawing.Icon(iconPath); }
                else { System.Windows.MessageBox.Show("Icono 'clipboard_icon.ico' no encontrado.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Error al cargar icono: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }

            trayIcon.Text = "Mi Historial Clipboard";
            trayIcon.Visible = true;
            trayIcon.Click += TrayIcon_Click;
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Mostrar/Ocultar Historial", null, TrayIcon_Click);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Salir", null, ExitApplication_Click);
            trayIcon.ContextMenuStrip = contextMenu;
        }

        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue) PositionWindowAtBottomAndFullWidth();
        }

        private void TrayIcon_Click(object? sender, EventArgs e)
        {
            if (this.IsVisible) this.Hide();
            else { this.Show(); this.WindowState = WindowState.Normal; this.Activate(); }
        }

        private void PositionWindowAtBottomAndFullWidth()
        {
            this.Width = SystemParameters.WorkArea.Width;
            this.Top = SystemParameters.WorkArea.Height - this.ActualHeight;
            this.Left = SystemParameters.WorkArea.Left;
            if (this.Top < SystemParameters.WorkArea.Top) this.Top = SystemParameters.WorkArea.Top;
        }

        private void ExitApplication_Click(object? sender, EventArgs e)
        {
            isExiting = true;
            trayIcon.Visible = false;
            trayIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!isExiting) { e.Cancel = true; this.Hide(); }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (isExiting && windowHandle != IntPtr.Zero)
            {
                RemoveClipboardFormatListener(windowHandle);
                HwndSource.FromHwnd(windowHandle)?.RemoveHook(WndProc);
                windowHandle = IntPtr.Zero;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e) { /* Opcional: if (this.WindowState == WindowState.Minimized && this.IsVisible) this.Hide(); */ }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) this.DragMove(); }

        private void LoadHistoryFromDb()
        {
            ClipboardHistoryUI.Clear();
            try
            {
                using (var dbCtx = new AppDbContext())
                {
                    var itemsFromDb = dbCtx.ClipboardHistoryItems.OrderByDescending(item => item.Timestamp).Take(MaxHistoryItemsInUI).ToList();
                    foreach (var dbItem in itemsFromDb.AsEnumerable().Reverse())
                    {
                        if (dbItem.DataType == "Text" && !string.IsNullOrWhiteSpace(dbItem.TextContent))
                        {
                            ClipboardHistoryUI.Add(dbItem.TextContent!);
                        }
                    }
                }
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Error al cargar historial: {ex.Message}", "Error DB", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            windowHandle = new WindowInteropHelper(this).Handle;
            if (windowHandle == IntPtr.Zero) return;
            if (!AddClipboardFormatListener(windowHandle)) { /* Log error */ }
            HwndSource.FromHwnd(windowHandle)?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                // Procesar el cambio del clipboard en el hilo de UI usando Dispatcher
                Dispatcher.Invoke(() =>
                {
                    if (blockNextClipboardUpdateProcessing)
                    {
                        blockNextClipboardUpdateProcessing = false; // Resetear la bandera
                        return; // Saltar este procesamiento porque fue iniciado por la propia app
                    }
                    ProcessClipboardChange(); // Nuevo método para encapsular la lógica
                });
                handled = true;
            }
            return IntPtr.Zero;
        }

        // Nuevo método para procesar el cambio del clipboard
        private void ProcessClipboardChange()
        {
            try
            {
                if (!System.Windows.Clipboard.ContainsText()) return;

                string currentText = System.Windows.Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(currentText)) return;

                using (var dbCtx = new AppDbContext())
                {
                    var existingItem = dbCtx.ClipboardHistoryItems
                                          .FirstOrDefault(item => item.DataType == "Text" && item.TextContent == currentText);

                    if (existingItem != null) // El ítem ya existe en la BD
                    {
                        existingItem.Timestamp = DateTime.UtcNow; // Actualizar timestamp para moverlo al "frente"
                        dbCtx.SaveChanges();

                        // Actualizar UI: remover y reinsertar al principio
                        if (ClipboardHistoryUI.Contains(currentText))
                        {
                            ClipboardHistoryUI.Remove(currentText);
                        }
                        ClipboardHistoryUI.Insert(0, currentText);
                    }
                    else // El ítem es nuevo
                    {
                        var newItem = new ClipboardItem
                        {
                            Timestamp = DateTime.UtcNow,
                            DataType = "Text",
                            TextContent = currentText
                        };
                        dbCtx.ClipboardHistoryItems.Add(newItem);
                        dbCtx.SaveChanges();

                        ClipboardHistoryUI.Insert(0, currentText);
                        if (ClipboardHistoryUI.Count > MaxHistoryItemsInUI)
                        {
                            ClipboardHistoryUI.RemoveAt(ClipboardHistoryUI.Count - 1);
                        }
                    }
                }
            }
            catch (COMException comEx) { Console.WriteLine($"Advertencia Clipboard: {comEx.Message}"); }
            catch (Exception ex) { Console.WriteLine($"Error al procesar cambio de clipboard: {ex.Message}"); }
        }


        // Anterior UpdateClipboardHistoryToDb() renombrado y modificado
        // Este método ya no se llama directamente desde WndProc, sino ProcessClipboardChange
        // Se mantiene la lógica pero ahora no tiene la bandera.

        private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is string selectedText)
            {
                try
                {
                    // 1. Marcar que el próximo cambio del clipboard NO debe ser procesado como una nueva entrada
                    blockNextClipboardUpdateProcessing = true;

                    // 2. Poner el texto seleccionado en el portapapeles del sistema
                    System.Windows.Clipboard.SetText(selectedText);
                    // Esto disparará WM_CLIPBOARDUPDATE, que llamará a WndProc -> ProcessClipboardChange.
                    // Pero como blockNextClipboardUpdateProcessing es true, se ignorará.

                    // 3. Forzar explícitamente la lógica de "mover al frente" para este ítem
                    //    Esto asegura que el ítem seleccionado se mueva al frente inmediatamente
                    //    en la BD y en la UI, independientemente del listener del clipboard.
                    MoveItemToFront(selectedText);


                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    System.Threading.Tasks.Task.Delay(100).ContinueWith(_ => Dispatcher.Invoke(() => Mouse.OverrideCursor = null));
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error al copiar/mover ítem: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    blockNextClipboardUpdateProcessing = false; // Resetear en caso de error
                }
                finally
                {
                    // Deseleccionar para permitir re-clic si es necesario y evitar bucles si la lógica no es perfecta.
                    if (HistoryListBox.SelectedItem != null)
                    {
                        // Esto puede volver a disparar SelectionChanged con e.AddedItems.Count == 0, por eso la comprobación al inicio.
                        // HistoryListBox.SelectedItem = null; 
                        // Si quieres mantenerlo seleccionado, comenta la línea de arriba.
                        // Para evitar problemas, es mejor no deseleccionar automáticamente aquí o manejarlo con cuidado.
                    }
                }
            }
        }

        // NUEVO: Método para mover un ítem existente al frente (BD y UI)
        private void MoveItemToFront(string textContent)
        {
            if (string.IsNullOrWhiteSpace(textContent)) return;

            try
            {
                using (var dbCtx = new AppDbContext())
                {
                    var existingItem = dbCtx.ClipboardHistoryItems
                                          .FirstOrDefault(item => item.DataType == "Text" && item.TextContent == textContent);

                    if (existingItem != null)
                    {
                        existingItem.Timestamp = DateTime.UtcNow; // Actualizar timestamp
                        dbCtx.SaveChanges();

                        // Actualizar UI
                        if (ClipboardHistoryUI.Contains(textContent))
                        {
                            ClipboardHistoryUI.Remove(textContent);
                        }
                        ClipboardHistoryUI.Insert(0, textContent);

                        // Opcional: Asegurar que el ítem se vea y esté seleccionado si el ListBox lo permite
                        // HistoryListBox.SelectedItem = textContent;
                        // HistoryListBox.ScrollIntoView(textContent);
                    }
                    // Si no existe, no hacemos nada aquí, ya que esto es para mover uno existente.
                    // La lógica de añadir nuevos está en ProcessClipboardChange.
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al mover ítem al frente: {ex.Message}");
            }
        }


        private void CleanupOldDbItems(TimeSpan retentionPeriod)
        {
            try
            {
                using (var dbCtx = new AppDbContext())
                {
                    var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);
                    var itemsToDelete = dbCtx.ClipboardHistoryItems.Where(item => item.Timestamp < cutoffDate);
                    if (itemsToDelete.Any()) { dbCtx.ClipboardHistoryItems.RemoveRange(itemsToDelete); dbCtx.SaveChanges(); }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error al limpiar BD: {ex.Message}"); }
        }
    }
}