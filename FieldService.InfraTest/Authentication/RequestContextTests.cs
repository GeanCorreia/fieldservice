using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;
using FieldService.Shared.Types;

namespace FieldService.InfraTest.Authentication;

public sealed class RequestContextTests
{
    [Fact]
    public void Should_create_request_context_with_hashed_ip_and_user_agent()
    {
        var ipAddress = "192.168.1.100";
        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        var channel = RequestChannel.Http;
        var traceId = "trace-1";

        var context = RequestContext.Create(channel, ipAddress, traceId, userAgent);

        Assert.NotEqual(Guid.Empty, context.RequestId);
        Assert.Equal(channel, context.Channel);
        Assert.NotNull(context.IpAddressHash);
        Assert.NotNull(context.UserAgentHash);
        Assert.NotEqual(ipAddress, context.IpAddressHash); // Deve ser hash, não valor original
        Assert.NotEqual(userAgent, context.UserAgentHash); // Deve ser hash, não valor original
        Assert.Equal(64, context.IpAddressHash.Length); // SHA-256 = 64 caracteres hex
        Assert.Equal(64, context.UserAgentHash.Length);
    }

    [Fact]
    public void Should_create_request_context_without_user_agent()
    {
        var token = "token-2";
        var ipAddress = "10.0.0.1";
        var channel = RequestChannel.WebSocket;
        var traceId = "trace-123";
        var context = RequestContext.Create( channel, ipAddress, traceId);

        Assert.NotEqual(Guid.Empty, context.RequestId);
        Assert.Equal(channel, context.Channel);
        Assert.NotNull(context.IpAddressHash);
        Assert.Null(context.UserAgentHash);
    }

    [Fact]
    public void Should_create_unique_correlation_id_for_each_call()
    {
        var ipAddress = "172.16.0.1";
        var traceId = "trace-456";
        
        var context1 = RequestContext.Create(RequestChannel.Http, ipAddress, traceId);
        var context2 = RequestContext.Create(RequestChannel.Http, ipAddress, traceId);

        Assert.NotEqual(context1.RequestId, context2.RequestId);
    }

    [Fact]
    public void Should_use_datetime_service_for_timestamp()
    {
        var ipAddress = "203.0.113.0";
        var beforeCreate = DateTime.UtcNow;
        var traceId = "trace-789";

        var context = RequestContext.Create(RequestChannel.Grpc, ipAddress, traceId);

        var afterCreate = DateTime.UtcNow;

        Assert.InRange(context.Timestamp, beforeCreate.AddHours(-5), afterCreate.AddHours(5));
    }

    [Fact]
    public void Should_generate_consistent_hash_for_same_ip()
    {
        var ipAddress = "198.51.100.1";
        var traceId = "trace-101";

        var context1 = RequestContext.Create(RequestChannel.Http, ipAddress, traceId);
        var context2 = RequestContext.Create(RequestChannel.Http, ipAddress, traceId);

        Assert.Equal(context1.IpAddressHash, context2.IpAddressHash);
    }

    [Fact]
    public void Should_throw_when_ip_address_is_null()
    {
        var token = "token-6";
        var traceId = "trace-103";
        Assert.Throws<ArgumentNullException>(() =>
             RequestContext.Create(RequestChannel.Http, null!, traceId));      }

    [Fact]
    public void Should_throw_when_ip_address_is_empty()
    {
        var token = "token-7";
        var traceId = "trace-102";
        Assert.Throws<ArgumentException>(() =>
            RequestContext.Create(RequestChannel.Http, string.Empty, traceId));
    }

    [Fact]
    public void Should_hash_different_ips_to_different_hashes()
    {
        var ip1 = "192.168.1.1";
        var ip2 = "192.168.1.2";
        var traceId = "trace-104";

        var context1 = RequestContext.Create(RequestChannel.Http, ip1, traceId);
        var context2 = RequestContext.Create(RequestChannel.Http, ip2, traceId);

        Assert.NotEqual(context1.IpAddressHash, context2.IpAddressHash);
    }

    [Fact]
    public void Should_hash_different_user_agents_to_different_hashes()
    {
        var token = "token-9";
        var ipAddress = "10.0.0.1";
        var userAgent1 = "Mozilla/5.0";
        var userAgent2 = "Chrome/96.0";
        var traceId = "trace-105";

        var context1 = RequestContext.Create(RequestChannel.Http, ipAddress, traceId, userAgent1);
        var context2 = RequestContext.Create(RequestChannel.Http, ipAddress, traceId, userAgent2);

        Assert.NotEqual(context1.UserAgentHash, context2.UserAgentHash);
    }
}
