using System;
using System.Collections.Generic;
using System.Text;
using Domain.FlashcardCollection;

namespace Application.FlashcardCollection;

public interface IFlashcardReviewRepository
{
    void Add(FlashcardReview review);
}
