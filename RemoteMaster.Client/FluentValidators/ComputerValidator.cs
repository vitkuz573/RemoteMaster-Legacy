using FluentValidation;
using RemoteMaster.Client.Models;

namespace RemoteMaster.Client.FluentValidators;

public class ComputerValidator : AbstractValidator<Computer>
{
    public ComputerValidator()
    {
        RuleFor(vm => vm.Name)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(vm => vm.IPAddress)
            .NotEmpty();
    }
}
