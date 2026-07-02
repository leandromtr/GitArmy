using FluentValidation;
using GitArmy.Domain.ValueObjects;

namespace GitArmy.Application.Commands.AnalyseProfile;

public class AnalyseProfileCommandValidator : AbstractValidator<AnalyseProfileCommand>
{
    public AnalyseProfileCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(39).WithMessage("Username cannot exceed 39 characters.")
            .Must(BeValidGitHubUsername).WithMessage("Invalid GitHub username format.");
    }

    private static bool BeValidGitHubUsername(string username)
    {
        try
        {
            GitHubUsername.Create(username);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
