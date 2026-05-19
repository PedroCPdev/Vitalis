using Microsoft.EntityFrameworkCore;

namespace Vitalis.Repositories;

public class TutorRepository : ITutorRepository
{
    private readonly AppDbContext _context;

    public TutorRepository(AppDbContext context)
    {
        _context = context;
    }

    public IEnumerable<Tutor> GetAll()
        => _context.Tutores
            .Include(t => t.Enderecos)
            .Include(t => t.Contatos)
            .ToList();

    public Tutor? GetById(long id)
        => _context.Tutores
            .Include(t => t.Enderecos)
            .Include(t => t.Contatos)
            .FirstOrDefault(t => t.Id == id);

    public Tutor? GetByCpf(string cpf)
        => _context.Tutores
            .FirstOrDefault(t => t.Cpf == cpf && t.Ativo);

    public void Add(Tutor tutor)
    {
        tutor.CreatedAt = DateTime.UtcNow;
        tutor.Senha = BCrypt.Net.BCrypt.HashPassword(tutor.Senha);
        _context.Tutores.Add(tutor);
        _context.SaveChanges();
    }

    public void Update(Tutor tutor)
    {
        var existente = _context.Tutores
            .AsNoTracking()
            .FirstOrDefault(t => t.Id == tutor.Id);

        if (existente != null)
            tutor.Senha = existente.Senha;

        _context.Tutores.Update(tutor);
        _context.SaveChanges();
    }

    public void Delete(long id)
    {
        var tutor = GetById(id);
        if (tutor != null)
        {
            _context.Tutores.Remove(tutor);
            _context.SaveChanges();
        }
    }
    
    public Tutor? GetByEmail(string email)
        => _context.Tutores
            .FirstOrDefault(t => t.Email == email);
}