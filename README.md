# SmartRequestLogger

![NuGet](https://img.shields.io/nuget/v/SmartRequestLogger)
![Build](https://github.com/yourusername/SmartRequestLogger/actions/workflows/publish.yml/badge.svg)
![License](https://img.shields.io/badge/license-MIT-blue.svg)

Lightweight ASP.NET Core middleware for structured request/response logging — correlation IDs,
timing, slow-request flagging, and configurable header redaction. No dependencies beyond
`Microsoft.Extensions.Logging`.

## Why

Most request-logging setups either log too little (just status codes) or too much (raw bodies
with auth tokens in them). SmartRequestLogger gives you a correlation ID per request out of the
box, flags slow requests automatically, and redacts sensitive headers by default — so it's safe
to drop into production without a config review.

## Install

```bash
dotnet add package SmartRequestLogger
```

## Usage

```csharp
// Program.cs
builder.Services.AddSmartRequestLogger(options =>
{
    options.SlowRequestThresholdMs = 500;
    options.LogRequestBody = false; // default
    options.IgnorePaths.Add("/swagger");
});

var app = builder.Build();

app.UseSmartRequestLogger(); // place early, after exception handling middleware

app.MapControllers();
app.Run();
```

Every response will include an `X-Correlation-ID` header (generated, or propagated if the caller
sent one), and logs will look like:

```
info: SmartRequestLogger.RequestLoggingMiddleware[0]
      => CorrelationId: 4f2a9e1b8c3d4a2e
      GET /api/orders/42 responded 200 in 34ms
```

Requests over the configured threshold are logged at `Warning` level with a `[SLOW]` marker,
so you can filter on log level alone to find performance outliers.

## Options

| Option                   | Default                                      | Description                                      |
| ------------------------ | -------------------------------------------- | ------------------------------------------------ |
| `CorrelationIdHeader`    | `X-Correlation-ID`                           | Header used to read/propagate the correlation ID |
| `LogRequestBody`         | `false`                                      | Log request body (truncated to `MaxBodyLength`)  |
| `LogResponseBody`        | `false`                                      | Log response body (truncated to `MaxBodyLength`) |
| `MaxBodyLength`          | `2000`                                       | Max characters logged before truncation          |
| `RedactedHeaders`        | Authorization, Cookie, Set-Cookie, X-Api-Key | Headers redacted in logs                         |
| `IgnorePaths`            | `/health`, `/favicon.ico`                    | Paths skipped entirely                           |
| `SlowRequestThresholdMs` | `1000`                                       | Threshold to log at `Warning` with `[SLOW]` flag |

## Releases

Versioning is automated with [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) —
there's no manual `dotnet pack -p:Version=...` or `git tag` step. The version comes from
`version.json` plus the git commit height on the branch being built, and the CI workflow tags
and publishes automatically on every push to `main`, `develop`, or `release/*`.

Unlike semantic-release, NBGV doesn't infer branch-specific prerelease labels (`-pre`, `-rc`) from
a single shared config — `version.json` is itself versioned in git, so each branch simply carries
its own copy:

| Branch      | `version.json` → `"version"` | Example output |
| ----------- | ---------------------------- | -------------- |
| `develop`   | `"1.1-pre"`                  | `1.1.0-pre.23` |
| `release/*` | `"1.1-rc"`                   | `1.1.0-rc.4`   |
| `main`      | `"1.1"`                      | `1.1.0`        |

In practice: when you cut a `release/1.1` branch from `develop`, edit `version.json` once on that
branch to say `"1.1-rc"`, commit it, and every subsequent merge into that branch is versioned and
published automatically from there — no further manual steps. Same when `release/1.1` merges to
`main`: update `version.json` to drop the prerelease label, and `main` starts publishing stable
`1.1.x` releases.

## Contributing

Issues and PRs welcome. Run tests with `dotnet test` from the repo root.

## License

MIT
