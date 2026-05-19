using Microsoft.AspNetCore.Mvc;
using Vitalis.Repositories;

[ApiController]
[Route("api/tutores")]
public class TutoresApiController : ControllerBase
{
    private readonly ITutorRepository _repo;
    private readonly IConfiguration _config;

    public TutoresApiController(ITutorRepository repo, IConfiguration config)
    {
        _repo = repo;
        _config = config;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var tutores = _repo.GetAll().Select(t => new
        {
            t.Id, t.Nome, t.Email, t.Cpf, t.Ativo, t.CreatedAt
        });
        return Ok(tutores);
    }

    [HttpGet("{id:long}")]
    public IActionResult GetById(long id)
    {
        var tutor = _repo.GetById(id);
        if (tutor == null) return NotFound(new { erro = "Tutor não encontrado" });

        return Ok(new
        {
            tutor.Id, tutor.Nome, tutor.Email, tutor.Cpf,
            tutor.Ativo, tutor.CreatedAt,
            tutor.Enderecos, tutor.Contatos
        });
    }

    [HttpGet("buscar")]
    public IActionResult BuscarPorCpf([FromQuery] string cpf)
    {
        if (!ValidarServiceToken())
            return Unauthorized(new { erro = "Token inválido" });

        if (string.IsNullOrWhiteSpace(cpf))
            return BadRequest(new { erro = "CPF é obrigatório" });

        var tutor = _repo.GetByCpf(cpf);
        if (tutor == null) return NotFound(new { erro = "Tutor não encontrado" });

        return Ok(new { tutor.Id, tutor.Nome, tutor.Cpf, tutor.Email, tutor.Ativo });
    }

    [HttpPost("cadastro")]
    public IActionResult Cadastrar([FromBody] CadastrarTutorDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (_repo.GetByCpf(dto.Cpf) != null)
            return Conflict(new { erro = "CPF já cadastrado" });

        var tutor = new Tutor
        {
            Nome  = dto.Nome,
            Cpf   = dto.Cpf,
            Email = dto.Email,
            Senha = dto.Senha,
            Ativo = true
        };

        _repo.Add(tutor);
        return CreatedAtAction(nameof(GetById), new { id = tutor.Id },
            new { tutor.Id, tutor.Nome, tutor.Email });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var tutor = _repo.GetByEmail(dto.Email);
        if (tutor == null || !BCrypt.Net.BCrypt.Verify(dto.Senha, tutor.Senha))
            return Unauthorized(new { erro = "Credenciais inválidas" });

        if (!tutor.Ativo)
            return Unauthorized(new { erro = "Conta desativada" });

        return Ok(new { tutor.Id, tutor.Nome, tutor.Email });
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, [FromBody] CadastrarTutorDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existente = _repo.GetById(id);
        if (existente == null) return NotFound(new { erro = "Tutor não encontrado" });

        existente.Nome  = dto.Nome;
        existente.Email = dto.Email;
        existente.Cpf   = dto.Cpf;

        _repo.Update(existente);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id)
    {
        var tutor = _repo.GetById(id);
        if (tutor == null) return NotFound(new { erro = "Tutor não encontrado" });

        _repo.Delete(id);
        return NoContent();
    }

    private bool ValidarServiceToken()
    {
        var token = Request.Headers["X-Service-Token"].ToString();
        return token == _config["ServiceToken"];
    }
}