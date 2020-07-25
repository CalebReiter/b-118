using System;

namespace b_118.Exceptions
{
    class PrefixMismatchException : ApplicationException
    {
        public PrefixMismatchException(string Message) : base(Message)
        {
        }
    }
}
