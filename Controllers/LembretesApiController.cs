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
            l.Id, l.ResponsavelId, l.PetId, l.Tipo,
            l.DataAgendada, l.Mensagem, l.Status
        }));

    [HttpGet("{id:long}")]
    public IActionResult GetById(long id)
    {
        var lembrete = _repo.GetById(id);
        if (lembrete == null) return NotFound(new { erro = "Lembrete não encontrado" });
        return Ok(lembrete);
    }

    [HttpGet("responsavel/{responsavelId:long}")]
    public IActionResult GetByResponsavel(long responsavelId)
    {
        var lembretes = _repo.GetByResponsavelId(responsavelId);
        return Ok(lembretes);
    }

    [HttpGet("responsavel/{responsavelId:long}/tipo/{tipo}")]
    public IActionResult GetByResponsavelETipo(long responsavelId, TipoLembrete tipo)
    {
        var lembretes = _repo.GetByResponsavelIdETipo(responsavelId, tipo);
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
            ResponsavelId        = dto.ResponsavelId,
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