using System;
using Xunit;
using Clientes.Api.Domain;

namespace Clientes.Tests;

public class ClienteTests
{
    [Fact]
    public void Novo_cliente_eh_ativo_e_exige_campos_validos()
    {
        var cliente = Cliente.Criar("Ana", "Ana Ltda", "AB123456789012", "Rua A");
        Assert.Equal(StatusCliente.Ativo, cliente.Status);
        Assert.False(cliente.Excluido);
    }

    [Fact]
    public void Cnpj_deve_ter_14_caracteres_alfanumericos()
    {
        Assert.Throws<ArgumentException>(() => Cliente.Criar("Ana", "Ana Ltda", "123", "Rua A"));
        Assert.Throws<ArgumentException>(() => Cliente.Criar("Ana", "Ana Ltda", "1234567890123!", "Rua A"));
    }
}
