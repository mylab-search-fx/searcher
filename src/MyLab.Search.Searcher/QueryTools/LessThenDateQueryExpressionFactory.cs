﻿using System;
using System.Globalization;
using System.Linq;

namespace MyLab.Search.Searcher.QueryTools
{
    class LessThenDateQueryExpressionFactory : IQueryExpressionFactory
    {
        public bool TryCreate(string literal, out IQueryExpression queryExpression)
        {
            queryExpression = null;

            if (literal.Length < 2 || literal[0] != '<') return false;

            if (DateTime.TryParseExact(literal.Substring(1), DateQueryFormats.Formats.ToArray(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                queryExpression = new RangeDateQueryExpression(null, dt);
            }
            return queryExpression != null;
        }
    }
}