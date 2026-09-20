namespace Financarias.Application.News;

/// <summary>Notícia do feed financeiro. Os campos são nulos quando o feed não os informa.</summary>
/// <param name="Title">Título da notícia.</param>
/// <param name="Link">Endereço da matéria no site de origem.</param>
/// <param name="Summary">Resumo em texto puro.</param>
/// <param name="SummaryHtml">Resumo como veio do feed, em HTML.</param>
/// <param name="Section">Seção da notícia: a primeira categoria do feed.</param>
/// <param name="Tags">Demais categorias do feed, como etiquetas.</param>
/// <param name="PublishedAt">Instante da publicação.</param>
public sealed record NewsArticle(
    string? Title,
    string? Link,
    string? Summary,
    string? SummaryHtml,
    string? Section,
    IReadOnlyList<string> Tags,
    DateTimeOffset? PublishedAt
);