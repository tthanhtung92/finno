using Finmy.SharedKernel.Results;

namespace Finmy.Budgeting.Domain.Envelopes;

public static class EnvelopeErrors
{
    public static readonly Error NameEmpty = new(
        "Budgeting.NameEmpty",
        "Envelope name cannot be empty.",
        ErrorType.Validation);

    public static readonly Error NameTooLong = new(
        "Budgeting.NameTooLong",
        "Envelope name exceeds the maximum allowed length.",
        ErrorType.Validation);

    public static readonly Error CategoryRequired = new(
        "Budgeting.CategoryRequired",
        "An envelope must be assigned to a category.",
        ErrorType.Validation);

    public static readonly Error PeriodInvalid = new(
        "Budgeting.PeriodInvalid",
        "The envelope period is invalid.",
        ErrorType.Validation);

    public static readonly Error AllocatedNotPositive = new(
        "Budgeting.AllocatedNotPositive",
        "The allocated amount must be greater than zero.",
        ErrorType.Validation);

    public static Error NotFound(Guid envelopeId)
    {
        return new(
            "Budgeting.NotFound",
            $"Envelope with Id '{envelopeId}' was not found.",
            ErrorType.NotFound);
    }

    public static Error CategoryNotFound(Guid categoryId)
    {
        return new(
            "Budgeting.CategoryNotFound",
            $"Category with Id '{categoryId}' was not found.",
            ErrorType.Validation);
    }
}