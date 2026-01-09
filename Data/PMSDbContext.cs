using Microsoft.EntityFrameworkCore;

namespace PMS.Web.Data;

public class PMSDbContext : DbContext
{
    public PMSDbContext(DbContextOptions<PMSDbContext> options)
        : base(options)
    {
    }

    // DbSets will be added here as models are created
}

