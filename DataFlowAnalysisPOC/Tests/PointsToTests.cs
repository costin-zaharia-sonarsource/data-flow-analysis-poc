using System;
using System.Threading.Tasks;
using DataFlowAnalysisPOC.Analyzers;
using DataFlowAnalysisPOC.Tests.Utils;
using NUnit.Framework;

namespace DataFlowAnalysisPOC.Tests
{
    public class PointsToTests
    {
        [Test]
        public async Task InsideMethod()
        {
            const string code = @"
namespace TestCases
{
    public class Foo
    {
        public int All(bool flag)
        {
            var x = new One();
            object y = x; // x and y point to the same instance

            var z = flag ? new Two() : y; // z can point to any

            if (flag) {
                y = null;
            }

            return 0;
        }
    }

    public class One {}
    public class Two : One {}
}";
            var compilation = CompilationBuilder.Create(code, new PointsToAnalyzer());

            foreach (var diagnostic in await compilation.GetAllDiagnosticsAsync())
            {
                Console.WriteLine(diagnostic.GetMessage());
            }
        }

        [Test]
        public async Task PointToConstThenToNull()
        {
            const string code = @"
namespace TestCases
{
    public class Clazz
    {
        public Clazz CONST = new Clazz();
        public int All()
        {
            var x = CONST;
            Foo(x);
            if (CONST != null)
            {
                x = null;
                Bar(x);
            }
            return 1;
        }
        public void Foo(Clazz x) {}
        public void Bar(Clazz x) {}
    }
}";
            var compilation = CompilationBuilder.Create(code, new PointsToAnalyzer());

            foreach (var diagnostic in await compilation.GetAllDiagnosticsAsync())
            {
                Console.WriteLine(diagnostic.GetMessage());
            }
        }

        [Test]
        public async Task StaticAndInstancePointer()
        {
            const string code = @"
namespace TestCases
{
    public class Clazz
    {
        public static Clazz STATIC = new Clazz();
        public Clazz CONST = new Clazz();
        public int All()
        {
            var x = STATIC;
            if (STATIC == null)
            {
                x = CONST;
            }
            Foo(x);
            return 1;
        }
        public void Foo(Clazz x) {}
    }
}";
            var compilation = CompilationBuilder.Create(code, new PointsToAnalyzer());

            foreach (var diagnostic in await compilation.GetAllDiagnosticsAsync())
            {
                Console.WriteLine(diagnostic.GetMessage());
            }
        }

        [Test]
        public async Task RecognizeConstant()
        {
            const string code = @"
namespace TestCases
{
    public class Clazz
    {
        public int CONST = 1;
        public int All()
        {
            var x = CONST;
            Foo(x);
            return 1;
        }
        public void Foo(int x) {}
        public void Bar(int x) {}
    }
}";
            var compilation = CompilationBuilder.Create(code, new PointsToAnalyzer());

            foreach (var diagnostic in await compilation.GetAllDiagnosticsAsync())
            {
                Console.WriteLine(diagnostic.GetMessage());
            }
        }
    }
}
