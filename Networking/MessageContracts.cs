namespace AetherPool.Networking
{
    /// <summary>
    /// Defines the different types of messages that can be sent between the client and server.
    /// Each value represents a single byte that will prefix the message payload.
    /// </summary>
    public enum MessageType : byte
    {
        /// <summary>
        /// A comprehensive state update. The payload for this message will contain
        /// all the necessary information, including the specific action being performed.
        /// </summary>
        STATE_UPDATE,

        /// <summary>
        /// A session management message sent from the server to the client indicating
        /// that the room is about to close due to inactivity or expiration.
        /// </summary>
        ROOM_CLOSING_IMMINENTLY,
    }
}
