using System.Linq.Expressions;
using Kasko.Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using Kasko.Entities.Concrete;

namespace Kasko.DataAccess;

public class KaskoContext : DbContext
{
    public KaskoContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<Customer> Customers { get; set; }

    public DbSet<Vehicle> Vehicles { get; set; }

    public DbSet<Quote> Quotes { get; set; }

    public DbSet<Policy> Policies { get; set; }

    public DbSet<Payment> Payments { get; set; }

    public DbSet<Coverage> Coverages { get; set; }
    
    public DbSet<QuoteCoverage> QuoteCoverages { get; set; }
    public DbSet<VehicleValueCatalog> VehicleValueCatalogs { get; set; }

    public DbSet<PricingRule> PricingRules => Set<PricingRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(x => x.IdentityNumber)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(x => x.Email)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.Property(x => x.EngineVolume)
                  .HasPrecision(4, 2);

            entity.Property(x => x.PlateNumber)
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(x => x.VIN)
                  .HasMaxLength(17)
                  .IsRequired();
            entity.HasIndex(x => x.PlateNumber)
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");

            
            entity.HasIndex(x => x.VIN)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            entity.Property(x => x.Brand)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Model)
                .HasMaxLength(50)
                .IsRequired();
            
           entity.Property(x => x.Color)
                .HasMaxLength(30)
                .IsRequired();
            
            entity.Property(x => x.MarketValue)
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Quote>(entity =>
        {
            entity.Property(x => x.QuoteNumber)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.PremiumAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.HasIndex(x => x.QuoteNumber)
                .IsUnique();

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Vehicle)
                .WithMany()
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Policy>(entity =>
        {
            entity.ToTable("Policies");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.PolicyNumber)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(x => x.PremiumAmount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Status)
                .IsRequired();

            entity.Property(x => x.StartDate)
                .IsRequired();

            entity.Property(x => x.EndDate)
                .IsRequired();

            entity.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            
            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            
            entity.HasOne(x => x.Vehicle)
                .WithMany()
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            
            entity.HasOne(x => x.Quote)
                .WithMany()
                .HasForeignKey(x => x.QuoteId)
                .OnDelete(DeleteBehavior.Restrict);

           entity.HasIndex(x => x.PolicyNumber)
                .IsUnique();
           
           entity.HasIndex(x => x.QuoteId)
                 .IsUnique()
                 .HasFilter("[IsDeleted] = 0");
            });

        modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(x => x.TransactionNumber)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .IsRequired();

                entity.HasIndex(x => x.TransactionNumber)
                    .IsUnique();

                entity.HasOne(x => x.Policy)
                    .WithMany()
                    .HasForeignKey(x => x.PolicyId)
                    .OnDelete(DeleteBehavior.Restrict);
            
        });
        modelBuilder.Entity<Coverage>(entity =>
        {
            entity.ToTable("Coverages");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.PricingType)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(x => x.BasePrice)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.Rate)
                .HasPrecision(9, 4);

            entity.Property(x => x.DefaultLimit)
                .HasPrecision(18, 2);

            entity.Property(x => x.IsRequired)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.HasIndex(x => x.Name)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });
        modelBuilder.Entity<QuoteCoverage>(entity =>
        {
            entity.ToTable("QuoteCoverages");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.CalculatedPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.Limit)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.Quote)
                .WithMany(x => x.QuoteCoverages)
                .HasForeignKey(x => x.QuoteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Coverage)
                .WithMany(x => x.QuoteCoverages)
                .HasForeignKey(x => x.CoverageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.QuoteId,
                x.CoverageId
            })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        });
        modelBuilder.Entity<VehicleValueCatalog>(entity =>
        {
            entity.ToTable("VehicleValueCatalogs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.BrandCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.TypeCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.BrandName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.TypeName)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.ModelYear)
                .IsRequired();

            entity.Property(x => x.Value)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.Source)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.EffectiveDate)
                .IsRequired();

            entity.Property(x => x.ImportedAt)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.BrandCode,
                x.TypeCode,
                x.ModelYear,
                x.EffectiveDate
            })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        });
        modelBuilder.Entity<PricingRule>(entity =>
        {
            entity.Property(x => x.Value)
                .HasPrecision(18, 4);

            entity.HasIndex(x => x.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });
        modelBuilder.Entity<PricingRule>().HasData(

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
         Code = "BASE_KASKO_RATE",
         Name = "Temel Kasko Oranı",
         Description = "Araç değerine uygulanacak temel kasko oranı.",
         Value = 0.0200m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
         Code = "AGE_0_2",
         Name = "0-2 Yaş Katsayısı",
         Value = 1.0000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
         Code = "AGE_3_5",
         Name = "3-5 Yaş Katsayısı",
         Value = 1.1000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
         Code = "AGE_6_8",
         Name = "6-8 Yaş Katsayısı",
         Value = 1.2000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
         Code = "AGE_9_12",
         Name = "9-12 Yaş Katsayısı",
         Value = 1.3500m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
         Code = "AGE_13_15",
         Name = "13-15 Yaş Katsayısı",
         Value = 1.5000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000007"),
         Code = "USAGE_PRIVATE",
         Name = "Özel Kullanım Katsayısı",
         Value = 1.0000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000008"),
         Code = "USAGE_COMMERCIAL",
         Name = "Ticari Kullanım Katsayısı",
         Value = 1.2500m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000009"),
         Code = "USAGE_RENTAL",
         Name = "Kiralık Kullanım Katsayısı",
         Value = 1.4000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000010"),
         Code = "DRIVER_25_PLUS",
         Name = "25+ Yaş Sürücü Katsayısı",
         Value = 1.0000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000011"),
         Code = "DRIVER_21_24",
         Name = "21-24 Yaş Sürücü Katsayısı",
         Value = 1.1500m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000012"),
         Code = "DRIVER_18_20",
         Name = "18-20 Yaş Sürücü Katsayısı",
         Value = 1.3000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000013"),
         Code = "CLAIMS_0",
         Name = "Hasarsızlık Katsayısı",
         Value = 0.9000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000014"),
         Code = "CLAIMS_1",
         Name = "1 Hasar Katsayısı",
         Value = 1.0000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000015"),
         Code = "CLAIMS_2",
         Name = "2 Hasar Katsayısı",
         Value = 1.1500m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000016"),
         Code = "CLAIMS_3_PLUS",
         Name = "3+ Hasar Katsayısı",
         Value = 1.3000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000017"),
         Code = "REGION_LOW",
         Name = "Düşük Bölge Riski",
         Value = 0.9500m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000018"),
         Code = "REGION_NORMAL",
         Name = "Normal Bölge Riski",
         Value = 1.0000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000019"),
         Code = "REGION_HIGH",
         Name = "Yüksek Bölge Riski",
         Value = 1.1000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1)
     }
 );
        ApplySoftDeleteQueryFilters(modelBuilder);
    }
    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(
                entityType.ClrType,
                "e");

            var property = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                new[] { typeof(bool) },
                parameter,
                Expression.Constant(nameof(BaseEntity.IsDeleted)));

            var condition = Expression.Equal(
                property,
                Expression.Constant(false));

            var lambda = Expression.Lambda(
                condition,
                parameter);

            entityType.SetQueryFilter(lambda);
        }
    }
}