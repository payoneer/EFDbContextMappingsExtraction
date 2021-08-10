using System.Data.Entity;

namespace FullNet_EF6_OneDbContext
{
    public class UniversityContext : DbContext
    {
        public const string StudentsTableName = "Students";
        public const string Schema = "SomeSchema";

        public UniversityContext(string connectionString) : base(connectionString)
        {

        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Student>()
                .ToTable(StudentsTableName, Schema);
        }

    }
}
