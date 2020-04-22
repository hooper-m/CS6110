using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Z3;

namespace ExclusiveIfs {
    public class SMTConversionVisitor : CSharpSyntaxVisitor<Expr> {

        private SyntaxNodeAnalysisContext analysis;
        private Context smt;

        public SMTConversionVisitor(Context smt, SyntaxNodeAnalysisContext analysis) {
            this.smt = smt;
            this.analysis = analysis;
        }

        public override Expr Visit(SyntaxNode node) {
            return base.Visit(node);
        }

        public BoolExpr VisitBoolExpr(SyntaxNode node) {
            return (BoolExpr)Visit(node);
        }

        public ArithExpr VisitArithExpr(SyntaxNode node) {
            return (ArithExpr)Visit(node);
        }

        public override Expr DefaultVisit(SyntaxNode node) {
            return smt.MkTrue();
        }

        public override Expr VisitLiteralExpression(LiteralExpressionSyntax node) {
            switch (node.Kind()) {
            case SyntaxKind.TrueLiteralExpression:
                return smt.MkTrue();
            case SyntaxKind.FalseLiteralExpression:
                return smt.MkFalse();
            case SyntaxKind.NumericLiteralExpression:
                return smt.MkInt(node.Token.ValueText);
            }

            return DefaultVisit(node);
        }

        public override Expr VisitBinaryExpression(BinaryExpressionSyntax node) {
            switch (node.Kind()) {
            case SyntaxKind.LogicalAndExpression:
                return smt.MkAnd(VisitBoolExpr(node.Left),
                                 VisitBoolExpr(node.Right));
            case SyntaxKind.LessThanExpression:
                return smt.MkLt(VisitArithExpr(node.Left),
                                VisitArithExpr(node.Right));
            case SyntaxKind.EqualsExpression:
                return smt.MkEq(Visit(node.Left),
                                Visit(node.Right));
            case SyntaxKind.AddExpression:
                return smt.MkAdd(VisitArithExpr(node.Left),
                                 VisitArithExpr(node.Right));
            case SyntaxKind.NotEqualsExpression:
                return smt.MkDistinct(VisitBoolExpr(node.Left),
                                      VisitBoolExpr(node.Right));
            case SyntaxKind.GreaterThanExpression:
                return smt.MkGt(VisitArithExpr(node.Left),
                                VisitArithExpr(node.Right));
            case SyntaxKind.GreaterThanOrEqualExpression:
                return smt.MkGe(VisitArithExpr(node.Left),
                                VisitArithExpr(node.Right));
            case SyntaxKind.LessThanOrEqualExpression:
                return smt.MkLe(VisitArithExpr(node.Left),
                                VisitArithExpr(node.Right));
            case SyntaxKind.SubtractExpression:
                return smt.MkSub(VisitArithExpr(node.Left),
                                 VisitArithExpr(node.Right));
            case SyntaxKind.MultiplyExpression:
                return smt.MkMul(VisitArithExpr(node.Left),
                                 VisitArithExpr(node.Right));
            case SyntaxKind.LogicalOrExpression:
                return smt.MkOr(VisitBoolExpr(node.Left),
                                VisitBoolExpr(node.Right));
            // division is slow in Z3
            //case SyntaxKind.SlashToken:
            }

            return DefaultVisit(node);
        }

        public override Expr VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) {

            switch (node.Kind()) {
            case SyntaxKind.LogicalNotExpression:
                return smt.MkNot(VisitBoolExpr(node.Operand));
            }

            return DefaultVisit(node);
        }

        public override Expr VisitIdentifierName(IdentifierNameSyntax node) {
            var nodeType = analysis.SemanticModel.GetTypeInfo(node).Type;
            if (nodeType.Name == "Boolean") {
                return smt.MkBoolConst(node.Identifier.ValueText);
            }
            else if (nodeType.Name == "Int32") {
                return smt.MkIntConst(node.Identifier.ValueText);
            }

            return DefaultVisit(node);
        }

        public override Expr VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) {
            return Visit(node.Expression);
        }
    }
}
