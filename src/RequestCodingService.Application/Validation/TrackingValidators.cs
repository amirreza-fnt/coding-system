using FluentValidation;
using RequestCodingService.Application.Dtos;

namespace RequestCodingService.Application.Validation;

public sealed class CreateTrackingRequestValidator : AbstractValidator<CreateTrackingRequestDto>
{
    public CreateTrackingRequestValidator()
    {
        RuleFor(x => x.SystemId).GreaterThan(0).WithMessage("SystemId is required.");
        RuleFor(x => x.NationalCode)
            .NotEmpty().WithMessage("National code is required.")
            .Matches(@"^\d{10}$").WithMessage("National code must be exactly 10 digits.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Mobile)
            .Matches(@"^09\d{9}$").When(x => !string.IsNullOrWhiteSpace(x.Mobile))
            .WithMessage("Mobile must be a valid Iranian mobile number (09xxxxxxxxx).");
        RuleFor(x => x.Landline).MaximumLength(20);
        RuleFor(x => x.Description).MaximumLength(4000);
    }
}

public sealed class OperatorSearchValidator : AbstractValidator<OperatorSearchQueryDto>
{
    public OperatorSearchValidator()
    {
        RuleFor(x => x.SystemId).GreaterThan(0);
        RuleFor(x => x.Counter)
            .InclusiveBetween(1, 99999).When(x => x.Counter.HasValue);
        RuleFor(x => x.NationalCode)
            .Matches(@"^\d{10}$").When(x => !string.IsNullOrWhiteSpace(x.NationalCode));
    }
}
