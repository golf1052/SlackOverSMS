using System.Collections.Generic;

namespace SlackSMSWPF
{
    public static class SecretsExample
    {
        /// <summary>
        /// Your Slack client ID
        /// </summary>
        public const string SlackClientId = "";

        /// <summary>
        /// Your Slack client secret
        /// </summary>
        public const string SlackClientSecret = "";

        /// <summary>
        /// Your Slackbot integration url (so you can send messages to a channel)
        /// </summary>
        public const string SlackBotUrl = "";

        /// <summary>
        /// Your Twilio SID
        /// </summary>
        public const string TwilioSid = "";

        /// <summary>
        /// Your Twilio Authorization Token
        /// </summary>
        public const string TwilioAuthToken = "";

        /// <summary>
        /// Your Twilio number
        /// </summary>
        public const string TwilioNumber = "";

        /// <summary>
        /// The port you will be listening over
        /// </summary>
        public const int LocalPort = 5000;

        public static List<SmsUser> SmsUsers;

        static SecretsExample()
        {
            SmsUsers = new List<SmsUser>();

            // Add users who can be reached through SMS
            SmsUsers.Add(SmsUser.CreateFromBoth("testuser1", "Tester", "+14015555555"));
            SmsUsers.Add(SmsUser.CreateFromBoth("ionia_rules", "Karma", "+16175555555"));
        }
    }
}
