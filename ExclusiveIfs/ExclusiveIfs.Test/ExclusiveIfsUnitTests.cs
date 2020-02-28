using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using ExclusiveIfs;

namespace ExclusiveIfs.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        private const string singleIf = @"
using System;
namespace Test {
    class Program {
        public static int test(bool b) {
            if (b) {
                return 1;
            }
            else {
                return 0;
            }
    }
}";

        private const string twoIfsSecondShouldBeIfElse = @"
namespace Test {
    class Program {
        public static int test(int x) {
            if (x > 1) return 1;
            if (x <= 1) return 0;
            return -1;
        }
    }
}";
        //No diagnostics expected to show up
        [DataTestMethod]
        [DataRow(""),
         DataRow(singleIf),]
        public void NoDiagnostic(string testCode)
        {
            VerifyCSharpDiagnostic(testCode);
        }

        //Diagnostic triggered and checked for
        [DataTestMethod]
        [DataRow(twoIfsSecondShouldBeIfElse, 6, 13),]
        public void DiagnosticRaised(string testCode, int line, int column)
        {
            var expected = new DiagnosticResult
            {
                Id = ExclusiveIfsAnalyzer.DiagnosticId,
                Message = new LocalizableResourceString(
                    nameof(Resources.AnalyzerMessageFormat),
                    Resources.ResourceManager,
                    typeof(Resources))
                    .ToString(),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line, column)
                }
            };

            VerifyCSharpDiagnostic(testCode, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ExclusiveIfsCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ExclusiveIfsAnalyzer();
        }
    }
}
