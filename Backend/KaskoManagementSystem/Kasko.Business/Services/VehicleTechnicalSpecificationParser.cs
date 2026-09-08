using System.Globalization;
using System.Text.RegularExpressions;
using Kasko.Entities.Enums;

namespace Kasko.Business.Services;

public sealed class VehicleTechnicalSpecification
{
    public decimal? EngineVolume { get; init; }

    public int? EnginePower { get; init; }

    public FuelType? FuelType { get; init; }

    public TransmissionType? TransmissionType { get; init; }

    public VehicleType? VehicleType { get; init; }
}

public static class VehicleTechnicalSpecificationParser
{
    private static readonly Regex EngineVolumeRegex =
        new(
            @"(?<![\d.,])(?<value>\d{1,2}(?:[.,]\d{1,3})?)(?=\s*(?:L|LT|LITRE|LITER)?\b)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ExplicitPowerRegex =
        new(
            @"(?<![\d.])(?<value>\d{2,3})\s*(?:HP|BHP|PS|BG)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ParenthesizedPowerRegex =
        new(
            @"\((?<value>\d{2,3})(?!\s*(?:AH|KW)\b)[^)]*\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PowerAfterEngineRegex =
        new(
            @"(?<![\d])(?<value>\d{2,3})(?=\s+(?:[A-Z][A-Z0-9+.\-]*\s+){0,3}(?:MT|AT|DCT|DSG|TCT|CVT|EDC|EAT|PDK|OV)\b)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DieselRegex =
        new(
            @"\b(DIZEL|DIESEL|TDI|JTD|HDI|CDI|CRDI|DCI|TDCI|D-4D|DDI|BLUEHDI)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HybridRegex =
        new(
            @"\b(HYBRID|HIBRIT|PHEV|PLUG-IN\s+HYBRID|MHEV|HEV|E-POWER|EPOWER)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ElectricRegex =
        new(
            @"\b(ELECTRIC|ELEKTRIK|BEV)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex GasolineRegex =
        new(
            @"\b(BENZIN|GASOLINE|PETROL|TB|TFSI|TSI|TCE|PURETECH|MPI|GDI)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AutomaticRegex =
        new(
            @"\b(DCT|DSG|TCT|CVT|EDC|EAT|AT|AUTO|AUTOMATIC|PDK|TIPTRONIC|S-TRONIC|STRONIC|XTRONIC|MULTITRONIC|ECVT|DHT|OV)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ManualRegex =
        new(
            @"\b(MT|MANUEL|MANUAL)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SedanRegex =
        new(
            @"\b(SEDAN)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HatchbackRegex =
        new(
            @"\b(HATCHBACK)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SuvRegex =
        new(
            @"\b(SUV)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PickupRegex =
        new(
            @"\b(PICKUP|PICK-UP)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CoupeRegex =
        new(
            @"\b(COUPE)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ConvertibleRegex =
        new(
            @"\b(CABRIO|CABRIOLET|CONVERTIBLE|ROADSTER)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex VanRegex =
        new(
            @"\b(VAN|MINIVAN|MPV)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);


    public static VehicleTechnicalSpecification Parse(
        string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return new VehicleTechnicalSpecification();
        }

        var normalized =
            Normalize(typeName);

        return new VehicleTechnicalSpecification
        {
            EngineVolume =
                ParseEngineVolume(normalized),

            EnginePower =
                ParseEnginePower(
                    normalized),

            FuelType =
                ParseFuelType(normalized),

            TransmissionType =
                ParseTransmissionType(
                    normalized),

            VehicleType =
                ParseVehicleType(normalized)
        };
    }


    private static string Normalize(
        string value)
    {
        return value
            .Replace('İ', 'I')
            .Replace('ı', 'i')
            .Trim()
            .ToUpperInvariant();
    }


    private static decimal? ParseEngineVolume(
        string value)
    {
        var match =
            EngineVolumeRegex.Match(value);

        if (!match.Success)
        {
            return null;
        }

        var raw =
            match.Groups["value"].Value
                .Replace(',', '.');

        if (!decimal.TryParse(
                raw,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var result))
        {
            return null;
        }

        /*
         * Araç motor hacmi için güvenli
         * pratik aralık.
         */
        if (result < 0.5m ||
            result > 16m)
        {
            return null;
        }

        return result;
    }


    private static int? ParseEnginePower(
        string value)
    {
        /*
         * 1. Öncelik:
         * Açık HP/BHP/PS/BG ifadesi.
         */
        var explicitMatch =
            ExplicitPowerRegex.Match(value);

        if (explicitMatch.Success &&
            TryGetPower(
                explicitMatch.Groups["value"].Value,
                out var explicitPower))
        {
            return explicitPower;
        }

        /*
         * 2. Öncelik:
         * (135), (155), (190) gibi açık
         * parantez içi güç ifadeleri.
         */
        var parenthesizedMatch =
            ParenthesizedPowerRegex.Match(value);

        if (parenthesizedMatch.Success &&
            TryGetPower(
                parenthesizedMatch.Groups["value"].Value,
                out var parenthesizedPower))
        {
            return parenthesizedPower;
        }

        /*
         * 3. Öncelik:
         * 1.6 JTD 105 MT
         * 1.5 HYBRID 160 DCT
         * gibi güç + şanzıman desenleri.
         */
        var afterEngineMatch =
            PowerAfterEngineRegex.Match(value);

        if (afterEngineMatch.Success &&
            TryGetPower(
                afterEngineMatch.Groups["value"].Value,
                out var afterEnginePower))
        {
            return afterEnginePower;
        }

        /*
         * Belirsiz durumda tahmin yapmıyoruz.
         */
        return null;
    }


    private static bool TryGetPower(
        string value,
        out int power)
    {
        power = 0;

        if (!int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            return false;
        }

        /*
         * Küçük sayıları güç olarak
         * yanlış kabul etmemek için.
         */
        if (parsed < 40 ||
            parsed > 600)
        {
            return false;
        }

        power = parsed;

        return true;
    }


    private static FuelType? ParseFuelType(
        string value)
    {
        /*
         * Önce HYBRID.
         * Çünkü plug-in hybrid gibi ifadelerde
         * birden fazla enerji kelimesi olabilir.
         */
        if (HybridRegex.IsMatch(value))
        {
            return FuelType.Hybrid;
        }

        if (ElectricRegex.IsMatch(value))
        {
            return FuelType.Electric;
        }

        if (DieselRegex.IsMatch(value))
        {
            return FuelType.Diesel;
        }

        if (GasolineRegex.IsMatch(value))
        {
            return FuelType.Gasoline;
        }

        return null;
    }


    private static TransmissionType? ParseTransmissionType(
        string value)
    {
        if (AutomaticRegex.IsMatch(value))
        {
            return TransmissionType.Automatic;
        }

        if (ManualRegex.IsMatch(value))
        {
            return TransmissionType.Manual;
        }

        return null;
    }


    private static VehicleType? ParseVehicleType(
        string value)
    {
        if (PickupRegex.IsMatch(value))
        {
            return VehicleType.Pickup;
        }

        if (SuvRegex.IsMatch(value))
        {
            return VehicleType.SUV;
        }

        if (SedanRegex.IsMatch(value))
        {
            return VehicleType.Sedan;
        }

        if (HatchbackRegex.IsMatch(value))
        {
            return VehicleType.Hatchback;
        }

        if (CoupeRegex.IsMatch(value))
        {
            return VehicleType.Coupe;
        }

        if (ConvertibleRegex.IsMatch(value))
        {
            return VehicleType.Convertible;
        }

        if (VanRegex.IsMatch(value))
        {
            return VehicleType.Van;
        }

        return null;
    }
}