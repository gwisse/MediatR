using FluentValidation;

namespace TestApp;

public sealed class GreetingRequestValidator : AbstractValidator<GreetingRequest>
{
    public GreetingRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().WithMessage("Name is required.");
    }
}
