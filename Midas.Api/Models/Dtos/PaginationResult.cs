namespace Midas.Api.Models.Dtos;

public class PaginationResult<Titem, TCursor> where TCursor : struct
{
	public IEnumerable<Titem> Items { get; set; } = [];
	public TCursor? NextCursor { get; set; }
}
