using System.Data.Entity.Migrations;

namespace Linage.Infrastructure.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<LiNageDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false; // Set to true if you want to allow column deletion
            ContextKey = "Linage.Infrastructure.LiNageDbContext";
        }

        protected override void Seed(LiNageDbContext context)
        {
            // This method will be called after migrating to the latest version
            // Use it to seed initial data if needed
            
            base.Seed(context);
        }
    }
}
