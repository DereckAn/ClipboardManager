// AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace BasicClipboardApp // Asegúrate que el namespace coincida con tu proyecto
{
    public class AppDbContext : DbContext
    {
        // Esta propiedad representa la tabla "ClipboardHistory" en la base de datos.
        // El nombre de la propiedad (ClipboardHistoryItems) será el nombre de la tabla.
        public DbSet<ClipboardItem> ClipboardHistoryItems { get; set; }

        // Ruta donde se guardará el archivo de la base de datos SQLite
        public string DbPath { get; }

        public AppDbContext()
        {
            // Define la ruta de tu base de datos en la carpeta de datos locales de la aplicación
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);

            // Crear un subdirectorio para tu aplicación (buena práctica)
            // Asegúrate que el nombre de la carpeta sea el mismo que usaste para el JSON si quieres
            string appDirectory = Path.Combine(path, "MyBasicClipboardApp");
            Directory.CreateDirectory(appDirectory); // No hace nada si ya existe

            DbPath = Path.Join(appDirectory, "clipboard_history.db"); // Nombre del archivo de la BD
        }

        // Este método configura la conexión a la base de datos
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }
}