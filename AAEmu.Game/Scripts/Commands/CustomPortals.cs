using System.Text.RegularExpressions;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Utils.Scripts;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Core.Managers.UnitManagers;

using NLog;

namespace AAEmu.Game.Scripts.Commands;

public class CustomPortals : ICommand
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
    public string[] CommandNames { get; set; } = ["set_portal"];

    public void OnLoad()
    {
        CommandManager.Instance.Register(CommandNames, this);
    }

    public string GetCommandLineHelp()
    {
        return "<Item Link>|random";
    }

    public string GetCommandHelpText()
    {
        return "Set the custom portal model to use when opening portals.";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        if (args.Length == 0)
        {
            CommandManager.SendDefaultHelpText(this, messageOutput);
            return;
        }

        if (args.Length == 1)
        {
            if (args[0] == "random")
            {
                character.SetOption(1664, "random");
                CommandManager.SendNormalText(this, messageOutput, $"Set \"{args[0]}\" as custom portal.");
            }
            else
            {
                string pattern = @"\|i(.*?),0";
                Match match = Regex.Match(args[0], pattern);
                if (match.Success)
                {
                    string result = match.Groups[1].Value;
                    if (character.Inventory.CheckItems(SlotType.Inventory, uint.Parse(result), 1))
                    {
                        character.SetOption(1664, result);
                        var customPortalBookTemplateId = uint.Parse(result);
                        var portalSkillId = ItemManager.Instance.GetTemplate(customPortalBookTemplateId).UseSkillId;
                        var openPortalEffectTemplate = (OpenPortalEffect)SkillManager.Instance.GetSkillTemplate(portalSkillId).Effects[0].Template;
                        // Store enter portal modelId
                        var enterModelId = NpcManager.Instance.GetTemplate(openPortalEffectTemplate.portalEnterId).ModelId.ToString();
                        character.SetOption(1665, enterModelId);
                        // Store exit portal modelId
                        var exitModelId = NpcManager.Instance.GetTemplate(openPortalEffectTemplate.portalExitId).ModelId.ToString();
                        character.SetOption(1666, exitModelId);
                        Logger.Debug($"Set custom portal: Enter ModelId {enterModelId} Exit ModelId {exitModelId}");
                        CommandManager.SendNormalText(this, messageOutput, $"Set \"{args[0]}\" as custom portal.");
                    }
                    else
                    {
                        CommandManager.SendNormalText(this, messageOutput, $"You do not own \"{args[0]}\" portal book.");
                        return;
                    }
                }
            }
        }
        else
        {
            CommandManager.SendNormalText(this, messageOutput, $"Cannot set \"{args[0]}\" as custom portal.");
        }
    }
}
