using Kasko.Business.Services;
using Kasko.Entities.Enums;

namespace Kasko.Business.Tests.Services;

public class VehicleTechnicalSpecificationParserTests
{
    [Fact]
    public void Parse_MitoTurboTct_ShouldExtractTechnicalInformation()
    {
        var result =
            VehicleTechnicalSpecificationParser.Parse(
                "MITO 1.4 TB MULTIAIR (135) TCT CITY");

        Assert.Equal(1.4m, result.EngineVolume);

        Assert.Equal(
            135,
            result.EnginePower);

        Assert.Equal(
            FuelType.Gasoline,
            result.FuelType);

        Assert.Equal(
            TransmissionType.Automatic,
            result.TransmissionType);
    }


    [Fact]
    public void Parse_DieselManual_ShouldExtractTechnicalInformation()
    {
        var result =
            VehicleTechnicalSpecificationParser.Parse(
                "JEEP COMPASS 1.6 120 MT LONGITUDE DIZEL E6");

        Assert.Equal(
            1.6m,
            result.EngineVolume);

        Assert.Equal(
            120,
            result.EnginePower);

        Assert.Equal(
            FuelType.Diesel,
            result.FuelType);

        Assert.Equal(
            TransmissionType.Manual,
            result.TransmissionType);
    }


    [Fact]
    public void Parse_HybridDct_ShouldExtractTechnicalInformation()
    {
        var result =
            VehicleTechnicalSpecificationParser.Parse(
                "TONALE EDIZIONE SPECIALE 1.5 HYBRID 160 DCT");

        Assert.Equal(
            1.5m,
            result.EngineVolume);

        Assert.Equal(
            160,
            result.EnginePower);

        Assert.Equal(
            FuelType.Hybrid,
            result.FuelType);

        Assert.Equal(
            TransmissionType.Automatic,
            result.TransmissionType);
    }


    [Fact]
    public void Parse_ElectricBatteryCapacity_ShouldNotTreatAhAsHorsePower()
    {
        var result =
            VehicleTechnicalSpecificationParser.Parse(
                "i3 (120 Ah) EDITION ELECTRIC");

        Assert.Null(
            result.EnginePower);

        Assert.Equal(
            FuelType.Electric,
            result.FuelType);
    }


    [Fact]
    public void Parse_GiuliaAutomatic_ShouldDetectAutomatic()
    {
        var result =
            VehicleTechnicalSpecificationParser.Parse(
                "GIULIA 2.0 280 VELOCE AWD AT");

        Assert.Equal(
            2.0m,
            result.EngineVolume);

        Assert.Equal(
            280,
            result.EnginePower);

        Assert.Equal(
            TransmissionType.Automatic,
            result.TransmissionType);
    }


    [Fact]
    public void Parse_Amarok_ShouldExtractDieselAutomatic()
    {
        var result =
            VehicleTechnicalSpecificationParser.Parse(
                "AMAROK AVENTURA 4MOTION 3.0 TDI V6 240 OV");

        Assert.Equal(
            3.0m,
            result.EngineVolume);

        Assert.Equal(
            240,
            result.EnginePower);

        Assert.Equal(
            FuelType.Diesel,
            result.FuelType);

        Assert.Equal(
            TransmissionType.Automatic,
            result.TransmissionType);
    }


    [Fact]
    public void Parse_ExplicitVehicleType_ShouldExtractVehicleType()
    {
        var result =
            VehicleTechnicalSpecificationParser.Parse(
                "FORD CAPRI PREMIUM 125KW AT SUV");

        Assert.Equal(
            VehicleType.SUV,
            result.VehicleType);
    }


    [Fact]
    public void Parse_UnknownTechnicalInformation_ShouldReturnNull()
    {
        var result =
            VehicleTechnicalSpecificationParser.Parse(
                "SPECIAL EDITION");

        Assert.Null(
            result.EngineVolume);

        Assert.Null(
            result.EnginePower);

        Assert.Null(
            result.FuelType);

        Assert.Null(
            result.TransmissionType);

        Assert.Null(
            result.VehicleType);
    }
}