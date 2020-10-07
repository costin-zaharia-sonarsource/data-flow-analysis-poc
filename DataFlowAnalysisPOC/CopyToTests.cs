using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using DataFlowAnalysisPOC.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace DataFlowAnalysisPOC
{
    public class CopyToTests
    {
        [Test]
        public async Task ReferenceAndValueCopy()
        {
            const string code = @"
namespace TestCases
{
    using System.Text;

    public class Foo
    {
        public int All()
        {
            var x = new StringBuilder();
            object y = x; // reference copy

            int c1 = 0;
            int c2 = c1; // value copy

            return c2;
        }
    }
}";
            var compilation = CreateCompilation(CSharpSyntaxTree.ParseText(code));

            foreach (var diagnostic in await compilation.GetAllDiagnosticsAsync())
            {
                Console.WriteLine(diagnostic.GetMessage());
            }
        }

        private static CompilationWithAnalyzers CreateCompilation(SyntaxTree syntaxTree) =>
            CSharpCompilation.Create("CopyAnalysisPOC", options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                             .AddSyntaxTrees(syntaxTree)
                             .AddReferences(MetadataReference.CreateFromFile(typeof(string).Assembly.Location))
                             .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new CopyAnalyzer()));
    }
}
