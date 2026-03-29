using Domain.SRS;

namespace Application.SRS;

public interface ISrsStateRepository
{
    Task<SrsState?> GetByFlashcardIdAsync(Guid flashcardId, CancellationToken cancellationToken);
    void Add(SrsState srsState);
}
