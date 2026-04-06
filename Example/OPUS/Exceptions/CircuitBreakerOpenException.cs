using System;


namespace Example.OPUS.Exceptions
{
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
    }
}
