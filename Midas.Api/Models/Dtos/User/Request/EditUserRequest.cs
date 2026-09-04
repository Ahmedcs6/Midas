namespace Midas.Api.Models.Dtos.User.Request;

public class EditUserRequest
{
	public string? FirstName { get; set; }

	public string? LastName { get; set; }

	public DateOnly? BirthDate { get; set; }

	public string? About { get; set; }

	public Address? Address { get; set; }
}
