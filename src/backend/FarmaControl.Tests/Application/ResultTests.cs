using FarmaControl.Application.Abstractions;

namespace FarmaControl.Tests.Application;

public sealed class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        Result<int> result = Result<int>.Success(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        var error = AppError.Validation("Nome e obrigatorio");

        Result<string> result = Result<string>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(error, result.Error);
    }
}
