﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;
using System.Linq;

namespace AWS.Logger.AspNetCore.Tests
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class TestFilter
    {
        public AWSLoggerConfigSection ConfigSection;

        public IConfiguration LoggerConfigSectionSetup(string jsonFileName,string configSectionInfoBlockName, [System.Runtime.CompilerServices.CallerFilePath]string sourceFilePath="")
        {
            var configurationBuilder = new ConfigurationBuilder()
                                       .SetBasePath(Path.GetDirectoryName(sourceFilePath))
                                       .AddJsonFile(jsonFileName);

            IConfiguration Config;
            if (configSectionInfoBlockName != null)
            {
                Config = configurationBuilder
                    .Build()
                    .GetSection(configSectionInfoBlockName);
            }

            else
            {
                Config = configurationBuilder
                      .Build()
                      .GetSection("AWS.Logging");
            }

            return Config;

        }
        [Fact]
        public void FilterLogLevel()
        {
            var coreLogger = new FakeCoreLogger();
            var logger = new AWSLogger("FilterLogLevel", coreLogger, AWSLoggerProvider.CreateLogLevelFilter(LogLevel.Warning));

            logger.LogTrace("trace");
            logger.LogDebug("debug");
            logger.LogInformation("information");
            logger.LogWarning("warning");
            logger.LogError("error");
            logger.LogCritical("critical");

            Assert.Equal(3, coreLogger.ReceivedMessages.Count);
            Assert.True(coreLogger.ReceivedMessages.Contains("warning\r\n"));
            Assert.True(coreLogger.ReceivedMessages.Contains("error\r\n"));
            Assert.True(coreLogger.ReceivedMessages.Contains("critical\r\n"));
        }

        [Fact]
        public void CustomFilter()
        {
            var coreLogger = new FakeCoreLogger();
            Func<string, LogLevel, bool> filter = (categoryName, level) =>
            {
                if (string.Equals(categoryName, "goodCategory", StringComparison.OrdinalIgnoreCase) && level >= LogLevel.Warning)
                    return true;
                return false;
            };
            var logger = new AWSLogger("goodCategory", coreLogger, filter);

            logger.LogTrace("trace");
            logger.LogWarning("warning");

            Assert.Equal(1, coreLogger.ReceivedMessages.Count);
            Assert.True(coreLogger.ReceivedMessages.Contains("warning\r\n"));
            string val;
            while (!coreLogger.ReceivedMessages.IsEmpty)
            {
                coreLogger.ReceivedMessages.TryDequeue(out val);
            }

            logger = new AWSLogger("badCategory", coreLogger, filter);

            logger.LogTrace("trace");
            logger.LogWarning("warning");

            Assert.Equal(0, coreLogger.ReceivedMessages.Count);
        }
        [Fact]
        public void ValidAppsettingsFilter()
        {
            var configSection = LoggerConfigSectionSetup("ValidAppsettingsFilter.json",null).GetSection("LogLevel");
            if (!(configSection != null && configSection.GetChildren().Count() > 0))
            {
                configSection = null;
            }
            var categoryName = typeof(TestFilter).GetTypeInfo().FullName;
            var coreLogger = new FakeCoreLogger();
            var logger = new AWSLogger(
                categoryName,
                coreLogger, 
                AWSLoggerProvider.CreateConfigSectionFilter(configSection, categoryName));

            logger.LogTrace("trace");
            logger.LogDebug("debug");
            logger.LogInformation("information");
            logger.LogWarning("warning");
            logger.LogError("error");
            logger.LogCritical("critical");

            Assert.Equal(5, coreLogger.ReceivedMessages.Count);
            Assert.True(coreLogger.ReceivedMessages.Contains("warning\r\n"));
            Assert.True(coreLogger.ReceivedMessages.Contains("error\r\n"));
            Assert.True(coreLogger.ReceivedMessages.Contains("critical\r\n"));

        }

        [Fact]
        public void InValidAppsettingsFilter()
        {
            var configSection = LoggerConfigSectionSetup("InValidAppsettingsFilter.json",null).GetSection("LogLevel");
            if (!(configSection != null && configSection.GetChildren().Count() > 0))
            {
                configSection = null;
            }
            var categoryName = typeof(TestFilter).GetTypeInfo().FullName;
            var coreLogger = new FakeCoreLogger();
            var logger = new AWSLogger(
                categoryName,
                coreLogger,
                AWSLoggerProvider.CreateConfigSectionFilter(configSection, categoryName));

            logger.LogTrace("trace");
            logger.LogDebug("debug");
            logger.LogInformation("information");
            logger.LogWarning("warning");
            logger.LogError("error");
            logger.LogCritical("critical");

            Assert.Equal(0, coreLogger.ReceivedMessages.Count);
            Assert.False(coreLogger.ReceivedMessages.Contains("warning\r\n"));
            Assert.False(coreLogger.ReceivedMessages.Contains("error\r\n"));
            Assert.False(coreLogger.ReceivedMessages.Contains("critical\r\n"));

            categoryName = "AWS.Log";
            logger = new AWSLogger(
                categoryName,
                coreLogger,
                AWSLoggerProvider.CreateConfigSectionFilter(configSection, categoryName));

            logger.LogTrace("trace");
            logger.LogDebug("debug");
            logger.LogInformation("information");
            logger.LogWarning("warning");
            logger.LogError("error");
            logger.LogCritical("critical");

            Assert.Equal(5, coreLogger.ReceivedMessages.Count);
            Assert.True(coreLogger.ReceivedMessages.Contains("warning\r\n"));
            Assert.True(coreLogger.ReceivedMessages.Contains("error\r\n"));
            Assert.True(coreLogger.ReceivedMessages.Contains("critical\r\n"));
        }

        [Fact]
        public void DefaultFilterCheck()
        {
            var configSection = LoggerConfigSectionSetup("DefaultFilterCheck.json", null).GetSection("LogLevel");
            if (!(configSection != null && configSection.GetChildren().Count() > 0))
            {
                configSection = null;
            }
            var categoryName = "AWS.Log";
            var coreLogger = new FakeCoreLogger();
            var logger = new AWSLogger(
                categoryName,
                coreLogger,
                AWSLoggerProvider.CreateConfigSectionFilter(configSection, categoryName));

            logger.LogTrace("trace");
            logger.LogDebug("debug");
            logger.LogInformation("information");
            logger.LogWarning("warning");
            logger.LogError("error");
            logger.LogCritical("critical");

            Assert.Equal(5, coreLogger.ReceivedMessages.Count);
            Assert.True(coreLogger.ReceivedMessages.Contains("warning\r\n"));
            Assert.True(coreLogger.ReceivedMessages.Contains("error\r\n"));
            Assert.True(coreLogger.ReceivedMessages.Contains("critical\r\n"));

        }

        [Fact]
        public void MissingLogLevelCheck()
        {
            var configSection = LoggerConfigSectionSetup("MissingLogLevelCheck.json", null).GetSection("LogLevel");
            if (!(configSection != null && configSection.GetChildren().Count() > 0))
            {
                configSection = null;
            }
            var categoryName = typeof(TestFilter).GetTypeInfo().FullName;
            var coreLogger = new FakeCoreLogger();
            var logger = new AWSLogger(
                categoryName,
                coreLogger,
                AWSLoggerProvider.CreateConfigSectionFilter(configSection, categoryName));

            logger.LogTrace("trace");
            logger.LogDebug("debug");
            logger.LogInformation("information");
            logger.LogWarning("warning");
            logger.LogError("error");
            logger.LogCritical("critical");

            Assert.Equal(6, coreLogger.ReceivedMessages.Count);
            Assert.True(coreLogger.ReceivedMessages.Contains("warning\r\n"));
            Assert.True(coreLogger.ReceivedMessages.Contains("error\r\n"));
            Assert.True(coreLogger.ReceivedMessages.Contains("critical\r\n"));

        }
    }
}
