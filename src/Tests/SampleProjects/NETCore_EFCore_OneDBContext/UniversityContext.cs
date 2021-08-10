using Microsoft.EntityFrameworkCore;

namespace NETCore_EFCore_OneDBContext
{
    public class UniversityContext:DbContext
    {
        public const string StudentsTableName = "Students";
        public const string Schema = "SomeSchema";
        public const string StoredProcName = "SP_CoursesForStudent";

        public UniversityContext(DbContextOptions<UniversityContext> options):base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasSequence<int>("SEQ_Students");

            modelBuilder.Entity<Student>()
                .ToTable(StudentsTableName, Schema);

            modelBuilder.HasDbFunction(typeof(UniversityContext).GetMethod(nameof(CoursesCountForStudent), new[] { typeof(int) }))
                .HasName(StoredProcName);
        }
        public int CoursesCountForStudent(int studentId)
            => throw new System.NotSupportedException();
    }
}
