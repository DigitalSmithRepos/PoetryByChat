using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Interfaces;
using TwitchLib.Communication.Models;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;

public class Program
{
    private static void Main(string[] args)
    {
        //Not sure if this is needed, but adding it to be safe
        foreach(int _ in Enumerable.Range(0,DateTime.Now.Second)) { }
        {
            Random.Shared.Next();
        }

        var clientOptions = new ClientOptions
        {
            SendDelay = (ushort)Config.SendDelay,
            MessagesAllowedInPeriod = 750,
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };

        WebSocketClient customClient = new WebSocketClient(clientOptions);
        var client = new TwitchClient(customClient);

        // Initialize the client with the credentials instance, and setting a default channel to connect to.
        client.Initialize(Config.ConnectionCredentials, Config.ChannelToConnectTo);

        // Bind callbacks to events
        client.OnConnected += OnConnected;
        client.OnJoinedChannel += OnJoinedChannel;
        // Connect
        client.Connect();

        var game = new PhrasalTemplateWordGameService(client, Config.ExeDirectoryPath + "\\Templates");

        Thread.Sleep(-1);
    }

    private static void OnConnected(object? sender, OnConnectedArgs e)
    {
        Console.WriteLine($"The bot {e.BotUsername} succesfully connected to Twitch.");
    }

    private static void OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Console.WriteLine($"The bot has been connected to the channel: {e.Channel}");
    }
}
