using System;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace MyntUI.Helpers
{
    public class SignalRLogEventSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        public SignalRLogEventSink(IFormatProvider formatProvider)
        {
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level < LogEventLevel.Information)
                return;

            LogEventPropertyValue dd;
            if (logEvent.Properties.TryGetValue("RequestPath", out dd))
            {
                if (dd.ToString().StartsWith("\"/signalr/"))
                    return;

                if (dd.ToString().StartsWith("\"/api/mynt/logs"))
                    return;
            }

            var message = logEvent.RenderMessage(_formatProvider);
            //Console.WriteLine("FROM MySink " + DateTimeOffset.Now.ToString() + " " + message);

            if (Globals.GlobalHubMyntLogs != null)
                Globals.GlobalHubMyntLogs.Clients.All.SendAsync("Send", message);
        }
    }

    public static class MySinkExtensions
    {
        public static LoggerConfiguration SignalRLogEventSink(this LoggerSinkConfiguration loggerConfiguration, IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new SignalRLogEventSink(formatProvider));
        }
    }
}
