namespace Maidas.Api.Models.Dtos;

public partial class UserDto
{
	public string FirstName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	public string UserName { get; set; } = string.Empty;

	public DateOnly? BirthDate { get; set; }

	public GenderType? Gender { get; set; }

	public string? About { get; set; }

	public Address Address { get; set; } = new();

	public string? ImageUrl { get; set; }

	public int FollowersNumber { get; set; }

	public int FollowingNumber { get; set; }
}
