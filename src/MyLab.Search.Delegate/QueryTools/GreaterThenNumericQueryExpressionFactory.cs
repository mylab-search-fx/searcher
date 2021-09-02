﻿namespace MyLab.Search.Delegate.QueryTools
{
    class GreaterThenNumericQueryExpressionFactory : IQueryExpressionFactory
    {
        public bool TryCreate(string literal, out IQueryExpression queryExpression)
        {
            queryExpression = null;

            if (literal.Length < 2 || !literal.StartsWith('>'))
                return false;

            if (int.TryParse(literal.Substring(1), out var val))
            {
                queryExpression = new RangeNumericQueryExpression
                {
                    Greater = val
                };
            }

            return queryExpression != null;
        }
    }
}