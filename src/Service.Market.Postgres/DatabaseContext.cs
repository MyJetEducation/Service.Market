using Microsoft.EntityFrameworkCore;
using MyJetWallet.Sdk.Postgres;
using MyJetWallet.Sdk.Service;
using Service.Market.Postgres.Models;

namespace Service.Market.Postgres
{
    public class DatabaseContext : MyDbContext
    {
        public const string Schema = "education";
        private const string MarketTableName = "market";

        public DatabaseContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<MarketEntity> AssetsDictionarEntities { get; set; }

        public static DatabaseContext Create(DbContextOptionsBuilder<DatabaseContext> options)
        {
            MyTelemetry.StartActivity($"Database context {Schema}")?.AddTag("db-schema", Schema);

            return new DatabaseContext(options.Options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);

            SetUserInfoEntityEntry(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private static void SetUserInfoEntityEntry(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MarketEntity>().ToTable(MarketTableName);
            modelBuilder.Entity<MarketEntity>().Property(e => e.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<MarketEntity>().Property(e => e.Date).IsRequired();
            modelBuilder.Entity<MarketEntity>().Property(e => e.Value);
            modelBuilder.Entity<MarketEntity>().HasKey(e => e.Id);
        }
    }
}
