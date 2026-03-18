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
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured == false)
            {
                optionsBuilder.UseNpgsql(@"Host=localhost;Port=5432;Database=Genom_db;Username=postgres;Password=postgres");
            }
            base.OnConfiguring(optionsBuilder);
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
        }
        public virtual DbSet<User> Users { set; get; }
        public virtual DbSet<Read> Reads { set; get; }
        public virtual DbSet<Project> Projects { set; get; }
        public virtual DbSet<Assembly> Assemblies { set; get; }
    }
}
