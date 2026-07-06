namespace Financarias.Application.News;

public static class NewsCategoryExtensions
{
    public static string ToFeedLabel(this NewsCategory category)
        => category.ToString();
}