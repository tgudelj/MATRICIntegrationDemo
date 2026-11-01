using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;
using Matric.Integration;

namespace IntegrationDemo {


    /// <summary>Runs the demonstration on one selected device while UDP callbacks continue receiving events.</summary>
    class Program {

        public static string DEMO_DECK_ID = CONST.DEMO_DECK_ID;
        public static string PIN = CONST.PIN;
        public static string CLIENT_ID;
        static Matric.Integration.Matric matric;

        private static DemoSession session;
        private static volatile bool closing;

        /// <summary>Owns the SDK and foreground animation; Ctrl+C cancels waits before disposing the receive socket.</summary>
        static int Main(string[] args) {
            Console.Title = "MATRIC Integration Demo .NET";
            using (var cancellation = new CancellationTokenSource()) {
                ConsoleCancelEventHandler cancel = (sender, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
                Console.CancelKeyPress += cancel;
                try {
                    var options = DemoOptions.Parse(args);
                    using (session = new DemoSession(options, cancellation.Token)) {
                        try {
                            // PIN configuration is intentionally unchanged; no interactive authorization is introduced here.
                            matric = new Matric.Integration.Matric(CONST.APP_NAME, CONST.PIN, options.ApiPort, options.ListenerPort);
                            matric.PIN = PIN;
                            matric.OnError += Matric_OnError;
                            matric.OnConnectedClientsReceived += Matric_OnConnectedClientsReceived;
                            matric.OnControlInteraction += Matric_OnControlInteraction;
                            matric.OnVariablesChanged += Matric_OnVariablesChanged;
                            CLIENT_ID = session.SelectClient(matric.GetConnectedClients);
                            StartDemo();
                        } finally {
                            closing = true;
                            matric?.Stop();
                            matric?.Dispose();
                        }
                    }
                    return 0;
                } catch (OperationCanceledException) { Console.WriteLine("Demo cancelled."); return 130; }
                catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
                finally { Console.CancelKeyPress -= cancel; }
            }
        }

        /// <summary>Displays variable notifications while the animation continues.</summary>
        private static void Matric_OnVariablesChanged(object sender, ServerVariablesChangedEventArgs data) {
            Console.WriteLine("Server variables changed");
            foreach (string varName in data.ChangedVariables) {
                Console.WriteLine($"{varName}: {data.Variables[varName].Value}");
            }
        }

        /// <summary>Reports receive errors while ignoring expected socket closure during shutdown.</summary>
        private static void Matric_OnError(Exception ex)
        {
            if (!closing) Console.Error.WriteLine(ex.Message);
        }

        /// <summary>Displays control interactions without performing animation work.</summary>
        private static void Matric_OnControlInteraction(object sender, object data)
        {
            Console.WriteLine("Control interaction:");
            Console.WriteLine(data.ToString());
        }

        /// <summary>Transfers client-list responses to the foreground polling loop.</summary>
        private static void Matric_OnConnectedClientsReceived(object source, List<ClientInfo> clients) {
            UpdateClientsList(clients);
        }

        /// <summary>Demonstrates temporary appearances and state; pacing delays are not command acknowledgements.</summary>
        private static void StartDemo() {
            string GRAY = "#111111";
            string YELLOW = "yellow";
            string GREEN = "green";
            string RED = "red";
            string BLACK = "black";
            string WHITE = "white";

            ServerVariable vString = new ServerVariable() {
                Name = "demo_string",
                VariableType = ServerVariable.ServerVariableType.STRING,
                Value = "Initial string"
            };
            ServerVariable vBool = new ServerVariable() {
                Name = "demo_bool",
                VariableType = ServerVariable.ServerVariableType.BOOL,
                Value = true
            };
            ServerVariable vNumber = new ServerVariable() {
                Name = "demo_number",
                VariableType = ServerVariable.ServerVariableType.NUMBER,
                Value = 1
            };
            ServerVariable vJsonObject = new ServerVariable() {
                Name = "demo_json",
                VariableType = ServerVariable.ServerVariableType.JSONOBJECT,
                Value = new { propNumber = 1, propString = "Hello from demo" }
            };

            List<ServerVariable> variables = new List<ServerVariable>() { 
                vString, vBool, vNumber, vJsonObject
            };

            matric.SetVariables(variables);
            session.Delay(5000);
            vString.Value = "New value...";
            vBool.Value = !((bool)vBool.Value);
            vNumber.Value = ((int)vNumber.Value) + 1;
            vJsonObject.Value = new { propNumber = 999, propString = "Is it working?" };
            matric.SetVariables(variables);
            session.Delay(5000);

            matric.SetDeck(CLIENT_ID, DEMO_DECK_ID, DemoPages.SEMAPHORE);
            session.WaitForDeck(matric.GetConnectedClients, CLIENT_ID, DEMO_DECK_ID);

            List<SetButtonsVisualStateArgs> listOn = new List<SetButtonsVisualStateArgs>();
            // Names identify controls within the selected device's loaded deck.
            listOn.Add(new SetButtonsVisualStateArgs(state:"on", buttonName: "BTN_GREEN"));
            listOn.Add(new SetButtonsVisualStateArgs(state:"on", buttonName: "BTN_YELLOW"));
            listOn.Add(new SetButtonsVisualStateArgs(state:"on", buttonName: "BTN_RED"));

            List<SetButtonsVisualStateArgs> listOff = new List<SetButtonsVisualStateArgs>();
            listOff.Add(new SetButtonsVisualStateArgs(state:"off", buttonName: "BTN_GREEN"));
            listOff.Add(new SetButtonsVisualStateArgs(state:"off", buttonName: "BTN_YELLOW"));
            listOff.Add(new SetButtonsVisualStateArgs(state:"off", buttonName: "BTN_RED"));

            matric.SetButtonsVisualState(CLIENT_ID, listOn);
            session.Delay(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOff);
            session.Delay(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOn);
            session.Delay(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOff);
            session.Delay(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOn);
            session.Delay(300);
            matric.SetButtonsVisualState(CLIENT_ID, listOff);


            matric.SetButtonProperties(CLIENT_ID, backgroundcolorOff: GREEN, buttonName: "BTN_GREEN");
            session.Delay(800);
            matric.SetButtonProperties(CLIENT_ID, backgroundcolorOff: GRAY, buttonName: "BTN_GREEN");
            matric.SetButtonProperties(CLIENT_ID, backgroundcolorOff: YELLOW, buttonName: "BTN_YELLOW");
            session.Delay(800);
            matric.SetButtonProperties(CLIENT_ID, backgroundcolorOff: GRAY, buttonName: "BTN_GREEN");
            matric.SetButtonProperties(CLIENT_ID, backgroundcolorOff: GRAY, buttonName: "BTN_YELLOW");
            matric.SetButtonProperties(CLIENT_ID, backgroundcolorOff: RED, buttonName: "BTN_RED");
            session.Delay(800);
            matric.SetButtonProperties(CLIENT_ID, fontSize: "10", buttonName: "BTN_GREEN");
            session.Delay(500);
            matric.SetButtonProperties(CLIENT_ID, fontSize: "20", buttonName: "BTN_GREEN");
            session.Delay(500);
            matric.SetButtonProperties(CLIENT_ID, fontSize: "30", buttonName: "BTN_GREEN");
            session.Delay(500);
            matric.SetButtonProperties(CLIENT_ID, fontSize: "40", buttonName: "BTN_GREEN");
            session.Delay(500);

            matric.SetButtonPropertiesEx(CLIENT_ID, new List<SetButtonPropsArgsEx> {
                new SetButtonPropsArgsEx
                {
                    text="RED!!!", ButtonName = "BTN_RED"
                },
                new SetButtonPropsArgsEx
                {
                    text="YELLOW!!!", ButtonName = "BTN_YELLOW"
                },
                new SetButtonPropsArgsEx
                {
                    text="GREEN!!!", ButtonName = "BTN_GREEN"
                }
            });
            session.Delay(2000);
            matric.SetActivePage(CLIENT_ID, DemoPages.SHIELDS_AND_WEAPONS);
            session.Delay(1500);
            matric.SetControlsState(CLIENT_ID, new List<SetControlsStateArgs> {
                    new SetControlsStateArgs
                    {
                        ControlId=null, ControlName="MULTI_POS_5", State = new { position = 4}
                    }
                    });
            //simulate battle
            int ammo = 500;
            int shields = 100;
            for (int i = 0; i < 100; i++) {
                matric.SetControlsState(CLIENT_ID, new List<SetControlsStateArgs> {
                    new SetControlsStateArgs
                    {
                        ControlId=null, ControlName="SHIELDS_SLIDER", State = new { value = shields}
                    }
                });
                if (shields < 30)
                {
                    matric.SetControlsState(CLIENT_ID, new List<SetControlsStateArgs> {
                    new SetControlsStateArgs
                    {
                        ControlId=null, ControlName="MULTI_POS_5", State = new { position = 2}
                    }
                    });
                    matric.SetButtonProperties(CLIENT_ID, buttonName:"SHIELDS_CRITICAL", textcolorOff: BLACK, backgroundcolorOff: RED);
                    matric.SetButtonProperties(CLIENT_ID, buttonName:"SHIELDS_PERCENT", backgroundcolorOff: GRAY, textcolorOff: RED, text: $@"{shields}%");
                }
                else if (shields < 50)
                {
                    matric.SetControlsState(CLIENT_ID, new List<SetControlsStateArgs> {
                    new SetControlsStateArgs
                    {
                        ControlId=null, ControlName="MULTI_POS_5", State = new { position = 3}
                    }
                    });
                    matric.SetButtonProperties(CLIENT_ID, buttonName: "SHIELDS_PERCENT", backgroundcolorOff: GRAY, textcolorOff: YELLOW, text: $@"{ shields}%");
                }
                else {
                    matric.SetButtonProperties(CLIENT_ID, buttonName: "SHIELDS_PERCENT", backgroundcolorOff: GRAY, textcolorOff: GREEN, text: $@"{ shields}%");
                }
                matric.SetButtonProperties(CLIENT_ID, buttonName: "AMMO_COUNT", backgroundcolorOff: GRAY, textcolorOff: WHITE, text: $@"{ ammo}");
                shields--;
                ammo = ammo -5;
                session.Delay(100);
            }

            //flash critical shields
            matric.SetControlsState(CLIENT_ID, new List<SetControlsStateArgs> {
                    new SetControlsStateArgs
                    {
                        ControlId=null, ControlName="MULTI_POS_5", State = new { position = 1}
                    }
                    });
            bool criticalOn = true;
            for (int i = 0; i < 10; i++) {
                if (criticalOn)
                {
                    matric.SetButtonProperties(CLIENT_ID, buttonName: "SHIELDS_CRITICAL", textcolorOff: GRAY, backgroundcolorOff: BLACK);
                }
                else {
                    matric.SetButtonProperties(CLIENT_ID, buttonName: "SHIELDS_CRITICAL", textcolorOff: GRAY, backgroundcolorOff: RED);
                }
                criticalOn = !criticalOn;
                session.Delay(400);
            }

            matric.SetActivePage(CLIENT_ID, DemoPages.DEMO_COMPLETE);
            session.Delay(1000);
            matric.SetButtonProperties(CLIENT_ID, buttonName: "WINK_BUTTON", text: CONST.SMILE_WINK);
            Console.WriteLine("Demo complete");

        }


        /// <summary>Publishes a snapshot without starting or restarting the animation on the receive callback.</summary>
        public static void UpdateClientsList(List<ClientInfo> connectedClients) {
            if (connectedClients != null) session.PublishClients(JArray.FromObject(connectedClients));
        }
    }
}
