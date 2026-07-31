# Financial Chat — .NET Challenge

A browser-based chat application where registered users talk in chatrooms and a
**decoupled bot** answers `/stock=stock_code` commands with live quotes from
[Stooq](https://stooq.com), delivered through **RabbitMQ**.

## Features

Mandatory:

- Registered users log in and talk with each other in a chatroom (SignalR, real time).
- `/stock=stock_code` commands (e.g. `/stock=aapl.us`).
- A decoupled bot — a separate .NET Worker Service process — calls the Stooq API,
  parses the CSV and posts the quote back through RabbitMQ as
  **`AAPL.US quote is $93.42 per share`**, owned by the bot (`StockBot`).
- Messages are ordered by timestamp and only the last **50** are shown.
- Unit tests (`dotnet test`, 50 tests).
- The `/stock` command is **never saved to the database** (it is echoed live to the
  room and disappears on reload; the bot's reply is a real, persisted message).

Bonus completed:

- **More than one chatroom** — create/switch rooms in the sidebar; bot replies are
  routed back to the room where the command was typed.
- **Messages that are not understood and bot exceptions are handled** — unknown
  `/commands` get a private hint; unknown tickers, Stooq timeouts and malformed
  payloads become friendly bot replies (the bot never crashes and never goes silent).

Bonus not done: full ASP.NET Core Identity (simple cookie auth with PBKDF2-hashed
passwords instead, which the challenge allows) and an installer (`docker compose`
plus `dotnet run` covers setup).

## Architecture

```
Browser A ⇄ SignalR ⇄ ┌────────────┐                       ┌──────────────────┐
Browser B ⇄ SignalR ⇄ │ ChatApp.Web│ ──(stock request)──►  │     RabbitMQ     │
                      │  ASP.NET 8 │ ◄──(stock reply)────  │  direct exchange │
                      │ EF+SQLite  │                       └────────┬─────────┘
                      └────────────┘                                │ consume/publish
                                                           ┌────────┴─────────┐
                                                           │ ChatApp.StockBot │──► Stooq API
                                                           │  Worker Service  │    (CSV)
                                                           └──────────────────┘
```

- **`ChatApp.Web`** — ASP.NET Core 8: Razor Pages (login/register + chat), SignalR
  hub, EF Core + SQLite persistence, cookie authentication. Publishes stock
  requests to RabbitMQ and hosts a background consumer that persists each bot
  reply (author `StockBot`) and broadcasts it to the room.
- **`ChatApp.StockBot`** — .NET 8 Worker Service, a **separate process** with zero
  reference to the web project (its only dependencies are `ChatApp.Contracts` and
  the RabbitMQ client). It consumes requests, calls Stooq, parses the CSV and
  publishes the reply. Kill it and chat keeps working; `/stock` commands queue up
  (durable queues) and are answered when it comes back — that is the decoupling
  the challenge asks for, and it is demonstrable (step 8 below).
- **`ChatApp.Contracts`** — the only thing the two processes share: message DTOs,
  queue/exchange names and a connect-with-retry helper. Both processes declare the
  topology idempotently, so startup order never matters.

Other decisions worth calling out: the last-50 query is
`ORDER BY timestamp DESC LIMIT 50` (indexed) reversed for display; timestamps are
stored in UTC and rendered in the viewer's local time; all chat content is
rendered with `textContent` (no HTML injection); Stooq numbers are parsed and
formatted with the invariant culture so a comma-decimal host cannot corrupt
prices; consumers use manual acks with prefetch 1 and discard undeserializable
messages without requeue to avoid poison loops.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Docker (only for RabbitMQ — or any local RabbitMQ on `localhost:5672`)

## Running

```bash
# 1. Start RabbitMQ (management UI: http://localhost:15672, guest/guest)
docker compose up -d

# 2. Start the web app (http://localhost:8080)
dotnet run --project src/ChatApp.Web

# 3. In another terminal, start the bot
dotnet run --project src/ChatApp.StockBot
```

The SQLite database is created and seeded automatically on first run.
Both apps retry the broker connection, so the startup order doesn't matter.

**Demo users** (seeded): `alice` / `Passw0rd!` and `bob` / `Passw0rd!` — or
register new users on the Register page.

## Testing with two users

1. Open `http://localhost:8080` in a normal window, log in as **alice**.
2. Open it in a private/incognito window, log in as **bob**.
3. Chat in both directions — messages appear instantly in both windows with
   author and time.
4. As alice, type `/stock=aapl.us` — within a few seconds **StockBot** posts
   `AAPL.US quote is $NNN.NN per share` in **both** windows.
5. Try `/stock=notarealticker` — StockBot answers with a friendly "no quote
   available" message. Try `/help` — you get a private "command not understood"
   hint (bob sees nothing).
6. Reload the page: the `/stock=...` command lines are gone (never persisted),
   the StockBot replies are still there, and only the last 50 messages render.
7. Create a room (e.g. `finance`) in the sidebar; it appears in bob's list live.
   Messages and `/stock` replies stay inside their room.
8. Decoupling check: stop the bot (Ctrl+C), chat keeps working and a `/stock`
   command just waits in the queue; restart the bot and the quote arrives.
   Stop RabbitMQ (`docker compose stop`) and the sender is told the stock
   service is unavailable while normal chat is unaffected.

## Running the tests

```bash
dotnet test
```

50 unit tests — no Docker, network or broker needed. They cover the
`/stock` command parser, the Stooq CSV parser (including `N/D` unknown tickers,
malformed payloads and comma-decimal cultures), the exact bot message format,
the last-50/ordering query (against in-memory SQLite, a real relational
provider) and the bot pipeline with a stubbed HTTP handler (timeouts, HTTP
errors and retry behavior).

## Project structure

```
FinancialChat.sln
├── src/
│   ├── ChatApp.Web/         # ASP.NET Core 8: Razor Pages, SignalR hub, EF Core + SQLite,
│   │                        # cookie auth, RabbitMQ publisher + reply consumer
│   ├── ChatApp.StockBot/    # Worker Service: request consumer, Stooq client, CSV parser
│   └── ChatApp.Contracts/   # Shared message DTOs + broker topology (the only coupling)
├── tests/
│   └── ChatApp.Tests/       # xUnit
└── docker-compose.yml       # RabbitMQ (with management UI)
```
