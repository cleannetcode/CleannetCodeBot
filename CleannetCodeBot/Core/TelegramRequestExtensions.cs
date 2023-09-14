using CleannetCodeBot.Infrastructure;
using CSharpFunctionalExtensions;
using Telegram.Bot.Types.Enums;

namespace CleannetCodeBot.Core;

public static class TelegramRequestExtensions
{
    public static Result IsPrivateChat(this TelegramRequest request)
    {
        return request.Update.Message is { Text: { }, From: { }, Chat: { Type: ChatType.Private } }
            ? Result.Success()
            : HandlerResults.NotMatchingType;
    }
}