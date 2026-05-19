using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Vitalis.Models;

[Table("TB_TUTOR")]
public class Tutor
{
    [Key]
    [Column("ID")]
    public long Id { get; set; }

    [Column("NOME")]
    [Required, StringLength(150)]
    public string Nome { get; set; } = null!;

    [Column("CPF")]
    [Required, StringLength(11)]
    public string Cpf { get; set; } = null!;  

    [Column("EMAIL")]
    [Required]
    public string Email { get; set; } = null!;

    [Column("SENHA")]
    [Required]
    public string Senha { get; set; } = null!;  

    [Column("ATIVO")]
    public bool Ativo { get; set; } = true;

    [Column("CREATED_AT")]
    public DateTime CreatedAt { get; set; }

    public List<TutorEndereco> Enderecos { get; set; } = [];
    public List<TutorContato> Contatos { get; set; } = [];
    public List<Lembrete> Lembretes { get; set; } = [];
}