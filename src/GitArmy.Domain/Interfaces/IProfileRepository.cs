using GitArmy.Domain.Entities;

namespace GitArmy.Domain.Interfaces;

public interface IProfileRepository
{
    Task<Profile?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task UpsertAsync(Profile profile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Profile>> GetTopRankingAsync(int count, CancellationToken cancellationToken = default);
}
