using FieldService.Shared;
using FieldService.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.InfraTest.Shared;

public sealed class HashServiceTests
{
    private readonly IHashService _hashService;

    public HashServiceTests()
    {
        var services = new ServiceCollection();
        services.AddSharedModule();
        
        var provider = services.BuildServiceProvider();
        _hashService = provider.GetRequiredService<IHashService>();
    }

    [Fact]
    public void Should_generate_sha256_hash()
    {
        var input = "test-value";
        var hash = _hashService.Generate(input);
        
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length); // SHA-256 produces 64 hex characters
        Assert.Matches("^[a-f0-9]{64}$", hash); // Lowercase hex
    }

    [Fact]
    public void Should_generate_consistent_hash_for_same_input()
    {
        var input = "consistent-value";
        var hash1 = _hashService.Generate(input);
        var hash2 = _hashService.Generate(input);
        
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Should_generate_different_hashes_for_different_inputs()
    {
        var hash1 = _hashService.Generate("value1");
        var hash2 = _hashService.Generate("value2");
        
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Should_throw_when_input_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => _hashService.Generate(null!));
    }

    [Fact]
    public void Should_throw_when_input_is_empty()
    {
        Assert.Throws<ArgumentException>(() => _hashService.Generate(string.Empty));
    }

    [Fact]
    public void Should_throw_when_input_is_whitespace()
    {
        Assert.Throws<ArgumentException>(() => _hashService.Generate("   "));
    }

    [Fact]
    public void Should_generate_known_hash_for_test_input()
    {
        // Known SHA-256 hash for "hello world"
        var hash = _hashService.Generate("hello world");
        var expectedHash = "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9";
        
        Assert.Equal(expectedHash, hash);
    }
}
