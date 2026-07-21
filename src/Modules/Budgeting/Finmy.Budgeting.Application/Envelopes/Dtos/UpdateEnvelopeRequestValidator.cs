using FluentValidation;

namespace Finmy.Budgeting.Application.Envelopes.Dtos;

public class UpdateEnvelopeRequestValidator : AbstractValidator<UpdateEnvelopeRequest>
{
    public UpdateEnvelopeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Allocated).GreaterThan(0m);
        RuleFor(x => x.PeriodEnd).GreaterThan(x => x.PeriodStart);
    }
}
