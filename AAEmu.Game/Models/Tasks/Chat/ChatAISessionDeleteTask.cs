using AAEmu.Game.Core.Managers;

namespace AAEmu.Game.Models.Tasks.Chat;

public class ChatAISessionEndTimerTask : Task
{
    private static object _lock = new();

    public override void Execute()
    {
        lock (_lock)
        {
            try
            {
                ChatAISessionManager.Instance.ChatAISessionTimeOutCheck();
            }
            catch
            {
                // Do nothing
            }
        }
    }
}