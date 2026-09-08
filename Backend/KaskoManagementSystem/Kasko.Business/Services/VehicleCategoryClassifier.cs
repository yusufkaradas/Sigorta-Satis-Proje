namespace Kasko.Business.Services;

public static class VehicleCategoryClassifier
{
    public static string Classify(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return "Diger";
        }

        var value = typeName.Trim().ToUpperInvariant();

        if (ContainsAny(value,
                "MOTOSIKLET",
                "MOTORCYCLE",
                "SCOOTER",
                "ATV",
                "QUAD"))
        {
            return "Motosiklet";
        }

        if (ContainsAny(value,
                "MOTOKARAVAN",
                "MOTORHOME"))
        {
            return "Motokaravan";
        }

        if (ContainsAny(value,
                "MINIBUS",
                "MIDIBUS"))
        {
            return "Minibus";
        }

        if (ContainsAny(value,
                "OTOBUS",
                "AUTOBUS"))
        {
            return "Otobus";
        }

        if (ContainsAny(value,
                "CEKICI",
                "TRACTOR HEAD"))
        {
            return "Cekici";
        }

        if (ContainsAny(value,
                "KAMYONET",
                "PANELVAN",
                "PANEL VAN"))
        {
            return "Kamyonet";
        }

        if (ContainsAny(value,
                "KAMYON",
                "TRUCK"))
        {
            return "Kamyon";
        }

        if (ContainsAny(value,
                "TRAKTOR",
                "TRACTOR"))
        {
            return "Traktor";
        }

        if (ContainsAny(value, "SEDAN"))
        {
            return "Sedan";
        }

        if (ContainsAny(value,
                "HATCHBACK",
                "HATCH"))
        {
            return "Hatchback";
        }

        if (ContainsAny(value, "SUV"))
        {
            return "SUV";
        }

        if (ContainsAny(value,
                "COUPE",
                "COUPÉ"))
        {
            return "Coupe";
        }

        if (ContainsAny(value,
                "CABRIO",
                "CONVERTIBLE"))
        {
            return "Convertible";
        }

        if (ContainsAny(value,
                "PICKUP",
                "PICK-UP"))
        {
            return "Pickup";
        }

        if (ContainsAny(value, "VAN"))
        {
            return "Van";
        }

        return "Diger";
    }

    private static bool ContainsAny(
        string value,
        params string[] terms)
    {
        return terms.Any(value.Contains);
    }
}