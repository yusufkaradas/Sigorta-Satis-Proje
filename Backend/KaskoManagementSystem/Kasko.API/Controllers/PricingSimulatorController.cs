using Kasko.Business.Exceptions;
using Kasko.Business.Pricing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class PricingSimulatorController : ControllerBase
{
    private readonly IPricingService _pricingService;

    public PricingSimulatorController(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    public class SimulationRequest
    {
        public decimal MarketValue { get; set; }
        public int ModelYear { get; set; }
        public int DriverAge { get; set; }
        public string Usage { get; set; } = "PRIVATE";
        public int ClaimsCount { get; set; }
        public string Region { get; set; } = "NORMAL";
        public Guid? PackageId { get; set; }
        public decimal Deductible { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Simulate([FromBody] SimulationRequest request)
    {
        if (request.MarketValue <= 0)
        {
            throw new BadRequestException("Araç değeri sıfırdan büyük olmalıdır.");
        }

        if (request.DriverAge < 18 || request.DriverAge > 99)
        {
            throw new BadRequestException("Sürücü yaşı 18 ile 99 arasında olmalıdır.");
        }

        var result = await _pricingService.CalculateAsync(new PricingRequest
        {
            MarketValue = request.MarketValue,
            ModelYear = request.ModelYear,
            DriverAge = request.DriverAge,
            Usage = request.Usage,
            ClaimsCount = request.ClaimsCount,
            Region = request.Region,
            PackageId = request.PackageId,
            Deductible = request.Deductible
        });

        var vehicleAge = Math.Max(0, DateTime.UtcNow.Year - request.ModelYear);

        var ageCode = vehicleAge switch
        {
            <= 2 => "AGE_0_2",
            <= 5 => "AGE_3_5",
            <= 8 => "AGE_6_8",
            <= 12 => "AGE_9_12",
            _ => "AGE_13_15"
        };

        var driverCode = request.DriverAge switch
        {
            >= 25 => "DRIVER_25_PLUS",
            >= 21 => "DRIVER_21_24",
            _ => "DRIVER_18_20"
        };

        var claimsCode = request.ClaimsCount switch
        {
            <= 0 => "CLAIMS_0",
            1 => "CLAIMS_1",
            2 => "CLAIMS_2",
            _ => "CLAIMS_3_PLUS"
        };

        var steps = new List<object>();
        var running = result.MarketValue * result.BaseRate;

        steps.Add(new { Label = "Temel Prim", Code = "BASE_KASKO_RATE", Detail = $"Araç Değeri × Temel Oran (%{result.BaseRate * 100:0.##})", Factor = result.BaseRate, Subtotal = decimal.Round(running, 2) });

        void AddFactor(string label, string code, string detail, decimal factor)
        {
            running *= factor;
            steps.Add(new { Label = label, Code = code, Detail = detail, Factor = factor, Subtotal = decimal.Round(running, 2) });
        }

        AddFactor("Araç Yaşı", ageCode, $"{vehicleAge} yaşında araç", result.AgeFactor);
        AddFactor("Kullanım Şekli", $"USAGE_{request.Usage.ToUpperInvariant()}", request.Usage.ToUpperInvariant() == "COMMERCIAL" ? "Ticari" : "Hususi", result.UsageFactor);
        AddFactor("Hasar Geçmişi", claimsCode, $"{request.ClaimsCount} hasar", result.ClaimsFactor);
        AddFactor("Sürücü Yaşı", driverCode, $"{request.DriverAge} yaş", result.DriverFactor);
        AddFactor("Bölge", $"REGION_{request.Region.ToUpperInvariant()}", request.Region.ToUpperInvariant() switch { "LOW" => "Düşük risk", "HIGH" => "Yüksek risk", _ => "Normal risk" }, result.RegionFactor);
        AddFactor("Paket", "PACKAGE", "Paket katsayısı", result.PackageFactor);
        AddFactor("Muafiyet", "DEDUCTIBLE", request.Deductible > 0 ? $"%{request.Deductible:0} muafiyet" : "Muafiyetsiz", result.DeductibleFactor);

        return Ok(new
        {
            result.MarketValue,
            result.BasePremium,
            result.RiskAdjustedPremium,
            result.CoveragePremium,
            result.TotalPremium,
            NetPremium = decimal.Round(result.TotalPremium / 1.05m, 2, MidpointRounding.AwayFromZero),
            Tax = result.TotalPremium - decimal.Round(result.TotalPremium / 1.05m, 2, MidpointRounding.AwayFromZero),
            Steps = steps,
            Coverages = result.Coverages
        });
    }
}
