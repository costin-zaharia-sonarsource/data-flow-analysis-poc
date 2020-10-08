using System.Threading.Tasks;
using DataFlowAnalysisPOC.Analyzers;
using DataFlowAnalysisPOC.Tests.Extensions;
using DataFlowAnalysisPOC.Tests.Utils;
using NUnit.Framework;

namespace DataFlowAnalysisPOC.Tests
{
    public class ValueContentTests
    {
        [Test]
        public async Task CheckValueContent()
        {
            const string code = @"
namespace TestCases
{
    public class Foo
    {
        public void Bar(bool flag, int param)
        {
            int c1 = 0;
            int c2 = 0;
            int c3 = c1 + c2;
        }
    }
}
";
            await CompilationBuilder.Create(code, new ValueContentAnalyzer())
                                    .PrintDiagnostics();
        }

        [Test]
        public async Task SwitchExpressionDifferentValues()
        {
            const string code = @"
namespace TestCases
{
    public class Foo
    {
        public void TernaryAndCopy(int flag, string param)
        {
            var c = flag == 1 ? ""a"" : ""b"";
            var d = c;
        }
    }
}
";
            await CompilationBuilder.Create(code, new ValueContentAnalyzer())
                                    .PrintDiagnostics();
        }
    }
}
