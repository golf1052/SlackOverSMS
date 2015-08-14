using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackSMSWPF
{
    public class SmsUser
    {
        public string Username { get; set; }
        public string RealName { get; set; }
        public string PhoneNumber { get; set; }

        public SmsUser()
        {
            Username = null;
            RealName = null;
            PhoneNumber = null;
        }

        public static SmsUser CreateFromUsername(string username, string phoneNumber)
        {
            SmsUser user = new SmsUser();
            user.Username = username;
            user.PhoneNumber = phoneNumber;
            return user;
        }

        public static SmsUser CreateFromRealName(string realName, string phoneNumber)
        {
            SmsUser user = new SmsUser();
            user.RealName = realName;
            user.PhoneNumber = phoneNumber;
            return user;
        }

        public static SmsUser CreateFromBoth(string username, string realName, string phoneNumber)
        {
            SmsUser user = new SmsUser();
            user.Username = username;
            user.RealName = realName;
            user.PhoneNumber = phoneNumber;
            return user;
        }
    }
}
