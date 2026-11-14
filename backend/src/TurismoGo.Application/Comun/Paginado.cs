using Microsoft.EntityFrameworkCore;

namespace TurismoGo.Application.Comun;

/// <summary>Resultado paginado.</summary>
public sealed record Paginado<T>(IReadOnlyList<T> Items, int Pagina, int Tamano, int Total)
{
    public int TotalPaginas => Tamano == 0 ? 0 : (int)Math.Ceiling(Total / (double)Tamano);
}

public static class PaginadoExtensions
{
    public const int TamanoMaximo = 50;

    public static (int pagina, int tamano) Normalizar(int? pagina, int? tamano, int porDefecto = 12) =>
        (Math.Max(1, pagina ?? 1), Math.Clamp(tamano ?? porDefecto, 1, TamanoMaximo));

    public static async Task<Paginado<T>> PaginarAsync<T>(
        this IQueryable<T> consulta, int pagina, int tamano, CancellationToken ct)
    {
        var total = await consulta.CountAsync(ct);
        var items = await consulta.Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(ct);
        return new Paginado<T>(items, pagina, tamano, total);
    }
}
