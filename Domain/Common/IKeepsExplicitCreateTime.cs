// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Common;

/// <summary>
/// Marks an entity whose CreateTime carries meaning of its own and must survive the insert. The context stamps
/// every added BaseEntity with the time of the save, which is right for almost every row and wrong for one that is
/// written late on behalf of an earlier moment: a stopped turn that persists after the user has moved on stamps its
/// history rows with the time the turn began, so the history keeps reading in the order it happened. An entity with
/// this marker keeps a CreateTime that is set; one without it (or with none) is stamped as before.
/// </summary>
public interface IKeepsExplicitCreateTime
{
}
