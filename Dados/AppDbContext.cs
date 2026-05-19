using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Tutor>         Tutores   { get; set; }
    public DbSet<TutorEndereco> Enderecos { get; set; }
    public DbSet<TutorContato>  Contatos  { get; set; }
    public DbSet<Lembrete>      Lembretes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lembrete>()
            .HasOne(l => l.Tutor)
            .WithMany(t => t.Lembretes)
            .HasForeignKey(l => l.TutorId);

        modelBuilder.Entity<Lembrete>()
            .Property(l => l.Tipo)
            .HasConversion<string>();

        modelBuilder.Entity<Lembrete>()
            .Property(l => l.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Lembrete>()
            .Property(l => l.CreatedAt)
            .HasDefaultValueSql("SYSDATE");

        modelBuilder.Entity<Tutor>()
            .Property(t => t.CreatedAt)
            .HasDefaultValueSql("SYSDATE");

        modelBuilder.Entity<Tutor>()
            .Property(t => t.Ativo)
            .HasColumnType("NUMBER(1)");

        modelBuilder.Entity<TutorEndereco>()
            .Property(e => e.Principal)
            .HasColumnType("NUMBER(1)");

        modelBuilder.Entity<TutorContato>()
            .Property(c => c.Principal)
            .HasColumnType("NUMBER(1)");
    }
}