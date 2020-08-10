using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenericHostConsoleApp
{
    public class TestService : ITestService
    {
        private readonly ILogger logger;

        public TestService(ILogger<TestService> logger)
        {
            this.logger = logger;
        }

        public void Hello()
        {
            logger.LogInformation("Hello!");
        }
    }
}
