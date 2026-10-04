using System;
using UnityEngine;

namespace OC2ControllerIcons
{
    // Which local player slots (Player 1-4) currently have someone joined.
    public static class PlayerSlots
    {
        public static int JoinedMask()
        {
            try
            {
                PlayerManager pm = GameUtils.RequestManager<PlayerManager>();
                if (pm == null) return 0;
                int mask = 0;
                for (int i = 0; i < ModSettings.MaxPlayers; i++)
                {
                    if (pm.GetUser((EngagementSlot)i) != null) mask |= 1 << i;
                }
                return mask;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // Joined players using a controller (keyboard players are left out: their button
        // layout does not matter).
        public static int PadMask()
        {
            try
            {
                PlayerManager pm = GameUtils.RequestManager<PlayerManager>();
                if (pm == null) return 0;
                int mask = 0;
                for (int i = 0; i < ModSettings.MaxPlayers; i++)
                {
                    GamepadUser user = pm.GetUser((EngagementSlot)i);
                    if (user != null && user.ControlType != GamepadUser.ControlTypeEnum.Keyboard) mask |= 1 << i;
                }
                return mask;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static int Count(int mask)
        {
            int n = 0;
            for (int i = 0; i < ModSettings.MaxPlayers; i++)
            {
                if ((mask & (1 << i)) != 0) n++;
            }
            return n;
        }
    }

    // Polls the joined players: assigns a layout to newly joined players and refreshes
    // every icon when the set of players changes (it affects the "same layout" check).
    public class PlayerSlotWatcher : MonoBehaviour
    {
        private int m_lastMask = -1;
        private float m_next;

        private void Update()
        {
            if (Time.unscaledTime < m_next) return;
            m_next = Time.unscaledTime + 0.25f;

            int mask = PlayerSlots.JoinedMask();
            if (mask == m_lastMask) return;
            int previous = m_lastMask < 0 ? 0 : m_lastMask;
            m_lastMask = mask;

            for (int i = 0; i < ModSettings.MaxPlayers; i++)
            {
                int bit = 1 << i;
                if ((mask & bit) != 0 && (previous & bit) == 0) ModSettings.OnPlayerJoined(i, mask);
            }
            ModSettings.NotifyChanged();
        }
    }
}
