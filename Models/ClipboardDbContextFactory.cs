using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.IO;

namespace Clipboard.Models
{
    /// <summary>
    /// Factory para que Entity Framework Tools pueda crear el DbContext en tiempo de diseño
    /// Esto es necesario para las migraciones en aplicaciones WinUI 3
    /// </summary>
    public class ClipboardDbContextFactory : IDesignTimeDbContextFactory<ClipboardDbContext>
    {
        public ClipboardDbContext CreateDbContext(string[] args)
        {
            // Crear opciones para el DbContext
            var optionsBuilder = new DbContextOptionsBuilder<ClipboardDbContext>();

            // Usar la misma lógica de path que en App.xaml.cs
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ClipboardManager",
                "clipboard.db"
            );

            // Crear directorio si no existe
            var directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            // Configurar SQLite
            optionsBuilder.UseSqlite($"Data Source={dbPath}");

            return new ClipboardDbContext(optionsBuilder.Options);
        }
    }
}