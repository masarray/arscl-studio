using System.Xml;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Editing;

public static class SclEditPolicy
{
    public static bool CanEditDescription(SclDocumentState state, SclNodeHandle handle)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Syntax.Metadata.RootNamespace == "http://www.iec.ch/61850/2003/SCL" &&
            state.Syntax.TryGetNodeInfo(handle, out var node) && node is not null &&
            node.Kind == SclSyntaxNodeKind.Element && node.LocalName == "IED" &&
            node.NamespaceUri == state.Syntax.Metadata.RootNamespace &&
            node.Parent == state.Syntax.RootHandle;
    }

    internal static void Validate(SclDocumentState state, IReadOnlyList<DescriptionChange> changes,
        bool after, CancellationToken cancellationToken)
    {
        if (changes.Count > 64)
        {
            throw new InvalidOperationException("A transaction is limited to 64 property changes.");
        }

        var targets = new HashSet<SclNodeHandle>();
        foreach (var change in changes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!targets.Add(change.Target) || !CanEditDescription(state, change.Target))
            {
                throw new InvalidOperationException("Only distinct, standard SCL IED descriptions may be edited in M2A.");
            }

            if ((change.Before?.Length ?? 0) > 4096)
            {
                throw new InvalidOperationException("Existing description exceeds the reversible editing limit.");
            }

            if (change.After is { } value)
            {
                if (value.Length > 4096)
                {
                    throw new InvalidOperationException("Description exceeds the 4096-character editing limit.");
                }

                XmlConvert.VerifyXmlChars(value);
            }

            state.Syntax.TryGetAttributeValue(change.Target, "desc", out var current);
            if (current != (after ? change.After : change.Before))
            {
                throw new InvalidOperationException(after
                    ? "Post-edit integrity check failed." : "Description changed since the edit was prepared.");
            }
        }
    }
}
