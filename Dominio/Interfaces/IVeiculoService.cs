using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MinimalAPI.Dominio.DTOs;
using MinimalAPI.Dominio.Entities;

namespace MinimalAPI.Dominio.Interfaces
{
    public interface IVeiculoService
    {
        List<Veiculo> ListaTodos(int? pagina = 1, string? nome = null, string? marca = null);
        Veiculo? ObterPorId(int id);
        void Adicionar(Veiculo veiculo);
        void Atualizar(Veiculo veiculo);
        void Deletar(Veiculo veiculo);
    }
}