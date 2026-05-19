using Microsoft.AspNetCore.Mvc;
using Vitalis.Models;
using Vitalis.Repositories;

[ApiController]
[Route("api/lembretes")]
public class LembretesApiController : ControllerBase
{
    private readonly ILembreteRepository _repo;
    private readonly IConfiguration _config;

    public LembretesApiController(ILembreteRepository repo, IConfiguration config)
    {
        _repo = repo;
        _config = config;
    }

    [HttpGet]
    public IActionResult GetAll()
        => Ok(_repo.GetAll().Select(l => new
        {
            l.Id, l.TutorId, l.PetId, l.Tipo,
            l.DataAgendada, l.Mensagem, l.Status
        }));

    [HttpGet("{id:long}")]
    public IActionResult GetById(long id)
    {
        var lembrete = _repo.GetById(id);
        if (lembrete == null) return NotFound(new { erro = "Lembrete não encontrado" });
        return Ok(lembrete);
    }

    [HttpGet("tutor/{tutorId:long}")]
    public IActionResult GetByTutor(long tutorId)
    {
        var lembretes = _repo.GetByTutorId(tutorId);
        return Ok(lembretes);
    }

    [HttpGet("tutor/{tutorId:long}/tipo/{tipo}")]
    public IActionResult GetByTutorETipo(long tutorId, TipoLembrete tipo)
    {
        var lembretes = _repo.GetByTutorIdETipo(tutorId, tipo);
        return Ok(lembretes);
    }

    [HttpPost]
    public IActionResult Criar([FromBody] CriarLembreteDto dto)
    {
        var token = Request.Headers["X-Service-Token"].ToString();
        if (token != _config["ServiceToken"])
            return Unauthorized(new { erro = "Token inválido" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var lembrete = new Lembrete
        {
            TutorId        = dto.TutorId,
            PetId          = dto.PetId,
            Tipo           = dto.Tipo,
            DataAgendada   = dto.DataAgendada,
            Mensagem       = dto.Mensagem,
            ReferenciaId   = dto.ReferenciaId,
            ReferenciaTipo = dto.ReferenciaTipo,
        };

        _repo.Add(lembrete);
        return CreatedAtAction(nameof(GetById), new { id = lembrete.Id }, new { lembrete.Id });
    }

    [HttpPatch("{id:long}/status")]
    public IActionResult AtualizarStatus(long id, [FromBody] AtualizarStatusDto dto)
    {
        var lembrete = _repo.GetById(id);
        if (lembrete == null) return NotFound(new { erro = "Lembrete não encontrado" });

        _repo.AtualizarStatus(id, dto.Status);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id)
    {
        var lembrete = _repo.GetById(id);
        if (lembrete == null) return NotFound(new { erro = "Lembrete não encontrado" });

        return NoContent();
    }
}