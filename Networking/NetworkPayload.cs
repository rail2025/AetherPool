using System;

namespace AetherPool.Networking
{
    /// <summary>
    /// Defines the specific action being performed in a multiplayer match.
    /// </summary>
    public enum PayloadActionType : byte
    {
        // Player Actions
        Shoot,
        PlaceCueBall,
        SetAim,

        // Game State Sync
        FullGameState,
        BallPositionsUpdate,
        TurnChange,
        Foul,
        GameEnd,

        // Session Management
        RequestRematch,
    }

    /// <summary>
    /// Represents the data structure sent within a STATE_UPDATE message.
    /// </summary>
    [Serializable]
    public class NetworkPayload
    {
        /// <summary>
        /// The specific action to be performed.
        /// </summary>
        public PayloadActionType Action { get; set; }

        /// <summary>
        /// The binary data associated with the action (e.g., shot power and angle).
        /// </summary>
        public byte[]? Data { get; set; }
    }
}
