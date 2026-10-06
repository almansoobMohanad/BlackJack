using Blackjack.Api;
using Blackjack.Api.Auth;
using Blackjack.Api.Data;
using Blackjack.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BlackjackDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BlackjackDb")));

builder.Services
    .AddAuthentication(PlayerTokenAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, PlayerTokenAuthenticationHandler>(PlayerTokenAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IDeckFactory, ShuffledDeckFactory>();
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<PlayerService>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Create the database, or upgrade it to the latest migration, on startup.
    using (var scope = app.Services.CreateScope())
    {
        scope.ServiceProvider.GetRequiredService<BlackjackDbContext>().Database.Migrate();
    }

    app.MapOpenApi();
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();

// Serve the Blazor client (Blackjack.Web) from this same site, so the page and the API share one address.
app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

// Lets the integration tests start the app.
public partial class Program;
