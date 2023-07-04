using FluentValidation;
using RemoteMaster.Client.Models;

namespace RemoteMaster.Client.FluentValidators;

public class FolderValidator : AbstractValidator<Folder>
{
    public FolderValidator()
    {
        RuleFor(vm => vm.Name)
            .NotEmpty()
            .MaximumLength(15);
    }
}
