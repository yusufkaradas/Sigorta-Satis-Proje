using System.Linq.Expressions;
using Kasko.Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using Kasko.Entities.Concrete;
using Kasko.DataAccess.Auditing;

namespace Kasko.DataAccess;

public class KaskoContext : DbContext
{
    private readonly ICurrentUserProvider? _currentUserProvider;

    public KaskoContext(DbContextOptions options) : base(options)
    {
    }

    public KaskoContext(DbContextOptions options, ICurrentUserProvider currentUserProvider) : base(options)
    {
        _currentUserProvider = currentUserProvider;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInfo();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditInfo()
    {
        var user = _currentUserProvider?.GetCurrentUser();

        if (string.IsNullOrWhiteSpace(user))
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrWhiteSpace(entry.Entity.CreatedBy))
            {
                entry.Entity.CreatedBy = user;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedBy = user;
            }
        }
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

    public DbSet<CoverageOption> CoverageOptions { get; set; }

    public DbSet<TariffChangeRequest> TariffChangeRequests { get; set; }
    public DbSet<VehicleValueCatalog> VehicleValueCatalogs { get; set; }

    public DbSet<InsurancePackage> InsurancePackages { get; set; }

    public DbSet<PackageCoverage> PackageCoverages { get; set; }

    public DbSet<PricingRule> PricingRules => Set<PricingRule>();

    public DbSet<PricingRuleChangeRequest> PricingRuleChangeRequests { get; set; }

    public DbSet<PreviousPolicy> PreviousPolicies { get; set; }

    public DbSet<QuotePricingSnapshot> QuotePricingSnapshots { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    public DbSet<BrandSetting> BrandSettings { get; set; }

    public DbSet<PolicyCancellationRequest> PolicyCancellationRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<QuotePricingSnapshot>(entity =>
        {
            entity.Property(x => x.MarketValue)
                .HasPrecision(18, 2);

            entity.Property(x => x.BaseRate)
                .HasPrecision(18, 6);

            entity.Property(x => x.AgeFactor)
                .HasPrecision(18, 6);

            entity.Property(x => x.UsageFactor)
                .HasPrecision(18, 6);

            entity.Property(x => x.DriverFactor)
                .HasPrecision(18, 6);

            entity.Property(x => x.ClaimsFactor)
                .HasPrecision(18, 6);

            entity.Property(x => x.RegionFactor)
                .HasPrecision(18, 6);

            entity.Property(x => x.PackageFactor)
                .HasPrecision(18, 6);

            entity.Property(x => x.DeductibleFactor)
                .HasPrecision(18, 6);

            entity.Property(x => x.CoveragePremium)
                .HasPrecision(18, 2);

            entity.Property(x => x.Discount)
                .HasPrecision(18, 2);

            entity.Property(x => x.FinalPremium)
                .HasPrecision(18, 2);
        });
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(x => x.IdentityNumber)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(x => x.Email)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
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
                .HasFilter("[IsDeleted] = 0 AND [VIN] <> ''");

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

            entity.Property(x => x.PackageName)
                .HasMaxLength(100);

            entity.Property(x => x.ReviewReason)
                .HasMaxLength(300);

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
        modelBuilder.Entity<TariffChangeRequest>(entity =>
        {
            entity.ToTable("TariffChangeRequests");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.TargetType)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.TargetName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Field)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.OldValue)
                .HasPrecision(18, 4);

            entity.Property(x => x.NewValue)
                .HasPrecision(18, 4);

