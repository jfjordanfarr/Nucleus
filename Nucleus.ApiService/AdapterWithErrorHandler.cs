// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System; 

namespace Nucleus.ApiService
{
    /// <summary>
    /// A Bot Framework Adapter implementation that includes comprehensive error handling.
    /// This class catches exceptions during bot turns and logs them, optionally sending trace activities.
    /// Based on standard Bot Framework template patterns.
    /// See: https://github.com/microsoft/BotBuilder-Samples/blob/main/samples/csharp_dotnetcore/02.echo-bot/AdapterWithErrorHandler.cs
    /// </summary>
    public class AdapterWithErrorHandler : CloudAdapter 
    {
        public AdapterWithErrorHandler(IConfiguration configuration, ILogger<IBotFrameworkHttpAdapter> logger) 
            : base(configuration, logger: logger)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");

                // Send a message to the user
                await turnContext.SendActivityAsync("The bot encountered an error or bug.");
                await turnContext.SendActivityAsync("To continue to run this bot, please fix the bot source code.");

                // Send a trace activity, which will be displayed in the Bot Framework Emulator
                // Note: For production environments, logging Application Insights is preferred
                await turnContext.TraceActivityAsync("OnTurnError Trace", exception.Message, "https://www.botframework.com/schemas/error", "TurnError");
            };
        }
    }
}
