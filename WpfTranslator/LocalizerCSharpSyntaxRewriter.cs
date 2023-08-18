using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfTranslator
{
    public class LocalizerCSharpSyntaxRewriter : CSharpSyntaxRewriter
    {
        private readonly Func<string, bool> filter;
        private readonly Func<string, string> translatorGetKeyFunc;
        private readonly string localizerGetStringMethodName;
        private readonly string localizedDescriptionAttributeName;
        private readonly string[] methodsToSkip;

        public LocalizerCSharpSyntaxRewriter(Func<string, bool> filter, Func<string, string> translatorGetKeyFunc, string localizerGetStringMethodName, string localizedDescriptionAttributeName, string[] methodsToSkip)
        {
            this.filter = filter;
            this.translatorGetKeyFunc = translatorGetKeyFunc;
            this.localizerGetStringMethodName = localizerGetStringMethodName;
            this.localizedDescriptionAttributeName = localizedDescriptionAttributeName;
            this.methodsToSkip = methodsToSkip;
        }

        public string LocalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException($"'{nameof(code)}' cannot be null or whitespace.", nameof(code));
            }

            var sucess = SyntaxFactory.ParseSyntaxTree(code).TryGetRoot(out var code_parsed);
            if (!sucess)
            {
                throw new Exception("Cannot parse syntax code.");
            }

            var result = Visit(code_parsed)?.ToFullString();

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new Exception("Localizer result cannot be null or whitespace.");
            }
            return result;
        }

        private SyntaxNode CreateLocalizerGetStringInvocationExpression(string valueText)
        {
            var keyText = translatorGetKeyFunc(valueText);

            var arg0 = SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(keyText)));

            var args = SyntaxFactory.ArgumentList().AddArguments(arg0);

            var exp = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(localizerGetStringMethodName), args);
            return exp;
        }

        private SyntaxNode CreateLocalizedDescriptionAtributeExpression(string valueText)
        {
            var keyText = translatorGetKeyFunc(valueText);

            var arg0 = SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(keyText)));

            var args = SyntaxFactory.AttributeArgumentList().AddArguments(arg0);

            var exp = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(localizedDescriptionAttributeName), args);
            return exp;
        }



        [return: NotNullIfNotNull("node")]
        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            switch (node?.Kind())
            {
                case SyntaxKind.StringLiteralExpression: // "str" and @"str"

                    var text = ((LiteralExpressionSyntax)node).Token.ValueText;

                    if (filter(text))
                    {
                        return CreateLocalizerGetStringInvocationExpression(text);
                    }
                    break;

                    //case SyntaxKind.InterpolatedStringExpression: // $"a{b}c" and @$"a{b}c"
                    //    var exp = (InterpolatedStringExpressionSyntax)node;
                    //    break;
                    //case SyntaxKind.InterpolatedStringText: // "a" and "c" from $"a{b}c"
                    //    break;
            }
            return base.Visit(node);
        }

        public override SyntaxNode? VisitAttribute(AttributeSyntax node)
        {
            if ((node.Name is SimpleNameSyntax identifierName && identifierName.Identifier.Text == "Description") ||
                (node.Name is QualifiedNameSyntax qualifiedName && qualifiedName.Right.Identifier.Text == "Description"))
            {
                var arg0 = node.ArgumentList?.Arguments.FirstOrDefault() ?? throw new NotSupportedException();
                if (arg0.Expression.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    var text = ((LiteralExpressionSyntax)arg0.Expression).Token.ValueText;

                    if (filter(text))
                    {
                        return CreateLocalizedDescriptionAtributeExpression(text);
                    }
                }
            }
            //return base.VisitAttribute(node);
            return node; // skip visiting inside attributes
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var methodName = node.Expression.ToString();
            if (methodsToSkip.Any(s => methodName.Contains(s)))
            {
                return node; // skip visiting inside the method
            }
            return base.VisitInvocationExpression(node);
        }

        //public override SyntaxToken VisitToken(SyntaxToken token)
        //{
        //    switch (token.Kind())
        //    {
        //        case SyntaxKind.StringLiteralToken: // "str" and @"str"
        //            var text = token.ValueText;

        //            text = translator(text);

        //            return SyntaxFactory.InvocationExpression(
        //                SyntaxFactory.IdentifierName(translatorFuncName),
        //                SyntaxFactory.ArgumentList(new SeparatedSyntaxList<ArgumentSyntax>() { SyntaxFactory.Argument(
        //                    SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
        //                    SyntaxFactory.Literal(text))) }));

        //        //case SyntaxKind.InterpolatedStringTextToken: // "a" and "c" from $"a{b}c"
        //        //    var textPart = token.ValueText;

        //        //    textPart = translator(textPart);

        //        //    return SyntaxFactory.Token(
        //        //        token.LeadingTrivia,
        //        //        SyntaxKind.InterpolatedStringTextToken,
        //        //        textPart, textPart,
        //        //        token.TrailingTrivia);

        //        default:
        //            return base.VisitToken(token);
        //    }
        //}

    }

}
