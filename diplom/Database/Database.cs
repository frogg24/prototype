using Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    public class Database: DbContext
    {
        private const string DefaultConnectionString = "Host=localhost;Port=5432;Database=Genom_db;Username=postgres;Password=postgres";

        public static string? ConnectionString { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

            if (!optionsBuilder.IsConfigured)
            {
                var connectionString =
                    ConnectionString
                    ?? Environment.GetEnvironmentVariable("GENOME_DB_CONNECTION")
                    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection")
                    ?? DefaultConnectionString;

                optionsBuilder.UseNpgsql(connectionString);
            }

            base.OnConfiguring(optionsBuilder);
        }
        public virtual DbSet<User> Users { set; get; }
        public virtual DbSet<Read> Reads { set; get; }
        public virtual DbSet<Project> Projects { set; get; }
        public virtual DbSet<Assembly> Assemblies { set; get; }
    }
}
