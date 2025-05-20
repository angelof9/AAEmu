namespace AAEmu.Game.Models.Game.Chat;

public class ChatAIConfig
{
    public string OpenAIEndpoint { get; set; }
    public string OpenAIApiKey { get; set; }
    public string Model { get; set; }
    public double ChatSessionTimeOut { get; set; }
    public float MaxNpcChatDistance { get; set; }
    public NPCs[] Allowed_NPCs { get; set; }
}

public class NPCs
{
    public string Name { get; set; }
    public bool IsEnabled { get; set; }
    public string Persona { get; set; }
}