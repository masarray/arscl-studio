using System.Xml;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Scl.Semantics;

public static class SclTopLevelIndexer
{
    public static SclTopLevelIndex Build(SclSyntaxDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!document.TryGetNode(document.RootHandle, out var rootNode) ||
            rootNode is not XmlElement root)
        {
            throw new InvalidOperationException("The SCL document does not have a valid root handle.");
        }

        var header = SclNodeHandle.None;
        var dataTypeTemplates = SclNodeHandle.None;
        var substations = new List<SclNodeHandle>();
        var communications = new List<SclNodeHandle>();
        var ieds = new List<SclIedSummary>();
        var privateElements = new List<SclNodeHandle>();

        foreach (XmlNode child in root.ChildNodes)
        {
            if (child is not XmlElement element ||
                !document.TryGetHandle(element, out var handle))
            {
                continue;
            }

            switch (element.LocalName)
            {
                case "Header":
                    header = handle;
                    break;

                case "Substation":
                    substations.Add(handle);
                    break;

                case "Communication":
                    communications.Add(handle);
                    break;

                case "IED":
                    ieds.Add(new SclIedSummary(
                        handle,
                        element.GetAttribute("name"),
                        element.HasAttribute("manufacturer")
                            ? element.GetAttribute("manufacturer")
                            : null));
                    break;

                case "DataTypeTemplates":
                    dataTypeTemplates = handle;
                    break;

                case "Private":
                    privateElements.Add(handle);
                    break;
            }
        }

        return new SclTopLevelIndex(
            header,
            substations,
            communications,
            ieds,
            dataTypeTemplates,
            privateElements);
    }
}
