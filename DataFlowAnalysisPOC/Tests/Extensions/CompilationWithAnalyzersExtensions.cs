using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataFlowAnalysisPOC.Tests.Extensions
{
    public static class CompilationWithAnalyzersExtensions
    {
        public static async Task PrintDiagnostics(this CompilationWithAnalyzers compilation)
        {
            foreach (var diagnostic in await compilation.GetAllDiagnosticsAsync())
            {
                Console.WriteLine(diagnostic.GetMessage());
            }
        }
    }
}
