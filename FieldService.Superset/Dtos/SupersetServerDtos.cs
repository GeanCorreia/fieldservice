namespace FieldService.Superset.Dtos;


internal record SupersetLoginRequest(
    string username, 
    string password, 
    string provider = "db", 
    bool refresh = true
);

internal record SupersetLoginResponse(
    string access_token
);


internal record SupersetResourcePayload(
    string type, 
    string id
);

internal record SupersetRlsPayload(
    string clause
);

internal record SupersetGuestTokenRequest(
    string userName,
    IEnumerable<SupersetResourcePayload> resources,
    IEnumerable<SupersetRlsPayload> rls
);

internal record SupersetGuestTokenResponse(
    string token
);