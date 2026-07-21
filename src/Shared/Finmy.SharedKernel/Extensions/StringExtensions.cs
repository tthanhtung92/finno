namespace Finmy.SharedKernel.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Nếu chuỗi rỗng hoặc chỉ có khoảng trắng, trả về null. 
    /// Ngược lại, trả về chuỗi đã được Trim.
    /// </summary>
    public static string? TrimOrNull(this string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}