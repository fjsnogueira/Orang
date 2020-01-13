// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Orang.Expressions
{
    internal class BinaryExpression : Expression
    {
        public BinaryExpression(string expressionText, string identifier, string value, ExpressionKind kind) : base(expressionText, identifier)
        {
            Value = value;
            Kind = kind;
        }

        public override ExpressionKind Kind { get; }

        public string Value { get; }
    }
}
