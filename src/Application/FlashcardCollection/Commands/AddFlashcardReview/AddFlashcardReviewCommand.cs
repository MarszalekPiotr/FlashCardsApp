using Application.Abstractions.Messaging;

namespace Application.FlashcardCollection.Commands.AddFlashcardReview;

public sealed record AddFlashcardReviewCommand(Guid FlaschardCollectionId, Guid FlashcardId, int ReviewResult) : ICommand<Guid>;
