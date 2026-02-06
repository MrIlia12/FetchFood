using DataAccess.Entities.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class Position
    {
        [Key]
        [Required]
        public int PositionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public decimal Price { get; set; }
        [Required]
        public PositionStatus Status { get; set; }
        public string? Ingredients { get; set; }
        public string? Description { get; set; }

        public string? Image { get; set; } // возможно, лучше использовать массив байт..

        // Связь с категорией
        [ForeignKey("Category")]
        public int? PositionCategoryId { get; set; }
        
        // Навигационное свойство
        public virtual PositionCategory? Category { get; set; }
    }
}
