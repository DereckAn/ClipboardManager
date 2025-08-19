using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
/*
 ¿Qué hace esta clase? 📚

  ClipboardDbContext es como el "administrador de la base de datos" de tu aplicación:

  1. Es el puente entre tu código C# y SQLite

  - Convierte tus clases C# en tablas de base de datos
  - Traduce tus consultas LINQ a SQL

2. DbSets = Tablas

  public DbSet<ClipboardItem> ClipboardItems { get; set; }  // Tabla "ClipboardItems"
  public DbSet<ClipboardType> ClipboardTypes { get; set; }  // Tabla "ClipboardTypes"

  3. OnModelCreating = Configuración de la base de datos

  - Líneas 23-27: Define la relación 1 a muchos
    - 1 ClipboardType → muchos ClipboardItems
    - Si borras un Type, se borran todos sus Items (Cascade)
  - Líneas 30-37: Datos iniciales (seed data)
    - Cuando crees la base de datos, automáticamente tendrás 6 tipos: Text, Image, File, Color,
  Code, Link

  4. En la práctica funcionará así:

  // Obtener todos los items de texto
  var textItems = context.ClipboardItems
      .Where(item => item.ClipboardType.Name == "Text")
      .ToList();

  // Agregar nuevo item
  var newItem = new ClipboardItem { Content = "Hola", ClipboardTypeId = 1 };
  context.ClipboardItems.Add(newItem);
  context.SaveChanges();
 */
namespace Clipboard.Models
{
    public class ClipboardDbContext : DbContext
    {
        public ClipboardDbContext(DbContextOptions<ClipboardDbContext> options ) : base(options ) { }

        //DbSets - Representan las tablas
        public DbSet<ClipboardItem> ClipboardItems { get; set; }
        public DbSet<ClipboardType> ClipboardTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuraciones adicionales
            modelBuilder.Entity<ClipboardItem>()
                .HasOne(ci => ci.ClipboardType)
                .WithMany(ct => ct.ClipboardItems)
                .HasForeignKey(ci => ci.ClipboardTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            //Datos iniciales -  tipos basicos de clipboard
            modelBuilder.Entity<ClipboardType>().HasData(
                new ClipboardType { Id = 1, Name = "Text", Description = "Plain text content"},
                new ClipboardType { Id = 2, Name = "Image", Description = "Image content"},
                new ClipboardType { Id = 3, Name = "File", Description = "File paths and file content"},
                new ClipboardType { Id = 4, Name = "Color", Description = "Color values"},
                new ClipboardType { Id = 5, Name = "Code", Description = "Source code snippets"},
                new ClipboardType { Id = 6, Name = "Link", Description = "Url to a webpage"}
                );
        }

    }
}
