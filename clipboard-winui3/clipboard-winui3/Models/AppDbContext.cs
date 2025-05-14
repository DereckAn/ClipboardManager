using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace clipboard_winui3.Models
{
    internal class AppDbContext : DbContext
    {
        public DbSet<ClipboardItem> ClipboardHistoryItems { get; set; }
        public string DbPath { get; }

        public AppDbContext()
        {
            // En WinUI 3 empaquetado, es mejor usar ApplicationData.Current.LocalFolder
            // para asegurar que se escribe en la ubicación correcta y con permisos.
            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            string appDirectory = Path.Combine(localFolder, "MyBasicClipboardApp_WinUI"); // Diferente nombre para no colisionar con la BD de WPF
            Directory.CreateDirectory(appDirectory);
            DbPath = Path.Join(appDirectory, "clipboard_history_winui.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }
}
