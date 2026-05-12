using Librify.Domain.Entities;
using Librify.Application.Auth.Repositories;
using Librify.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Librify.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            .ConfigureAwait(false);

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await db.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
}
