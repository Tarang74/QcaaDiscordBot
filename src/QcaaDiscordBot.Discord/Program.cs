﻿using System;
using System.Reflection;
using DSharpPlus;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QcaaDiscordBot.Core.Models;
using QcaaDiscordBot.Core.Repositories;
using QcaaDiscordBot.Core.Services;
using QcaaDiscordBot.Discord;
using QcaaDiscordBot.Discord.Helpers;
using QcaaDiscordBot.Discord.Services;
using QcaaDiscordBot.Infrastructure.Repositories;
using Serilog;
using Serilog.Events;

using var logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var logFactory = new LoggerFactory().AddSerilog(logger);

var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton(new DiscordClient(new DiscordConfiguration
        {
            Token = hostContext.Configuration["Discord:Token"],
            TokenType = TokenType.Bot,
            AutoReconnect = true,
            MinimumLogLevel = LogLevel.None,
            LoggerFactory = logFactory,
            MessageCacheSize = 512,
            Intents = DiscordIntents.All
        }));

        services.InjectBotServices(Assembly.GetEntryAssembly());

        services.AddMarten(options =>
        {
            options.Connection(hostContext.Configuration.GetConnectionString("Marten"));

            // Use the more permissive schema auto create behavior
            // while in development
            if (hostContext.HostingEnvironment.IsDevelopment())
            {
                options.AutoCreateSchemaObjects = AutoCreate.All;
            }
        });
        
        services.AddSingleton<IUserReportService, UserReportService>();
        services.AddSingleton<IUserReportRepository, UserReportRepository>();
        
        services.AddHostedService<Bot>();
    });

var host = hostBuilder.Build();

host.Services.InitializeMicroservices(Assembly.GetEntryAssembly());

host.Run();