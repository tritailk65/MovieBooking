using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public abstract class ApplicationDbContextFactory<TDbContext> : IDesignTimeDbContextFactory<TDbContext>  where TDbContext : DbContext
{
    public TDbContext CreateDbContext(params string[] args)
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        Apply(builder);
        return  (TDbContext)Activator.CreateInstance(typeof(TDbContext), builder.Options);
    }

    public static void Apply(DbContextOptionsBuilder builder)
    {
        string contextName = typeof(TDbContext).Name;
            
        builder.UseNpgsql($"Host=localhost;Username=postgres;Password=123", m =>
        {
           m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
            m.MigrationsHistoryTable($"__{contextName}");
        });
    }

    public TDbContext CreateDbContext(DbContextOptionsBuilder<TDbContext> optionsBuilder)
    {
        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), optionsBuilder.Options);
    }
}