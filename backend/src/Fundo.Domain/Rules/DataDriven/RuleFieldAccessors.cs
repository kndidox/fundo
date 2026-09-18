using System.Globalization;

namespace Fundo.Domain.Rules.DataDriven;

/// <summary>
/// Maps a data-driven rule's <see cref="RuleDefinition.Field"/> (a plain string, e.g. "State")
/// to how it's actually read off an <see cref="ApplicationSubmission"/>. Referencing an existing
/// field in a new rule is pure data (a new row); making a new field available to rules needs one
/// line here.
/// </summary>
public static class RuleFieldAccessors
{
    private static readonly IReadOnlyDictionary<string, Func<ApplicationSubmission, string>> Accessors =
        new Dictionary<string, Func<ApplicationSubmission, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["FirstName"] = s => s.FirstName,
            ["LastName"] = s => s.LastName,
            ["City"] = s => s.City,
            ["State"] = s => s.State,
            ["ZipCode"] = s => s.ZipCode,
            ["CompanyName"] = s => s.CompanyName,
            ["Ssn"] = s => s.Ssn,
            ["RequestedAmount"] = s => s.RequestedAmount.ToString(CultureInfo.InvariantCulture)
        };

    public static bool TryGetValue(string field, ApplicationSubmission submission, out string value)
    {
        if (Accessors.TryGetValue(field, out var accessor))
        {
            value = accessor(submission);
            return true;
        }

        value = string.Empty;
        return false;
    }
}
