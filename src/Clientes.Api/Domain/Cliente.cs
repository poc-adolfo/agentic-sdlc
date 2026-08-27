namespace Clientes.Api.Domain;

public sealed class Cliente
{
    private Cliente(string nome, string razaoSocial, string cnpj, string endereco)
    {
        Id = Guid.NewGuid(); Nome = nome; RazaoSocial = razaoSocial; Cnpj = cnpj; Endereco = endereco;
        Status = StatusCliente.Ativo;
    }
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string RazaoSocial { get; private set; }
    public string Cnpj { get; private set; }
    public string Endereco { get; private set; }
    public StatusCliente Status { get; private set; }
    public bool Excluido { get; private set; }
    public static Cliente Criar(string nome, string razaoSocial, string cnpj, string endereco)
    { Validate(nome, razaoSocial, cnpj, endereco); return new(nome, razaoSocial, cnpj, endereco); }
    public void Atualizar(string nome, string razaoSocial, string cnpj, string endereco)
    { Validate(nome, razaoSocial, cnpj, endereco); Nome=nome; RazaoSocial=razaoSocial; Cnpj=cnpj; Endereco=endereco; }
    public void Inativar() => Status=StatusCliente.Inativo;
    public void Reativar() => Status=StatusCliente.Ativo;
    public void Excluir() => Excluido=true;
    private static void Validate(string nome,string razao,string cnpj,string endereco)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length>50) throw new ArgumentException("Nome é obrigatório e deve ter até 50 caracteres.", nameof(nome));
        if (string.IsNullOrWhiteSpace(razao) || razao.Length>50) throw new ArgumentException("Razão Social é obrigatória e deve ter até 50 caracteres.", nameof(razao));
        if (string.IsNullOrWhiteSpace(cnpj) || cnpj.Length!=14 || !cnpj.All(char.IsLetterOrDigit)) throw new ArgumentException("CNPJ deve ter exatamente 14 caracteres alfanuméricos.", nameof(cnpj));
        if (string.IsNullOrWhiteSpace(endereco) || endereco.Length>150) throw new ArgumentException("Endereço é obrigatório e deve ter até 150 caracteres.", nameof(endereco));
    }
}
