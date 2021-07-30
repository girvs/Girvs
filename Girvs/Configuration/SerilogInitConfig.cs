using System.Collections.Generic;
using System.Text.Json;

namespace Girvs.Configuration
{
    public class SerilogInitConfig
    {
        public static string GetJsonString()
        {
            var config = new
            {
                Using = new List<string> {"Serilog.Settings.Configuration", "Serilog.Sinks.File"},
                MinimumLevel = new
                {
                    Default = "Debug",
                    Override = new
                    {
                        Microsoft = "Debug",
                        System = "Warning"
                    }
                },
                WriteTo = new List<dynamic>()
                {
                    new
                    {
                        Name = "Console",
                        Args = new
                        {
                            outputTemplate =
                                "{Timestamp:yyyy-MM-dd HH:mm-dd } || [{Level:u3}] || {SourceContext:l} || {Message:lj} || {Exception} ||end {NewLine}",
                            restrictedToMinimumLevel = "Debug"
                        }
                    },

                    new
                    {
                        Name = "File",
                        Args = new
                        {
                            restrictedToMinimumLevel = "Warning",
                            RollingInterval = "Hour",
                            path = "./logs/Warning/log-Warning-.log",
                            outputTemplate =
                                "{Timestamp:yyyy-MM-dd HH:mm-dd } || [{Level:u3}] || {SourceContext:l} || {Message:lj} || {Exception} ||end {NewLine}"
                        }
                    },
                    new
                    {
                        Name = "File",
                        Args = new
                        {
                            restrictedToMinimumLevel = "Information",
                            RollingInterval = "Hour",
                            path = "./logs/Information/log-Information-.log",
                            outputTemplate =
                                "{Timestamp:yyyy-MM-dd HH:mm-dd } || [{Level:u3}] || {SourceContext:l} || {Message:lj} || {Exception} ||end {NewLine}"
                        }
                    },
                    new
                    {
                        Name = "File",
                        Args = new
                        {
                            restrictedToMinimumLevel = "Error",
                            RollingInterval = "Hour",
                            path = "./logs/Error/log-Error-.log",
                            outputTemplate =
                                "{Timestamp:yyyy-MM-dd HH:mm-dd } || [{Level:u3}] || {SourceContext:l} || {Message:lj} || {Exception} ||end {NewLine}"
                        }
                    },
                    new
                    {
                        Name = "File",
                        Args = new
                        {
                            restrictedToMinimumLevel = "Fatal",
                            RollingInterval = "Hour",
                            path = "./logs/Fatal/log-Fatal-.log",
                            outputTemplate =
                                "{Timestamp:yyyy-MM-dd HH:mm-dd } || [{Level:u3}] || {SourceContext:l} || {Message:lj} || {Exception} ||end {NewLine}"
                        }
                    },
                }
            };

            return JsonSerializer.Serialize(new
            {
                Serilog = config
            });
        }
    }
}