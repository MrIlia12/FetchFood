using System;
using DataAccess.Entities.Models;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
	public class Position
	{
		[Key]
		public int PositionId { get; set; }

        [Required]
        public string PositionName	{ get; set; }

        [Required]
        public int Price { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Status { get; set; }

		public byte[] Image { get; set; }

        public ICollection<PositionsOrder> PositionsOrders { get; set; }

    }
}
