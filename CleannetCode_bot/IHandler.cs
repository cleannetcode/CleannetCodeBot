using CSharpFunctionalExtensions;

namespace CleannetCode_bot
{
    public interface IUpdateHandler
    {
        Task<Result> HandleAsync();
    }
}