using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace IntegrationDemo {
    /// <summary>Shared demo identities and defaults; PIN authorization remains configured exactly as before.</summary>
    public static class CONST {
        public static string DEMO_DECK_ID = "7f18056c-7a2a-46ac-9956-662e7d0b78ec";
        public static string SMILE_WINK = "😉";
        public static string APP_NAME = "MATRIC Integration Demo";
        public static int API_PORT = 5300;
        public static string PIN = "0450";
    }

    /// <summary>Page identities in the supplied deck.</summary>
    public static class DemoPages {
        public static string SEMAPHORE = "00d9c649-f2df-405f-abfd-06d38f8626be";
        public static string SHIELDS_AND_WEAPONS = "16856457-d2f8-405e-927e-749b1dc7d7ec";
        public static string DEMO_COMPLETE = "b0a0b559-9e2c-42af-9c64-95951ef09cbd";
    }

    /// <summary>Validated command-line settings for both demos, independent of the UDP protocol.</summary>
    public sealed class DemoOptions {
        public int ApiPort = CONST.API_PORT;
        public int ListenerPort = 50301;
        public int TimeoutMs = 10000;
        public int? LegacyDeckDelayMs;
        public string ClientId;

        /// <summary>Reads explicit name=value options; receive ports stay stable unless the user changes them.</summary>
        public static DemoOptions Parse(string[] args) {
            var options = new DemoOptions();
            foreach (string arg in args) {
                int separator = arg.IndexOf('=');
                if (separator < 0) throw new ArgumentException("Use --port=5300, --listen-port=50301, --client-id=<id>, --timeout-ms=10000 or --legacy-deck-delay-ms=1500.");
                string name = arg.Substring(0, separator), value = arg.Substring(separator + 1);
                if (name == "--client-id") {
                    if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Client ID cannot be empty.");
                    options.ClientId = value;
                    continue;
                }
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                    throw new ArgumentException("Invalid integer for " + name);
                switch (name) {
                    case "--port": options.ApiPort = Port(number); break;
                    case "--listen-port": options.ListenerPort = Port(number); break;
                    case "--timeout-ms":
                        if (number <= 0) throw new ArgumentException("Timeout must be positive.");
                        options.TimeoutMs = number; break;
                    case "--legacy-deck-delay-ms":
                        if (number < 0) throw new ArgumentException("Legacy delay cannot be negative.");
                        options.LegacyDeckDelayMs = number; break;
                    default: throw new ArgumentException("Unknown option: " + name);
                }
            }
            return options;
        }

        /// <summary>Rejects invalid ports before attempting to bind or send.</summary>
        private static int Port(int value) {
            if (value < 1 || value > 65535) throw new ArgumentException("Ports must be between 1 and 65535.");
            return value;
        }
    }

    /// <summary>Transfers client snapshots from receive callbacks to the foreground demo, with bounded waits and cancellation.</summary>
    public sealed class DemoSession : IDisposable {
        private readonly BlockingCollection<JArray> snapshots = new BlockingCollection<JArray>(1);
        private readonly object sync = new object();
        private readonly DemoOptions options;
        private readonly CancellationToken cancellation;
        private bool disposed;

        /// <summary>Uses caller-owned cancellation and validated settings; does not own any sockets.</summary>
        public DemoSession(DemoOptions options, CancellationToken cancellation) {
            this.options = options;
            this.cancellation = cancellation;
        }

        /// <summary>Keeps only the newest snapshot and never runs animations or waits on the receive thread.</summary>
        public void PublishClients(JArray clients) {
            if (clients == null) return;
            lock (sync) {
                if (disposed) return;
                snapshots.TryTake(out _);
                snapshots.TryAdd(clients);
            }
        }

        /// <summary>Requests connected clients with bounded retries and chooses one explicit target for every animation command.</summary>
        public string SelectClient(Action requestClients) {
            JArray clients = WaitForSnapshot(requestClients, list => {
                foreach (JObject client in list) {
                    string id = (string)client["clientId"];
                    if (!string.IsNullOrWhiteSpace(id) && (options.ClientId == null || options.ClientId == id)) return true;
                }
                return false;
            }, "No matching connected device replied. Check MATRIC, API/PIN settings, ports, and the device connection.");
            string selected = null;
            foreach (JObject client in clients) {
                string id = (string)client["clientId"];
                Console.WriteLine(id + " " + (string)client["clientName"]);
                if (selected == null && !string.IsNullOrWhiteSpace(id) && (options.ClientId == null || options.ClientId == id)) selected = id;
            }
            Console.WriteLine("Running demo on device " + selected + ". Press Ctrl+C to cancel.");
            return selected;
        }

        /// <summary>Polls reported deck membership; older servers/clients require an explicitly selected timing fallback.</summary>
        public void WaitForDeck(Action requestClients, string clientId, string deckId) {
            if (options.LegacyDeckDelayMs.HasValue) {
                Console.WriteLine("Using legacy deck delay; this does not confirm loading or rendering.");
                Delay(options.LegacyDeckDelayMs.Value);
                return;
            }
            WaitForSnapshot(requestClients, clients => {
                foreach (JObject client in clients)
                    if ((string)client["clientId"] == clientId && (string)client["currentDeckId"] == deckId) return true;
                return false;
            }, "Deck membership was not confirmed. Check the imported deck/device. Older servers or clients require explicit --legacy-deck-delay-ms=<ms>. Rendering completion is not acknowledged.");
        }

        /// <summary>Retries only the read-only client query; never retries deck switches or action-changing commands automatically.</summary>
        private JArray WaitForSnapshot(Action requestClients, Func<JArray, bool> matches, string failure) {
            while (snapshots.TryTake(out _)) { }
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < options.TimeoutMs) {
                cancellation.ThrowIfCancellationRequested();
                requestClients();
                int remaining = options.TimeoutMs - (int)watch.ElapsedMilliseconds;
                if (remaining > 0 && snapshots.TryTake(out JArray clients, Math.Min(500, remaining), cancellation) && matches(clients)) return clients;
                // Empty or stale replies must not turn retries into a tight loop.
                remaining = options.TimeoutMs - (int)watch.ElapsedMilliseconds;
                if (remaining > 0) Delay(Math.Min(100, remaining));
            }
            throw new TimeoutException(failure);
        }

        /// <summary>Pauses animation on the foreground thread and responds immediately to cancellation.</summary>
        public void Delay(int milliseconds) {
            cancellation.ThrowIfCancellationRequested();
            if (cancellation.WaitHandle.WaitOne(milliseconds)) cancellation.ThrowIfCancellationRequested();
        }

        /// <summary>Stops accepting late callbacks and releases snapshot resources after the foreground demo has stopped.</summary>
        public void Dispose() {
            lock (sync) {
                if (disposed) return;
                disposed = true;
                snapshots.Dispose();
            }
        }
    }
}
