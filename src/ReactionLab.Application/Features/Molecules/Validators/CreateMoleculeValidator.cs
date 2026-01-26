using FluentValidation;
using ReactionLab.Application.Features.Molecules.Commands;

namespace ReactionLab.Application.Features.Molecules.Validators;

public class CreateMoleculeValidator : AbstractValidator<CreateMoleculeCommand>
{
    public CreateMoleculeValidator()
    {
        RuleFor(x => x.Molecule.Formula)
            .NotEmpty()
            .WithMessage("Formula is required")
            .MaximumLength(100)
            .WithMessage("Formula must not exceed 100 characters");

        RuleFor(x => x.Molecule.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Molecule.IUPACName)
            .MaximumLength(300)
            .When(x => x.Molecule.IUPACName is not null)
            .WithMessage("IUPAC name must not exceed 300 characters");

        RuleFor(x => x.Molecule.MolecularWeight)
            .GreaterThan(0)
            .When(x => x.Molecule.MolecularWeight.HasValue)
            .WithMessage("Molecular weight must be greater than 0");

        RuleFor(x => x.Molecule.Category)
            .MaximumLength(100)
            .When(x => x.Molecule.Category is not null)
            .WithMessage("Category must not exceed 100 characters");

        RuleFor(x => x.Molecule.ImageUrl)
            .MaximumLength(500)
            .When(x => x.Molecule.ImageUrl is not null)
            .WithMessage("Image URL must not exceed 500 characters");

        RuleFor(x => x.Molecule.Model3DUrl)
            .MaximumLength(500)
            .When(x => x.Molecule.Model3DUrl is not null)
            .WithMessage("3D Model URL must not exceed 500 characters");

        RuleForEach(x => x.Molecule.Elements)
            .ChildRules(element =>
            {
                element.RuleFor(e => e.ElementId)
                    .NotEmpty()
                    .WithMessage("Element ID is required");

                element.RuleFor(e => e.Count)
                .GreaterThan(0)
                .WithMessage("Element count must be greater than 0");
            })
            .When(x => x.Molecule.Elements is not null);
    }
}