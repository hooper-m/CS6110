using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExclusiveIfs {
    class Z3InputVisitor : CSharpSyntaxVisitor<String> {

        Dictionary<string, string> declarations;
        SyntaxNodeAnalysisContext analysisContext;

        public string GetDeclarations() {
            string declarations = "";
            foreach (string decl in this.declarations.Values) {
                declarations += decl;
            }

            return declarations;
        }

        public Z3InputVisitor(SyntaxNodeAnalysisContext analysis) {
            analysisContext = analysis;
            declarations = new Dictionary<string, string>();
        }

        public override string DefaultVisit(SyntaxNode node) {
            return "true";
        }

        public override string VisitLiteralExpression(LiteralExpressionSyntax node) {
            switch (node.Kind()) {
            case SyntaxKind.TrueLiteralExpression:
                return "true";
            case SyntaxKind.FalseLiteralExpression:
                return "false";
            case SyntaxKind.NumericLiteralExpression:
                return node.Token.ValueText;
            }

            return DefaultVisit(node);
        }

        public override string VisitBinaryExpression(BinaryExpressionSyntax node) {
            switch (node.Kind()) {
            case SyntaxKind.LogicalAndExpression:
                return $"(and {Visit(node.Left)} {Visit(node.Right)})";
            case SyntaxKind.LessThanExpression:
                return $"(< {Visit(node.Left)} {Visit(node.Right)})";
            case SyntaxKind.EqualsExpression:
                return $"(= {Visit(node.Left)} {Visit(node.Right)})";
            case SyntaxKind.AddExpression:
                return $"(+ {Visit(node.Left)} {Visit(node.Right)})";
            case SyntaxKind.NotEqualsExpression:
                // smt.MkDistinct()?
                return $"(not (= {Visit(node.Left)} {Visit(node.Right)}))";
            case SyntaxKind.GreaterThanExpression:
                return $"(> {Visit(node.Left)} {Visit(node.Right)})";
            case SyntaxKind.GreaterThanOrEqualExpression:
                return $"(>= {Visit(node.Left)} {Visit(node.Right)})";
            case SyntaxKind.LessThanOrEqualExpression:
                return $"(<= {Visit(node.Left)} {Visit(node.Right)})";
            case SyntaxKind.SubtractExpression:
                return $"(- {Visit(node.Left)} {Visit(node.Right)})";
            case SyntaxKind.MultiplyExpression:
                return $"(* {Visit(node.Left)} {Visit(node.Right)})";
            case SyntaxKind.LogicalOrExpression:
                return $"(or {Visit(node.Left)} {Visit(node.Right)})";
                // division is slow in Z3
                //case SyntaxKind.SlashToken:
            }

            return DefaultVisit(node);
        }

        public override string VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) {

            switch (node.Kind()) {
            case SyntaxKind.LogicalNotExpression:
                return $"(not {Visit(node.Operand)})";
            }

            return DefaultVisit(node);
        }

        public override string VisitIdentifierName(IdentifierNameSyntax node) {
            string identifierName = node.Identifier.ValueText;

            if (!declarations.ContainsKey(identifierName)) {
                var nodeType = analysisContext.SemanticModel.GetTypeInfo(node).Type;
                if (nodeType != null && nodeType.Name == "Boolean") {
                    declarations[identifierName] = $"(declare-fun {identifierName} () Bool)";
                }
                else if (nodeType != null && nodeType.Name == "Int32") {
                    declarations[identifierName] = $"(declare-fun {identifierName} () Int)";
                }
            }

            return identifierName;
        }

        public override string VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) {
            return Visit(node.Expression);
        }
    }
}
