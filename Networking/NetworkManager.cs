using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using AetherPool.Serialization;
using AetherPool.Networking;

namespace AetherPool.Networking
{
    /// <summary>
    /// Manages the WebSocket connection to the server for AetherPool multiplayer.
    /// </summary>
    public class NetworkManager : IDisposable
    {
        private ClientWebSocket? webSocket;
        private CancellationTokenSource? cancellationTokenSource;

        /// <summary>
        /// Occurs when the client successfully connects to the server.
        /// </summary>
        public event Action? OnConnected;

        /// <summary>
        /// Occurs when the client disconnects from the server.
        /// </summary>
        public event Action? OnDisconnected;

        /// <summary>
        /// Occurs when a network error is encountered. The string parameter contains the error message.
        /// </summary>
        public event Action<string>? OnError;

        /// <summary>
        /// Occurs when a STATE_UPDATE message is received from the server.
        /// </summary>
        public event Action<NetworkPayload>? OnStateUpdateReceived;

        /// <summary>
        /// Occurs when the server sends a warning that the room is about to close.
        /// </summary>
        public event Action? OnRoomClosingWarning;

        /// <summary>
        /// Gets a value indicating whether the WebSocket is currently connected.
        /// </summary>
        public bool IsConnected => webSocket?.State == WebSocketState.Open;

        /// <summary>
        /// Asynchronously connects to the specified server with a given passphrase and client type.
        /// </summary>
        /// <param name="serverUri">The WebSocket server URI.</param>
        /// <param name="passphrase">The passphrase for the room.</param>
        /// <param name="clientType">The client identifier (e.g., "ab" for AetherPool/AetherBreaker).</param>
        public async Task ConnectAsync(string serverUri, string passphrase, string clientType)
        {
            if (IsConnected) return;

            try
            {
                webSocket = new ClientWebSocket();
                cancellationTokenSource = new CancellationTokenSource();
                Uri connectUri = new Uri($"{serverUri}?passphrase={Uri.EscapeDataString(passphrase)}&client={clientType}");

                await webSocket.ConnectAsync(connectUri, cancellationTokenSource.Token);

                OnConnected?.Invoke();
                _ = Task.Run(() => StartListening(cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Connection failed: {ex.Message}");
                await DisconnectAsync();
            }
        }

        /// <summary>
        /// Asynchronously disconnects from the server.
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (webSocket == null) return;

            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }

            if (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    // Use a timeout for closing the connection gracefully.
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", cts.Token);
                }
                catch (Exception) { /* This is expected if the connection was abruptly terminated. */ }
            }

            webSocket?.Dispose();
            webSocket = null;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// Starts a long-running task to listen for incoming messages from the WebSocket server.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        private async Task StartListening(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192]; // 8KB buffer should be sufficient for pool game state
            try
            {
                while (webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync();
                    }
                    else
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        HandleReceivedMessage(ms.ToArray());
                    }
                }
            }
            catch (OperationCanceledException) { /* Expected when disconnecting. */ }
            catch (Exception ex)
            {
                OnError?.Invoke($"Network error: {ex.Message}");
                await DisconnectAsync();
            }
        }

        /// <summary>
        /// Handles a fully received message, parsing its type and invoking the appropriate event.
        /// </summary>
        /// <param name="messageBytes">The complete binary message received from the server.</param>
        private void HandleReceivedMessage(byte[] messageBytes)
        {
            if (messageBytes.Length < 1) return;

            MessageType type = (MessageType)messageBytes[0];
            byte[] payloadBytes = new byte[messageBytes.Length - 1];
            Array.Copy(messageBytes, 1, payloadBytes, 0, payloadBytes.Length);

            switch (type)
            {
                case MessageType.STATE_UPDATE:
                    var payload = PayloadSerializer.Deserialize(payloadBytes);
                    if (payload != null)
                    {
                        OnStateUpdateReceived?.Invoke(payload);
                    }
                    break;

                case MessageType.ROOM_CLOSING_IMMINENTLY:
                    OnRoomClosingWarning?.Invoke();
                    break;
            }
        }

        /// <summary>
        /// Asynchronously sends a state update to the server.
        /// </summary>
        /// <param name="payload">The NetworkPayload object containing the action and data.</param>
        public async Task SendStateUpdateAsync(NetworkPayload payload)
        {
            if (!IsConnected || webSocket == null || cancellationTokenSource == null) return;

            try
            {
                byte[] payloadBytes = PayloadSerializer.Serialize(payload);

                byte[] messageToSend = new byte[1 + payloadBytes.Length];
                messageToSend[0] = (byte)MessageType.STATE_UPDATE;
                Array.Copy(payloadBytes, 0, messageToSend, 1, payloadBytes.Length);

                await webSocket.SendAsync(new ArraySegment<byte>(messageToSend), WebSocketMessageType.Binary, true, cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Failed to send message: {ex.Message}");
                await DisconnectAsync();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }
    }
}
