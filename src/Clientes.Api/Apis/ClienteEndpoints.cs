using System.Text.Json;
using Clientes.Api.Contracts;
using Clientes.Api.Domain;

namespace Clientes.Api.Apis;
public static class ClienteEndpoints
{
    public static IEndpointRouteBuilder MapClienteEndpoints(this IEndpointRouteBuilder app)
    {
        var group=app.MapGroup("/api/v1/clientes").WithTags("Clientes");
        group.MapGet("/", (IStore s) => Results.Ok(s.All().Where(x=>!x.Excluido).OrderBy(x=>x.Nome).ThenBy(x=>x.RazaoSocial).ThenBy(x=>x.Id).Select(ToResponse)));
        group.MapGet("/{id}", (string id,IStore s) => Find(id,s));
        group.MapPost("/", (ClienteRequest r,IStore s,HttpContext c) => { try { var x=Cliente.Criar(r.Nome??"",r.RazaoSocial??"",r.Cnpj??"",r.Endereco??""); s.Add(x); c.Response.Headers.Location=$"/api/v1/clientes/{x.Id}"; return Results.Created($"/api/v1/clientes/{x.Id}",ToResponse(x)); } catch(ArgumentException e){return Bad(c, e.ParamName??"cliente",e.Message);} });
        group.MapPut("/{id}", async (string id,HttpRequest req,IStore s,HttpContext c) => { using var doc=await JsonDocument.ParseAsync(req.Body); var allowed=new[]{"nome","razaoSocial","cnpj","endereco"}; var bad=doc.RootElement.EnumerateObject().Select(p=>p.Name).Where(n=>!allowed.Contains(n,StringComparer.OrdinalIgnoreCase)).ToArray(); if(bad.Length>0)return Bad(c,"propriedades",$"Propriedades não permitidas: {string.Join(", ",bad)}"); if(!TryRequest(doc.RootElement,out var r))return Bad(c,"cliente","Corpo inválido."); var x=s.Find(id); if(x is null||x.Excluido)return NotFound(c); try{x.Atualizar(r.Nome!,r.RazaoSocial!,r.Cnpj!,r.Endereco!);return Results.Ok(ToResponse(x));}catch(ArgumentException e){return Bad(c,e.ParamName??"cliente",e.Message);} });
        group.MapPost("/{id}/inativar", (string id,IStore s,HttpContext c)=>Change(id,s,c,false));
        group.MapPost("/{id}/reativar", (string id,IStore s,HttpContext c)=>Change(id,s,c,true));
        group.MapDelete("/{id}", (string id,IStore s,HttpContext c)=>{var x=s.Find(id);if(x is null)return NotFound(c);x.Excluir();return Results.NoContent();});
        return app;
    }
    static IResult Change(string id,IStore s,HttpContext c,bool active){var x=s.Find(id);if(x is null||x.Excluido)return NotFound(c);if(active)x.Reativar();else x.Inativar();return Results.Ok(ToResponse(x));}
    static IResult Find(string id,IStore s){var x=s.Find(id);return x is null||x.Excluido?Results.NotFound():Results.Ok(ToResponse(x));}
    static ClienteResponse ToResponse(Cliente x)=>new(x.Id.ToString(),x.Nome,x.RazaoSocial,x.Cnpj,x.Endereco,x.Status.ToString());
    static bool TryRequest(JsonElement e,out ClienteRequest r){r=new(e.TryGetProperty("nome",out var n)?n.GetString():null,e.TryGetProperty("razaoSocial",out var rs)?rs.GetString():null,e.TryGetProperty("cnpj",out var c)?c.GetString():null,e.TryGetProperty("endereco",out var en)?en.GetString():null);return true;}
    static IResult NotFound(HttpContext c)=>Results.Problem(detail:"Cliente não encontrado.",instance:c.Request.Path,statusCode:404,title:"Recurso não encontrado",type:"https://httpstatuses.com/404");
    static IResult Bad(HttpContext c,string field,string message)=>Results.ValidationProblem(new Dictionary<string,string[]>{{field,new[]{message}}},statusCode:400,title:"Requisição inválida",type:"https://httpstatuses.com/400",instance:c.Request.Path);
}
public interface IStore { IEnumerable<Cliente> All(); Cliente? Find(string id); void Add(Cliente x); }
public sealed class MemoryStore : IStore { private readonly List<Cliente> _items=[]; public IEnumerable<Cliente> All()=>_items; public Cliente? Find(string id)=>Guid.TryParse(id,out var g)?_items.FirstOrDefault(x=>x.Id==g):null; public void Add(Cliente x)=>_items.Add(x); }
