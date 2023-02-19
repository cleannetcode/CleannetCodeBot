using CSharpFunctionalExtensions;

namespace CleannetCode_bot.Infrastructure;

public static class HandlerResults
{
    private const string NotMatchingTypeOfUpdate = "NotMatchingTypeOfUpdate";

    public static Result NotMatchingType => Result.Failure(NotMatchingTypeOfUpdate);

    /// <summary>
    ///     Можем фильтрировать результы
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TypeIsMatched(this Result result) => result.IsFailure && result.Error == NotMatchingTypeOfUpdate;
}