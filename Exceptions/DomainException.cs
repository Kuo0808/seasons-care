using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.Exceptions
{
    public class DomainException : Exception
    {
        public string ErrorCode { get; }
        public int StatusCode { get; }
        public IDictionary<string, string[]>? Errors { get; }

        public DomainException(string message, string errorCode, int statusCode = 400, IDictionary<string, string[]>? errors = null) 
            : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
            Errors = errors;
        }
    }
}
