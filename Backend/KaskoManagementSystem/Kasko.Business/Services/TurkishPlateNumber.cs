using System.Text.RegularExpressions;

namespace Kasko.Business.Services;

public static class TurkishPlateNumber
{
    private static readonly Regex ValidPlateRegex =
        new(
            @"^(?<city>0[1-9]|[1-7][0-9]|8[01])(?:(?<letters>[A-Z])(?<numbers>[0-9]{4,5})|(?<letters>[A-Z]{2})(?<numbers>[0-9]{3,4})|(?<letters>[A-Z]{3})(?<numbers>[0-9]{2,3}))$",
            RegexOptions.Compiled);

    public static string Normalize(string? plateNumber)
    {
        if (string.IsNullOrWhiteSpace(plateNumber))
        {
            return string.Empty;
        }

        return new string(
            plateNumber
                .Trim()
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
    }

    public static bool IsValid(string? plateNumber)
    {
        var normalized =
            Normalize(plateNumber);

        if (normalized.Length < 5 ||
            normalized.Length > 10)
        {
            return false;
        }

        return ValidPlateRegex.IsMatch(normalized);
    }

    public static string FormatForDisplay(
        string? plateNumber)
    {
        var normalized =
            Normalize(plateNumber);

        if (!IsValid(normalized))
        {
            return normalized;
        }

        var match =
            ValidPlateRegex.Match(normalized);

        var city =
            match.Groups["city"].Value;

        var letters =
            match.Groups["letters"].Value;

        var numbers =
            match.Groups["numbers"].Value;

        return $"{city} {letters} {numbers}";
    }
}