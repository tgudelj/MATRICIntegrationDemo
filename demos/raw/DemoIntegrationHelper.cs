using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IntegrationDemo {
    /// <summary>Small raw UDP example that preserves the existing JSON wire format without depending on the SDK.</summary>
    public sealed class DemoIntegrationHelper : IDisposable {
        private readonly UdpClient udpClient;
        private readonly IPEndPoint serverEP;
        private readonly Action<string> onMessage;
        private readonly Action<Exception> onError;
        private readonly object sync = new object();
        private bool disposed;
        public string PIN { get; set; } = CONST.PIN;

        /// <summary>Binds a stable local receive port and starts receiving on the same socket used for commands.</summary>
        public DemoIntegrationHelper(Action<string> onMessage, Action<Exception> onError, int apiPort = 5300, int listenerPort = 50301) {
            this.onMessage = onMessage ?? throw new ArgumentNullException(nameof(onMessage));
            this.onError = onError ?? throw new ArgumentNullException(nameof(onError));
            serverEP = new IPEndPoint(IPAddress.Loopback, apiPort);
            udpClient = new UdpClient(new IPEndPoint(IPAddress.Loopback, listenerPort));
            try { udpClient.BeginReceive(ReceiveCallback, null); }
            catch { udpClient.Dispose(); throw; }
        }

        /// <summary>Decodes UTF-8 and keeps receiving after malformed packets or callback errors; shutdown is not an error.</summary>
        private void ReceiveCallback(IAsyncResult result) {
            try {
                var remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = udpClient.EndReceive(result, ref remote);
                if (IPAddress.IsLoopback(remote.Address)) onMessage(Encoding.UTF8.GetString(bytes));
            } catch (Exception error) {
                lock (sync) { if (disposed) return; }
                ReportError(error);
            } finally {
                lock (sync) {
                    if (!disposed) {
                        try { udpClient.BeginReceive(ReceiveCallback, null); }
                        catch (Exception error) { ReportError(error); }
                    }
                }
            }
        }

        /// <summary>Isolates error-reporting callbacks so they cannot terminate a UDP worker thread.</summary>
        private void ReportError(Exception error) {
            try { onError(error); } catch { /* Error reporting must not terminate the receiver. */ }
        }

        /// <summary>Requests registration/authorization without changing the demo's existing PIN workflow.</summary>
        public void Connect() { Send("CONNECT"); }

        /// <summary>Requests the current device list; responses are delivered asynchronously.</summary>
        public void GetConnectedClients() { Send("GETCONNECTEDCLIENTS"); }

        /// <summary>Updates visual properties; null preserves a property and empty text/images explicitly clear it.</summary>
        public void SetButtonProperties(string clientId, string buttonName, string text = null, string textcolorOn = null, string textcolorOff = null,
            string backgroundcolorOff = null, string backgroundcolorOn = null, string imageOn = null, string imageOff = null, string fontSize = null, string buttonId = null) {
            Send("SETBUTTONPROPS", clientId, new { buttonId, buttonName, data = new {
                imageOff, imageOn, textcolorOn, textcolorOff, backgroundcolorOn, backgroundcolorOff, fontSize, text
            } });
        }

        /// <summary>Selects a page in the target device's loaded deck.</summary>
        public void SetActivePage(string clientId, string pageId) { Send("SETACTIVEPAGE", clientId, new { pageId }); }

        /// <summary>Requests a deck and optional page; sending does not acknowledge device readiness.</summary>
        public void SetDeck(string clientId, string deckId, string pageId = null) { Send("SETDECK", clientId, new { deckId, pageId }); }

        /// <summary>Control selector and an actual state object, serialized as JSON rather than interpolated text.</summary>
        public sealed class SetControlStateItem {
            public string controlId;
            public string controlName;
            public object state;
            /// <summary>Constructs one control state update by ID or name.</summary>
            public SetControlStateItem(string controlId, object state, string controlName = null) {
                this.controlId = controlId; this.state = state; this.controlName = controlName;
            }
        }

        /// <summary>Legacy button-only visual state update.</summary>
        public sealed class VisualStateItem {
            public string buttonId;
            public string buttonName;
            public string state;
            /// <summary>Constructs an on/off visual update by ID or name.</summary>
            public VisualStateItem(string buttonId, string state, string buttonName = null) {
                this.buttonId = buttonId; this.state = state; this.buttonName = buttonName;
            }
        }

        /// <summary>Sends a batch of legacy button appearances without triggering input actions.</summary>
        public void SetButtonsVisualState(string clientId, List<VisualStateItem> list) { Send("SETBUTTONSVISUALSTATE", clientId, new { data = list }); }

        /// <summary>Sends a batch of object-valued control states.</summary>
        public void SetControlsState(string clientId, List<SetControlStateItem> list) { Send("SETCONTROLSSTATE", clientId, new { data = list }); }

        /// <summary>Serializes escaped JSON with genuine null values and reports send errors to the foreground caller.</summary>
        private void Send(string command, string clientId = null, object payload = null) {
            var message = JObject.FromObject(new { command, appName = CONST.APP_NAME, appPIN = command == "CONNECT" ? null : PIN, clientId });
            if (payload != null)
                foreach (var property in JObject.FromObject(payload).Properties()) message.Add(property.Name, property.Value);
            byte[] bytes = Encoding.UTF8.GetBytes(message.ToString(Formatting.None));
            lock (sync) {
                if (disposed) throw new ObjectDisposedException(nameof(DemoIntegrationHelper));
                udpClient.Send(bytes, bytes.Length, serverEP);
            }
        }

        /// <summary>Stops pending receive work and releases the UDP port deterministically.</summary>
        public void Dispose() {
            lock (sync) {
                if (disposed) return;
                disposed = true;
                udpClient.Dispose();
            }
        }
    }
}
