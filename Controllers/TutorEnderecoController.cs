using Microsoft.AspNetCore.Mvc;
using Vitalis.Repositories;

[ApiController]
[Route("api/tutores/{tutorId:long}/enderecos")]
public class TutorEnderecoController : ControllerBase
{
    private readonly ITutorEnderecoRepository _repo;

    public TutorEnderecoController(ITutorEnderecoRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public IActionResult GetAll(long tutorId)
        => Ok(_repo.GetByTutorId(tutorId));

    [HttpGet("{id:long}")]
    public IActionResult GetById(long tutorId, long id)
    {
        var endereco = _repo.GetById(id);
        if (endereco == null || endereco.TutorId != tutorId)
            return NotFound(new { erro = "Endereço não encontrado" });

        return Ok(endereco);
    }

    [HttpPost]
    public IActionResult Add(long tutorId, [FromBody] TutorEndereco endereco)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        endereco.TutorId = tutorId;
        _repo.Add(endereco);
        return CreatedAtAction(nameof(GetById),
            new { tutorId, id = endereco.Id }, endereco);
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long tutorId, long id, [FromBody] TutorEndereco endereco)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existente = _repo.GetById(id);
        if (existente == null || existente.TutorId != tutorId)
            return NotFound(new { erro = "Endereço não encontrado" });

        endereco.Id      = id;
        endereco.TutorId = tutorId;
        _repo.Update(endereco);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long tutorId, long id)
    {
        var existente = _repo.GetById(id);
        if (existente == null || existente.TutorId != tutorId)
            return NotFound(new { erro = "Endereço não encontrado" });

        _repo.Delete(id);
        return NoContent();
    }

    [HttpPatch("{id:long}/principal")]
    public IActionResult SetPrincipal(long tutorId, long id)
    {
        var existente = _repo.GetById(id);
        if (existente == null || existente.TutorId != tutorId)
            return NotFound(new { erro = "Endereço não encontrado" });

        _repo.SetPrincipal(tutorId, id);
        return NoContent();
    }
}