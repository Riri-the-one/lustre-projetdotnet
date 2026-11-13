namespace ProjetDotNet.Models.DTOs;
public class OrderDetailModelDTO
{
    public string DivId { get; set; } = string.Empty;
    public IEnumerable<OrderDetail> OrderDetail { get; set; } = [];
}
