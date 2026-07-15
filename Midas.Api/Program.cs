using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Midas.Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration
	.GetSection(JwtSettings.SectionName)
	.Get<JwtSettings>()
	?? throw new InvalidOperationException("JwtSettings section is missing or invalid in configuration.");

builder.Services.Configure<JwtSettings>(
	builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.Configure<EmailSettings>(
	builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
	options.User.RequireUniqueEmail = true;
	options.SignIn.RequireConfirmedEmail = true;
})
	.AddUserValidator<CustomUserValidator>()
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = jwtSettings.Issuer,
			ValidAudience = jwtSettings.Audience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
			ClockSkew = TimeSpan.Zero
		};
	});

builder.Services.AddAuthorization();
builder.Services.AddTransient<GlobalExceptionHandlingMiddleware>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Swagger"));
}
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
