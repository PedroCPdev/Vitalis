namespace Vitalis.Repositories;

public class TutorContatoRepository : ITutorContatoRepository
{
    private readonly AppDbContext _context;

    public TutorContatoRepository(AppDbContext context)
    {
        _context = context;
    }

    public IEnumerable<TutorContato> GetByTutorId(long tutorId)
        => _context.Contatos
            .Where(c => c.TutorId == tutorId)
            .ToList();

    public TutorContato? GetById(long id)
        => _context.Contatos.FirstOrDefault(c => c.Id == id);

    public void Add(TutorContato contato)
    {
        bool temOutros = _context.Contatos.Any(c => c.TutorId == contato.TutorId);
        if (!temOutros)
            contato.Principal = true;

        if (contato.Principal)
            DesmarcarOutros(contato.TutorId, excludeId: null);

        _context.Contatos.Add(contato);
        _context.SaveChanges();
    }

    public void Update(TutorContato contato)
    {
        if (contato.Principal)
            DesmarcarOutros(contato.TutorId, excludeId: contato.Id);

        _context.Contatos.Update(contato);
        _context.SaveChanges();
    }

    public void Delete(long id)
    {
        var contato = GetById(id);
        if (contato == null) return;

        _context.Contatos.Remove(contato);
        _context.SaveChanges();

        if (contato.Principal)
        {
            var substituto = _context.Contatos
                .Where(c => c.TutorId == contato.TutorId)
                .OrderByDescending(c => c.Id)
                .FirstOrDefault();

            if (substituto != null)
            {
                substituto.Principal = true;
                _context.SaveChanges();
            }
        }
    }

    public void SetPrincipal(long tutorId, long contatoId)
    {
        DesmarcarOutros(tutorId, excludeId: contatoId);

        var alvo = GetById(contatoId);
        if (alvo != null)
        {
            alvo.Principal = true;
            _context.SaveChanges();
        }
    }

    private void DesmarcarOutros(long tutorId, long? excludeId)
    {
        var outros = _context.Contatos
            .Where(c => c.TutorId == tutorId && c.Principal &&
                        (excludeId == null || c.Id != excludeId))
            .ToList();

        foreach (var c in outros)
            c.Principal = false;
    }
}