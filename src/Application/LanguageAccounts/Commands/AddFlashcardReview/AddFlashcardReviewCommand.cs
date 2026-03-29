using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Commands.AddFlashcardReview;

public sealed record AddFlashcardReviewCommand(Guid FlashcardId, int ReviewResult) : ICommand<Guid>;
