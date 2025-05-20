using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using AAEmu.Game.Models;
using AAEmu.Game.Models.Tasks.Chat;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.NPChar;

using OpenAI;
using OpenAI.Chat;
using NLog;

namespace AAEmu.Game.Models.Game.Chat;

public class ChatAISession
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
    public ChatAISessionEndTimerTask ChatAISessionEndTimerTask { get; set; }
    public Character Character { get; set; }
    public string NpcName { get; set; }
    public DateTime LastUpdatedTime { get; set; }
    public ChatClient OpenAIClient { get; set; }
    public List<ChatMessage> ChatHistory = new List<ChatMessage>();

    public ChatAISession(Character p_char, string npc_name)
    {
        Logger.Debug("ChatAISession: Initialize");
        Character = p_char;
        NpcName = npc_name;

        try
        {
            var clientOptions = new OpenAIClientOptions
            {
                Endpoint = new Uri(AppConfiguration.Instance.ChatAI.OpenAIEndpoint),
            };
            OpenAIClient = new( model: AppConfiguration.Instance.ChatAI.Model,
                                credential: new System.ClientModel.ApiKeyCredential(AppConfiguration.Instance.ChatAI.OpenAIApiKey),
                                clientOptions);
            SetSystemPromp();
            LastUpdatedTime = DateTime.UtcNow;
        }
        catch (Exception e)
        {
            Logger.Error("ChatAISession: Initialize error");
            LastUpdatedTime = DateTime.MaxValue;
        }
    }

    public void SetSystemPromp()
    {
        foreach(var iterNpc in AppConfiguration.Instance.ChatAI.Allowed_NPCs)
        {
            if (iterNpc.Name == NpcName)
            {
                Logger.Debug("ChatAISession: SetSystemPromp");
                ChatHistory.Add(new SystemChatMessage(iterNpc.Persona));
            }
        }
    }

    public string GetAnswer(string message)
    {
        try
        {
            ChatHistory.Add(new UserChatMessage(message));
            ChatCompletion completion = OpenAIClient.CompleteChat(ChatHistory);
            ChatHistory.Add(new AssistantChatMessage(completion));
            var answer = CleanUpAnswer(completion.Content[0].Text);
            Logger.Debug($"ChatAISession: GetAnswer {answer}");
            LastUpdatedTime = DateTime.UtcNow;
            return answer;
        }
        catch (Exception e)
        {
            Logger.Error("ChatAISession: GetAnswer error");
            return null;
        }
    }

    private string CleanUpAnswer(string message)
    {
        // Remove AI thinking
        var tmp = Regex.Replace(message, @"(<think>|<thinking>)[\s\S]*?(</think>|</thinking>)", string.Empty, RegexOptions.Multiline);
        // Remove blank/empty lines
        tmp = Regex.Replace(tmp, @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline);
        return tmp;
    }
}