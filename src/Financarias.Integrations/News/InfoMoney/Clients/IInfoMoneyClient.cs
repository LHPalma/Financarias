using Refit;

namespace Financarias.Integrations.News.InfoMoney.Clients;

public interface IInfoMoneyClient
{
    [Get("/feed/")]
    Task<Stream> GetFeedAsync(CancellationToken cancellationToken = default);
}