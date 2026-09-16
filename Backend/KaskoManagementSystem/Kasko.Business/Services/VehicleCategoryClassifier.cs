namespace Kasko.Business.Services;

public static class VehicleCategoryClassifier
{
    public const string Otomobil = "Otomobil";

    public static readonly string[] Categories =
    {
        "Otomobil",
        "Kamyonet",
        "Kamyon",
        "Cekici",
        "Minibus",
        "Otobus",
        "Motosiklet",
        "Traktor",
        "Diger"
    };

    private static readonly (string Category, string[] Terms)[] Rules =
    {
        ("Traktor", new[] { "ZIRAI TRAKTOR", "TRAKTOR", "TRACTOR" }),
        ("Motosiklet", new[] { "MOTORSIKLET", "MOTOSIKLET", "MOTORCYCLE", "SCOOTER", "ATV", "QUAD" }),
        ("Diger", new[] { "KARAVAN", "MOTOKARAVAN", "MOTORHOME", "CARAVAN", "ROMORK", "DORSE", "TREYLER" }),
        ("Cekici", new[] { "CEKICI", "TRACTOR HEAD" }),
        ("Otobus", new[] { "OTOBUS", "MIDIBUS", "AUTOBUS" }),
        ("Minibus", new[] { "MINIBUS" }),
        ("Kamyonet", new[] { "KAMYONET", "PANELVAN", "PANEL VAN", "PICKUP", "PICK-UP", "PICK UP", "KAPALI KASA", "ACIK KASA" }),
        ("Kamyon", new[] { "KAMYON", "TRUCK" })
    };

    public static string Classify(string? typeName)
    {
        return Classify(null, typeName);
    }

    public static string Classify(string? brandName, string? typeName)
    {
        var value =
            $"{Normalize(brandName)} {Normalize(typeName)}".Trim();

        if (value.Length == 0)
        {
            return Otomobil;
        }

        foreach (var rule in Rules)
        {
            if (rule.Terms.Any(value.Contains))
            {
                return rule.Category;
            }
        }

        return Otomobil;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Trim()
            .ToUpperInvariant()
            .Replace('İ', 'I')
            .Replace('Ş', 'S')
            .Replace('Ğ', 'G')
            .Replace('Ü', 'U')
            .Replace('Ö', 'O')
            .Replace('Ç', 'C')
            .Replace('ı', 'I')
            .Replace('İ', 'I');
    }
}
