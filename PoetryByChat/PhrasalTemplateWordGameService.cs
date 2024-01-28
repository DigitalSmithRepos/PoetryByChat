using System.Threading;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

public class PhrasalTemplateWordGameService
{
    public IReadOnlyList<string> PhrasalTemplateFilePaths { get; }

    private readonly TwitchClient client;

    public PhrasalTemplateWordGameService(TwitchClient client, string phrasalTemplatesDirectory) 
        : this(client, GetPhrasalTemplateFilePaths(phrasalTemplatesDirectory))
    { }

    public PhrasalTemplateWordGameService(TwitchClient client, IEnumerable<string> phrasalTemplateFilePaths)
    {
        this.client = client;
        PhrasalTemplateFilePaths = phrasalTemplateFilePaths.ToArray();
        foreach (var joinedChannel in client.JoinedChannels)
        {
            OnJoinedChannel(joinedChannel.Channel);
        }
        client.OnJoinedChannel += OnJoinedChannel;

        client.OnChatCommandReceived += OnChatCommandReceived;
    }

    private void OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        OnJoinedChannel(e.Channel);
    }

    private void OnJoinedChannel(string channel)
    {
        client.SendMessage(channel, $"Poerty service has been connected to the channel");

        ConsoleUtility.OnClose += () =>
        {
            OnLeftChannel(channel);
        };
    }

    private void OnLeftChannel(string channel)
    {
        try
        {
            client.SendMessage(channel, $"Poerty service has been disconnected from the channel");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        if("poem" == e.Command.CommandText.ToLower()) 
        {
            _ = Run(e.Command.ChatMessage.Channel, default);
        }
    }

    public Task Run(string channel, CancellationToken cancellationToken)
        => Run(channel, LoadRandomPhrasalTemplate(), cancellationToken);

    public async Task Run(string channel, PhrasalTemplate template, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var blankCount = template.blanks.Length;

        Action? cleanupListeners = null;

        List<string>[] responses = new List<string>[blankCount];
        var filledBlanks = new string[blankCount];

        try
        {
            for (int i = 0; i < blankCount; i++)
            {
                string wordCommand = "word" + (i + 1);
                responses[i] = new List<string>();

                TaskCompletionSource<object?> taskCompletionSource = new TaskCompletionSource<object?>();
                using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken)))
                {
                    void TryAddResponse(string response)
                    {
                        if (!string.IsNullOrWhiteSpace(response))
                        {
                            responses[i].Add(response.Trim());
                            taskCompletionSource.TrySetResult(null);
                        }
                    }

                    void HandleMessageReceived(object? sender, OnMessageReceivedArgs e)
                    {
                        var chatReplyData = e.ChatMessage.ChatReply;
                        if (chatReplyData != null 
                            && chatReplyData.ParentUserLogin == Config.Username.ToLower() 
                            && chatReplyData.ParentMsgBody.EndsWith(wordCommand, StringComparison.OrdinalIgnoreCase) 
                            && !string.IsNullOrWhiteSpace(e.ChatMessage.Message))
                        {
                            var msg = e.ChatMessage.Message;

                            // strip off the @DisplayName
                            var indexOfSpaceAfterReplyName = msg.IndexOf(' ');
                            if (indexOfSpaceAfterReplyName > 0)
                            {
                                msg = msg.Substring(indexOfSpaceAfterReplyName);
                            }

                            TryAddResponse(msg);
                        }
                    }

                    void HandleChatCommand(object? sender, OnChatCommandReceivedArgs e)
                    {
                        if (e.Command.CommandText == wordCommand)
                        {
                            TryAddResponse(e.Command.ArgumentsAsString);
                        }
                    }

                    client.OnChatCommandReceived += HandleChatCommand;
                    client.OnMessageReceived += HandleMessageReceived;
                    cleanupListeners += () =>
                    {
                        client.OnChatCommandReceived -= HandleChatCommand;
                        client.OnMessageReceived -= HandleMessageReceived;
                    };


                    var msg = $"[{template.blanks[i]}] Reply or use !{wordCommand}";
                    if (!msg.EndsWith(wordCommand)) //reply detection hack won't work if it dosen't have this ending
                    {
                        Console.WriteLine("Reply detection broken due to unexpected line ending");
                    }
                    client.SendMessage(channel, $"[{template.blanks[i]}] Reply or use !{wordCommand}");
                    await taskCompletionSource.Task;
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            var delay = Config.PoemCreationDelay;
            if (delay > 0)
            {
                (int delaySeconds, int remainder) = Math.DivRem(delay, 1000);
                if(remainder > 0)
                {
                    delaySeconds += 1;
                }
                client.SendMessage(channel, $"Poem will be finalized in less than {delaySeconds} second{(delaySeconds==1?"":"s")} copyThis pastaThat ");

                await Task.Delay(delay);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        finally
        {
            cleanupListeners?.Invoke();
        }

        for (int i = 0; i < blankCount; i++)
        {
            var responseList = responses[i];
            filledBlanks[i] = responseList[Random.Shared.Next(responseList.Count)];
        }

        foreach (var line in template.BuildLines(filledBlanks))
        {
            client.SendMessage(channel, line);
        }
        client.SendMessage(channel, $"— Chat");
    }


    public static string[] GetPhrasalTemplateFilePaths(string phrasalTemplatesDirectory)
    {
        if (!Directory.Exists(phrasalTemplatesDirectory))
        {
            throw new Exception($"Unable to load phrasal templates. The folder could not be found: {phrasalTemplatesDirectory}");
        }

        return Directory.GetFiles(phrasalTemplatesDirectory);
    }

    public PhrasalTemplate LoadRandomPhrasalTemplate()
    {
        var shuffledTemplateFilePaths = PhrasalTemplateFilePaths.OrderBy(x => Random.Shared.NextDouble()).ToList();

        foreach(var filePath in shuffledTemplateFilePaths)
        {
            if(TryLoadPhrasalTemplate(filePath, out var phrasalTemplate))
            {
                return phrasalTemplate;
            }
        }

        throw new Exception($"Unable to load any phrasal templates. No valid template could not be found");
    }
    
    public List<PhrasalTemplate> LoadPhrasalTemplates()
    {
        List<PhrasalTemplate> phrasalTemplates = new List<PhrasalTemplate>();
        foreach (var file in PhrasalTemplateFilePaths)
        {
            if(TryLoadPhrasalTemplate(file, out var phrasalTemplate))
            {
                phrasalTemplates.Add(phrasalTemplate);
            }
        }

        return phrasalTemplates;
    }

    private bool TryLoadPhrasalTemplate(string phrasalTemplateFilePath, out PhrasalTemplate phrasalTemplate)
    {
        Console.WriteLine($"Loading {phrasalTemplateFilePath}");
        try
        {
            phrasalTemplate = JsonUtility.DeserializeFile<PhrasalTemplate>(phrasalTemplateFilePath)!;
            if (phrasalTemplate != null && phrasalTemplate.blanks.Length > 0 && phrasalTemplate.templateLines.Length > 0)
            {
                Console.WriteLine($"- Loaded");
                return true;
            }
            else
            {
                Console.WriteLine($"- Failed to load. Failed to deserialize a valid phrasal template");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"- Failed to load. {e}");
        }
        phrasalTemplate = null!;
        return false;
    }
}
