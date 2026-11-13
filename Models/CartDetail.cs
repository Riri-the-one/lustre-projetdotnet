using System.ComponentModel.DataAnnotations;
using ProjetDotNet.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetDotNet.Models
{
    [Table("CartDetail")]
    public class CartDetail
    {
        public int Id { get; set; }
        [Required]
        public int ShoppingCartId { get; set; }
        [Required]
        public int ProductId { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public decimal UnitPrice { get; set; }
        public Product Product { get; set; } = null!;
        public ShoppingCart ShoppingCart { get; set; } = null!;
    }
}
