using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.Hosting
{
    public interface IExitService
    {
        public int? ExitCode { get; }

        public bool IsExiting { get; }

        public bool Requset(int exitCode);
    }
}
