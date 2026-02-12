using System.Text.RegularExpressions;
using FluentValidation;

namespace MezuroApp.Application.Dtos.Auth;

public class ResetPasswordRequestDto
{
  
    public string Email { get; set; } = default!;

    public string Token { get; set; } = default!;

     
    public string NewPassword { get; set; } = default!;
}
public class ResetPasswordRequestDtoValidation : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestDtoValidation()
    {


        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Email cannot be empty and must be valid.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Parol boş ola bilməz.")
            .MinimumLength(8).WithMessage("Parol ən az 8 simvol olmalıdır.")
            .Matches("[A-Z]").WithMessage("Parolda ən azı bir böyük hərf olmalıdır.")
            .Matches("[a-z]").WithMessage("Parolda ən azı bir kiçik hərf olmalıdır.")
            .Matches("[0-9]").WithMessage("Parolda ən azı bir rəqəm olmalıdır.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Parolda ən azı bir xüsusi simvol olmalıdır.");

    }
}