// Services/WindowService.cs
using Microsoft.UI.Windowing; // Para AppWindow, OverlappedPresenter, DisplayArea, TitleBarHeightOption
using Microsoft.UI.Xaml;
using System;
using Windows.Graphics;     // Para RectInt32, SizeInt32
using WinRT.Interop;        // Para WindowNative

namespace clipboard_winui3.Services
{
    public class WindowService
    {
        private AppWindow? _appWindow;
        private Window? _mainWindow;

        public void InitializeMainWindow(Window window)
        {
            _mainWindow = window;
            _appWindow = GetAppWindowForCurrentWindow(window);

            if (_appWindow != null)
            {
                // 1. Configurar para que el contenido de la app ocupe toda la ventana
                //    y no haya una barra de título estándar.
                _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

                // 2. Colapsar la altura de la barra de título para evitar que una barra invisible
                //    pueda ser agarrada para mover la ventana.
                _appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;

                // 3. Especificar que no hay áreas designadas para arrastrar la ventana.
                _appWindow.TitleBar.SetDragRectangles(new RectInt32[0]);

                // 4. Configurar el Presenter (cómo se muestra la ventana)
                if (_appWindow.Presenter is OverlappedPresenter presenter)
                {
                    // Indicar explícitamente que no queremos ni borde ni barra de título dibujados por el sistema.
                    presenter.SetBorderAndTitleBar(false, false); // <--- ¡NUEVA LÍNEA CLAVE!

                    presenter.IsResizable = false;      // No se puede redimensionar
                    presenter.IsMaximizable = false;    // No se puede maximizar
                    presenter.IsMinimizable = false;    // No se puede minimizar
                }

                // 5. Obtener dimensiones de la pantalla completa (incluyendo barra de tareas)
                var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    // Usamos .OuterBounds para el área total de la pantalla física.
                    var screenTotalWidth = displayArea.OuterBounds.Width;
                    var screenTotalHeight = displayArea.OuterBounds.Height;

                    var screenOriginX = displayArea.OuterBounds.X;
                    var screenOriginY = displayArea.OuterBounds.Y;

                    // 6. Definir dimensiones y posición de la ventana
                    var windowWidth = screenTotalWidth;
                    var windowHeight = 450;

                    var windowX = screenOriginX;
                    var windowY = screenOriginY + screenTotalHeight - windowHeight;

                    _appWindow.MoveAndResize(new RectInt32(windowX, windowY, windowWidth, windowHeight));
                }
            }
        }

        private AppWindow GetAppWindowForCurrentWindow(Window window)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            Microsoft.UI.WindowId wndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        public void ShowWindow()
        {
            _mainWindow?.Activate();
            _appWindow?.Show();
        }

        public void HideWindow()
        {
            _appWindow?.Hide();
        }
    }
}