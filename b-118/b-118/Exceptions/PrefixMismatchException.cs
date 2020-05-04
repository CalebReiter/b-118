using System;
using System.Collections.Generic;
using System.Text;

namespace b_118.Exceptions
{
    class PrefixMismatchException : ApplicationException
    {
        public PrefixMismatchException(string Message) : base(Message)
        {
        }
    }
}
