namespace Midas.Api.Models.Dtos;

public partial class EditUser
{
	public string FirstName { get; set; } = null!;

	public string LastName { get; set; } = null!;

	public DateOnly? BirthDate { get; set; }

	public string? About { get; set; }

	public Address Address { get; set; } = new();

	public string? ImageUrl { get; set; }
}
