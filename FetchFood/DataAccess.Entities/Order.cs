using System;
using DataAccess.Entities.Models;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
	public class Order
	{
		[Key]
        public int OrderId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CourierId { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public int price { get; set; }

        public DateTime DateOrder { get; set; } 


        public User User { get; set; }

        public User Courier { get; set; }

        public ICollection<PositionsOrder> PositionsOrders { get; set; }


    }
}
