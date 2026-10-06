using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blackjack.Contracts;
using Microsoft.JSInterop;

namespace Blackjack.Web.Services;

// Talks to Blackjack.Api. The browser never decides anything about the game itself:
// it sends the player's choice and shows whatever the server sends back.
public class BlackjackApi(HttpClient http, IJSRuntime js)
{
    private const string TokenStorageKey = "blackjack.playerToken";

    private string? _token;

    // Loads the token saved by an earlier visit. Returns false if there isn't one.
    public async Task<bool> LoadSavedTokenAsync()
    {
        _token = await js.InvokeAsync<string?>("localStorage.getItem", TokenStorageKey);
        return _token is not null;
    }

    public async Task SignOutAsync()
    {
        _token = null;
        await js.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
    }

    public async Task<PlayerSessionDto> SignUpAsync(string name)
    {
        var session = await SendAsync<PlayerSessionDto>(HttpMethod.Post, "api/players", new CreatePlayerRequest(name));
        _token = session.Token;
        await js.InvokeVoidAsync("localStorage.setItem", TokenStorageKey, session.Token);
        return session;
    }

    public Task<ProfileDto> GetProfileAsync() =>
        SendAsync<ProfileDto>(HttpMethod.Get, "api/players/me");

    public Task<RoundDto> DealAsync(int bet) =>
        SendAsync<RoundDto>(HttpMethod.Post, "api/rounds", new DealRequest(bet));

    public Task<RoundDto> HitAsync(long roundId) =>
        SendAsync<RoundDto>(HttpMethod.Post, $"api/rounds/{roundId}/hit");

    public Task<RoundDto> StandAsync(long roundId) =>
        SendAsync<RoundDto>(HttpMethod.Post, $"api/rounds/{roundId}/stand");

    public Task<WalletDto> GetWalletAsync() =>
        SendAsync<WalletDto>(HttpMethod.Get, "api/wallet");

    public Task<WalletDto> RefillAsync() =>
        SendAsync<WalletDto>(HttpMethod.Post, "api/wallet/refill");

    public Task<List<LeaderboardEntryDto>> GetLeaderboardAsync() =>
        SendAsync<List<LeaderboardEntryDto>>(HttpMethod.Get, "api/leaderboard?top=10");

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (_token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiError.FromResponseAsync(response);
        }

        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}

// An error response from the API, carrying the server's explanation.
public class ApiError(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;

    public static async Task<ApiError> FromResponseAsync(HttpResponseMessage response)
    {
        string? detail = null;
        try
        {
            detail = (await response.Content.ReadFromJsonAsync<ProblemBody>())?.Detail;
        }
        catch (Exception)
        {
            // Not a ProblemDetails body; fall back to a generic message below.
        }

        return new ApiError(response.StatusCode, detail ?? $"The server said {(int)response.StatusCode} {response.ReasonPhrase}.");
    }

    private record ProblemBody(string? Detail);
}
