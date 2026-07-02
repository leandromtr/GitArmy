using GitArmy.Domain.Entities;
using GitArmy.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GitArmy.Infrastructure.Persistence;

internal sealed class ProfileRepository : IProfileRepository
{
    private readonly AppDbContext _context;

    public ProfileRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Profile?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Profiles
            .FirstOrDefaultAsync(p => p.Username == username, cancellationToken);
    }

    public async Task UpsertAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var tracked = _context.ChangeTracker.Entries<Profile>()
            .FirstOrDefault(e => e.Entity.Username == profile.Username);

        if (tracked is null)
            _context.Profiles.Add(profile);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Profile>> GetTopRankingAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.Profiles
            .AsNoTracking()
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.SubstitutionYear)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
