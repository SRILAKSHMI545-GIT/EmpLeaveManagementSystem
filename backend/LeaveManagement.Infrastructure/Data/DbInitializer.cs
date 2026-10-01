using LeaveManagement.Core.Entities;
using LeaveManagement.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LeaveManagement.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Departments
            if (!await context.Departments.AnyAsync())
            {
                var engineering = new Department { Name = "Engineering", Description = "Software, QA, and Infrastructure" };
                var hr = new Department { Name = "Human Resources", Description = "People, Talent, and Operations" };
                var sales = new Department { Name = "Sales & Marketing", Description = "Growth, Sales, and Marketing" };
                var finance = new Department { Name = "Finance", Description = "Accounting, Payroll, and Compliance" };

                context.Departments.AddRange(engineering, hr, sales, finance);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded Departments.");
            }

            // 2. Seed Leave Types
            if (!await context.LeaveTypes.AnyAsync())
            {
                var types = new List<LeaveType>
                {
                    new() { Name = "Annual Leave", Description = "Standard paid annual time off", DefaultAnnualQuota = 20, RequiresApproval = true },
                    new() { Name = "Sick Leave", Description = "Medical illness and health recovery", DefaultAnnualQuota = 10, RequiresApproval = true },
                    new() { Name = "Casual Leave", Description = "Short personal leaves and urgent matters", DefaultAnnualQuota = 7, RequiresApproval = true },
                    new() { Name = "Maternity/Paternity", Description = "Parental leave for newborn or adoption", DefaultAnnualQuota = 60, RequiresApproval = true },
                    new() { Name = "Unpaid Leave", Description = "Leave without pay for special circumstances", DefaultAnnualQuota = 30, RequiresApproval = true }
                };

                context.LeaveTypes.AddRange(types);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded Leave Types.");
            }

            // 3. Seed Users
            if (!await context.Users.AnyAsync())
            {
                var engDept = await context.Departments.FirstAsync(d => d.Name == "Engineering");
                var hrDept = await context.Departments.FirstAsync(d => d.Name == "Human Resources");

                var defaultPassword = BCrypt.Net.BCrypt.HashPassword("Password123!");

                // Admin
                var admin = new User
                {
                    Name = "Admin User",
                    Email = "admin@company.com",
                    PasswordHash = defaultPassword,
                    Role = UserRole.Admin,
                    DepartmentId = hrDept.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                context.Users.Add(admin);
                await context.SaveChangesAsync();

                // Manager
                var manager = new User
                {
                    Name = "Sarah Connor",
                    Email = "manager@company.com",
                    PasswordHash = defaultPassword,
                    Role = UserRole.Manager,
                    DepartmentId = engDept.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                context.Users.Add(manager);
                await context.SaveChangesAsync();

                // Employees reporting to Sarah Connor
                var emp1 = new User
                {
                    Name = "John Doe",
                    Email = "employee@company.com",
                    PasswordHash = defaultPassword,
                    Role = UserRole.Employee,
                    DepartmentId = engDept.Id,
                    ManagerId = manager.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var emp2 = new User
                {
                    Name = "Alice Smith",
                    Email = "alice@company.com",
                    PasswordHash = defaultPassword,
                    Role = UserRole.Employee,
                    DepartmentId = engDept.Id,
                    ManagerId = manager.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var emp3 = new User
                {
                    Name = "Bob Johnson",
                    Email = "bob@company.com",
                    PasswordHash = defaultPassword,
                    Role = UserRole.Employee,
                    DepartmentId = engDept.Id,
                    ManagerId = manager.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                context.Users.AddRange(emp1, emp2, emp3);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded Users.");

                // 4. Seed Leave Balances for Year 2026 and current year
                var allUsers = await context.Users.ToListAsync();
                var leaveTypes = await context.LeaveTypes.ToListAsync();
                var currentYear = DateTime.UtcNow.Year;

                foreach (var user in allUsers)
                {
                    foreach (var lt in leaveTypes)
                    {
                        var used = 0m;
                        if (user.Email == "employee@company.com" && lt.Name == "Annual Leave")
                        {
                            used = 3; // seeded 3 used days
                        }
                        if (user.Email == "alice@company.com" && lt.Name == "Sick Leave")
                        {
                            used = 2;
                        }

                        context.LeaveBalances.Add(new LeaveBalance
                        {
                            UserId = user.Id,
                            LeaveTypeId = lt.Id,
                            Year = currentYear,
                            TotalDays = lt.DefaultAnnualQuota,
                            UsedDays = used,
                            LastUpdatedAt = DateTime.UtcNow
                        });
                    }
                }
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded Leave Balances.");

                // 5. Seed Sample Leave Requests
                var annualLt = leaveTypes.First(lt => lt.Name == "Annual Leave");
                var sickLt = leaveTypes.First(lt => lt.Name == "Sick Leave");
                var casualLt = leaveTypes.First(lt => lt.Name == "Casual Leave");

                var sampleRequests = new List<LeaveRequest>
                {
                    // Approved request for John Doe
                    new()
                    {
                        UserId = emp1.Id,
                        LeaveTypeId = annualLt.Id,
                        StartDate = DateTime.UtcNow.AddDays(-14).Date,
                        EndDate = DateTime.UtcNow.AddDays(-12).Date,
                        DaysRequested = 3,
                        Reason = "Family vacation trip",
                        Status = LeaveRequestStatus.Approved,
                        DecidedById = manager.Id,
                        ManagerComment = "Have a wonderful trip!",
                        CreatedAt = DateTime.UtcNow.AddDays(-20),
                        DecidedAt = DateTime.UtcNow.AddDays(-18)
                    },
                    // Pending request for John Doe
                    new()
                    {
                        UserId = emp1.Id,
                        LeaveTypeId = casualLt.Id,
                        StartDate = DateTime.UtcNow.AddDays(7).Date,
                        EndDate = DateTime.UtcNow.AddDays(8).Date,
                        DaysRequested = 2,
                        Reason = "Personal business and doctor checkup",
                        Status = LeaveRequestStatus.Pending,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    // Pending request for Alice Smith
                    new()
                    {
                        UserId = emp2.Id,
                        LeaveTypeId = annualLt.Id,
                        StartDate = DateTime.UtcNow.AddDays(14).Date,
                        EndDate = DateTime.UtcNow.AddDays(18).Date,
                        DaysRequested = 5,
                        Reason = "Attending brother's wedding",
                        Status = LeaveRequestStatus.Pending,
                        CreatedAt = DateTime.UtcNow.AddHours(-10)
                    },
                    // Approved request for Bob Johnson
                    new()
                    {
                        UserId = emp3.Id,
                        LeaveTypeId = sickLt.Id,
                        StartDate = DateTime.UtcNow.AddDays(2).Date,
                        EndDate = DateTime.UtcNow.AddDays(3).Date,
                        DaysRequested = 2,
                        Reason = "Scheduled minor dental surgery",
                        Status = LeaveRequestStatus.Approved,
                        DecidedById = manager.Id,
                        ManagerComment = "Approved. Take care Bob.",
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        DecidedAt = DateTime.UtcNow.AddDays(-2)
                    }
                };

                context.LeaveRequests.AddRange(sampleRequests);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded Leave Requests.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding database.");
            throw;
        }
    }
}
