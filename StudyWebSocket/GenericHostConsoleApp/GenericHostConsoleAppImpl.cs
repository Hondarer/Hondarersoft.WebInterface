﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenericHostConsoleApp
{
    public class GenericHostConsoleAppImpl : LifetimeEventsHostedService
    {
        private readonly ITestService testService;

        private readonly IConfiguration configration;

        public GenericHostConsoleAppImpl(ILogger<GenericHostConsoleAppImpl> logger, IHostApplicationLifetime appLifetime, ITestService testService, IConfiguration configration) : base(logger, appLifetime)
        {
            this.testService = testService;
            this.configration = configration;
        }

        protected override void OnStarted()
        {
            testService.Hello();
            logger.LogInformation("{0} {1} {2}", configration.GetValue<string>("Option1"), configration.GetValue<int>("Option2"), configration.GetValue<Guid>("Option3"));

            base.OnStarted();

            Task.Run(() => 
            {
                Thread.Sleep(5000);

                //Console.WriteLine("Press any key");
                //Console.ReadLine();

                Environment.ExitCode = 123;

                appLifetime.StopApplication();
            });
        }
    }
}