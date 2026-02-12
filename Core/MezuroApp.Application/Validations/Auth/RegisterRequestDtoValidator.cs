using FluentValidation;
using MezuroApp.Application.Dtos.Auth;

namespace MezuroApp.Application.Validations.Auth
{
    public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email boş ola bilməz.")
                .EmailAddress().WithMessage("Düzgün bir email formatı daxil edin.")
                .MaximumLength(256).WithMessage("Email 256 simvoldan uzun ola bilməz.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Parol boş ola bilməz.")
                .MinimumLength(8).WithMessage("Parol ən az 8 simvol olmalıdır.")
                .Matches("[A-Z]").WithMessage("Parolda ən azı bir böyük hərf olmalıdır.")
                .Matches("[a-z]").WithMessage("Parolda ən azı bir kiçik hərf olmalıdır.")
                .Matches("[0-9]").WithMessage("Parolda ən azı bir rəqəm olmalıdır.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Parolda ən azı bir xüsusi simvol olmalıdır.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ad boş ola bilməz.")
                .MaximumLength(100).WithMessage("Ad 100 simvoldan uzun ola bilməz.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Soyad boş ola bilməz.")
                .MaximumLength(100).WithMessage("Soyad 100 simvoldan uzun ola bilməz.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Telefon nömrəsi boş ola bilməz.")
                .Matches(@"^\+994[0-9]{9}$").WithMessage("Telefon nömrəsi düzgün formatda olmalıdır (+994XXXXXXXXX).");
        }
    }
}