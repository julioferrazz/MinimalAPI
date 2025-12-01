using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MinimalAPI.Dominio.DTOs;
using MinimalAPI.Dominio.Entities;
using MinimalAPI.Dominio.Interfaces;
using MinimalAPI.Infraestrutura.Database;

namespace MinimalAPI.Dominio.Services
{
    public class AdministradorService : IAdministradorService
    {
        private readonly DbContexto _contexto;

        public AdministradorService(DbContexto contexto)
        {
            _contexto = contexto;
        }

        public Administrador Adicionar(Administrador administrador)
        {
            _contexto.Administradores.Add(administrador);
            _contexto.SaveChanges();

            return administrador;
        }

        public List<Administrador> ListaTodos(int? pagina)
        {
            var query = _contexto.Administradores.AsQueryable();
            int paginacao = 10;
            if (pagina != null)
            {
                query = query.Skip(((int)pagina - 1) * paginacao).Take(paginacao);
            }
            return query.ToList();
        }

        public Administrador? Login(LoginDTO loginDTO)
        {
            return _contexto.Administradores.Where(a => a.Email == loginDTO.Email && a.Senha == loginDTO.Senha).FirstOrDefault();

        }

        public Administrador? ObterPorId(int id)
        {
            return _contexto.Administradores.Where(a => a.Id == id).FirstOrDefault();
        }
    }
}