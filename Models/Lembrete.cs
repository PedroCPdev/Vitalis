using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Vitalis.Models;

[Table("TB_LEMBRETE")]
public class Lembrete
{
    [Key][Column("ID")] public long Id { get; set; }

    [Column("TUTOR_ID")] public long TutorId { get; set; }
    [ForeignKey("TutorId")] public Tutor? Tutor { get; set; }

    [Column("PET_ID")] public long PetId { get; set; } 

    [Column("TIPO")][Required]
    public TipoLembrete Tipo { get; set; }

    [Column("DATA_AGENDADA")] public DateTime DataAgendada { get; set; }

    [Column("MENSAGEM")][Required] public string Mensagem { get; set; } = null!;

    [Column("STATUS")] public StatusLembrete Status { get; set; } = StatusLembrete.PENDENTE;

    [Column("REFERENCIA_ID")]   public long? ReferenciaId { get; set; }
    [Column("REFERENCIA_TIPO")] public string? ReferenciaTipo { get; set; }

    [Column("CREATED_AT")] public DateTime CreatedAt { get; set; }
}