using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;

namespace ArSclStudio.Engine.Search;

public static class SclSemanticSearch
{
    public static SclSearchResultProjection[] Search(
        SclDocumentState state,
        string query,
        int maximumResults,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumResults);

        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var term = query.Trim();
        var results = new List<SclSearchResultProjection>(
            Math.Min(maximumResults, 64));

        var inspected = 0;

        foreach (var node in state.SemanticIndex.Nodes)
        {
            if ((inspected++ & 0xFF) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (!Matches(node.DisplayName, term) &&
                !Matches(node.Badge, term) &&
                !Matches(node.Kind.ToString(), term))
            {
                continue;
            }

            var details = SclNodeDetailsProjector.Create(
                state,
                node.Handle);

            results.Add(new SclSearchResultProjection(
                node.Handle,
                node.DisplayName,
                node.Kind.ToString(),
                details.Path));

            if (results.Count >= maximumResults)
            {
                break;
            }
        }

        return [.. results];
    }

    private static bool Matches(
        string? value,
        string term) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(
            term,
            StringComparison.OrdinalIgnoreCase);
}
