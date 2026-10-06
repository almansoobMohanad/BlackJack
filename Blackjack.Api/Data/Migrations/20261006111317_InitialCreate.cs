using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blackjack.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameRounds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Bet = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PlayerCards = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    DealerCards = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    DeckCards = table.Column<string>(type: "varchar(210)", unicode: false, maxLength: 210, nullable: false),
                    Result = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Payout = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRounds", x => x.Id);
                    table.CheckConstraint("CK_GameRounds_Bet_Positive", "[Bet] > 0");
                    table.ForeignKey(
                        name: "FK_GameRounds_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leaderboard",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    RoundsPlayed = table.Column<int>(type: "int", nullable: false),
                    RoundsWon = table.Column<int>(type: "int", nullable: false),
                    NetWinnings = table.Column<int>(type: "int", nullable: false),
                    BiggestWin = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaderboard", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_Leaderboard_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                    table.CheckConstraint("CK_Wallets_Balance_NotNegative", "[Balance] >= 0");
                    table.ForeignKey(
                        name: "FK_Wallets_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    GameRoundId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_GameRounds_GameRoundId",
                        column: x => x.GameRoundId,
                        principalTable: "GameRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameRounds_OneActiveRoundPerPlayer",
                table: "GameRounds",
                column: "PlayerId",
                unique: true,
                filter: "[State] <> 'RoundOver'");

            migrationBuilder.CreateIndex(
                name: "IX_GameRounds_PlayerId_StartedAt",
                table: "GameRounds",
                columns: new[] { "PlayerId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboard_NetWinnings",
                table: "Leaderboard",
                column: "NetWinnings");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Name",
                table: "Players",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_TokenHash",
                table: "Players",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_GameRoundId",
                table: "Transactions",
                column: "GameRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_WalletId_Id",
                table: "Transactions",
                columns: new[] { "WalletId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_PlayerId",
                table: "Wallets",
                column: "PlayerId",
                unique: true);

            // Added by hand: the audit trail is append-only, enforced by the database itself,
            // so not even a bug in the app (or someone with a SQL prompt) can quietly rewrite history.
            migrationBuilder.Sql($"""
                CREATE TRIGGER [{BlackjackDbContext.AppendOnlyTrigger}] ON [Transactions]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    THROW 50001, 'Transactions is an append-only audit trail: rows cannot be changed or deleted.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DROP TRIGGER [{BlackjackDbContext.AppendOnlyTrigger}];");

            migrationBuilder.DropTable(
                name: "Leaderboard");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "GameRounds");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
