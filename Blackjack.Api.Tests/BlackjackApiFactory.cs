using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blackjack.Api.Data;
using Blackjack.Api.Services;
using Blackjack.Contracts;
using Blackjack.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Blackjack.Api.Tests;

// Starts the real API against a separate SQL Server database (Blackjack_Tests),
// wiped at the start of each test run. The app applies migrations on startup.
public class BlackjackApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ConnectionString =
        @"Server=.\SQLEXPRESS;Database=Blackjack_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    public TestDeckFactory Decks { get; } = new();

    public async Task InitializeAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureDeletedAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    public BlackjackDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<BlackjackDbContext>().UseSqlServer(ConnectionString).Options);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:BlackjackDb", ConnectionString);
        builder.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<IDeckFactory>(Decks)));
    }

    // Signs up a new player with a unique name and returns a client that's logged in as them.
    public async Task<(HttpClient Client, PlayerSessionDto Session)> SignUpAsync()
    {
        var client = CreateClient();
        var name = "p" + Guid.NewGuid().ToString("N")[..12];
        var response = await client.PostAsJsonAsync("api/players", new CreatePlayerRequest(name));
        response.EnsureSuccessStatusCode();

        var session = (await response.Content.ReadFromJsonAsync<PlayerSessionDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        return (client, session);
    }
}

// Deals the next queued deck if there is one, otherwise a normal shuffled deck.
public class TestDeckFactory : IDeckFactory
{
    private readonly ConcurrentQueue<Deck> _queued = new();

    // Queues a deck that deals these ranks first (player, dealer, player, dealer, then hits),
    // followed by a shuffled deck so there's always something left to draw.
    public void Next(params Rank[] ranks) =>
        _queued.Enqueue(new Deck(ranks.Select(rank => new Card(rank, Suit.Hearts)).Concat(new Deck().Cards)));

    public Deck Create() => _queued.TryDequeue(out var deck) ? deck : new Deck();
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<BlackjackApiFactory>
{
    public const string Name = "API";
}
