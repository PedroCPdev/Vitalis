namespace Vitalis.Repositories;

public class TutorEnderecoRepository : ITutorEnderecoRepository
{
    private readonly AppDbContext _context;

    public TutorEnderecoRepository(AppDbContext context)
    {
        _context = context;
    }

    public IEnumerable<TutorEndereco> GetByTutorId(long tutorId)
        => _context.Enderecos
            .Where(e => e.TutorId == tutorId)
            .ToList();

    public TutorEndereco? GetById(long id)
        => _context.Enderecos.FirstOrDefault(e => e.Id == id);

    public void Add(TutorEndereco endereco)
    {
        bool temOutros = _context.Enderecos.Any(e => e.TutorId == endereco.TutorId);
        if (!temOutros)
            endereco.Principal = true;

        if (endereco.Principal)
            DesmarcarOutros(endereco.TutorId, excludeId: null);

        _context.Enderecos.Add(endereco);
        _context.SaveChanges();
    }

    public void Update(TutorEndereco endereco)
    {
        if (endereco.Principal)
            DesmarcarOutros(endereco.TutorId, excludeId: endereco.Id);

        _context.Enderecos.Update(endereco);
        _context.SaveChanges();
    }

    public void Delete(long id)
    {
        var endereco = GetById(id);
        if (endereco == null) return;

        _context.Enderecos.Remove(endereco);
        _context.SaveChanges();

        if (endereco.Principal)
        {
            var substituto = _context.Enderecos
                .Where(e => e.TutorId == endereco.TutorId)
                .OrderByDescending(e => e.Id)
                .FirstOrDefault();

            if (substituto != null)
            {
                substituto.Principal = true;
                _context.SaveChanges();
            }
        }
    }

    public void SetPrincipal(long tutorId, long enderecoId)
    {
        DesmarcarOutros(tutorId, excludeId: enderecoId);

        var alvo = GetById(enderecoId);
        if (alvo != null)
        {
            alvo.Principal = true;
            _context.SaveChanges();
        }
    }

    private void DesmarcarOutros(long tutorId, long? excludeId)
    {
        var outros = _context.Enderecos
            .Where(e => e.TutorId == tutorId && e.Principal &&
                        (excludeId == null || e.Id != excludeId))
            .ToList();

        foreach (var e in outros)
            e.Principal = false;

    }
}