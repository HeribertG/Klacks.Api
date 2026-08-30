// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// Correlation token returned by <see cref="IRecipeRunRecorder.BeginOrResumeAsync"/>: the chat loop
/// carries it through the turn and hands it to CompleteAsync/AbortAsync so the final status change
/// never has to re-look-up the row by (conversation, recipe).
/// </summary>
public sealed record RecipeRunHandle(Guid RunId, string RecipeName, Guid UserId, string ConversationId);
