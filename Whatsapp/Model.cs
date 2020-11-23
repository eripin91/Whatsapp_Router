using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Whatsapp
{
    public partial class BaseEntities : DbContext
    {
        public BaseEntities(DbContextOptions<BaseEntities> options)
            : base(options)
        { }
        public DbSet<ContestRouter_Contest> ContestRouter_Contests { get; set; }
        public DbSet<ContestRouter_WA_Account> ContestRouter_WA_Accounts { get; set; }
        public DbSet<ContestRouter_WA_Inbound> ContestRouter_WA_Inbounds { get; set; }
        public DbSet<ContestRouter_WA_Outbound> ContestRouter_WA_Outbounds { get; set; }
        
    }
    public class ContestRouter_Contest
    {
        [Key]
        public int ContestRouterId { get; set; }
        public int ContestId { get; set; }
        public string ContestType { get; set; }
        public string ContestName { get; set; }
        public System.DateTime StartDate { get; set; }
        public System.DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string ContestNumber { get; set; }
        public string Keyword { get; set; }
        public string ContestInboundUrl { get; set; }
    }

    public class ContestRouter_WA_Account
    {
        [Key]
        public int WaAccountId { get; set; }
        public string Provider { get; set; }
        public string WhatsappID { get; set; }
        public string WhatsappNumber { get; set; }
    }

    #region whatsapp_inbound
    public class ContestRouter_WA_Inbound
    {
        [Key]
        public int WhatsappInboundId { get; set; }
        public int? ContestId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ContactName { get; set; }
        public string ContactWaId { get; set; }
        public string NotificationFrom { get; set; }
        public string NotificationTo { get; set; }
        public string NotificationMessageId { get; set; }
        public string NotificationMessageType { get; set; }
        public string NotificationMessageBody { get; set; }
        public string NotificationMessageDetails { get; set; }
        public string NotificationMessageUrl { get; set; }
        public string NotificationMessageMimeType { get; set; }
        public string NotificationMessageCaption { get; set; }
        public DateTime? NotificationTimestamp { get; set; }
    }    
    public class Whatsapp_Inbound_Sinch
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("statuses")]
        public Status_Model[] Statuses { get; set; }

        [JsonProperty("contacts")]
        public Contact[] Contacts { get; set; }

        [JsonProperty("notifications")]
        public Notification[] Notifications { get; set; }
    }
    public class Status_Model
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("state")]
        public string state { get; set; }
        [JsonProperty("message_id")]
        public string MessageId { get; set; }

        [JsonProperty("recipient")]
        public string Recipient { get; set; }
    }
    public class Contact
    {
        [JsonProperty("profile")]
        public Profile Profile { get; set; }

        [JsonProperty("wa_id")]
        public string WaId { get; set; }
    }
    public class Profile
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
    public class Notification
    {
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("timestamp")]
        public DateTime? Timestamp { get; set; }

        [JsonProperty("message_id")]
        public string MessageId { get; set; }

        [JsonProperty("message")]
        public Inbound_Sinch_Message Inbound_sinch_message { get; set; }
    }
    public class Inbound_Sinch_Message
    {
        public DateTime? CreatedOn { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("mime_type")]
        public string MimeType { get; set; }

        [JsonProperty("caption")]
        public string Caption { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }
    }

    public class Inbound_Webapp
    {
        [JsonProperty("CreatedOn")]
        public DateTime? CreatedOn { get; set; }

        [JsonProperty("MobileNo")]
        public string MobileNo { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; }

        [JsonProperty("FileLink")]
        public string FileLink { get; set; }

        [JsonProperty("EntrySource")]
        public string EntrySource { get; set; }
    }
#endregion
    #region whatsapp_outbound
    public class ContestRouter_WA_Outbound
    {
        [Key]
        public int WhatsappOutboundId { get; set; }
        public int? ContestId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string Response { get; set; }
        public string OutboundNumber { get; set; }
        public string OutboundMessageType { get; set; }
        public string OutboundMessageText { get; set; }
        public string OutboundMessageUrl { get; set; }
        public string OutboundMessageCaption { get; set; }
    }
    public class Whatsapp_Outbound_Smsdome
    {
        [JsonProperty("contest_id")]
        public int ContestId { get; set; } = 0;

        [JsonProperty("outbound_number")]
        public string OutboundNumber { get; set; }

        [JsonProperty("whatsapp_outbound_sinch")]
        public Whatsapp_Outbound_Sinch Whatsapp_outbound_sinch { get; set; }
    }
    public class Whatsapp_Outbound_Sinch
    {
        [JsonProperty("to")]
        public string[] To { get; set; }

        [JsonProperty("message")]
        public Outbound_Sinch_Text_Message Outbound_sinch_text_message { get; set; }
    }

    public class Outbound_Sinch_Text_Message
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class Outbound_Webapp
    {
        [JsonProperty("ContestId")]
        public int ContestId { get; set; } = 0;

        [JsonProperty("MobileNo")]
        public string MobileNo { get; set; }

        [JsonProperty("MessageText")]
        public string MessageText { get; set; }

        [JsonProperty("MessageType")]
        public string MessageType { get; set; }
    }
        #endregion
        public class BaseContextFactory : IDesignTimeDbContextFactory<BaseEntities>
    {
        public BaseEntities CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BaseEntities>();
            optionsBuilder.UseSqlServer(Environment.GetEnvironmentVariable("SqlConnectionString"));

            return new BaseEntities(optionsBuilder.Options);
        }

    }
}
