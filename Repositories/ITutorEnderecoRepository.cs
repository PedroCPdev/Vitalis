
namespace Vitalis.Repositories;

public interface ITutorEnderecoRepository
{
    IEnumerable<TutorEndereco> GetByTutorId(long tutorId);
    TutorEndereco? GetById(long id);
    void Add(TutorEndereco endereco);
    void Update(TutorEndereco endereco);
    void Delete(long id);
    void SetPrincipal(long tutorId, long enderecoId);
}