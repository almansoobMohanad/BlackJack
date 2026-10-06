# Blackjack

A web blackjack game in C#. The game rules run entirely on the server (ASP.NET Core),
the browser UI is Blazor WebAssembly, and players, wallets and results are stored in SQL Server.

## Running it

You need the .NET 10 SDK and SQL Server Express (instance `.\SQLEXPRESS`).

```powershell
dotnet tool restore                        # installs dotnet-ef for this repo
dotnet run --project Blackjack.Api         # creates/updates the database, then starts the site
```

Open http://localhost:5098. The API serves the Blazor app too, so there's only one thing to run.

```powershell
dotnet test                                # rule tests + API tests (API tests use a Blackjack_Tests database)
```

To use a different SQL Server, change `ConnectionStrings:BlackjackDb` in
`Blackjack.Api/appsettings.json` (and `ConnectionString` in `Blackjack.Api.Tests/BlackjackApiFactory.cs`).

## Projects

| Project | What it is |
|---|---|
| `Blackjack.Core` | The rules: `Card`, `Deck`, `Hand`, `BlackjackRound`. No UI, no database. |
| `Blackjack.Contracts` | The JSON shapes (DTOs) the API and browser exchange. |
| `Blackjack.Api` | ASP.NET Core Web API, EF Core + SQL Server. Also hosts the Blazor app. |
| `Blackjack.Web` | Blazor WebAssembly UI. References **Contracts only**, not Core. |
| `Blackjack.Tests` | Unit tests for the rules. |
| `Blackjack.Api.Tests` | Integration tests: real HTTP calls against a real SQL Server database. |

## Server-authoritative game logic

The browser can only *ask*: "deal with this bet", "hit", "stand". Everything else is decided on the server.

- **The rules aren't in the browser.** `Blackjack.Web` has no reference to `Blackjack.Core`,
  so the rules aren't even downloaded (`Blackjack.Core.wasm` returns 404).
- **The deck never leaves the server.** Each round gets a fresh deck shuffled with a
  cryptographic random number generator (`RandomNumberGenerator`) and saved in `GameRounds.DeckCards`.
- **The dealer's hole card is withheld.** While it's the player's turn, the API sends `null` for that
  card and leaves out the dealer's total (`RoundMapper.ToDto`). A test checks the hidden card
  doesn't appear anywhere in the raw JSON.
- **Payouts are calculated on the server** from the final hands. The client never sends amounts won.
- **Every request is checked.** Players sign in with a random 256-bit token (only its SHA-256 hash is
  stored). A round can only be played by its owner, only in the right state, and only once.

## Database

| Table | Purpose |
|---|---|
| `Players` | Name (unique) and token hash. |
| `Wallets` | Current balance, one per player. `CHECK (Balance >= 0)` and a `rowversion` column. |
| `Transactions` | The audit trail: every balance change with its type, amount, balance after, and round. |
| `GameRounds` | Bet, cards, saved deck, state, result and payout for every round. |
| `Leaderboard` | Per-player rounds played, rounds won, net winnings, biggest win. |

### Wallet changes and the audit trail

- **One gateway:** `WalletService.Record` is the only code that changes a balance, and it always
  writes a `Transactions` row in the same step. It refuses to run outside a database transaction.
- **Database transactions:** dealing, hitting, standing, signing up and refilling each run inside
  `BlackjackDbContext.InTransactionAsync`. The bet, the round, the payout, the audit rows and the
  leaderboard update are saved together, or not at all.
- **No race conditions:** each action first locks the player's wallet row
  (`WITH (UPDLOCK, ROWLOCK)`), so simultaneous requests queue instead of double-spending. The
  `rowversion` column is a second safety net. A filtered unique index guarantees at most one
  unfinished round per player.
- **Append-only:** a trigger (`TR_Transactions_AppendOnly`) rejects any `UPDATE` or `DELETE` on
  `Transactions`, so history can't be rewritten, even from a SQL prompt.
- **It always adds up:** for every wallet, the sum of its `Transactions.Amount` equals its balance,
  and each row's `BalanceAfter` is the running total. The tests check this.

## API

| Method | Path | Notes |
|---|---|---|
| POST | `/api/players` | Sign up `{ "name": "..." }` and get back a token |
| GET | `/api/players/me` | Profile, balance, any round in progress |
| POST | `/api/rounds` | Deal `{ "bet": 25 }` |
| GET | `/api/rounds/current` | The round in progress (204 if none) |
| POST | `/api/rounds/{id}/hit` | |
| POST | `/api/rounds/{id}/stand` | Dealer plays out the rest |
| GET | `/api/wallet` | Balance and recent audit-trail entries |
| POST | `/api/wallet/refill` | Only when out of chips |
| GET | `/api/leaderboard?top=10` | Public |

Authenticated calls send `Authorization: Bearer <token>`. `Blackjack.Api/Blackjack.Api.http`
has ready-made requests. In development the OpenAPI document is at `/openapi/v1.json`.

## Rules

One fresh 52-card deck per round. The dealer hits below 17 and stands on all 17s, including soft 17.
Blackjack pays 3:2 (rounded down), a win pays 1:1, and a tie is a push. Hitting to 21 stands automatically.
