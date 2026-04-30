using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TEMApps.Models;
using TEMApps.Models.Models;
using TEMPApps.Models.Models;

namespace TEMApps.Data;

public class ApplicationDbContext : DbContext, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<OtpCode> OtpCodes { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<VisitorDetail> VisitorDetails { get; set; }
    public DbSet<Show> Shows { get; set; }
    public DbSet<ShowCapacity> ShowCapacities { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<Venue> Venues { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<ContactRecord> ContactRecords { get; set; }
    public DbSet<MerchandiseItem> MerchandiseItems { get; set; }
    public DbSet<CafeItem> CafeItems { get; set; }
    public DbSet<TicketHold> TicketHolds { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceVenue> DeviceVenues { get; set; }
    public DbSet<WebhookLogEntity> WebhookLogs { get; set; } = null!;
    public DbSet<PaymentLinkEntity> PaymentLinks { get; set; } = null!;
    public DbSet<RefundEntity> Refunds { get; set; } = null!;
    public DbSet<OAuthToken> OAuthTokens { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<RevokedToken> RevokedTokens { get; set; }
    public DbSet<BookingLog> BookingLogs { get; set; }
    public DbSet<RescheduleTicket> RescheduleTickets { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    // Data Protection Keys
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
 

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Define fixed date for seeding
        var seedDate = new DateTime(2023, 9, 29, 0, 0, 0, DateTimeKind.Local);

        // Define fixed GUID values for roles and users
        var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var adminUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var superAdminRoleId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var superAdminUserId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        // Status seeding with static GUIDs and fixed DateTime
        modelBuilder.Entity<Status>().HasData(
            new Status { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Upcoming", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Status { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Past", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Status { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Cancelled", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Status { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Failed", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Status { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Active", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Status { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Completed", CreatedAt = seedDate, UpdatedAt = seedDate }
        );
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique(true);
            entity.HasIndex(e => e.PhoneNumber).IsUnique(true);
            entity.HasIndex(e => e.GoogleId).IsUnique().HasFilter("\"GoogleId\" IS NOT NULL");
            entity.HasIndex(e => e.AppleId).IsUnique().HasFilter("\"AppleId\" IS NOT NULL");
            entity.HasQueryFilter(e => !e.IsDeleted);
             entity.Property(e => e.Email)
            .HasColumnType("text")
            .IsRequired(false); // allow NULL in DB

        entity.Property(e => e.PhoneNumber)
            .HasColumnType("text")
            .IsRequired(false); // allow NULL in DB
        });
        modelBuilder.Entity<ContactRecord>().HasKey(cr => cr.Id);
        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.ShowId });
        modelBuilder.Entity<Booking>()
        .Property(b => b.BookingDate)
        .HasColumnType("timestamp without time zone");

        modelBuilder.Entity<TicketHold>()
        .Property(t => t.ExpiryTime)
        .HasColumnType("timestamp without time zone");

        modelBuilder.Entity<OtpCode>(entity =>
        {
            entity.Property(e => e.ExpiresAt)
                  .HasColumnType("timestamp without time zone")
                  .IsRequired();

            entity.Property(e => e.UsedAt)
                  .HasColumnType("timestamp without time zone");
        });
        modelBuilder.Entity<AuditLog>()
         .Property(t => t.PerformedAt)
         .HasColumnType("timestamp without time zone");

        modelBuilder.Entity<DeviceVenue>()
        .Property(t => t.AssignedDate)
        .HasColumnType("timestamp without time zone");

             modelBuilder.Entity<User>()
            .Property(t => t.LastLoginAt)
            .HasColumnType("timestamp without time zone");
        modelBuilder.Entity<User>()
          .Property(t => t.PendingPhoneNumberExpiresAt)
          .HasColumnType("timestamp without time zone");
        modelBuilder.Entity<User>()
          .Property(t => t.PendingEmailExpiresAt)
          .HasColumnType("timestamp without time zone");
        modelBuilder.Entity<ContactRecord>()
         .Property(t => t.SubmittedAt)
         .HasColumnType("timestamp without time zone");
        modelBuilder.Entity<Booking>()
          .Property(t => t.CancellationDate)
          .HasColumnType("timestamp without time zone");
        modelBuilder.Entity<RevokedToken>()
          .Property(t => t.ExpiresAt)
          .HasColumnType("timestamp without time zone");
        modelBuilder.Entity<RevokedToken>()
          .Property(t => t.RevokedAt)
          .HasColumnType("timestamp without time zone");
        modelBuilder.Entity<WebhookLogEntity>()
      .Property(t => t.EventTime)
      .HasColumnType("timestamp without time zone");

        modelBuilder.Entity<WebhookLogEntity>()
            .Property(t => t.ProcessedAt)
            .HasColumnType("timestamp without time zone");
        modelBuilder.Entity<BookingLog>()
         .Property(t => t.SessionCreatedAt)
         .HasColumnType("timestamp without time zone");
        modelBuilder.Entity<BookingLog>()
      .Property(t => t.TempBookingCreatedAt)
      .HasColumnType("timestamp without time zone");
        modelBuilder.Entity<RefreshToken>()
    .Property(t => t.ExpiresAt)
    .HasColumnType("timestamp without time zone");
        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
        modelBuilder.Entity<DeviceVenue>()
            .HasOne(dv => dv.Venue)
            .WithMany(v => v.DeviceVenues)
            .IsRequired(false);

        // Permission configuration
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(e => new { e.Resource, e.Action }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // UserRole configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // RolePermission configuration
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
            entity.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // OtpCode configuration
        modelBuilder.Entity<OtpCode>(entity =>
        {
            entity.HasIndex(e => new { e.Recipient, e.Type, e.Code });
            entity.HasOne(o => o.User)
                .WithMany(u => u.OtpCodes)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // City configuration
        modelBuilder.Entity<City>(entity =>
        {
            entity.Property(c => c.Status).HasDefaultValue("Active");
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(c => c.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Booking configuration
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(b => b.Show)
                .WithMany()
                .HasForeignKey(b => b.ShowId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(b => b.Coupon)
                .WithMany()
                .HasForeignKey(b => b.CouponId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(b => b.Status)
                .WithMany()
                .HasForeignKey(b => b.StatusId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.Property(b => b.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(b => b.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
   
        modelBuilder.Entity<TicketHold>()
            .Property(x => x.RowVersion)
            .IsRowVersion();
        // VisitorDetail configuration
        modelBuilder.Entity<VisitorDetail>(entity =>
        {
            entity.HasOne(v => v.Booking)
                .WithMany(b => b.VisitorDetails)
                .HasForeignKey(v => v.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(v => v.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(v => v.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Coupon configuration
        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasIndex(c => c.Code).IsUnique();
            entity.Property(c => c.UsageLimit).HasDefaultValue(0);
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(c => c.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.IsRead).HasDefaultValue(false);
            entity.Property(n => n.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(n => n.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Make optional to align with query filter
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.Status).HasDefaultValue("Confirmed");
            entity.Property(o => o.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(o => o.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Make optional to align with query filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems) // Assuming Order has a collection of OrderItems
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete if Order is deleted
            entity.Property(oi => oi.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(oi => oi.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasQueryFilter(e => !e.IsDeleted); // Matching query filter with Order
        });

        // PaymentTransaction configuration
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.Property(p => p.Currency).HasDefaultValue("INR");
            entity.Property(p => p.Status).HasDefaultValue("Pending");
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(p => p.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Status).HasDefaultValue("Active");
            entity.Property(p => p.StockCount).HasDefaultValue(0);
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(p => p.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Refund configuration
        modelBuilder.Entity<Refund>(entity =>
        {
            entity.Property(r => r.Status).HasDefaultValue("Initiated");
            entity.Property(r => r.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(r => r.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ShowCapacity configuration
        modelBuilder.Entity<ShowCapacity>(entity =>
        {
            entity.Property(s => s.Name).HasDefaultValue("General Admission");
            entity.Property(s => s.Type).HasDefaultValue("General");
            entity.Property(s => s.Currency).HasDefaultValue("INR");
            entity.Property(s => s.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(s => s.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(s => s.Show)
                .WithMany()
                .HasForeignKey(s => s.ShowId)
                .OnDelete(DeleteBehavior.SetNull);
           
        });

     

        // Venue configuration
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.Property(v => v.Status).HasDefaultValue("Active");
            entity.Property(v => v.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(v => v.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {

            // Make sure FK is configured explicitly
            entity.HasOne(a => a.User)
                .WithMany() // or WithMany(u => u.AuditLogs) if User has a collection
                .HasForeignKey(a => a.UserId)
                .IsRequired(false) // since UserId is nullable
                .OnDelete(DeleteBehavior.Restrict); // prevent cascade delete
        });


        // Banner configuration
        modelBuilder.Entity<Banner>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.Property(b => b.Status)
                  .HasDefaultValue("Active");

            entity.HasOne(b => b.User)
                  .WithMany()
                  .HasForeignKey(b => b.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(b => b.City)
                  .WithMany()
                  .HasForeignKey(b => b.CityId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(b => b.Venue)
                  .WithMany()
                  .HasForeignKey(b => b.VenueId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
        // Apply this configuration to all entities that inherit BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                // CreatedAt
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.CreatedAt))
                    .HasColumnType("timestamp without time zone");

                // UpdatedAt
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.UpdatedAt))
                    .HasColumnType("timestamp without time zone");
            }
        }
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseModel).IsAssignableFrom(entityType.ClrType))
            {
                // CreatedAt
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseModel.CreatedAt))
                    .HasColumnType("timestamp without time zone");

                // UpdatedAt
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseModel.UpdatedAt))
                    .HasColumnType("timestamp without time zone");
            }
        }
        modelBuilder.Entity<Show>()
        .HasQueryFilter(s => !s.IsDeleted);
        // Show configuration
        modelBuilder.Entity<Show>(entity =>
        {
            entity.HasOne(s => s.Venue)
                .WithMany()
                .HasForeignKey(s => s.VenueId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<Show>()
        .HasMany(s => s.ShowCapacities)
        .WithOne(sc => sc.Show)  // Assuming ShowCapacity has a Show reference
        .HasForeignKey(sc => sc.ShowId);

        // Configure entities 
        modelBuilder.Entity<WebhookLogEntity>().HasIndex(e => e.ZohoEventId).IsUnique();
        modelBuilder.Entity<PaymentLinkEntity>().HasIndex(e => e.ZohoPaymentLinkId).IsUnique();
        modelBuilder.Entity<RefundEntity>().HasIndex(e => e.ZohoRefundId).IsUnique();
        // Call to seed other entities like roles, users, etc.
        SeedData(modelBuilder, seedDate, adminRoleId, userRoleId, adminUserId, superAdminRoleId, superAdminUserId);
    }



    private static void SeedData(ModelBuilder modelBuilder, DateTime seedDate, Guid adminRoleId, Guid userRoleId, Guid adminUserId, Guid superAdminRoleId, Guid superAdminUserId)
    {
        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = adminRoleId,
                Name = "Admin",
                Description = "Administrator role with full access",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Role
            {
                Id = userRoleId,
                Name = "User",
                Description = "Standard user role",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Role
            {
                Id = Guid.Parse("e29b20cd-1c58-45ab-8df7-77fc9006f1b2"),
                Name = "FacilityReception",
                Description = "Facility Reception role with limited access",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                CreatedBy = "System",
                UpdatedBy = "System"
            }
        );
        // Seed SuperAdmin role
        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = superAdminRoleId,
            Name = "SuperAdmin",
            Description = "Super administrator with full access",
            IsActive = true,
            CreatedAt = seedDate,
            UpdatedAt = seedDate,
            CreatedBy = "System",
            UpdatedBy = "System"
        });

        // Optionally seed one SuperAdmin user (only one)
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = superAdminUserId,
            FirstName = "Super",
            LastName = "Admin",
            Email = "superadmin7223@yopmail.com",
            EmailConfirmed = true,
            IsActive = true,
            PhoneNumber = "9999999998",
            CreatedAt = seedDate,
            UpdatedAt = seedDate,
            CreatedBy = "System",
            UpdatedBy = "System"
        });

        // Assign SuperAdmin role to super admin user
        modelBuilder.Entity<UserRole>().HasData(new UserRole
        {
            Id = Guid.Parse("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa"), // static GUID for this seed mapping
            UserId = superAdminUserId,
            RoleId = superAdminRoleId,
            CreatedAt = seedDate,
            UpdatedAt = seedDate,
            CreatedBy = "System",
            UpdatedBy = "System"
        });
        // Seed Permissions with static GUIDs
        var permissions = new[]
        {
        new { Id = Guid.Parse("4dcb1696-6170-4a2f-bb09-347edb076115"), Resource = "Users", Action = "Read" },
        new { Id = Guid.Parse("66cbf0ea-cf92-45c5-97b1-053bc50f2f16"), Resource = "Users", Action = "Write" },
        new { Id = Guid.Parse("e42d77b8-2f7a-47f9-b4c7-71b375a460bc"), Resource = "Users", Action = "Delete" },
        new { Id = Guid.Parse("34c96d68-77ba-4bc4-a3da-5f79e43023d7"), Resource = "Storage", Action = "Upload" },
        new { Id = Guid.Parse("184a2682-16c5-43a0-8b7f-e9057f320aa2"), Resource = "Storage", Action = "Download" },
        new { Id = Guid.Parse("728b87a9-5736-4c4a-8a3b-3077f7ebdb9d"), Resource = "Storage", Action = "Delete" }
    };

        // Define static GUIDs for RolePermissions
        var rolePermissionIds = new[]
        {
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
        Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
        Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
        Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")
    };

        int index = 0;
        foreach (var perm in permissions)
        {
            modelBuilder.Entity<Permission>().HasData(new Permission
            {
                Id = perm.Id,
                Name = $"{perm.Resource}.{perm.Action}",
                Description = $"Permission to {perm.Action.ToLower()} {perm.Resource.ToLower()}",
                Resource = perm.Resource,
                Action = perm.Action,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                CreatedBy = "System",
                UpdatedBy = "System"
            });

            // Assign all permissions to Admin role with static GUIDs
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission
            {
                Id = rolePermissionIds[index], // Use static GUID
                RoleId = adminRoleId,
                PermissionId = perm.Id,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                CreatedBy = "System",
                UpdatedBy = "System"
            });
            index++;
        }

        // Seed Admin User
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = adminUserId,
            FirstName = "System",
            LastName = "Admin",
            Email = "admin@TEMApps.com",
            EmailConfirmed = true,
            IsActive = true,
            PhoneNumber = "1234567890",
            CreatedAt = seedDate,
            UpdatedAt = seedDate,
            CreatedBy = "System",
            UpdatedBy = "System"
        });

        // Assign Admin role to Admin user with a static GUID
        modelBuilder.Entity<UserRole>().HasData(new UserRole
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"), // Use static GUID
            UserId = adminUserId,
            RoleId = adminRoleId,
            CreatedAt = seedDate,
            UpdatedAt = seedDate,
            CreatedBy = "System",
            UpdatedBy = "System"
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.Now;
                    entry.Entity.UpdatedAt = DateTime.Now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.Now;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
