using System.Collections.Generic;
using DataAccess.Entities.Models;
using System.ComponentModel.DataAnnotations;


namespace DataAccess.Entities
{
	public class User
	{
        [Key]
        public int UserId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserName { get; set; }
        

        public UserRole Role { get; set; }


        public ICollection<Order> Orders { get; set; }
    }
}
