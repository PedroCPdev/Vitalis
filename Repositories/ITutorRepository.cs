namespace Vitalis.Repositories;

public interface ITutorRepository
{
    IEnumerable<Tutor> GetAll();
    Tutor? GetById(long id);
    Tutor? GetByCpf(string cpf);
    void Add(Tutor tutor);
    void Update(Tutor tutor);
    void Delete(long id);
    Tutor? GetByEmail(string email);
}