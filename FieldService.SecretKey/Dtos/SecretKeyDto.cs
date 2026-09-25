using FieldService.SecretKey.Entities;
using FieldService.SecretKey.Interfaces;
using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.SecretKey.Dtos;

public record SecretKeyDto(
    SecretKeyReferenceDto Reference,
    ISecretKeyType Secret
    );
    
public record SecretKeyReferenceDto(
    Guid Id,
    SecretKeyType Type,
    Guid TenantId,
    string Name,
    bool IsDeleted 
    );

public record SecretKeyHistoryDto(
    DateTimeOffset AtTime,
    SecretKeyReferenceDto Reference
    );


    
