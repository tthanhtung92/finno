using FluentValidation;

namespace Finmy.Budgeting.Application.Envelopes.Dtos;

public class ListEnvelopesRequestValidator : AbstractValidator<ListEnvelopesRequest>
{
    public ListEnvelopesRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
