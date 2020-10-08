using System.Threading.Tasks;
using DataFlowAnalysisPOC.Analyzers;
using DataFlowAnalysisPOC.Tests.Extensions;
using DataFlowAnalysisPOC.Tests.Utils;
using NUnit.Framework;

namespace DataFlowAnalysisPOC.Tests
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
            await CompilationBuilder.Create(code, new CopyAnalyzer())
                                    .PrintDiagnostics();
        }
    }
}
