using Application.Abstractions.Data;
using Application.Abstractions.Repository;
using Application.SRS;
using Domain.SRS;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SRS;

internal sealed class SrsStateRepository : BaseWriteRepository, ISrsStateRepository
{
    public SrsStateRepository(IApplicationDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public async Task<SrsState?> GetByFlashcardIdAsync(Guid flashcardId, CancellationToken cancellationToken)
    {
        return await _applicationDbContext.SrsStates
            .SingleOrDefaultAsync(s => s.FlashcardId == flashcardId, cancellationToken);
    }

    public void Add(SrsState srsState)
    {
        _applicationDbContext.SrsStates.Add(srsState);
    }
}
