﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using Exemple;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.BotBuilderSamples
{
	/// <summary>
	/// The Startup class configures services and the request pipeline.
	/// </summary>
	public class Startup
	{
		private ILoggerFactory _loggerFactory;
		private bool _isProduction = false;

		public Startup(IHostingEnvironment env)
		{
			_isProduction = env.IsProduction();

			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
				.AddEnvironmentVariables();

			Configuration = builder.Build();
		}

		/// <summary>
		/// Gets the configuration that represents a set of key/value application configuration properties.
		/// </summary>
		/// <value>
		/// The <see cref="IConfiguration"/> that represents a set of key/value application configuration properties.
		/// </value>
		public IConfiguration Configuration { get; }

		/// <summary>
		/// This method gets called by the runtime. Use this method to add services to the container.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> specifies the contract for a collection of service descriptors.</param>
		/// <seealso cref="IStatePropertyAccessor{T}"/>
		/// <seealso cref="https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/dependency-injection"/>
		/// <seealso cref="https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0"/>
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddBot<ExempleBot>(options =>
			{
				//...

				// The Memory Storage used here is for local bot debugging only. When the bot
				// is restarted, everything stored in memory will be gone.
				IStorage dataStore = new MemoryStorage();

				// Create conversation and user state objects.
				options.State.Add(new ConversationState(dataStore));
				options.State.Add(new UserState(dataStore));
			});

			// Create and register state accessors.
			// Accessors created here are passed into the IBot-derived class on every turn.
			services.AddSingleton<BotAccessors>(sp =>
			{
				var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
				var conversationState = options.State.OfType<ConversationState>().FirstOrDefault();
				var userState = options.State.OfType<UserState>().FirstOrDefault();

				// Create the custom state accessor.
				// State accessors enable other components to read and write individual properties of state.
				var accessors = new BotAccessors(conversationState, userState)
				{
					UserInfoAccessor = userState.CreateProperty<UserInfo>(BotAccessors.UserInfoAccessorName),
					DialogStateAccessor = conversationState.CreateProperty<DialogState>(BotAccessors.DialogStateAccessorName),
				};

				return accessors;
			});
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			_loggerFactory = loggerFactory;

			app.UseDefaultFiles()
				.UseStaticFiles()
				.UseBotFramework();
		}
	}
}

