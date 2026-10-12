using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace UPZMG.Persistence
{
    public class AppDBContextFactory : IDesignTimeDbContextFactory<AppDBContext>
    {
        public AppDBContext CreateDbContext(string[] args)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var basePath = FindSettingsFolder(currentDirectory) ?? currentDirectory;

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets<AppDBContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException(
                    $"Connection string 'Default' was not found. Base path used: {basePath}");

            var optionsBuilder = new DbContextOptionsBuilder<AppDBContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new AppDBContext(optionsBuilder.Options);
        }

        /// <summary>
        /// The nearest folder with an appsettings.json, looking at the current directory and
        /// then upwards for the API project. The design-time tools run from the directory of
        /// whichever project starts them, which is not always the one holding the settings.
        /// </summary>
        private static string? FindSettingsFolder(string start)
        {
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "appsettings.json")))
                    return directory.FullName;

                var api = Path.Combine(directory.FullName, "UPZMG.API");
                if (File.Exists(Path.Combine(api, "appsettings.json")))
                    return api;
            }

            return null;
        }
    }
}