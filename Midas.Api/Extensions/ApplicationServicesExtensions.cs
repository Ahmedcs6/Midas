using Midas.Api.Middlewares;

namespace Midas.Api.Extensions;

public static class ApplicationServicesExtensions
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services)
	{
		services.AddTransient<GlobalExceptionHandlingMiddleware>();
		services.AddHttpContextAccessor();
		services.AddScoped<IClientInfoProvider, ClientInfoProvider>();
		services.AddScoped<IJwtService, JwtService>();
		services.AddScoped<ISessionService, SessionService>();
		services.AddScoped<IAccountService, AccountService>();
		services.AddScoped<IUserService, UserService>();
		services.AddScoped<IPostService, PostService>();
		services.AddScoped<IFileStorage, LocalFileStorage>();
		services.AddScoped<ICurrentUser, CurrentUser>();

		return services;
	}
}
