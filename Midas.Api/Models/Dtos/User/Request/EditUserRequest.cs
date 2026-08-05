namespace Midas.Api.Models.Dtos.User.Request;

public class EditUserRequest
{
	public string FirstName { get; set; } = null!;

	public string LastName { get; set; } = null!;

	public DateOnly? BirthDate { get; set; }

	public string? About { get; set; }

	public Address Address { get; set; } = new();
}
