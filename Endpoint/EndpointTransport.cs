using KSerialization;

namespace Endpoint
{
    class EndpointTransport : KMonoBehaviour, ISidescreenButtonControl
    {
        // TODO: Preserve across save/load.
        bool stay_at_destination;

        // TODO: Fix missing string.
        public string SidescreenTitleKey => "Transport Options";

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
    }
}
