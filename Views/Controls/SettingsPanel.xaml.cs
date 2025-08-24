using Clipboard.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Clipboard.Views.Controls
{
    public sealed partial class SettingsPanel : UserControl
    {
        public SettingsViewModel ViewModel { get; set; }

        public SettingsPanel()
        {
            InitializeComponent();
        }
    }
}
