using ArSclStudio.Scl.Semantics;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Documents;

public sealed record SclDocumentState(
    string SourcePath,
    string DisplayName,
    SclSyntaxDocument Syntax,
    SclTopLevelIndex TopLevelIndex,
    SclSemanticIndex SemanticIndex,
    DocumentRevision Revision);
