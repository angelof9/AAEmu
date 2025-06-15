using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

using System.Text.RegularExpressions;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Skills.Effects;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSAddFriendPacket : GamePacket
{
    public CSAddFriendPacket() : base(CSOffsets.CSAddFriendPacket, 1)
    {
    }

    public override void Read(PacketStream stream)
    {
        var name = stream.ReadString();

        Logger.Debug("AddFriend, name: {0}", name);

        if (name == "## random")
        {
            Connection.ActiveChar.SetOption(1664, "random");
            Connection.ActiveChar.SendMessage($"Set '{name}' as custom portal.");
        }
        else
        {
            string pattern = @"## \|i(.*?),0";
            Match match = Regex.Match(name, pattern);
            if (match.Success)
            {
                string result = match.Groups[1].Value;
                if (Connection.ActiveChar.Inventory.CheckItems(SlotType.Inventory, uint.Parse(result), 1))
                {
                    Connection.ActiveChar.SetOption(1664, result);
                    var customPortalBookTemplateId = uint.Parse(result);
                    var portalSkillId = ItemManager.Instance.GetTemplate(customPortalBookTemplateId).UseSkillId;
                    var openPortalEffectTemplate = (OpenPortalEffect)SkillManager.Instance.GetSkillTemplate(portalSkillId).Effects[0].Template;
                    // Store enter portal modelId
                    var enterModelId = NpcManager.Instance.GetTemplate(openPortalEffectTemplate.portalEnterId).ModelId.ToString();
                    Connection.ActiveChar.SetOption(1665, enterModelId);
                    // Store exit portal modelId
                    var exitModelId = NpcManager.Instance.GetTemplate(openPortalEffectTemplate.portalExitId).ModelId.ToString();
                    Connection.ActiveChar.SetOption(1666, exitModelId);
                    Logger.Debug($"Set custom portal: Enter ModelId {enterModelId} Exit ModelId {exitModelId}");
                    Connection.ActiveChar.SendMessage($"Set '{name}' as custom portal.");
                }
                else
                {
                    Connection.ActiveChar.SendMessage($"You do not own '{name}' portal book.");
                }
            }
            else
            {
                Connection.ActiveChar.Friends.AddFriend(name);
            }
        }
    }
}
