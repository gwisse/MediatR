using FluentValidation;

namespace MediatR.DemoApp;

public sealed class GreetingRequestValidator : AbstractValidator<GreetingRequest>
{
    public GreetingRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().WithMessage("Name is required.");
    }
}
