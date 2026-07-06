using System.Net;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using Financarias.Application.News;
using Financarias.Integrations.News.InfoMoney.Clients;

namespace Financarias.Integrations.News.InfoMoney.Providers;

public sealed partial class InfoMoneyNewsProvider(
    IInfoMoneyClient client
) : INewsFeedGateway
{
    public async Task<IReadOnlyList<NewsArticle>> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = await client.GetFeedAsync(cancellationToken);

        using var reader = XmlReader.Create(stream);

        var feed = SyndicationFeed.Load(reader);

        return feed.Items.Select(item =>
        {
            var html = item.Summary?.Text;

            var categories = item.Categories
                .Select(category => category.Name)
                .Where(categoryName => !string.IsNullOrWhiteSpace(categoryName))
                .ToList();

            return new NewsArticle(
                Title: item.Title?.Text,
                Link: item.Links.FirstOrDefault()?.Uri?.ToString(),
                Summary: CleanSummary(html),
                SummaryHtml: html,
                Section: categories.FirstOrDefault(),
                Tags: categories.Skip(1).ToList(),
                PublishedAt: item.PublishDate == default ? null : item.PublishDate
            );
        }).ToList();
    }

    private static string? CleanSummary(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        var text = TagRegex().Replace(html, " ");
        text = WebUtility.HtmlDecode(text);
        text = WhitespaceRegex().Replace(text, " ");
        text = BoilerplateRegex().Replace(text, " ").Trim();

        return text.Length == 0 ? null : text;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"The post\b.*?\bappeared first on InfoMoney\s*\.?", RegexOptions.Singleline)]
    private static partial Regex BoilerplateRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}