﻿using System;

namespace MyLab.Search.Searcher
{
    public class InvalidTokenException : Exception
    {
        public InvalidTokenException(string message, Exception inner) : base(message, inner)
        {

        }

        public InvalidTokenException(string message) : base(message)
        {

        }
    }
}