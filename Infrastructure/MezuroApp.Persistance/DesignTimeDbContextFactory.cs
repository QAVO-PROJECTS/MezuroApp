using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MezuroApp.Persistance.Context;
using System.IO;

namespace MezuroApp.Persistance
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MezuroAppDbContext>
    {
        public MezuroAppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MezuroAppDbContext>();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            optionsBuilder.UseNpgsql(configuration.GetConnectionString("Default"));

            return new MezuroAppDbContext(optionsBuilder.Options);
        }
    }
}