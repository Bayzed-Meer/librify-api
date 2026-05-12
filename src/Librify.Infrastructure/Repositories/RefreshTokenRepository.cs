using Librify.Domain.Entities;
using Librify.Application.Auth.Repositories;
using Librify.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Librify.Infrastructure.Repositories;

public class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken cancellationToken = default)
        => await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
        => await db.RefreshTokens.AddAsync(token, cancellationToken).ConfigureAwait(false);

    public async Task RevokeByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        await db.RefreshTokens
            .Where(rt => rt.FamilyId == familyId && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.IsRevoked, true), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
}
