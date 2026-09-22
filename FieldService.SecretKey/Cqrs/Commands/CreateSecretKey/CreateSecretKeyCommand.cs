using FieldService.SecretKey.Dtos;
using FieldService.Shared.Types;
using MediatR;

namespace FieldService.SecretKey.Cqrs.Commands.CreateSecretKey;

public record CreateSecretKeyCommand(
    Guid UserId,
    SecretKeyDto SecretKeyDto) : IRequest;