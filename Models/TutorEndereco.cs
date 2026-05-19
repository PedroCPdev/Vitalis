using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("TB_TUTOR_ENDERECO")]
public class TutorEndereco
{
    [Key][Column("ID")] public long Id { get; set; }

    [Column("TUTOR_ID")] public long TutorId { get; set; }
    [ForeignKey("TutorId")] public Tutor? Tutor { get; set; }

    [Column("LOGRADOURO")][Required] public string Logradouro { get; set; } = null!;
    [Column("NUMERO")][Required]    public string Numero { get; set; } = null!;
    [Column("COMPLEMENTO")]         public string? Complemento { get; set; }
    [Column("BAIRRO")][Required]    public string Bairro { get; set; } = null!;
    [Column("CIDADE")][Required]    public string Cidade { get; set; } = null!;

    [Column("ESTADO")][Required, StringLength(2)]
    public string Estado { get; set; } = null!;  

    [Column("CEP")][Required, StringLength(8)]
    public string Cep { get; set; } = null!;   

    [Column("PRINCIPAL")] public bool Principal { get; set; }
}