namespace Clientes.Api.Contracts;
public sealed record ClienteRequest(string? Nome,string? RazaoSocial,string? Cnpj,string? Endereco);
public sealed record ClienteResponse(string Id,string Nome,string RazaoSocial,string Cnpj,string Endereco,string Status);
