using Microsoft.AspNetCore.Mvc;
using Vitalis.Repositories;

[ApiController]
[Route("api/responsavel")]
public class ResponsavelsApiController : ControllerBase
{
    private readonly IResponsavelRepository _repo;
    private readonly IConfiguration _config;

    public ResponsavelsApiController(IResponsavelRepository repo, IConfiguration config)
    {
        _repo = repo;
        _config = config;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var responsavels = _repo.GetAll().Select(t => new
        {
            t.Id, t.Nome, t.Email, t.Cpf, t.Ativo, t.CreatedAt
        });
        return Ok(responsavels);
    }

    [HttpGet("{id:long}")]
    public IActionResult GetById(long id)
    {
        var responsavel = _repo.GetById(id);
        if (responsavel == null) return NotFound(new { erro = "Responsavel não encontrado" });

        return Ok(new
        {
            responsavel.Id, responsavel.Nome, responsavel.Email, responsavel.Cpf,
            responsavel.Ativo, responsavel.CreatedAt,
            responsavel.Enderecos, responsavel.Contatos
        });
    }

    [HttpGet("buscar")]
    public IActionResult BuscarPorCpf([FromQuery] string cpf)
    {
        if (!ValidarServiceToken())
            return Unauthorized(new { erro = "Token inválido" });

        if (string.IsNullOrWhiteSpace(cpf))
            return BadRequest(new { erro = "CPF é obrigatório" });

        var responsavel = _repo.GetByCpf(cpf);
        if (responsavel == null) return NotFound(new { erro = "Responsavel não encontrado" });

        return Ok(new { responsavel.Id, responsavel.Nome, responsavel.Cpf, responsavel.Email, responsavel.Ativo });
    }

    [HttpPost("cadastro")]
    public IActionResult Cadastrar([FromBody] CadastrarResponsavelDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (_repo.GetByCpf(dto.Cpf) != null)
            return Conflict(new { erro = "CPF já cadastrado" });

        var responsavel = new Responsavel
        {
            Nome  = dto.Nome,
            Cpf   = dto.Cpf,
            Email = dto.Email,
            Senha = dto.Senha,
            Ativo = true
        };

        _repo.Add(responsavel);
        return CreatedAtAction(nameof(GetById), new { id = responsavel.Id },
            new { responsavel.Id, responsavel.Nome, responsavel.Email });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var responsavel = _repo.GetByEmail(dto.Email);
        if (responsavel == null || !BCrypt.Net.BCrypt.Verify(dto.Senha, responsavel.Senha))
            return Unauthorized(new { erro = "Credenciais inválidas" });

        if (!responsavel.Ativo)
            return Unauthorized(new { erro = "Conta desativada" });

        return Ok(new { responsavel.Id, responsavel.Nome, responsavel.Email });
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, [FromBody] CadastrarResponsavelDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existente = _repo.GetById(id);
        if (existente == null) return NotFound(new { erro = "Responsavel não encontrado" });

        existente.Nome  = dto.Nome;
        existente.Email = dto.Email;
        existente.Cpf   = dto.Cpf;

        _repo.Update(existente);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id)
    {
        var responsavel = _repo.GetById(id);
        if (responsavel == null) return NotFound(new { erro = "Responsavel não encontrado" });

        _repo.Delete(id);
        return NoContent();
    }

    private bool ValidarServiceToken()
    {
        var token = Request.Headers["X-Service-Token"].ToString();
        return token == _config["ServiceToken"];
    }
}