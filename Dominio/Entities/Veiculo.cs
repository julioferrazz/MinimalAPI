using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MinimalAPI.Dominio.Entities
{
    public class Veiculo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = default!;
        
        [Required]
        [StringLength(255)]
        public string Nome { get; set; } = default!;

        [Required]
        [StringLength(100)]
        public string Marca { get; set; } = default!;

        [Required]
        [StringLength(10)]
        public int Ano { get; set; } = default!;
    }
}