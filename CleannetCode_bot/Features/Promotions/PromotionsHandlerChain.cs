using CleannetCode_bot.Infrastructure;
using CleannetCode_bot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;

namespace CleannetCode_bot.Features.Promotions;

public record UserEntry(long Id, string? Username, string FirstName, string LastName);

public record UsernameEntry(long Id);

public class PromotionsHandlerSaveMembersChain : IHandlerChain
{
    private readonly IGenericRepository<long, UserEntry> _usersById;
    private readonly IGenericRepository<string, UsernameEntry> _usersByName;

    public PromotionsHandlerSaveMembersChain(
        IGenericRepository<long, UserEntry> usersById,
        IGenericRepository<string, UsernameEntry> usersByName)
    {
        _usersById = usersById;
        _usersByName = usersByName;
    }

    public int OrderInChain => -1;

    public async Task<Result> HandleAsync(TelegramRequest request, CancellationToken cancellationToken = default)
    {
        var message = request.Update.Message;
        if (message is not { From: {} })
            return Result.Success();

        var user = message.From;
        var userFoundById = await _usersById.ReadAsync(user.Id, cancellationToken);
        if (userFoundById is null)
            await _usersById.SaveAsync(
                user.Id,
                new(user.Id, user.Username, user.FirstName, user.LastName), 
                cancellationToken);
        if (user.Username is not null)
        {
            var userFoundByName = await _usersByName.ReadAsync(user.Username, cancellationToken);
            if (userFoundByName is null)
                await _usersByName.SaveAsync(user.Username, new(user.Id), cancellationToken);
        }

    }
}

public class PromotionsHandlerChain : IHandlerChain
{
    private readonly IPromotionsStorage _storage;

    public PromotionsHandlerChain(IPromotionsStorage storage)
    {
        _storage = storage;
    }
    public int OrderInChain { get; } = 1;

    public async Task<Result> HandleAsync(TelegramRequest request, CancellationToken cancellationToken = default)
    {
        var message = request.Update.Message;

        if (message is not { Entities: {}, From: {}, Text: {} }) { return Result.Success(); }

        if (!message.Text.StartsWith("/promotion")) { return Result.Success(); }

        var promoter = await _storage.GetPromoterAsync(message.From.Id, cancellationToken);
        var now = DateTime.UtcNow;
        if (promoter is null)
        {
            promoter = new()
            {
                Id = message.From.Id, Username = message.From.Username ?? message.From.FirstName
            };
            return Result.Success();
        }
        // Пользователь не может быть повышен после его повышения в течении 7 дней
        if (promoter.IsPromotedLessThanSevenDays(now))
            return Result.Failure("IsPromotedLessThanSevenDays");
        if (promoter.HadPromoteOtherUserLessThanSevenDays(now))
            return Result.Failure("HadPromoteOtherUserLessThanSevenDays");

        // проверить что пользователь, который промоутит, может промоутить (последний промоут от него был неделю назад)
        // проверить что промоутящий пользователь был запромоучен неделю назад

        // взять пользователя из текста команды
        // var userEntity = message.Entities

        // проверяем что уровень промоутящего больше чем у промоутируемого

        // промоутим пользователя

        // сохраняем инфо о том кто и кого запромоутил
        return Result.Success();
    }
    private static bool IsPromotedLessThanSevenDays(Promoter promoter, DateTime now)
    {

        return promoter.LevelUppers.Any(x => x.Promoted.AddDays(7) > now);
    }
}