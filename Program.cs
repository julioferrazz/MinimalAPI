using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalAPI.Dominio.DTOs;
using MinimalAPI.Dominio.Entities;
using MinimalAPI.Dominio.Enums;
using MinimalAPI.Dominio.Interfaces;
using MinimalAPI.Dominio.ModelViews;
using MinimalAPI.Dominio.Services;
using MinimalAPI.Infraestrutura.Database;

#region Builder
var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();
if (string.IsNullOrEmpty(key))
    key = "123456";

builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
    option.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorService, AdministradorService>();
builder.Services.AddScoped<IVeiculoService, VeiculoService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT aqui: "
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddDbContext<DbContexto>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("sqlserver"));
});

var app = builder.Build();
#endregion

#region Home
app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous().WithTags("Home");
#endregion

#region Administrador

string GerarTokenJWT(Administrador administrador)
{
    var SecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>()
    {
        new Claim("Email", administrador.Email),
        new Claim("Perfil", administrador.Perfil),
        new Claim(ClaimTypes.Role, administrador.Perfil),
    };


    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.Now.AddDays(1),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}


app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorService administradorServico) =>
{
    var administrador = administradorServico.Login(loginDTO);
    if (administrador != null)
    {
        string token = GerarTokenJWT(administrador);
        return Results.Ok(new AdmLogado
        {
            Email = administrador.Email,
            Perfil = administrador.Perfil,
            Token = token
        });
    }
    else
    {
        return Results.Unauthorized();
    }
}).AllowAnonymous().WithTags("Administradores");

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
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm"}).WithTags("Administradores");

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
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm"}).WithTags("Administradores");

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

}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm"}).WithTags("Administradores");
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
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm"}).WithTags("Veículos");

app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoService veiculoService) =>
{
    var veiculos = veiculoService.ListaTodos();
    return Results.Ok(veiculos);
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm, Editor"}).WithTags("Veículos");

app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoService veiculoService) =>
{
    var veiculo = veiculoService.ObterPorId(id);
    return veiculo != null ? Results.Ok(veiculo) : Results.NotFound();
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm, Editor"}).WithTags("Veículos");

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
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm"}).WithTags("Veículos");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoService veiculoService) =>
{
    var veiculoExistente = veiculoService.ObterPorId(id);
    if (veiculoExistente == null) return Results.NotFound();

    veiculoService.Deletar(veiculoExistente);
    return Results.NoContent();
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm"}).WithTags("Veículos");
#endregion

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
