using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipboard.Models
{
    public class ClipboardItem
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(600)]
        public string? Preview { get; set; }

        public long Size { get; set; }

        // Foreing key for clipboardtype
        public int ClipboardTypeId { get; set; }

        //Navegation 
        [ForeignKey("ClipboardTypeId")]
        public virtual ClipboardType ClipboardType { get; set; } = null!;

        // Porpiedades diferentes para cada tipo de contenido 
        [MaxLength(100)]
        public string? Format { get; set; }

        public bool IsFavorite { get; set; } = false;

        // Para contenido binario como imagenes
        public byte[]? BinaryData { get; set; }
    }
}
