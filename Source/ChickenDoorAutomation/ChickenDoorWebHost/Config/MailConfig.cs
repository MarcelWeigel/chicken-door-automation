using System;
using System.Collections.Generic;
using System.Text;

namespace ChickenDoorWebHost.Config
{
    public class MailConfig
    {
        public string SendGridApiKey { get; set; }
        public string Sender { get; set; }
        public List<string> Receivers { get; set; } = new List<string>();
    }
}
