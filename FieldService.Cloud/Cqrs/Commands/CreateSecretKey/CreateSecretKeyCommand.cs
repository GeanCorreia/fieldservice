using FieldService.Cloud.Dtos;
using FieldService.Shared.Types;
using MediatR;

namespace FieldService.Cloud.Cqrs.Commands.CreateSecretKey;

public record CreateSecretKeyCommand(
    SecretKeyDto SecretKeyDto,
    Guid? UserId = null) : IRequest;