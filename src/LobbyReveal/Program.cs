using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Ekko;
using Newtonsoft.Json;
using Spectre.Console;

namespace LobbyReveal
{
    internal class Program
    {
        private static List<LobbyHandler> _handlers = new List<LobbyHandler>();
        private static bool _update = true;

        private static Random random = new Random();

        private static void OpenLinkInDefaultBrowser(LobbyHandler handler) {
            try {
                var region = handler.GetRegion();

                var link =
                    $"https://www.op.gg/multisearch/{region ?? Region.EUW}?summoners=" +
                    HttpUtility.UrlEncode($"{string.Join(",", handler.GetSummoners())}");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    Process.Start("cmd", $"/c start {link}");
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    Process.Start("xdg-open", link);
                } else {
                    Process.Start("open", link);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error opening link: {ex.Message}");
            }
        }

        public async static Task Main(string[] args)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
             
            Console.Title = new string(Enumerable.Repeat(chars, 15)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            var watcher = new LeagueClientWatcher();
            watcher.OnLeagueClient += (clientWatcher, client) =>
            {
                var handler = new LobbyHandler(new LeagueApi(client.ClientAuthInfo.RiotClientAuthToken,
                    client.ClientAuthInfo.RiotClientPort));
                _handlers.Add(handler);
                handler.OnUpdate += (lobbyHandler, names) => { _update = true; };
                handler.Start();
                _update = true;
            };
            new Thread(async () => { await watcher.Observe(); })
            {
                IsBackground = true
            }.Start();

            new Thread(() => { Refresh(); })
            {
                IsBackground = true
            }.Start();


            while (true)
            {
                var input = Console.ReadKey(true);
                if (!int.TryParse(input.KeyChar.ToString(), out var i) || i > _handlers.Count || i < 1)
                {
                    Console.WriteLine("Invalid input.");
                    _update = true;
                    continue;
                }

                OpenLinkInDefaultBrowser(_handlers[i - 1]);
                _update = true;
            }

        }

        private static void Refresh()
        {
            while (true)
            {
                if (_update)
                {
                    Console.Clear();
                    AnsiConsole.Write(new Markup("[u][yellow]https://www.github.com/Xh4H/LobbyReveal[/][/]")
                        .Centered());
                    AnsiConsole.Write(new Markup("[u][green][b]v1.0.2 - Xh4H[/][/][/]").Centered());
                    Console.WriteLine();
                    Console.WriteLine();
                    for (int i = 0; i < _handlers.Count; i++)
                    {
                        var lobby_summoners = _handlers[i].GetSummoners();
                        var link =
                            $"https://www.op.gg/multisearch/{_handlers[i].GetRegion() ?? Region.EUW}?summoners=" +
                            HttpUtility.UrlEncode($"{string.Join(",", lobby_summoners)}");

                        AnsiConsole.Write(
                            new Panel(new Text($"{string.Join("\n", lobby_summoners)}\n\n{link}")
                                    .LeftJustified())
                                .Expand()
                                .SquareBorder()
                                .Header($"[red]Client {i + 1}[/]"));
                        Console.WriteLine();

                        var summoners_count = _handlers[i].GetCount();
                        if (summoners_count > 0) {
                            if (summoners_count == 5) {
                                AnsiConsole.Write(new Markup("[u][green][b]Full lobby detected! Press client number to open op.gg in browser.[/][/][/]")
                                    .LeftJustified());
                            } else {
                                AnsiConsole.Write(new Markup("[u][yellow][b]Partial lobby detected! " + summoners_count + "/5 players detected.[/][/][/]")
                                    .LeftJustified());
                            }
                        } else {
                            AnsiConsole.Write(new Markup("[u][cyan][b]Waiting for a lobby.[/][/][/]")
                                    .LeftJustified());
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                    
                    Console.WriteLine();
                    _update = false;
                }

                Thread.Sleep(2000);
            }
        }
    }
}