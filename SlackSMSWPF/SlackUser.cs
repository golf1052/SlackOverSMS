using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SlackSMSWPF
{
    public class SlackUser
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string RealName { get; set; }

        public SlackUser(string id, string username, string firstname, string lastname, string realname)
        {
            Id = id;
            Username = username;
            FirstName = firstname;
            LastName = lastname;
            RealName = realname;
        }
    }
}
