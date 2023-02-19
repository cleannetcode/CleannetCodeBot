namespace CleannetCode_bot.Features.Promotions;

public class PromotionsStorage : IPromotionsStorage
{
    public Task<Promoter?> GetPromoterAsync(long promoterId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}