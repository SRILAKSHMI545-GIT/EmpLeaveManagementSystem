namespace LeaveManagement.Core.Entities;

public class LeaveBalance
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }

    public int Year { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays => TotalDays - UsedDays;

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
