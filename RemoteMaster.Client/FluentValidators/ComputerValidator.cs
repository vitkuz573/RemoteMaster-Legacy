using FluentValidation;
using RemoteMaster.Client.Models;
using System.Net;

namespace RemoteMaster.Client.FluentValidators;

public class ComputerValidator : AbstractValidator<Computer>
{
    public ComputerValidator()
    {
        RuleFor(vm => vm.Name)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(vm => vm.IPAddress)
            .NotEmpty()
            .Must(BeAValidIp).WithMessage("IP address is invalid.");
    }

    private bool BeAValidIp(string ip)
    {
        return IPAddress.TryParse(ip, out _);
    }
}
