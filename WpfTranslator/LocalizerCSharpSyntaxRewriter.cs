using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpTranslator
{
    public class LocalizerCSharpSyntaxRewriter : CSharpSyntaxRewriter
    {
        [return: NotNullIfNotNull("node")]
        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            switch (node?.Kind())
            {
                case SyntaxKind.StringLiteralExpression: // "str" and @"str"
                    break;
                case SyntaxKind.InterpolatedStringExpression: // $"a{b}c" and @$"a{b}c"
                    break;
                case SyntaxKind.InterpolatedStringText: // "a" and "c" from $"a{b}c"
                    break;
            }
            return base.Visit(node);
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            switch (token.Kind())
            {
                case SyntaxKind.StringLiteralToken: // "str" and @"str"
                    var text = token.ValueText;

                    text += "#"; // todo

                    return SyntaxFactory.Token(
                        token.LeadingTrivia,
                        SyntaxKind.StringLiteralToken,
                        $"\"{text}\"", $"\"{text}\"",
                        token.TrailingTrivia);

                case SyntaxKind.InterpolatedStringTextToken: // "a" and "c" from $"a{b}c"
                    var textPart = token.ValueText;

                    textPart += "#"; // todo

                    return SyntaxFactory.Token(
                        token.LeadingTrivia,
                        SyntaxKind.InterpolatedStringTextToken,
                        textPart, textPart,
                        token.TrailingTrivia);

                default:
                    return base.VisitToken(token);
            }
        }

    }

}
