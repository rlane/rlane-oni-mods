using KSerialization;
using System.Linq;
using System.Collections.Generic;

namespace Endpoint
{
    class EndpointTransport : KMonoBehaviour, ISidescreenButtonControl
    {
        [KSerialization.Serialize]
        public bool stay_at_destination;

        [KSerialization.Serialize]
        public bool has_reached_destination;

        public string SidescreenTitleKey => "STRINGS.UI.UISIDESCREENS.ENDPOINTTRANSPORT.TITLE";

        public string SidescreenStatusMessage
        {
            get
            {
                if (stay_at_destination)
                {
                    return "Duplicants will stay at the destination if it is habitable.";
                }
                else
                {
                    return "Duplicants will return with the rocket when the mission is complete.";
                }

            }
        }

        public string SidescreenButtonText
        {
            get
            {
                if (!stay_at_destination)
                {
                    return "STAY AT DESTINATION";
                }
                else
                {
                    return "RETURN TO COLONY";
                }
            }
        }

        public void OnSidescreenButtonPressed()
        {
            stay_at_destination = !stay_at_destination;
        }

        public void SetReachedDestination(bool reached_destination, SpaceDestination destination)
        {
            if (reached_destination != has_reached_destination)
            {
                has_reached_destination = reached_destination;
                if (reached_destination && destination.type == "Endpoint" && stay_at_destination)
                {
                    var storage = GetComponent<MinionStorage>();
                    var ids = storage.GetStoredMinionInfo().Select((x) => x.id).ToList();
                    var state = EndpointState.Load();
                    foreach (var id in ids)
                    {
                        var minion = storage.DeserializeMinion(id, transform.position);
                        var identity = minion.GetComponent<MinionIdentity>();
                        var name = identity.nameStringKey;
                        Debug.Log("Transported " + name + " to " + destination.type);
                        // Delete duplicant.
                        minion.GetComponent<Schedulable>().GetSchedule().Unassign(minion.GetComponent<Schedulable>());
                        identity.GetSoleOwner().UnassignAll();
                        identity.GetEquipment().UnequipAll();
                        Components.MinionAssignablesProxy.Remove(identity.assignableProxy.Get());
                        Components.MinionResumes.Remove(minion.GetComponent<MinionResume>());
                        minion.gameObject.SetActive(false);
                        // Show message.
                        Messenger.Instance.QueueMessage(new EndpointMessage(minion.name));
                        // Hacks to avoid crash in SkillsScreen.
                        Components.LiveMinionIdentities.Add(identity);
                        Components.LiveMinionIdentities.Remove(identity);
                        Game.Instance.userMenu.Refresh(gameObject);
                        // Record duplicant as rescued in the state file.
                        if (!state.times_rescued.ContainsKey(name))
                        {
                            state.times_rescued[name] = 0;
                        }
                        state.times_rescued[name] += 1;
                    }
                    state.Save();
                }
            }
        }
    }

    class EndpointMessage : Message
    {
        [Serialize]
        private string name;

        public EndpointMessage(string name)
        {
            this.name = name;
        }

        public EndpointMessage()
        {
        }

        public override string GetSound()
        {
            return null;
        }

        public override string GetMessageBody()
        {
            return string.Empty;
        }

        public override string GetTooltip()
        {
            return "Future iterations of " + name + " will receive a boost to all attributes";
        }

        public override string GetTitle()
        {
            return "Duplicant arrived at Endpoint";
        }

        public override bool ShowDialog()
        {
            return false;
        }

        public override void OnClick()
        {
        }
    }
}
