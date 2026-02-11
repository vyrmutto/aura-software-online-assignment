using ClinicPOS.Application.Users.Dtos;
using FluentValidation;

namespace ClinicPOS.Application.Users.Validators;

public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    private static readonly string[] ValidRoles = ["Admin", "User", "Viewer"];

    public CreateUserValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Role).NotEmpty().Must(r => ValidRoles.Contains(r))
            .WithMessage("Role must be Admin, User, or Viewer");
        RuleFor(x => x.BranchIds).NotEmpty();
    }
}
