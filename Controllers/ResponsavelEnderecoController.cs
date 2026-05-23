using Microsoft.AspNetCore.Mvc;
using Vitalis.Repositories;

[ApiController]
[Route("api/responsavel/{responsavelId:long}/enderecos")]
public class ResponsavelEnderecoController : ControllerBase
{
    private readonly IResponsavelEnderecoRepository _repo;

    public ResponsavelEnderecoController(IResponsavelEnderecoRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public IActionResult GetAll(long responsavelId)
        => Ok(_repo.GetByResponsavelId(responsavelId));

    [HttpGet("{id:long}")]
    public IActionResult GetById(long responsavelId, long id)
    {
        var endereco = _repo.GetById(id);
        if (endereco == null || endereco.ResponsavelId != responsavelId)
            return NotFound(new { erro = "Endereço não encontrado" });

        return Ok(endereco);
    }

    [HttpPost]
    public IActionResult Add(long responsavelId, [FromBody] ResponsavelEndereco endereco)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        endereco.ResponsavelId = responsavelId;
        _repo.Add(endereco);
        return CreatedAtAction(nameof(GetById),
            new { responsavelId, id = endereco.Id }, endereco);
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long responsavelId, long id, [FromBody] ResponsavelEndereco endereco)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existente = _repo.GetById(id);
        if (existente == null || existente.ResponsavelId != responsavelId)
            return NotFound(new { erro = "Endereço não encontrado" });

        endereco.Id      = id;
        endereco.ResponsavelId = responsavelId;
        _repo.Update(endereco);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long responsavelId, long id)
    {
        var existente = _repo.GetById(id);
        if (existente == null || existente.ResponsavelId != responsavelId)
            return NotFound(new { erro = "Endereço não encontrado" });

        _repo.Delete(id);
        return NoContent();
    }

    [HttpPatch("{id:long}/principal")]
    public IActionResult SetPrincipal(long responsavelId, long id)
    {
        var existente = _repo.GetById(id);
        if (existente == null || existente.ResponsavelId != responsavelId)
            return NotFound(new { erro = "Endereço não encontrado" });

        _repo.SetPrincipal(responsavelId, id);
        return NoContent();
    }
}