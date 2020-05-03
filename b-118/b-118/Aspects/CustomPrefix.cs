using b_118.Exceptions;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace b_118.Aspects
{
    class CustomPrefix
    {

        private string _prefix;

        public CustomPrefix(string prefix)
        {
            _prefix = prefix;
        }

        public async Task Verify(string prefix, Func<Task> action)
        {
            if (_prefix == prefix)
            {
                await action();
            }
            else
                throw new PrefixMismatchException($"{prefix} does not match {_prefix}");

        }

    }
}
