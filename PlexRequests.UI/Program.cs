﻿#region Copyright
// /************************************************************************
//    Copyright (c) 2016 Jamie Rees
//    File: Program.cs
//    Created By: Jamie Rees
//   
//    Permission is hereby granted, free of charge, to any person obtaining
//    a copy of this software and associated documentation files (the
//    "Software"), to deal in the Software without restriction, including
//    without limitation the rights to use, copy, modify, merge, publish,
//    distribute, sublicense, and/or sell copies of the Software, and to
//    permit persons to whom the Software is furnished to do so, subject to
//    the following conditions:
//   
//    The above copyright notice and this permission notice shall be
//    included in all copies or substantial portions of the Software.
//   
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  ************************************************************************/
#endregion
using System;
using System.Collections.Generic;
using System.Data;

using Microsoft.Owin.Hosting;

using Mono.Data.Sqlite;

using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Layouts;
using NLog.Targets;

using PlexRequests.Core;
using PlexRequests.Core.SettingModels;
using PlexRequests.Helpers;
using PlexRequests.Store;
using PlexRequests.Store.Repository;

namespace PlexRequests.UI
{
    class Program
    {
        static void Main(string[] args)
        {
            WriteOutVersion();

            var s = new Setup();
            var connection = s.SetupDb();

            //ConfigureTargets(connection);


            var uri = GetStartupUri();

            using (WebApp.Start<Startup>(uri))
            {
                Console.WriteLine($"Request Plex is running on {uri}");
                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
            }
        }

        private static void WriteOutVersion()
        {
            var assemblyVer = AssemblyHelper.GetAssemblyVersion();
            Console.WriteLine($"Version: {assemblyVer}");
        }

        private static string GetStartupUri()
        {
            var uri = "http://localhost:3579/";
            var service = new SettingsServiceV2<RequestPlexSettings>(new JsonRepository(new DbConfiguration(new SqliteFactory()), new MemoryCacheProvider()));
            var settings = service.GetSettings();

            if (settings.Port != 0)
            {
                uri = $"http://localhost:{settings.Port}";
            }

            return uri;
        }

        private static void ConfigureTargets(string connectionString)
        {
            LogManager.ThrowExceptions = true;
            // Step 1. Create configuration object 
            var config = new LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration 
            var databaseTarget = new DatabaseTarget { CommandType = CommandType.Text,ConnectionString = connectionString,
                DBProvider = "Mono.Data.Sqlite, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756",
                Name = "database"};


            var messageParam = new DatabaseParameterInfo { Name = "@Message", Layout = "${message}" };
            var callsiteParam = new DatabaseParameterInfo { Name = "@Callsite", Layout = "${callsite}" };
            var levelParam = new DatabaseParameterInfo { Name = "@Level", Layout = "${level}" };
            var usernameParam = new DatabaseParameterInfo { Name = "@Username", Layout = "${identity}" };
            var dateParam = new DatabaseParameterInfo { Name = "@Date", Layout = "${date}" };
            var loggerParam = new DatabaseParameterInfo { Name = "@Logger", Layout = "${logger}" };
            var exceptionParam = new DatabaseParameterInfo { Name = "@Exception", Layout = "${exception:tostring}" };

            databaseTarget.Parameters.Add(messageParam);
            databaseTarget.Parameters.Add(callsiteParam);
            databaseTarget.Parameters.Add(levelParam);
            databaseTarget.Parameters.Add(usernameParam);
            databaseTarget.Parameters.Add(dateParam);
            databaseTarget.Parameters.Add(loggerParam);
            databaseTarget.Parameters.Add(exceptionParam);

            databaseTarget.CommandText = "INSERT INTO Log (Username,Date,Level,Logger, Message, Callsite, Exception) VALUES(@Username,@Date,@Level,@Logger, @Message, @Callsite, @Exception);";
            config.AddTarget("database", databaseTarget);

            // Step 4. Define rules
            var rule1 = new LoggingRule("*", LogLevel.Error, databaseTarget);
            config.LoggingRules.Add(rule1);

            try
            {

                // Step 5. Activate the configuration
                LogManager.Configuration = config;
            }
            catch (Exception e)
            {
                
                throw;
            }

            // Example usage
            Logger logger = LogManager.GetLogger("Example");

            logger.Error("error log message");
        }
    }
}