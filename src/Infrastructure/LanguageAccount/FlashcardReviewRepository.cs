using Application.Abstractions.Data;
using Application.Abstractions.Repository;
using Application.SRS;
using Domain.SRS;

namespace Infrastructure.SRS;

internal sealed class FlashcardReviewRepository : BaseWriteRepository, IFlashcardReviewRepository
{
    public FlashcardReviewRepository(IApplicationDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public void Add(FlashcardReview review)
    {
        _applicationDbContext.FlashcardReviews.Add(review);
    }
}
