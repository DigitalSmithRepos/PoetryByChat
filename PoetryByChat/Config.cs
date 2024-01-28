using System.Text.Json.Serialization;
using TwitchLib.Client.Models;

public class Config
{
    private static Config Instance 
        => (_instance ??= JsonUtility.DeserializeFile<Config>(ExeDirectoryPath + "\\config.json")!) 
        ?? throw new Exception("Failed to load config");
    private static Config _instance = null!;

    public static string ExeFilePath { get; } = System.Reflection.Assembly.GetExecutingAssembly().Location;
    public static string ExeDirectoryPath { get; } = Path.GetDirectoryName(ExeFilePath)!;

    [JsonInclude]
    public string oauthToken = null!; //A Twitch OAuth token which can be used to connect to the chat

    public static string Username => Instance.username;
    [JsonInclude]
    public string username = null!; //The username which was used to generate the OAuth token

    public static string ChannelToConnectTo => Instance.channelToConnectTo;
    [JsonInclude]
    public string channelToConnectTo = "";

    public static int PoemCreationDelay => Instance.poemCreationDelay;
    [JsonInclude]
    public int poemCreationDelay = 15000;

    public static int SendDelay => Instance.sendDelay;
    [JsonInclude]
    public int sendDelay = 500;

    public static ConnectionCredentials ConnectionCredentials
        => Instance.GetConnectionCredentials();
    private ConnectionCredentials _connectionCredentials = null!;

    private ConnectionCredentials GetConnectionCredentials()
        => _connectionCredentials ??= new ConnectionCredentials(username, oauthToken);
}