using System;
using DataAccess.Entities.Models;
using System.ComponentModel.DataAnnotations;


namespace DataAccess.Entities
{
	public class PositionsOrder
	{
        [Key]
        public int OrderId { get; set; }

        [Required]
        public int PositionId { get; set; }

        [Required]
        public int Count { get; set; }

        public Order Order { get; set; }

        public Position Position { get; set; }
	}
}
