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
        private const string singleIfLastStatement = @"
namespace Test {
    class Program {
        public static void test(bool b, ref int x) {
            x++;
            if (b)
                x--;
        }
    }
}";
        private const string satIfsInBlock = @"
namespace Test {
    class Program {
        public static int test(int x) {
            if (x > 1) return 1;
            if (x < 1) return 0;
            return -1;
        }
    }
}";

        private const string satIfsInSwitch = @"
namespace Test {
    class Program {
        public static int test(int x, int y) {
            switch (x) {
            case 0:
                if (y > 1)
                    return 0;
                if (y < 1) {
                    return 1;
                }
                break;
            default:
                return 2;
            }
        }
    }
}";
        private const string threeSatIfs = @"
namespace Test {
    class Program {
        public static int test (int x)
        {
            if (x >= 0) return 1;
            if (x == 0) {
                return 0;
            }
            if (x <= 0)
                return -1;
            return 5;
        }
    }
}";
        private const string trivialUnsatIfs = @"
namespace Test {
    class Program {
        public static int test(bool b) {
            int x = 0;
            if (b)  x = 1;
            if (!b) x = 2;
            return x;
        }
    }
}";
        private const string exclusiveIfsInBlock = @"
namespace Test {
    class Program {
        public static int test(int x) {
            if (x > 1)  return 1;
            if (x <= 1) return 0;
            return -1;
        }
    }
}";

        private const string exclusiveIfsInSwitchCase = @"
namespace Test {
    class Program {
        public static int test(int x, int y) {
            switch (x) {
            case 0:
                if (y > 1)
                    return 0;
                if (y < 1) { return 1; }
                break;
            default:
                return 2;
            }
        }
    }
}";
        // 7, 17, 9, 17
        private const string threeUnsatIfs = @"
namespace Test {
    class Program {
        public static int test (int x)
        {
            if (x > 0) { return 1; }
            if (x == 0) 
                return 0;
            if (x < 0)
            {
                return -1;
            }
            return 5;
        }
    }
}";
        private const string unsatRange = @"
namespace Test {
    class Program {
        public static int test (int x) {
            int y = 0;
            if (0 < x && x <= 10)
                y = 1;
            if (20 < x && x < 50)
                y = 2;
            return y;
        }
    }
}";
        private const string unsatParen = @"
namepsace Test {
    class Program {
        public static int test (bool p, bool q) {
            int x = 0;
            if (p || q)
                x = 1;
            if (!(p && q))
                x = 2;
            return x;
        }
    }
}";
        // not yet passing this test
        private const string unsatLocal = @"
namespace Test {
    class Program {
        public static int test (bool p, bool q) {
            int y = 0;
            bool r = p || q;
            if (r)
                y = 1;
            if (!(p && q))
                y = 2;
            return y;
        }
    }
}";
        //No diagnostics expected to show up
        [DataTestMethod]
        [DataRow(""),
         DataRow(singleIf),
         DataRow(singleIfLastStatement),
         //DataRow(satIfsInBlock),
         //DataRow(satIfsInSwitch),
         DataRow(threeSatIfs),]
        public void NoDiagnostic(string testCode)
        {
            VerifyCSharpDiagnostic(testCode);
        }

        //Diagnostic triggered and checked for
        [DataTestMethod]
        [DataRow(exclusiveIfsInBlock, 5, 13),
         DataRow(exclusiveIfsInSwitchCase, 7, 17),
         DataRow(trivialUnsatIfs, 6, 13),
         DataRow(unsatRange, 6, 13),
         DataRow(unsatParen, 6, 13),
         DataRow(unsatLocal, 7, 13),
         ]
        public void DiagnosticRaised(string testCode, int line, int column)
        {
            VerifyCSharpDiagnostic(testCode, Result(line, column));
        }

        [DataTestMethod]
        [DataRow(threeUnsatIfs, 6, 13, 7, 13),]
        public void TwoDiagnosticsRaised(string testCode, int line1, int col1, int line2, int col2)
        {
            DiagnosticResult[] expected =
            {
                Result(line1, col1),
                Result(line2, col2),
            };

            VerifyCSharpDiagnostic(testCode, expected);
        }

        private DiagnosticResult Result(int line, int column)
        {
            return new DiagnosticResult {
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
