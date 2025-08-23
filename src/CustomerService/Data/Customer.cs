namespace CustomerService.Data;

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Document { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Status { get; set; } = "PENDING";
    // pending, approved, rejected, ready, card_failedd
}