// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Application.DTOs.Registrations;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class AuthMapper
{
    [MapperIgnoreTarget(nameof(AppUser.Id))]
    [MapperIgnoreTarget(nameof(AppUser.NormalizedUserName))]
    [MapperIgnoreTarget(nameof(AppUser.NormalizedEmail))]
    [MapperIgnoreTarget(nameof(AppUser.EmailConfirmed))]
    [MapperIgnoreTarget(nameof(AppUser.PasswordHash))]
    [MapperIgnoreTarget(nameof(AppUser.SecurityStamp))]
    [MapperIgnoreTarget(nameof(AppUser.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(AppUser.PhoneNumber))]
    [MapperIgnoreTarget(nameof(AppUser.PhoneNumberConfirmed))]
    [MapperIgnoreTarget(nameof(AppUser.TwoFactorEnabled))]
    [MapperIgnoreTarget(nameof(AppUser.LockoutEnabled))]
    [MapperIgnoreTarget(nameof(AppUser.AccessFailedCount))]
    [MapperIgnoreTarget(nameof(AppUser.LockoutEnd))]
    [MapperIgnoreTarget(nameof(AppUser.PasswordResetToken))]
    [MapperIgnoreTarget(nameof(AppUser.PasswordResetTokenExpires))]
    public partial AppUser ToAppUser(RegistrationResource resource);
}
