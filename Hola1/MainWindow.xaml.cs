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
using System.Windows.Media;      // NUEVO: Para VisualTreeHelper
using System.Windows.Forms;     // Para NotifyIcon
using System.Drawing;           // Para Icon

namespace BasicClipboardApp
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
        private bool blockNextClipboardUpdateProcessing = false;

        public MainWindow()
        {
            InitializeComponent();

            ClipboardHistoryUI = new ObservableCollection<string>();
            this.DataContext = this;

            InitializeDatabase();
            LoadHistoryFromDb();
            InitializeTrayIcon();

            this.IsVisibleChanged += MainWindow_IsVisibleChanged;

            // NUEVO: Suscribirse al evento PreviewMouseWheel del ListBox
            HistoryListBox.PreviewMouseWheel += HistoryListBox_PreviewMouseWheel;
        }

        // --- Inicialización ---
        private void InitializeDatabase()
        {
            using (var dbCtx = new AppDbContext()) // Asumiendo AppDbContext.cs
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

        // --- Lógica de la Ventana (Visibilidad, Posicionamiento, Cierre) ---
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

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) this.DragMove();
        }

        // --- Lógica del Portapapeles y Base de Datos ---
        private void LoadHistoryFromDb()
        {
            ClipboardHistoryUI.Clear();
            try
            {
                using (var dbCtx = new AppDbContext()) // Asumiendo AppDbContext.cs
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
                Dispatcher.Invoke(() =>
                {
                    if (blockNextClipboardUpdateProcessing)
                    {
                        blockNextClipboardUpdateProcessing = false;
                        return;
                    }
                    ProcessClipboardChange();
                });
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void ProcessClipboardChange()
        {
            try
            {
                if (!System.Windows.Clipboard.ContainsText()) return;
                string currentText = System.Windows.Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(currentText)) return;

                using (var dbCtx = new AppDbContext()) // Asumiendo AppDbContext.cs
                {
                    var existingItem = dbCtx.ClipboardHistoryItems.FirstOrDefault(item => item.DataType == "Text" && item.TextContent == currentText);
                    if (existingItem != null)
                    {
                        existingItem.Timestamp = DateTime.UtcNow;
                        dbCtx.SaveChanges();
                        if (ClipboardHistoryUI.Contains(currentText)) ClipboardHistoryUI.Remove(currentText);
                        ClipboardHistoryUI.Insert(0, currentText);
                    }
                    else
                    {
                        var newItem = new ClipboardItem // Asumiendo ClipboardItem.cs
                        {
                            Timestamp = DateTime.UtcNow,
                            DataType = "Text",
                            TextContent = currentText
                        };
                        dbCtx.ClipboardHistoryItems.Add(newItem);
                        dbCtx.SaveChanges();
                        ClipboardHistoryUI.Insert(0, currentText);
                        if (ClipboardHistoryUI.Count > MaxHistoryItemsInUI) ClipboardHistoryUI.RemoveAt(ClipboardHistoryUI.Count - 1);
                    }
                }
            }
            catch (COMException comEx) { Console.WriteLine($"Advertencia Clipboard: {comEx.Message}"); }
            catch (Exception ex) { Console.WriteLine($"Error al procesar clipboard: {ex.Message}"); }
        }

        private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is string selectedText)
            {
                try
                {
                    blockNextClipboardUpdateProcessing = true;
                    System.Windows.Clipboard.SetText(selectedText);
                    MoveItemToFront(selectedText); // Mover al frente inmediatamente

                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    System.Threading.Tasks.Task.Delay(100).ContinueWith(_ => Dispatcher.Invoke(() => Mouse.OverrideCursor = null));
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error al copiar/mover ítem: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    blockNextClipboardUpdateProcessing = false;
                }
                // No deseleccionar automáticamente para evitar problemas con re-selección o bucles
            }
        }

        private void MoveItemToFront(string textContent)
        {
            if (string.IsNullOrWhiteSpace(textContent)) return;
            try
            {
                using (var dbCtx = new AppDbContext()) // Asumiendo AppDbContext.cs
                {
                    var existingItem = dbCtx.ClipboardHistoryItems.FirstOrDefault(item => item.DataType == "Text" && item.TextContent == textContent);
                    if (existingItem != null)
                    {
                        existingItem.Timestamp = DateTime.UtcNow;
                        dbCtx.SaveChanges();
                        if (ClipboardHistoryUI.Contains(textContent)) ClipboardHistoryUI.Remove(textContent);
                        ClipboardHistoryUI.Insert(0, textContent);
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error al mover ítem al frente: {ex.Message}"); }
        }

        // --- NUEVO: Manejador para el scroll horizontal con la rueda del mouse ---
        private void HistoryListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Primero, intentar obtener el ScrollViewer interno del ListBox
            ScrollViewer? scrollViewer = FindVisualChild<ScrollViewer>(HistoryListBox);

            if (scrollViewer != null)
            {
                // Determinar si el scroll horizontal es posible
                if (scrollViewer.ScrollableWidth > 0)
                {
                    double scrollAmount = 48; // Cantidad de scroll, ajustar según preferencia (equivalente a 3 líneas de texto por defecto)
                                              // O usar un multiplicador de e.Delta si se prefiere
                                              // double scrollAmount = Math.Abs(e.Delta / (double)Mouse.MouseWheelDeltaForOneLine) * 16.0; // 16 es el alto de línea por defecto

                    if (e.Delta < 0) // Rueda hacia abajo/atrás -> scroll a la derecha
                    {
                        scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + scrollAmount);
                    }
                    else if (e.Delta > 0) // Rueda hacia arriba/adelante -> scroll a la izquierda
                    {
                        scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - scrollAmount);
                    }
                    e.Handled = true; // Marcar el evento como manejado para evitar el scroll vertical
                }
                // Si ScrollableWidth es 0, no hay nada que scrollear horizontalmente, dejar que el evento siga (o no hacer nada)
            }
        }

        // --- NUEVO: Función auxiliar para encontrar un hijo visual ---
        public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                {
                    return t;
                }
                else
                {
                    T? childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }

        // Opcional: Método para limpiar ítems antiguos de la base de datos
        private void CleanupOldDbItems(TimeSpan retentionPeriod)
        {
            try
            {
                using (var dbCtx = new AppDbContext()) // Asumiendo AppDbContext.cs
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