using System;
using System.Collections.Generic;
using System.Text;
using Application.FlashcardCollection;

namespace Application.Authorization.FlashcardCollection;

public class CanAccessFlashcardCollectionSpecification : IAuthorizationSpecification<Guid>
{   
    private IFlashcardCollectionReadRepository _flashcardCollectionReadRepository;

    public CanAccessFlashcardCollectionSpecification(IFlashcardCollectionReadRepository flashcardCollectionReadRepository)
    {
        _flashcardCollectionReadRepository = flashcardCollectionReadRepository;
    }

    public async  Task<bool> IsSatisfiedByAsync(Guid flashcardCollectionId, Guid userId, CancellationToken cancellationToken)
    {
        var flashcardCollection = await _flashcardCollectionReadRepository.GetByIdAsync(flashcardCollectionId);
        if (flashcardCollection is null)
        {
           return false;
        }

        return flashcardCollection.UserId == userId;
    }
}
