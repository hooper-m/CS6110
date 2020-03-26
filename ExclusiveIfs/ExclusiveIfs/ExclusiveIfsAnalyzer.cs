using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Z3;

namespace ExclusiveIfs {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExclusiveIfsAnalyzer : DiagnosticAnalyzer {
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

            using (Context smt = new Context()) {

                var solver = smt.MkSolver();
                SMTConversionVisitor visitor = new SMTConversionVisitor(smt, analysis);

                //BoolExpr ifCondition1 = visitor.VisitBoolExpr(ifStatement.Condition);
                //BoolExpr ifCondition2 = visitor.VisitBoolExpr(nextIfStatement.Condition);

                //solver.Assert(smt.MkAnd(ifCondition1, ifCondition2));

                solver.Assert(visitor.VisitBoolExpr(ifStatement.Condition));
                solver.Assert(visitor.VisitBoolExpr(nextIfStatement.Condition));
                //solver.Assert(smt.MkOr(smt.MkNot(ifCondition1), smt.MkNot(ifCondition2)));
                if (solver.Check() == Status.UNSATISFIABLE) {
                    analysis.ReportDiagnostic(Diagnostic.Create(Rule, nextIfStatement.Condition.GetLocation()));
                }
                //SMTConversionWalker walker = new SMTConversionWalker(SMTContext, context, ifStatement.Condition);

                //BoolExpr ifCondition1 = SyntaxToZ3(context, SMTContext, ifStatement.Condition);
                //BoolExpr ifCondition2 = SyntaxToZ3(context, SMTContext, nextIfStatement.Condition);

                //var solver = SMTContext.MkSolver();
                //solver.Assert(SMTContext.MkAnd(ifCondition1, ifCondition2));
                //if (solver.Check() == Status.UNSATISFIABLE) {
                //    context.ReportDiagnostic(Diagnostic.Create(Rule, nextIfStatement.GetLocation()));
                //}
            }
        }

        private static BoolExpr SyntaxToZ3(SyntaxNodeAnalysisContext analysisContext, Context SMTContext, SyntaxNode condition) {

            if (condition is LiteralExpressionSyntax literal) {
                switch (literal.Kind()) {
                case SyntaxKind.TrueLiteralExpression:
                    return SMTContext.MkTrue();
                case SyntaxKind.FalseLiteralExpression:
                    return SMTContext.MkFalse();
                }
            }
            // bool b = true;
            // if (b) ...
            else if (condition is IdentifierNameSyntax id) {
                var idType = analysisContext.SemanticModel.GetTypeInfo(condition).Type;
                if (idType.Name == "Boolean") {
                    return SMTContext.MkBoolConst(id.Identifier.ValueText);
                }
                //else if (idType.Name == "Inte")
            }

            else if (condition is PrefixUnaryExpressionSyntax pref) {
                // !b
                if (pref.OperatorToken.Kind() == SyntaxKind.ExclamationToken) {
                    return SMTContext.MkNot(SyntaxToZ3(analysisContext, SMTContext, pref.Operand));
                }
            }

            else if (condition is BinaryExpressionSyntax bin) {
                switch (bin.OperatorToken.Kind()) {
                case SyntaxKind.AmpersandAmpersandToken:
                    return SMTContext.MkAnd(SyntaxToZ3(analysisContext, SMTContext, bin.Left),
                                            SyntaxToZ3(analysisContext, SMTContext, bin.Right));
                case SyntaxKind.BarBarToken:
                    return SMTContext.MkOr(SyntaxToZ3(analysisContext, SMTContext, bin.Left),
                                           SyntaxToZ3(analysisContext, SMTContext, bin.Right));
                case SyntaxKind.EqualsEqualsToken:
                    return SMTContext.MkEq(SyntaxToZ3(analysisContext, SMTContext, bin.Left),
                                           SyntaxToZ3(analysisContext, SMTContext, bin.Right));
                case SyntaxKind.ExclamationEqualsToken:
                    // SMTContext.MkDistinct()?
                    return SMTContext.MkNot(SMTContext.MkEq(SyntaxToZ3(analysisContext, SMTContext, bin.Left),
                                                            SyntaxToZ3(analysisContext, SMTContext, bin.Right)));
                // doesn't work, since MkGt needs ArithExp

                // (5 + 5) < 6
                // 1 + 2 + 3
                //case SyntaxKind.GreaterThanToken:
                //    return SMTContext.MkGt(SyntaxToZ3(analysisContext, SMTContext, bin.Left),
                //                           SyntaxToZ3(analysisContext, SMTContext, bin.Right));
                }
            }

            return SMTContext.MkBool(true);
        }
    }
}