            entity.Property(x => x.Reason)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.RequestedByName)
                .HasMaxLength(150);

            entity.Property(x => x.DecisionNote)
                .HasMaxLength(500);

            entity.Property(x => x.Status)
                .HasConversion<int>();
        });
        modelBuilder.Entity<CoverageOption>(entity =>
        {
            entity.ToTable("CoverageOptions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Limit)
                .HasPrecision(18, 2);

            entity.Property(x => x.ExtraPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.HasOne(x => x.Coverage)
                .WithMany(x => x.Options)
                .HasForeignKey(x => x.CoverageId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<QuoteCoverage>(entity =>
        {
            entity.ToTable("QuoteCoverages");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.OptionName)
                .HasMaxLength(100);

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
            entity.Property(x => x.VehicleCategory)
                  .HasMaxLength(50)
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
            entity.HasIndex(x => new
            {
                x.BrandCode,
                x.TypeCode,
                x.ModelYear,
                x.EffectiveDate
            })
           .IsUnique()
           .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(x => new
            {
                x.BrandCode,
                x.BrandName
            })
            .HasDatabaseName("IX_VehicleValueCatalogs_ActiveBrands")
            .HasFilter("[IsDeleted] = 0 AND [IsActive] = 1");

            entity.HasIndex(x => new
            {
                x.BrandCode,
                x.TypeCode,
                x.TypeName
            })
            .HasDatabaseName("IX_VehicleValueCatalogs_ActiveTypes")
            .HasFilter("[IsDeleted] = 0 AND [IsActive] = 1");
        });

        modelBuilder.Entity<PricingRule>(entity =>
        {
            entity.Property(x => x.Value)
                .HasPrecision(18, 4);

            entity.HasIndex(x => new { x.Code, x.Version })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }); modelBuilder.Entity<PreviousPolicy>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PricingRuleChangeRequest>(entity =>
        {
            entity.Property(x => x.OldValue)
                  .HasPrecision(18, 4);

            entity.Property(x => x.NewValue)
                  .HasPrecision(18, 4);
            entity.HasOne(x => x.PricingRule)
                .WithMany()
                .HasForeignKey(x => x.PricingRuleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Requester)
                .WithMany()
                .HasForeignKey(x => x.RequestedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Approver)
                .WithMany()
                .HasForeignKey(x => x.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);
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
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = new DateTime(2026, 8, 31),
     },
     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000020"),
         Code = "BASE_KASKO_RATE",
         Name = "Temel Kasko Oranı V2",
         Description = "01.09.2026 itibarıyla geçerli yeni temel kasko oranı.",
         Value = 0.0215m,
         IsActive = true,
         IsDeleted = false,
         Version = 2,
         EffectiveFrom = new DateTime(2026, 9, 1),
         EffectiveUntil = null,
         CreatedDate = new DateTime(2026, 9, 1)
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
         Code = "AGE_0_2",
         Name = "0-2 Yaş Katsayısı",
         Value = 1.0000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
         Code = "AGE_3_5",
         Name = "3-5 Yaş Katsayısı",
         Value = 1.1000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
         Code = "AGE_6_8",
         Name = "6-8 Yaş Katsayısı",
         Value = 1.2000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
         Code = "AGE_9_12",
         Name = "9-12 Yaş Katsayısı",
         Value = 1.3500m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
         Code = "AGE_13_15",
         Name = "13-15 Yaş Katsayısı",
         Value = 1.5000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000007"),
         Code = "USAGE_PRIVATE",
         Name = "Özel Kullanım Katsayısı",
         Value = 1.0000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000008"),
         Code = "USAGE_COMMERCIAL",
         Name = "Ticari Kullanım Katsayısı",
         Value = 1.2500m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000009"),
         Code = "USAGE_RENTAL",
         Name = "Kiralık Kullanım Katsayısı",
         Value = 1.4000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000010"),
         Code = "DRIVER_25_PLUS",
         Name = "25+ Yaş Sürücü Katsayısı",
         Value = 1.0000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000011"),
         Code = "DRIVER_21_24",
         Name = "21-24 Yaş Sürücü Katsayısı",
         Value = 1.1500m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000012"),
         Code = "DRIVER_18_20",
         Name = "18-20 Yaş Sürücü Katsayısı",
         Value = 1.3000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000013"),
         Code = "CLAIMS_0",
         Name = "Hasarsızlık Katsayısı",
         Value = 0.9000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000014"),
         Code = "CLAIMS_1",
         Name = "1 Hasar Katsayısı",
         Value = 1.0000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000015"),
         Code = "CLAIMS_2",
         Name = "2 Hasar Katsayısı",
         Value = 1.1500m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000016"),
         Code = "CLAIMS_3_PLUS",
         Name = "3+ Hasar Katsayısı",
         Value = 1.3000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000017"),
         Code = "REGION_LOW",
         Name = "Düşük Bölge Riski",
         Value = 0.9500m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000018"),
         Code = "REGION_NORMAL",
         Name = "Normal Bölge Riski",
         Value = 1.0000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
     },

     new PricingRule
     {
         Id = Guid.Parse("10000000-0000-0000-0000-000000000019"),
         Code = "REGION_HIGH",
         Name = "Yüksek Bölge Riski",
         Value = 1.1000m,
         IsActive = true,
         IsDeleted = false,
         CreatedDate = new DateTime(2026, 1, 1),
         Version = 1,
         EffectiveFrom = new DateTime(2026, 1, 1),
         EffectiveUntil = null
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
        modelBuilder.Entity<InsurancePackage>(entity =>
        {
            entity.ToTable("InsurancePackages");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.Factor)
                .HasPrecision(18, 4);

            entity.Property(x => x.IsActive)
                .IsRequired();
        });

        modelBuilder.Entity<PackageCoverage>(entity =>
        {
            entity.ToTable("PackageCoverages");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.InsurancePackage)
                .WithMany(x => x.PackageCoverages)
                .HasForeignKey(x => x.InsurancePackageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Coverage)
                .WithMany()
                .HasForeignKey(x => x.CoverageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.IsDefault)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.InsurancePackageId,
                x.CoverageId
            })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        });
    }
}