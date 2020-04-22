using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
//using Microsoft.Z3;

namespace ExclusiveIfs {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExclusiveIfsAnalyzer : DiagnosticAnalyzer {
        #region BOILERPLATE
        public const string DiagnosticId = "ExclusiveIfs";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context) {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.IfStatement);
        }
        #endregion // BOILERPLATE

        private static void AnalyzeNode(SyntaxNodeAnalysisContext analysis) {
            var ifStatement = (IfStatementSyntax) analysis.Node;

            // If next node is not an if statement, return
            var outerStatements = ifStatement.Parent.ChildNodes().ToImmutableArray();
            int nextStatementIndex = outerStatements.IndexOf(ifStatement) + 1;

            if (nextStatementIndex >= outerStatements.Length
                || !(outerStatements[nextStatementIndex] is IfStatementSyntax)) {
                return;
            }

            IfStatementSyntax nextIfStatement = (IfStatementSyntax) outerStatements[nextStatementIndex];

            // Invoking z3.exe
            Z3InputVisitor visitor = new Z3InputVisitor(analysis);
            string z3input = $"(assert (and {visitor.Visit(ifStatement.Condition)} {visitor.Visit(nextIfStatement.Condition)})) (check-sat)";
            z3input = visitor.GetDeclarations() + z3input;
            string z3output = "";
            using (StreamWriter file = File.CreateText("z3in.txt")) {
                file.WriteLine(z3input);
            }
            using (Process z3 = new Process()) {
                z3.StartInfo.FileName = @"C:\Program Files (x86)\z3-4.8.7-x64-win\z3-4.8.7-x64-win\bin\z3.exe";
                //z3.StartInfo.Arguments = @"C:\Users\Udnamtam\Source\Repos\CS6110\ExclusiveIfs\ExclusiveIfs.Vsix\bin\Debug\z3in.txt";
                z3.StartInfo.Arguments = @"C:\Users\Udnamtam\Source\Repos\CS6110\ExclusiveIfs\ExclusiveIfs.Test\bin\Debug\netcoreapp2.0\z3in.txt";
                z3.StartInfo.UseShellExecute = false;
                z3.StartInfo.CreateNoWindow = true;
                z3.StartInfo.RedirectStandardOutput = true;
                z3.Start();
                z3output = z3.StandardOutput.ReadToEnd();
                z3.WaitForExit();
            }
            if (z3output.Contains("unsat")) {
                analysis.ReportDiagnostic(Diagnostic.Create(Rule, ifStatement.GetLocation()));
            }

            // Using the Z3 library
            //using (Context smt = new Context()) {

            //    var solver = smt.MkSolver();
            //    SMTConversionVisitor visitor = new SMTConversionVisitor(smt, analysis);

            //    solver.Assert(visitor.VisitBoolExpr(ifStatement.Condition));
            //    solver.Assert(visitor.VisitBoolExpr(nextIfStatement.Condition));
            //    if (solver.Check() == Status.UNSATISFIABLE) {
            //        analysis.ReportDiagnostic(Diagnostic.Create(Rule, ifStatement.GetLocation()));
            //    }
            //}
        }
    }
}