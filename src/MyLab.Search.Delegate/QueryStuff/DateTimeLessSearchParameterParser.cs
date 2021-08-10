﻿namespace MyLab.Search.Delegate.QueryStuff
{
    class DateTimeLessSearchParameterParser : ISearchParameterParser
    {
        public bool CanParse(string word)
        {
            return word.StartsWith("<") && SupportedDateTimeFormat.CanParse(word.Substring(1));
        }

        public ISearchQueryParam Parse(string word, int rank)
        {
            var val = SupportedDateTimeFormat.Parse(word.Substring(1));
            return new DateTimeRangeQueryParameter(null, val, rank);
        }
    }
}