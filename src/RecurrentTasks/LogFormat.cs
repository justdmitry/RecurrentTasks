using System;
using System.Collections.Generic;
using System.Text;

namespace RecurrentTasks
{
    public class LogFormat
    {
        public string Format { get; private set; }
        public object[] Args { get; private set; }

        public LogFormat(string format, params object[] args)
        {
            Format = format;
            Args = args;
        }
    }
}
