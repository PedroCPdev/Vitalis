using Vitalis.Models;

namespace Vitalis.Repositories;

public interface ILembreteRepository
{
    IEnumerable<Lembrete> GetAll();
    IEnumerable<Lembrete> GetByTutorId(long tutorId);
    IEnumerable<Lembrete> GetByTutorIdETipo(long tutorId, TipoLembrete tipo);
    Lembrete? GetById(long id);
    void Add(Lembrete lembrete);
    void AtualizarStatus(long id, StatusLembrete status);
    void Delete(long id);
}