using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("TB_TUTOR_CONTATO")]
public class TutorContato
{
    [Key][Column("ID")] public long Id { get; set; }

    [Column("TUTOR_ID")] public long TutorId { get; set; }
    [ForeignKey("TutorId")] public Tutor? Tutor { get; set; }

    [Column("TIPO")][Required] public string Tipo { get; set; } = null!; 
    [Column("VALOR")][Required] public string Valor { get; set; } = null!;
    [Column("PRINCIPAL")] public bool Principal { get; set; }
}