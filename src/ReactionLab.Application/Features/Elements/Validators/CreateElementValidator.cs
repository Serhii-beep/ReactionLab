using FluentValidation;
using ReactionLab.Application.Features.Elements.Commands;

namespace ReactionLab.Application.Features.Elements.Validators;

public class CreateElementValidator : AbstractValidator<CreateElementCommand>
{
    public CreateElementValidator()
    {
        RuleFor(x => x.Element.AtomicNumber)
            .InclusiveBetween(1, 118)
            .WithMessage("Atomic number must be between 1 and 118");

        RuleFor(x => x.Element.Symbol)
            .NotEmpty()
            .WithMessage("Symbol is required")
            .MaximumLength(3)
            .WithMessage("Symbol must not exceed 3 characters")
            .Matches("^[A-Z][a-z]{0, 2}$")
            .WithMessage("Symbol must start with uppercase letter, followed by up to 2 lowercase letters");

        RuleFor(x => x.Element.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(50)
            .WithMessage("Name must not exceed 50 characters");

        RuleFor(x => x.Element.AtomicMass)
            .GreaterThan(0)
            .WithMessage("Atomic mass must be greater than 0");

        RuleFor(x => x.Element.Period)
            .InclusiveBetween(1, 7)
            .WithMessage("Period must be between 1 and 7");

        RuleFor(x => x.Element.Group)
            .InclusiveBetween(1, 18)
            .When(x => x.Element.Group.HasValue)
            .WithMessage("Group must be between 1 and 18");

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
