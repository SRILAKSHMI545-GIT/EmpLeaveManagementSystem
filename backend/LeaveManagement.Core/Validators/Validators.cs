using FluentValidation;
using LeaveManagement.Core.DTOs.Auth;
using LeaveManagement.Core.DTOs.Leave;

namespace LeaveManagement.Core.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required").MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email is required");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("Password must be at least 6 characters long");
        RuleFor(x => x.Role).Must(r => r == "Employee" || r == "Manager" || r == "Admin")
            .WithMessage("Role must be Employee, Manager, or Admin");
    }
}

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email is required");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
    }
}

public class CreateLeaveRequestDtoValidator : AbstractValidator<CreateLeaveRequestDto>
{
    public CreateLeaveRequestDtoValidator()
    {
        RuleFor(x => x.LeaveTypeId).GreaterThan(0).WithMessage("Leave type is required");
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required");
        RuleFor(x => x.EndDate).NotEmpty().WithMessage("End date is required")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date cannot be earlier than start date");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason is required").MaximumLength(500);
    }
}
