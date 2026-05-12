# Data Model: User Authentication

## Entities

### User

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `Guid` | PK |
| `DisplayName` | `string` | NOT NULL, max 100 chars |
| `Email` | `string` | NOT NULL, max 256 chars, UNIQUE, indexed, stored as lowercase |
| `PasswordHash` | `string` | NOT NULL |
| `CreatedAt` | `DateTimeOffset` | NOT NULL, UTC |

**Invariants**:
- `Email` is normalised to `email.ToLowerInvariant()` before every write and lookup (FR-011).
- `PasswordHash` is the PBKDF2/HMACSHA512 output from `PasswordHasher<User>` — never the raw password.

---

### RefreshToken

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `Guid` | PK |
| `Token` | `string` | NOT NULL, max 32 chars, UNIQUE, indexed |
| `UserId` | `Guid` | NOT NULL, FK → `User.Id`, CASCADE DELETE |
| `FamilyId` | `Guid` | NOT NULL, indexed — shared across all rotations of one login session |
| `ExpiresAt` | `DateTimeOffset` | NOT NULL, UTC |
| `IsRevoked` | `bool` | NOT NULL, default `false` |
| `CreatedAt` | `DateTimeOffset` | NOT NULL, UTC |

**Invariants**:
- A token is **valid** only when `IsRevoked = false` AND `ExpiresAt > UtcNow`.
- On **rotation**: old token's `IsRevoked` → `true`; new token inserted with same `FamilyId`.
- On **reuse detection**: ALL tokens with the same `FamilyId` are set to `IsRevoked = true` (FR-013).
- On **logout**: only the presented token's `IsRevoked` → `true` (single-token invalidation).

---

## EF Core Configuration

```csharp
// User
modelBuilder.Entity<User>(entity =>
{
    entity.HasKey(u => u.Id);
    entity.HasIndex(u => u.Email).IsUnique();
    entity.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
    entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
});

// RefreshToken
modelBuilder.Entity<RefreshToken>(entity =>
{
    entity.HasKey(rt => rt.Id);
    entity.HasIndex(rt => rt.Token).IsUnique();
    entity.HasIndex(rt => rt.FamilyId);
    entity.Property(rt => rt.Token).HasMaxLength(32).IsRequired();
    entity.HasOne<User>()
          .WithMany()
          .HasForeignKey(rt => rt.UserId)
          .OnDelete(DeleteBehavior.Cascade);
});
```

All `DateTimeOffset` columns map to `timestamp with time zone` in PostgreSQL (Npgsql default).

---

## Repository Interfaces (Domain layer)

### IUserRepository

```csharp
public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string normalisedEmail, CancellationToken ct = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

### IRefreshTokenRepository

```csharp
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeByFamilyIdAsync(Guid familyId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

---

## Migration

One migration: `AddUserAuthentication`
- Creates `Users` table with unique index on `Email`
- Creates `RefreshTokens` table with unique index on `Token`, non-unique index on `FamilyId`, FK to `Users` with cascade delete
