using FluentValidation;
using ReactionLab.Application.Features.Reactions.Commands;

namespace ReactionLab.Application.Features.Reactions.Validators;

public class UpdateReactionValidator : AbstractValidator<UpdateReactionCommand>
{
    public UpdateReactionValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Reaction ID is required");

        RuleFor(x => x.Reaction.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Reaction.Equation)
            .NotEmpty()
            .WithMessage("Equation is required")
            .MaximumLength(500)
            .WithMessage("Equation must not exceed 500 characters");

        RuleFor(x => x.Reaction.EquationBalanced)
            .MaximumLength(500)
            .When(x => x.Reaction.EquationBalanced is not null)
            .WithMessage("Balanced equation must not exceed 500 characters");

        RuleFor(x => x.Reaction.Category)
            .MaximumLength(100)
            .When(x => x.Reaction.Category is not null)
            .WithMessage("Category must not exceed 100 characters");

        RuleFor(x => x.Reaction.CatalystInfo)
            .MaximumLength(200)
            .When(x => x.Reaction.CatalystInfo is not null)
            .WithMessage("Catalyst info must not exceed 200 characters");

        RuleFor(x => x.Reaction.AnimationType)
            .MaximumLength(50)
            .When(x => x.Reaction.AnimationType is not null)
            .WithMessage("Animation type must not exceed 50 characters");

        RuleFor(x => x.Reaction.EffectPreset)
            .MaximumLength(50)
            .When(x => x.Reaction.EffectPreset is not null)
            .WithMessage("Effect preset must not exceed 50 characters");

        RuleFor(x => x.Reaction.DifficultyLevel)
            .InclusiveBetween(1, 5)
            .WithMessage("Difficulty level must be between 1 and 5");

        RuleFor(x => x.Reaction.RequiredTemperature)
            .GreaterThan(0)
            .When(x => x.Reaction.RequiredTemperature.HasValue)
            .WithMessage("Required temperature must be greater than 0");

        RuleFor(x => x.Reaction.RequiredPressure)
            .GreaterThan(0)
            .When(x => x.Reaction.RequiredPressure.HasValue)
            .WithMessage("Required pressure must be greater than 0");

        RuleFor(x => x.Reaction.AnimationDurationMs)
            .GreaterThan(0)
            .When(x => x.Reaction.AnimationDurationMs.HasValue)
            .WithMessage("Animation duration must be greater than 0");
    }
}
