using IntegrationDemo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Checks raw wire compatibility, callback recovery, cancellation, and deck readiness without a running MATRIC instance.</summary>
internal static class DemoChecks {
    /// <summary>Runs isolated checks and reports failures through the process exit code.</summary>
    private static int Main() {
        try { Wire(); Session(); Console.WriteLine("PASS All demo checks."); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>Exercises escaped strings, real nulls, independent images, state objects, UTF-8, malformed packets, and disposal.</summary>
    private static void Wire() {
        using (var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
        using (var received = new ManualResetEventSlim()) {
            int port = ((IPEndPoint)server.Client.LocalEndPoint).Port;
            int listenerPort;
            string unicode = "Željko 😉";
            int errors = 0;
            using (var receiver = new DemoIntegrationHelper(json => {
                var message = JObject.Parse(json);
                if ((string)message["text"] == unicode) received.Set();
            }, error => Interlocked.Increment(ref errors), port, 0)) {
                receiver.SetButtonProperties(null, "Button \"one\"", text: "Quote \" slash \\ line\n" + unicode, imageOff: "off.png", imageOn: "on.png");
                var remote = new IPEndPoint(IPAddress.Any, 0);
                server.Client.ReceiveTimeout = 2000;
                var message = JObject.Parse(Encoding.UTF8.GetString(server.Receive(ref remote)));
                listenerPort = remote.Port;
                Require(message["clientId"].Type == JTokenType.Null, "Broadcast null became a string.");
                Require((string)message["data"]["imageOn"] == "on.png" && (string)message["data"]["imageOff"] == "off.png", "Images were mixed up.");
                Require((string)message["data"]["text"] == "Quote \" slash \\ line\n" + unicode, "Text did not round-trip.");
                receiver.SetDeck("tablet", "deck", null);
                message = JObject.Parse(Encoding.UTF8.GetString(server.Receive(ref remote)));
                Require(message["pageId"].Type == JTokenType.Null, "Optional page became an empty string.");
                receiver.SetControlsState("tablet", new List<DemoIntegrationHelper.SetControlStateItem> {
                    new DemoIntegrationHelper.SetControlStateItem(null, new { value = 50 }, "slider")
                });
                message = JObject.Parse(Encoding.UTF8.GetString(server.Receive(ref remote)));
                Require(message["data"][0]["state"].Type == JTokenType.Object, "State was encoded as a JSON string.");
                Send(server, remote, "not JSON");
                Send(server, remote, JsonConvert.SerializeObject(new { text = unicode }));
                Require(received.Wait(2000), "Receiver stopped after malformed JSON or corrupted UTF-8.");
                Require(errors == 1, "Malformed packet was not reported once.");
            }
            using (var rebound = new UdpClient(new IPEndPoint(IPAddress.Loopback, listenerPort))) { }
            Console.WriteLine("PASS Raw wire format, UTF-8, callback recovery, and socket disposal.");
        }
    }

    /// <summary>Tests foreground polling without callback blocking or restarting animation, plus option validation and cancellation.</summary>
    private static void Session() {
        Require(DemoOptions.Parse(new string[0]).ApiPort == 5300, "Wrong default port.");
        var options = DemoOptions.Parse(new[] { "--port=5400", "--listen-port=5401", "--client-id=second", "--timeout-ms=100" });
        Require(options.ApiPort == 5400 && options.ListenerPort == 5401, "Port overrides were not applied.");
        foreach (string bad in new[] { "--port=0", "--port=65536", "--timeout-ms=0", "--legacy-deck-delay-ms=-1", "--unknown=1", "--client-id=" }) {
            bool rejected = false;
            try { DemoOptions.Parse(new[] { bad }); } catch (ArgumentException) { rejected = true; }
            Require(rejected, "Invalid option accepted: " + bad);
        }
        using (var cancel = new CancellationTokenSource())
        using (var session = new DemoSession(options, cancel.Token)) {
            Action publish = () => session.PublishClients(JArray.Parse("[{\"clientId\":\"first\",\"clientName\":\"First\"},{\"clientId\":\"second\",\"clientName\":\"Second\",\"currentDeckId\":\"ready\"}]"));
            Require(session.SelectClient(publish) == "second", "Explicit target was not honored.");
            session.WaitForDeck(publish, "second", "ready");
            bool timedOut = false;
            try { session.WaitForDeck(publish, "second", "missing"); } catch (TimeoutException) { timedOut = true; }
            Require(timedOut, "An unrelated deck was treated as ready.");
            timedOut = false;
            try { session.SelectClient(() => { }); } catch (TimeoutException) { timedOut = true; }
            Require(timedOut, "Discovery did not time out.");
            cancel.Cancel();
            bool cancelled = false;
            try { session.Delay(10000); } catch (OperationCanceledException) { cancelled = true; }
            Require(cancelled, "Animation delay ignored cancellation.");
        }
        using (var session = new DemoSession(new DemoOptions { LegacyDeckDelayMs = 0 }, CancellationToken.None)) {
            session.WaitForDeck(() => { throw new Exception("Legacy fallback should not query deck membership."); }, "old", "deck");
            session.Dispose();
            session.PublishClients(new JArray());
        }
        Console.WriteLine("PASS Target selection, deck polling, bounded timeouts, explicit legacy fallback, and cancellation.");
    }

    /// <summary>Sends one datagram through a private loopback socket.</summary>
    private static void Send(UdpClient sender, IPEndPoint remote, string text) {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        sender.Send(bytes, bytes.Length, remote);
    }

    /// <summary>Fails a check with a specific diagnostic.</summary>
    private static void Require(bool condition, string message) {
        if (!condition) throw new Exception(message);
    }
}
