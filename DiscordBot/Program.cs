using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace DiscordBot {
    class Program {
        static void Main(string[] args) => new Program().Start();

        private DiscordClient _client;

        public void Start() {
            // Create an instance of DiscordClient.
            _client = new DiscordClient(x => {
                x.LogLevel = LogSeverity.Info;
            });

            // Register CommandService to DiscordClient.
            _client.UsingCommands(x => {
                x.PrefixChar = '!';
                //x.HelpMode = HelpMode.Public;
            });

            // Setup commands.
            _client.GetService<CommandService>().CreateCommand("help")
                .Parameter("CommandName", ParameterType.Optional)
                .Description("What do you THINK it does?")
                .Do(async e => {
                    // Get a list of all available commands.
                    Dictionary<string, Command> commands = new Dictionary<string, Command>();
                    foreach (Command command in _client.GetService<CommandService>().AllCommands) {
                        commands.Add(command.Text, command);
                    }

                    // Define what command has been passed if any.
                    string commandName = e.GetArg("CommandName");

                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine("kenjugs-bot v0.1")
                            .AppendLine();

                    if (string.IsNullOrEmpty(commandName)) {
                        sb.AppendLine("The current commands can be found below:");

                        foreach (KeyValuePair<string, Command> kvp in commands) {
                            if (kvp.Key == "help") { continue; }
                            sb.AppendLine($"**{kvp.Key}**");
                        }

                        sb.AppendLine()
                            .AppendLine("For more specific help, run `help <command>`");
                    } else {
                        if (commands.ContainsKey(commandName)) {
                            sb.AppendLine($"**Command**: `{commandName}`");

                            if (commands[commandName].Parameters.Count() > 0) {
                                sb.AppendLine($"**Usage**: `{commandName} <{commands[commandName].Parameters.First().Name}>`")
                                    .AppendLine();

                                sb.AppendLine("**Parameters**:");

                                foreach (CommandParameter param in commands[commandName].Parameters) {
                                    sb.AppendLine($"`{param.Name}: {param.Type}`");
                                }

                                sb.AppendLine();
                            } else {
                                sb.AppendLine($"**Usage**: `{commandName}`")
                                    .AppendLine();
                            }

                            sb.AppendLine(commands[commandName].Description);
                        } else {
                            // Show normal help message.
                        }
                    }
                    await e.Channel.SendMessage(sb.ToString());
                });

            _client.GetService<CommandService>().CreateCommand("joygun")
                .Alias(new string[] { "jg" })
                .Do(async e => {
                    await e.Channel.SendMessage(":joy: :gun:");
                });

            _client.GetService<CommandService>().CreateCommand("roll")
                .Description("Rolls a random number between 1 and `MaxNum` inclusive.")
                .Parameter("MaxNum", ParameterType.Optional)
                .Do(async e => {
                    int num = 6;
                    if (e.GetArg("MaxNum") != null) {
                        try {
                            num = Convert.ToInt32(e.GetArg("MaxNum"));

                            if (num < 6) {
                                num = 6;
                            }
                        } catch (OverflowException) {
                            num = 20;
                        } catch (FormatException) {
                            await e.Channel.SendMessage("NaN");
                            return;
                        }

                        Random rng = new Random();
                        int rand = rng.Next(0, num) + 1;
                        await e.Channel.SendMessage($"{e.Message.User.Name} rolled: {rand}");
                    }
                });

            _client.GetService<CommandService>().CreateCommand("twitch")
                .Description("Search Twitch.tv for `GameName` with more than 500 viewers")
                .Parameter("GameName", ParameterType.Optional)
                .Do(async e => {
                    // Log request.
                    _client.Log.Info($"Command={e.Message.Text}", $"Request from: {e.User}");

                    // Define HttpClient request.
                    HttpClient httpClient = Authorization.CreateTwitchRequest();

                    string gameName = e.GetArg("GameName");

                    // Format input to be properly URL-encoded.
                    if (string.IsNullOrEmpty(gameName) || System.Text.RegularExpressions.Regex.IsMatch(gameName, @"[^a-zA-Z0-9_\s]+|^\W*$")) {
                        gameName = System.Web.HttpUtility.UrlEncode("street fighter v");
                    } else {
                        gameName = System.Text.RegularExpressions.Regex.Replace(gameName, @"\s+", " ");
                        gameName = System.Web.HttpUtility.UrlEncode(gameName);
                    }

                    string apiGet = $"https://api.twitch.tv/kraken/search/streams?query={gameName}&limit=100";
                    string twitchResponse;

                    try {
                        twitchResponse = await httpClient.GetStringAsync(apiGet);
                    } catch (Exception ex) {
                        _client.Log.Error("Twitch Request", ex.Message);
                        await e.Channel.SendMessage("Something went wrong. Try again.");
                        return;
                    }
                    
                    JObject twitchObject = JsonConvert.DeserializeObject<JObject>(twitchResponse);

                    while (twitchObject["streams"].HasValues) {
                        var streams = twitchObject["streams"];
                        foreach (var item in streams) {
                            if (item.Value<int>("viewers") > 500) {
                                await e.Channel.SendMessage(item["channel"].Value<string>("status"));
                                await e.Channel.SendMessage(item["channel"].Value<string>("url"));

                                // Current rate limit seems to be 5 messages / 5 seconds
                                // .NET wrapper has no way to report rate limit.
                                System.Threading.Thread.Sleep(2500);
                            }
                        }

                        apiGet = twitchObject["_links"].Value<string>("next");
                        twitchResponse = await httpClient.GetStringAsync(apiGet);
                        twitchObject = JsonConvert.DeserializeObject<JObject>(twitchResponse);
                    }

                    await e.Channel.SendMessage("**End Stream List**");
                });

            _client.Log.Message += async (sender, ev) => {
                string logMessage = $"[{DateTime.Now}][{ev.Severity}] {ev.Source}: {ev.Message}";
                await System.Threading.Tasks.Task.Run(() => { System.IO.File.AppendAllLines("bot.log", new List<string> { logMessage }); });
                await System.Threading.Tasks.Task.Run(() => { Console.WriteLine(logMessage); });
            };

            // Connect bot and wait until disconnected.
            _client.ExecuteAndWait(async () => {
                await Authorization.AuthDiscordConnectAsync(_client);
            });
        }
    }
}
