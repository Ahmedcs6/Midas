using System.ComponentModel.DataAnnotations.Schema;
namespace Midas.Api.Models;

public partial class ApplicationUser : IdentityUser
{
	public string FirstName { get; set; } = null!;

	public string LastName { get; set; } = null!;

	[NotMapped]
	public string FullName => $"{FirstName} {LastName}";

	public DateOnly? BirthDate { get; set; }

	public GenderType? Gender { get; set; }

	public string? About { get; set; }

	public Address Address { get; set; } = new();

	public string? ImageUrl { get; set; }

	public ICollection<Post> Posts { get; set; } = [];

	public ICollection<Comment> Comments { get; set; } = [];

	public ICollection<React> Reacts { get; set; } = [];

	public ICollection<Follow> Followers { get; set; } = [];

	public ICollection<Follow> Followings { get; set; } = [];

	public ICollection<Notification> Notifications { get; set; } = [];

	public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
