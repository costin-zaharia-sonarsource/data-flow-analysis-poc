using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataFlowAnalysisPOC.Tests.Utils
{
    public static class CompilationBuilder
    {
        public static CompilationWithAnalyzers Create(string code, DiagnosticAnalyzer analyzer) =>
            Create(CSharpSyntaxTree.ParseText(code), analyzer);

        private static CompilationWithAnalyzers Create(SyntaxTree syntaxTree, params DiagnosticAnalyzer[] analyzers) =>
            CSharpCompilation.Create("DataFlowAnalysisPOC", options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                             .AddSyntaxTrees(syntaxTree)
                             .AddReferences(MetadataReference.CreateFromFile(typeof(string).Assembly.Location))
                             .WithAnalyzers(ImmutableArray.Create(analyzers));
    }
}
