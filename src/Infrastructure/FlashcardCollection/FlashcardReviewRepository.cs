using Application.Abstractions.Data;
using Application.Abstractions.Repository;
using Application.FlashcardCollection;
using Domain.FlashcardCollection;

namespace Infrastructure.FlashcardCollection;


internal sealed class FlashcardReviewRepository : BaseWriteRepository, IFlashcardReviewRepository
{
    public FlashcardReviewRepository(IApplicationDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public async Task AddAsync(FlashcardReview review)
    {
        await _applicationDbContext.FlashcardReviews.AddAsync(review);
    }
}
