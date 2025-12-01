using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Dominio.DTOs;
using MinimalAPI.Dominio.Entities;
using MinimalAPI.Dominio.Enums;
using MinimalAPI.Dominio.Interfaces;
using MinimalAPI.Dominio.ModelViews;
using MinimalAPI.Dominio.Services;
using MinimalAPI.Infraestrutura.Database;

#region Builder
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAdministradorService, AdministradorService>();
builder.Services.AddScoped<IVeiculoService, VeiculoService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DbContexto>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("sqlserver"));
});

var app = builder.Build();
#endregion

#region Home
app.MapGet("/", () => Results.Redirect("/swagger")).WithTags("Home");
#endregion

#region Administrador
app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorService administradorServico) =>
{
    if (administradorServico.Login(loginDTO) != null)
    {
        return Results.Ok("Login feito com sucesso!");
    }
    else
    {
        return Results.Unauthorized();
    }
}).WithTags("Administradores");

app.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorService administradorServico) =>
{
    var adms = new List<AdministradorModelView>();
    var administradores = administradorServico.ListaTodos(pagina);
    foreach (var adm in administradores)
    {
        adms.Add(new AdministradorModelView
        {
            Id = adm.Id,
            Email = adm.Email,
            Perfil = adm.Perfil
        });
    }
    return Results.Ok(adms);
}).WithTags("Administradores");

app.MapGet("/administradores/{id}", ([FromRoute] int id, IAdministradorService administradorServico) =>
{
    var administrador = administradorServico.ObterPorId(id);
    var admin = new AdministradorModelView
    {
        Id = administrador.Id,
        Email = administrador.Email,
        Perfil = administrador.Perfil
    };
    return admin != null ? Results.Ok(admin) : Results.NotFound();
}).WithTags("Administradores");

app.MapPost("/administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorService administradorServico) =>
{
    var validacao = new ErrosDeValidacao
    {
        Mensagem = new List<string>()
    };
    if (string.IsNullOrEmpty(administradorDTO.Email))
        validacao.Mensagem.Add("O email é obrigatório.");
    if (string.IsNullOrEmpty(administradorDTO.Senha))
        validacao.Mensagem.Add("A senha é obrigatória.");
    if (administradorDTO.Perfil == null)
        validacao.Mensagem.Add("O perfil é obrigatório.");


    if (validacao.Mensagem.Count > 0)
        return Results.BadRequest(validacao);

    var administrador = new Administrador
    {
        Email = administradorDTO.Email,
        Senha = administradorDTO.Senha,
        Perfil = administradorDTO.Perfil?.ToString() ?? Perfil.Editor.ToString()
    };
    administradorServico.Adicionar(administrador);

    return Results.Created($"/administradores/{administrador.Id}", new AdministradorModelView
    {
        Id = administrador.Id,
        Email = administrador.Email,
        Perfil = administrador.Perfil
    });

}).WithTags("Administradores");
#endregion

#region Veiculo

ErrosDeValidacao ValidaDTO(VeiculoDTO veiculoDTO)
{
    var validacao = new ErrosDeValidacao
    {
        Mensagem = new List<string>()
    };

    if (string.IsNullOrEmpty(veiculoDTO.Nome))
        validacao.Mensagem.Add("O nome do veículo é obrigatório.");
    if (string.IsNullOrEmpty(veiculoDTO.Marca))
        validacao.Mensagem.Add("A Marca do veículo é obrigatória.");
    if (veiculoDTO.Ano < 1886)
        validacao.Mensagem.Add("O ano do veículo é inválido.");

    return validacao;
}

app.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoService veiculoService) =>
{
    var validacao = ValidaDTO(veiculoDTO);
    if (validacao.Mensagem.Count > 0)
        return Results.BadRequest(validacao);


    var veiculo = new Veiculo
    {
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano
    };
    veiculoService.Adicionar(veiculo);
    return Results.Created($"/veiculos/{veiculo.Id}", veiculo);
}).WithTags("Veículos");

app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoService veiculoService) =>
{
    var veiculos = veiculoService.ListaTodos();
    return Results.Ok(veiculos);
}).WithTags("Veículos");

app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoService veiculoService) =>
{
    var veiculo = veiculoService.ObterPorId(id);
    return veiculo != null ? Results.Ok(veiculo) : Results.NotFound();
}).WithTags("Veículos");

app.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoService veiculoService) =>
{
    var veiculoExistente = veiculoService.ObterPorId(id);
    if (veiculoExistente == null) return Results.NotFound();

    var validacao = ValidaDTO(veiculoDTO);
    if (validacao.Mensagem.Count > 0)
        return Results.BadRequest(validacao);

    veiculoExistente.Nome = veiculoDTO.Nome;
    veiculoExistente.Marca = veiculoDTO.Marca;
    veiculoExistente.Ano = veiculoDTO.Ano;

    veiculoService.Atualizar(veiculoExistente);
    return Results.Ok(veiculoExistente);
}).WithTags("Veículos");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoService veiculoService) =>
{
    var veiculoExistente = veiculoService.ObterPorId(id);
    if (veiculoExistente == null) return Results.NotFound();

    veiculoService.Deletar(veiculoExistente);
    return Results.NoContent();
}).WithTags("Veículos");
#endregion

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
