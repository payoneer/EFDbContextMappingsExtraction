using Microsoft.EntityFrameworkCore;

namespace FullNet_EFCore_OneDbContext
{
    public class UniversityContext : DbContext
    {
        public const string StudentsTableName = "Students";
        public const string Schema = "SomeSchema";

        public UniversityContext(DbContextOptions<UniversityContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Student>()
                .ToTable(StudentsTableName, Schema);
        }

    }
}
