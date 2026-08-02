# Testing JWT Auth + Rolling Refresh Tokens via Postman

Manual verification steps for the Hot Chocolate GraphQL auth flow (login, bearer-protected
queries, refresh, logout).

## Setup

- Run the API: `dotnet run --project src/Backend/DotNet/Telara.OpsApi`
- **Use HTTPS**: `https://localhost:7162/graphql` — the refresh cookie is `Secure=true`, so it
  will not be set/sent over plain HTTP. Do not use the `:5158` http endpoint for this test.
- Postman's cookie jar must be enabled (default) so the `telara_refresh_token` cookie persists
  between requests to the same host.
- A user must already exist (seeded via `Telara.SQLScripts`) — there is no `register` mutation,
  only `login` / `refreshToken` / `logout`.

## 1. Login

`POST https://localhost:7162/graphql`

```graphql
mutation Login($email: String!, $password: String!) {
  login(input: { email: $email, password: $password }) {
    accessToken
    accessTokenExpiresAtUtc
    user { id email firstName lastName roleName }
  }
}
```

Variables:

```json
{ "email": "...", "password": "..." }
```

The response has **no refresh token field** — it's set as an HttpOnly cookie
(`telara_refresh_token`, path `/graphql`) automatically. Postman's cookie manager should pick it
up on its own.

## 2. Call a protected query with the access token

Set header `Authorization: Bearer <accessToken>`, then run a query marked `[Authorize]`, e.g.
`stationEquipmentReadings` — good smoke test that the bearer token validates.

## 3. Refresh

`POST /graphql` again with:

```graphql
mutation {
  refreshToken {
    accessToken
    accessTokenExpiresAtUtc
    user { id email }
  }
}
```

No variables — it reads the cookie, rotates it (same `familyId`, new token value — rolling
refresh with reuse detection), and returns a fresh `AuthPayload`.

## 4. Logout

```graphql
mutation { logout }
```

Revokes the entire refresh-token family and clears the cookie server-side. Confirm by re-calling
`refreshToken` afterward — it should fail.

## Notes

- Access token lifetime: 15 min. Refresh cookie lifetime: 14 days
  (`Telara.OpsApi/appsettings.Development.json` — `Jwt:AccessTokenMinutes`,
  `Jwt:RefreshTokenDays`).
- JWT `Issuer` / `Audience` / `SigningKey` are **not** in appsettings — they live in .NET user
  secrets. Inspect with:
  `dotnet user-secrets list --project src/Backend/DotNet/Telara.OpsApi`
  (not needed for Postman testing itself, just for reference).

## Relevant source

- `Telara.OpsApi/Program.cs` — GraphQL endpoint mapping, JWT bearer config
- `Telara.OpsApi/GraphQL/Mutation.cs` — `login`, `refreshToken`, `logout`
- `Telara.OpsApi/GraphQL/Query.cs` — `[Authorize]`-protected queries
- `Telara.OpsApi/GraphQL/Types/LoginInput.cs`, `AuthPayload.cs`, `UserProfile.cs`
- `Telara.OpsApi/Auth/TokenService.cs`
- `Telara.OpsApi/appsettings.Development.json`
- `Telara.OpsApi/Properties/launchSettings.json`
