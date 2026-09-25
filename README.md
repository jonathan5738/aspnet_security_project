# Security Overview
> **Note** When it comes to testing, I primarily tested my API using an .http file to make HTTP requests. I did not implement integration testing, as it was too time-consuming and honestly overkill for this simple API, Please check SecurityProject.http file
---

This document summarizes the security controls implemented in this project, for quick review by developers and auditors. It covers **XSS**, **SQL Injection**, **Authentication**, and **Authorization**.

---

## 1. SQL Injection Prevention

**Implementation:** `PostService.cs`, `UserService.cs`

All database queries use **parameterized queries** via `NpgsqlCommand` and `cmd.Parameters.AddWithValue(...)` instead of string concatenation. This is the primary and most reliable defense against SQL injection, applied consistently across:
- User creation, lookup by email, lookup by id
- Post creation, retrieval, update, deletion

```csharp
var query = "SELECT * FROM users WHERE email=@email";
cmd.Parameters.AddWithValue("email", email);
```

> **Note:** `Program.cs` mentions that EF Core (which supports prepared statements natively) was considered but not used; raw Npgsql with parameters is used instead. This is still safe as long as every query keeps using parameters — any future query built with string interpolation would reintroduce the risk.

---

## 2. Cross-Site Scripting (XSS) Prevention

**Implementation:** `InputSanitizer.cs`

User-supplied text (first name, last name, post title/excerpt/body) is passed through `InputSanitizer.Sanitize()` before being stored, which:
- Strips `<script>` tags
- Strips all HTML tags
- Strips common SQL keywords (defense in depth alongside parameterized queries)
- Strips quote characters (`'`, `"`) and semicolons

```csharp
cmd.Parameters.AddWithValue("title", InputSanitizer.Sanitize(data.Title));
```
---

## 3. Authentication

**Implementation:** `JwtUtils.cs`, `Program.cs`, `UserService.cs`

- **Password storage:** Passwords are hashed with **BCrypt** (`BC.HashPassword`) before being stored, and verified with `BC.Verify` on login — plaintext passwords are never persisted.
- **Tokens:** On successful register/login, a **JWT** is issued (`JwtUtils.GenerateToken`), signed with `HmacSha256` using a symmetric key from configuration, with issuer, audience, and a 10-hour expiration.
- **Token validation:** `Program.cs` configures `AddJwtBearer` with:
  - `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, and `ValidateIssuerSigningKey` all enabled
  - Signing key sourced from configuration, not hardcoded
- **PII protection:** First name, last name, and email are encrypted at rest via `IEncryptionService` and decrypted only when returned to the authenticated owner (`/users/profile`).

---

## 4. Authorization

**Implementation:** `OwnershipMiddleware.cs`, `Program.cs`

- **Role-based access:** Sensitive endpoints (`/users/profile`, `POST /posts`, `PUT /posts/{id}`, `DELETE /posts/{id}`) are protected with `.RequireAuthorization("UserPolicy")`, which requires a valid JWT with the `User` role.
- **Resource ownership:** `OwnershipMiddleware` is applied to `PUT` and `DELETE` requests under `/posts`. It:
  1. Validates the route `id` is a well-formed integer
  2. Loads the post and confirms it exists
  3. Extracts the caller's user id from the JWT `NameIdentifier` claim
  4. Returns `403 Forbidden` if the caller is **not** the post's author
  5. Only then forwards the request to the endpoint handler

```csharp
app.UseWhen(context =>
    context.Request.Path.StartsWithSegments("/posts") &&
    (context.Request.Method == "PUT" || context.Request.Method == "DELETE"),
    app => app.UseCheckAuthorOwnership());
```

This prevents users from editing or deleting posts they don't own, even if they are otherwise authenticated.

---

## Summary Table

| Risk               | Mitigation                                      | File(s) |
|---------------------|--------------------------------------------------|---------|
| SQL Injection       | Parameterized queries (Npgsql)                   | `PostService.cs`, `UserService.cs` |
| XSS                 | Regex-based input sanitization                   | `InputSanitizer.cs` |
| Authentication      | BCrypt password hashing + JWT (HMAC-SHA256)      | `JwtUtils.cs`, `Program.cs`, `UserService.cs` |
| Authorization       | Role policy + resource-ownership middleware      | `OwnershipMiddleware.cs`, `Program.cs` |
| Data confidentiality| Field-level encryption of PII                    | `UserService.cs` |

