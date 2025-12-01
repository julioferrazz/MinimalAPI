using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Dominio.DTOs;
using MinimalAPI.Dominio.Entities;
using MinimalAPI.Dominio.Interfaces;
using MinimalAPI.Infraestrutura.Database;

namespace MinimalAPI.Dominio.Services
{
    public class VeiculoService : IVeiculoService
    {
        private readonly DbContexto _contexto;

        public VeiculoService(DbContexto contexto)
        {
            _contexto = contexto;
        }

        public void Adicionar(Veiculo veiculo)
        {
            _contexto.Veiculos.Add(veiculo);
            _contexto.SaveChanges();
        }

        public void Atualizar(Veiculo veiculo)
        {
            _contexto.Veiculos.Update(veiculo);
            _contexto.SaveChanges();
        }

        public void Deletar(Veiculo veiculo)
        {
            _contexto.Veiculos.Remove(veiculo);
            _contexto.SaveChanges();
        }

        public List<Veiculo> ListaTodos(int? pagina = 1, string? nome = null, string? marca = null)
        {
            var query = _contexto.Veiculos.AsQueryable();
            if (!string.IsNullOrEmpty(nome))
            {
                query = query.Where(v => EF.Functions.Like(v.Nome.ToLower(), $"%{nome}%"));
            }

            int paginacao = 10;
            if (pagina != null)
            {
                query = query.Skip(((int)pagina - 1) * paginacao).Take(paginacao);
            }
            return query.ToList();
        }

        public Veiculo? ObterPorId(int id)
        {
            return _contexto.Veiculos.Where(v => v.Id == id).FirstOrDefault();
        }
    }
}