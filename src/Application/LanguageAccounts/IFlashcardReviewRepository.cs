using Domain.SRS;

namespace Application.SRS;

public interface IFlashcardReviewRepository
{
    void Add(FlashcardReview review);
}
