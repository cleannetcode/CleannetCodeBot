using CSharpFunctionalExtensions;

namespace CleannetCodeBot.Core;

public static class Errors
{
    public static Result NotMatchingStateResult()
    {
        return Result.Failure("Not matching state for features handler");
    }
}