using Microsoft.EntityFrameworkCore;
using Rira.Akbaritabar.Test.Server.Models;

namespace Rira.Akbaritabar.Test.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Person> Persons => Set<Person>();
}
