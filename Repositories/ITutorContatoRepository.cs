namespace Vitalis.Repositories;

public interface ITutorContatoRepository
{
    IEnumerable<TutorContato> GetByTutorId(long tutorId);
    TutorContato? GetById(long id);
    void Add(TutorContato contato);
    void Update(TutorContato contato);
    void Delete(long id);
    void SetPrincipal(long tutorId, long contatoId);
}