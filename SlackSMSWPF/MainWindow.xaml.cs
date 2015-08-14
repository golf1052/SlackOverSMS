using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;
using Twilio;
using Windows.Networking.Sockets;

namespace SlackSMSWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HttpClient client;

        string accessToken = "";
        string generalChannelId = "";
        MessageWebSocket webSocket;
        StreamWriter writer;

        TwilioRestClient twilioClient;

        List<SlackUser> users;

        bool serverIsDead;

        public MainWindow()
        {
            InitializeComponent();
            serverIsDead = false;
            client = new HttpClient();
            webSocket = null;
            writer = null;
            users = new List<SlackUser>();
            Uri url = new Uri("https://slack.com/oauth/authorize?client_id=" + Secrets.SlackClientId + "&scope=client");
            urlBox.Text = url.ToString();
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            // use this terribad method of logging in because the WPF WebBrowser doesn't support shit
            string code = codeBox.Text;
            Uri oauthAccess = new Uri("https://slack.com/api/oauth.access?client_id=" + Secrets.SlackClientId + "&client_secret=" + Secrets.SlackClientSecret + "&code=" + code);
            JObject responseObject = await GetWebData(oauthAccess);
            accessToken = (string)responseObject["access_token"];
            await ConnectToWebSocket();
            await OpenConnection();
        }

        private void Cleanup()
        {
            if (webSocket != null)
            {
                writer.Dispose();
                writer = null;
                webSocket.Dispose();
                webSocket = null;
                users.Clear();
            }
        }

        private async Task ConnectToWebSocket()
        {
            // first clean up any potential existing sockets or whatever since we might be reconnecting
            // due to a server migration
            Cleanup();
            Uri startUrl = new Uri("https://slack.com/api/rtm.start?token=" + accessToken);
            DateTime initialConnectionTime = DateTime.UtcNow;
            bool connected = false;
            JObject responseObject = null;
            do
            {
                responseObject = await GetWebData(startUrl);
                if ((bool)responseObject["ok"])
                {
                    connected = true;
                    break;
                }
                else
                {
                    // if there was an error connecting wait 2 seconds
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            } // only try to connect for 30 seconds since thats how long the WebSocket url lasts
            while (DateTime.UtcNow - initialConnectionTime < TimeSpan.FromSeconds(30));

            if (!connected)
            {
                WriteToOutputBox("Something happened while trying to connect (we couldn't connect)");
                return;
            }
            string socketUrl = (string)responseObject["url"];

            // find the general channel, that's where we want to look for messages 
            foreach (JObject channel in responseObject["channels"])
            {
                if ((bool)channel["is_general"])
                {
                    generalChannelId = (string)channel["id"];
                }
            }

            // load in the slack users
            foreach (JObject user in responseObject["users"])
            {
                users.Add(new SlackUser((string)user["id"], (string)user["name"],
                    (string)user["profile"]["first_name"], (string)user["profile"]["last_name"], (string)user["profile"]["real_name"]));
            }

            // setup and connect to the websocket
            webSocket = new MessageWebSocket();
            webSocket.Control.MessageType = SocketMessageType.Utf8;
            webSocket.MessageReceived += WebSocket_MessageReceived;
            await webSocket.ConnectAsync(new Uri(socketUrl));

            // we also set up a writer so we can write to the websocket even though we currently don't use it
            // potential
            writer = new StreamWriter(webSocket.OutputStream.AsStreamForWrite(), System.Text.Encoding.UTF8);
        }

        async Task OpenConnection()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:" + Secrets.LocalPort + "/");
            listener.Start();
            await RunListener(listener);
        }

        async Task RunListener(HttpListener listener)
        {
            await Task.Run(async () =>
            {
                while (listener.IsListening)
                {
                    if (serverIsDead)
                    {
                        // put this reconnect here so weird stuff doesn't happen idk
                        serverIsDead = false;
                        await ConnectToWebSocket();
                    }
                    await Task.Delay(10);
                    HttpListenerContext context = listener.GetContext();
                    NameValueCollection query = context.Request.QueryString;
                    string body = query["Body"];
                    string fromNumber = query["From"];
                    string outputMessage = "";
                    
                    // try to find the user
                    SmsUser user = GetSmsUserByNumber(fromNumber);
                    if (user == null)
                    {
                        // if the user isn't in our SMS list maybe we want to just ignore it
                        if (allowOutsideMessagesCheckbox.IsChecked.HasValue && allowOutsideMessagesCheckbox.IsChecked.Value)
                        {
                            outputMessage += fromNumber + " says: ";
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        // if we do then get their name
                        if (!string.IsNullOrEmpty(user.RealName))
                        {
                            outputMessage += user.RealName + " says: ";
                        }
                        else if (!string.IsNullOrEmpty(user.Username))
                        {
                            outputMessage += user.Username + " says: ";
                        }
                        else
                        {
                            outputMessage += fromNumber + " says: ";
                        }
                    }

                    // and send a message using slackbot to a channel (probably should be the general channel since we are listening on that)
                    outputMessage += body;
                    StringContent content = new StringContent(outputMessage, Encoding.UTF8);
                    HttpResponseMessage response = await client.PostAsync(new Uri(Secrets.SlackBotUrl), content);
                }
            });
        }

        private async void WebSocket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            StreamReader reader = new StreamReader(args.GetDataStream().AsStreamForRead(), System.Text.Encoding.UTF8);

            // get the result in and check if its of type message
            string result = null;
            result = await reader.ReadToEndAsync();
            WriteToOutputBox(result);
            JObject responseObject = JObject.Parse(result);
            string responseType = (string)responseObject["type"];
            if (responseType == "message")
            {
                // then check if it was from the general channel
                string channel = (string)responseObject["channel"];
                if (channel == generalChannelId)
                {
                    // then try to pull the user
                    string sendingUser = (string)responseObject["user"];
                    if (string.IsNullOrEmpty(sendingUser))
                    {
                        return;
                    }
                    SlackUser user = GetSlackUser(sendingUser);
                    if (user == null)
                    {
                        return;
                    }
                    // and pull what they said
                    string messageText = (string)responseObject["text"];

                    // construct our output message
                    string outputMessage = "";
                    if (user.FirstName != null)
                    {
                        outputMessage += user.FirstName + ": ";
                    }
                    else if (user.RealName != null)
                    {
                        outputMessage += user.RealName + ": ";
                    }
                    else
                    {
                        outputMessage += user.Username + ": ";
                    }

                    SmsUser smsToUser = null;
                    string rest = null;
                    if (messageText.StartsWith("<@"))
                    {
                        // if they are an actual slack member then pull up their info using their id
                        int nextBracket = messageText.IndexOf('>');
                        if (nextBracket == -1)
                        {
                            return;
                        }
                        string userId = messageText.Substring(2, nextBracket - 2);
                        SlackUser slackToUser = GetSlackUser(userId);
                        if (slackToUser == null)
                        {
                            return;
                        }

                        // and then try to find their SMS info
                        smsToUser = GetSmsUserByName(slackToUser.Username);
                        if (smsToUser == null)
                        {
                            smsToUser = GetSmsUserByName(slackToUser.FirstName);
                        }
                        if (smsToUser == null)
                        {
                            return;
                        }
                        rest = messageText.Substring(nextBracket + 1);
                    }
                    foreach (SmsUser smsUser in Secrets.SmsUsers)
                    {
                        if (smsUser.RealName == null)
                        {
                            // SMS user needs to at least have a real name
                            return;
                        }
                        // if they arent a slack member then just match using their name
                        if (messageText.StartsWith("@" + smsUser.RealName.ToLower()))
                        {
                            smsToUser = smsUser;
                            rest = messageText.Substring(smsToUser.RealName.Length + 1);
                            break;
                        }
                    }
                    if (smsToUser == null)
                    {
                        return;
                    }
                    if (rest.StartsWith(":"))
                    {
                        rest = rest.Substring(1);
                    }
                    rest = rest.Trim();
                    outputMessage += rest;

                    // then send the message to them
                    Message sentMessage = null;
                    try
                    {
                        twilioClient = new TwilioRestClient(Secrets.TwilioSid, Secrets.TwilioAuthToken);
                        sentMessage = twilioClient.SendMessage(Secrets.TwilioNumber, smsToUser.PhoneNumber, outputMessage);
                    }
                    catch (Exception ex)
                    {
                        WriteToOutputBox("Twilio SendMessage Exception: " + ex.Message);
                    }
                }
            }
            if (responseType == "team_migration_started")
            {
                // hold my socket we're going in
                // server is going down: https://api.slack.com/events/team_migration_started
                serverIsDead = true;
                // we shoudn't probably call ConnectToWebSocket here because we need this method to exit
                // because ConnectToWebSocket turns everything into null
                // so I stuck it in the listener since that's separate
                return;
            }
        }

        public static async Task<JObject> GetWebData(Uri uri)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                return JObject.Parse(responseString);
            }
            else
            {
                throw new Exception("Exception in getting web data");
            }
        }

        SlackUser GetSlackUser(string str)
        {
            if (str == null)
            {
                return null;
            }
            foreach (SlackUser user in users)
            {
                if (str.ToLower() == user.Id.ToLower() || str == user.Username.ToLower())
                {
                    return user;
                }
            }
            return null;
        }
        
        SmsUser GetSmsUserByNumber(string phoneNumber)
        {
            if (phoneNumber == null)
            {
                return null;
            }
            foreach (SmsUser user in Secrets.SmsUsers)
            {
                if (phoneNumber == user.PhoneNumber)
                {
                    return user;
                }
            }
            return null;
        }

        SmsUser GetSmsUserByName(string str)
        {
            if (str == null)
            {
                return null;
            }
            foreach (SmsUser user in Secrets.SmsUsers)
            {
                if (str.ToLower() == user.Username.ToLower() || str.ToLower() == user.RealName.ToLower())
                {
                    return user;
                }
            }
            return null;
        }

        private void WriteToOutputBox(string str)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                outputBox.Items.Add(str);
            }));
        }
    }
}
