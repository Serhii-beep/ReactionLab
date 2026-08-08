using FluentValidation;
using ReactionLab.Application.Features.Elements.Commands;

namespace ReactionLab.Application.Features.Elements.Validators;

public class UpdateElementValidator : AbstractValidator<UpdateElementCommand>
{
    public UpdateElementValidator()
    {
        RuleFor(x => x.Element.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(50)
            .WithMessage("Name must not exceed 50 characters");

        RuleFor(x => x.Element.AtomicMass)
            .GreaterThan(0)
            .WithMessage("Atomic mass must be greater than 0");

        RuleFor(x => x.Element.Electronegativity)
            .InclusiveBetween(0m, 4m)
            .When(x => x.Element.Electronegativity.HasValue)
            .WithMessage("Electronegativity must be between 0 and 4");

        RuleFor(x => x.Element.DisplayColor)
            .NotEmpty()
            .WithMessage("Display color is required")
            .Matches("^#[0-9A-Fa-f]{6}$")
            .WithMessage("Display color must be a valid hex color (e.g., #FF5733)");

        RuleFor(x => x.Element.Radius3D)
            .GreaterThan(0)
            .WithMessage("3D radius must be greater than 0");
    }
}
