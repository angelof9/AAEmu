using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using AAEmu.Commons.Utils;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Chat;
using AAEmu.Game.Models.Tasks.Chat;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Managers.UnitManagers;

using NLog;

namespace AAEmu.Game.Core.Managers;

public class ChatAISessionManager : Singleton<ChatAISessionManager>
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
    private static ConcurrentDictionary<string, ChatAISession> _chatAISessions;
    private double ChatAISessionTimeOut = 5;

    protected ChatAISessionManager()
    {
        _chatAISessions = new ConcurrentDictionary<string, ChatAISession>();
    }

    public bool Initialize()
    {
        Logger.Debug("ChatAISessions Manager: Initialize");
        ChatAISessionTimeOut = AppConfiguration.Instance.ChatAI.ChatSessionTimeOut;
        ChatAISessionTimeOutCheck();
        return true;
    }

    public bool IsNPCAllowed(string npc_name)
    {
        Logger.Debug("ChatAISessions Manager: IsNPCAllowed");
        foreach(var iterNpc in AppConfiguration.Instance.ChatAI.Allowed_NPCs)
        {
            if (iterNpc.Name == npc_name && iterNpc.IsEnabled)
            {
                Logger.Debug($"ChatAISessions Manager: IsNPCAllowed - Allowed Npc Name '{npc_name}'");
                return true;
            }
        }
        Logger.Debug($"ChatAISessions Manager: IsNPCAllowed - Not allowed Npc Name '{npc_name}'");
        return false;
    }

    public bool IsCharacterNearNpc(Character p_char, string npc_name)
    {
        var npcs = WorldManager.GetAround<Npc>(p_char, AppConfiguration.Instance.ChatAI.MaxNpcChatDistance);
        for (var i = 0; i < npcs.Count; i++)
        {
            var curName = LocalizationManager.Instance.Get("npcs", "name", npcs[i].TemplateId);
            Logger.Debug($"ChatAISessions Manager: IsCharacterNearNpc - Test npc '{curName}'");
            if (curName == npc_name)
            {
                Logger.Debug($"ChatAISessions Manager: IsCharacterNearNpc - Close enough to '{npc_name}'");
                return true;
            }
        }
        return false;
    }

    public void ChatAISessionRequest(Character p_char, string npc_name, string message)
    {
        // Show message to user
        var packet = new SCChatMessagePacket(ChatType.Whispered, npc_name, message, 0, 0);
        p_char.SendPacket(packet);

        ChatAISession chatAISession = null;
        string key = $"{p_char.Id}_{npc_name}";
        if (_chatAISessions.ContainsKey(key) && _chatAISessions.TryGetValue(key, out chatAISession))
        {
            Logger.Debug("ChatAISessions Manager: ChatAISessionRequest - Session found");
        }
        else
        {
            Logger.Debug("ChatAISessions Manager: ChatAISessionRequest - Create Session");
            chatAISession = new ChatAISession(p_char, npc_name);
        }

        if ((chatAISession != null) && (chatAISession.LastUpdatedTime < DateTime.MaxValue))
        {
            Logger.Debug("ChatAISessions Manager: ChatAISessionRequest - Let's do something fun");
            var AIResponse = chatAISession.GetAnswer(message);
            if (AIResponse != null)
            {
                Logger.Debug("ChatAISessions Manager: ChatAISessionRequest - HOHO Got answer, send it back to client");
                packet = new SCChatMessagePacket(ChatType.Whisper, npc_name, AIResponse, 0, 0);
                p_char.SendPacket(packet);
                ChatAISessionAddOrUpdate(chatAISession);
            }
            else
            {
                Logger.Error("ChatAISessions Manager: ChatAISessionRequest - No answer from AI");
                p_char.SendErrorMessage(ErrorMessageType.InternalError);
                ChatAISessionRemove(chatAISession);
            }
        }
        else
        {
            Logger.Error("ChatAISessions Manager: ChatAISessionRequest - Cannot acquire session");
            p_char.SendErrorMessage(ErrorMessageType.InvalidTarget);
            // Cleanup faulty session
            ChatAISessionRemove(chatAISession);
        }
    }

    private void ChatAISessionAddOrUpdate(ChatAISession chatSession)
    {
        Logger.Debug("ChatAISessions Manager: ChatAISessionAddOrUpdate");
        string key = $"{chatSession.Character.Id}_{chatSession.NpcName}";
        _chatAISessions.AddOrUpdate(key, chatSession, (charId, chatSess) => { return chatSession; });
    }

    private void ChatAISessionRemove(ChatAISession chatSession)
    {
        Logger.Debug("ChatAISessions Manager: ChatAISessionRemove");
        string key = $"{chatSession.Character.Id}_{chatSession.NpcName}";
        _chatAISessions.TryRemove(key, out _);
    }

    public void ChatAISessionTimeOutCheck()
    {
        Logger.Debug("ChatAISessions Manager: ChatAISessionTimeOutCheck - Begin");
        foreach(ChatAISession chatAISession in _chatAISessions.Values)
        {
            if ((DateTime.UtcNow - chatAISession.LastUpdatedTime) > TimeSpan.FromMinutes(ChatAISessionTimeOut))
            {
                Logger.Debug($"ChatAISessionTimeOutCheck - Session CharacterId:'{chatAISession.Character.Id}' Expired");
                ChatAISessionRemove(chatAISession);
            }
        }

        // Start next cleanup Task
        var nextCleanupTime = DateTime.UtcNow + TimeSpan.FromMinutes(ChatAISessionTimeOut);
        if (nextCleanupTime < DateTime.MaxValue)
        {
            var chatAISessionEndTimerTask = new ChatAISessionEndTimerTask();
            TaskManager.Instance?.Schedule(chatAISessionEndTimerTask, TimeSpan.FromMinutes(ChatAISessionTimeOut));
            Logger.Debug("ChatAISessions Manager: ChatAISessionTimeOutCheck - Next ChatAISession cleanup scheduled at " + nextCleanupTime.ToString());
        }
        else
        {
            Logger.Debug("ChatAISessions Manager: ChatAISessionTimeOutCheck - No new ChatAISession cleanup scheduled");
        }
    }
}