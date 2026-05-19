using Microsoft.AspNetCore.Mvc;
using Vitalis.Repositories;

[ApiController]
[Route("api/tutores/{tutorId:long}/contatos")]
public class TutorContatoController : ControllerBase
{
    private readonly ITutorContatoRepository _repo;

    public TutorContatoController(ITutorContatoRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public IActionResult GetAll(long tutorId)
        => Ok(_repo.GetByTutorId(tutorId));

    [HttpGet("{id:long}")]
    public IActionResult GetById(long tutorId, long id)
    {
        var contato = _repo.GetById(id);
        if (contato == null || contato.TutorId != tutorId)
            return NotFound(new { erro = "Contato não encontrado" });

        return Ok(contato);
    }

    [HttpPost]
    public IActionResult Add(long tutorId, [FromBody] TutorContato contato)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        contato.TutorId = tutorId;
        _repo.Add(contato);
        return CreatedAtAction(nameof(GetById),
            new { tutorId, id = contato.Id }, contato);
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long tutorId, long id, [FromBody] TutorContato contato)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existente = _repo.GetById(id);
        if (existente == null || existente.TutorId != tutorId)
            return NotFound(new { erro = "Contato não encontrado" });

        contato.Id      = id;
        contato.TutorId = tutorId;
        _repo.Update(contato);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long tutorId, long id)
    {
        var existente = _repo.GetById(id);
        if (existente == null || existente.TutorId != tutorId)
            return NotFound(new { erro = "Contato não encontrado" });

        _repo.Delete(id);
        return NoContent();
    }

    [HttpPatch("{id:long}/principal")]
    public IActionResult SetPrincipal(long tutorId, long id)
    {
        var existente = _repo.GetById(id);
        if (existente == null || existente.TutorId != tutorId)
            return NotFound(new { erro = "Contato não encontrado" });

        _repo.SetPrincipal(tutorId, id);
        return NoContent();
    }
}