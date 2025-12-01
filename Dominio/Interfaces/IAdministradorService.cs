using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MinimalAPI.Dominio.DTOs;
using MinimalAPI.Dominio.Entities;

namespace MinimalAPI.Dominio.Interfaces
{
    public interface IAdministradorService
    {
        Administrador? Login(LoginDTO loginDTO);
        Administrador Adicionar(Administrador administrador);
        List<Administrador> ListaTodos(int? pagina);
        Administrador? ObterPorId(int id);
    }
}