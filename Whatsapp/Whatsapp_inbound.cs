using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Collections.Generic;

namespace Whatsapp
{
    public class Whatsapp_inbound
    {
        private readonly BaseEntities _context;
        public Whatsapp_inbound(BaseEntities context)
        {
            _context = context;
        }

        [FunctionName("whatsapp_inbound")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "whatsapp/inbound")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                Whatsapp_Inbound_Sinch Whatsapp_inbound_sinch = JsonConvert.DeserializeObject<Whatsapp_Inbound_Sinch>(requestBody);



                if (Whatsapp_inbound_sinch == null)
                {
                    log.LogInformation("err : Whatsapp_inbound_sinch not found");
                    return new BadRequestObjectResult("Whatsapp_inbound_sinch not found");
                }
                if (Whatsapp_inbound_sinch.Statuses != null)
                {
                    log.LogInformation("err : Whatsapp_inbound_sinch delivery report");
                    return new OkObjectResult("Whatsapp_inbound_sinch delivery report");
                }
                if (Whatsapp_inbound_sinch.Contacts == null)
                {
                    log.LogInformation("err : Whatsapp_inbound_sinch contact not found");
                    return new BadRequestObjectResult("Whatsapp_inbound_sinch contact not found");
                }
                if (Whatsapp_inbound_sinch.Notifications == null)
                {
                    log.LogInformation("err : Whatsapp_inbound_sinch notification not found");
                    return new BadRequestObjectResult("Whatsapp_inbound_sinch notification not found");
                }

                if (Whatsapp_inbound_sinch.Contacts?.Length != Whatsapp_inbound_sinch.Notifications?.Length)
                {
                    log.LogInformation("err : Whatsapp_inbound_sinch contact and notification array length not same");
                    return new BadRequestObjectResult("Whatsapp_inbound_sinch contact and notification array length not same");
                }

                List<string> successMessageIdList = new List<string>();

                for (int i = 0; i < Whatsapp_inbound_sinch.Contacts?.Length; i++)
                {
                    Dictionary<string, string> botIdMap = new Dictionary<string, string>();
                    botIdMap.Add("0e2492a8-9a00-4a53-80a3-962e499a69e8", "6597673663");

                    string inboundNumber = botIdMap[Whatsapp_inbound_sinch.Notifications[i]?.To] ?? string.Empty;

                    string entryText = Whatsapp_inbound_sinch.Notifications[i]?.Inbound_sinch_message?.Body ?? string.Empty;
                    string keyword = entryText.IndexOf(" ") > -1
                                      ? entryText.Substring(0, entryText.IndexOf(" "))
                                      : entryText;

                    //get contest router
                    ContestRouter_Contest contestRouter = _context.ContestRouter_Contests.
                        FirstOrDefault(x => x.IsActive &&
                                            x.InboundNumber == inboundNumber &&
                                            x.Keyword == keyword);

                    if (contestRouter == null)
                    {
                        log.LogInformation("err : Contest not found");
                        return new BadRequestObjectResult("Contest not found");
                    }

                    //save to db
                    Whatsapp_Inbound Whatsapp_inbound = new Whatsapp_Inbound
                    {
                        ContestId = contestRouter?.ContestId,
                        CreatedOn = DateTime.UtcNow,
                        ContactName = Whatsapp_inbound_sinch?.Contacts[i]?.Profile.Name ?? string.Empty,
                        ContactWaId = Whatsapp_inbound_sinch?.Contacts[i]?.WaId ?? string.Empty,
                        NotificationFrom = Whatsapp_inbound_sinch?.Notifications[i]?.From ?? string.Empty,
                        NotificationTo = Whatsapp_inbound_sinch?.Notifications[i]?.To ?? string.Empty,
                        NotificatinMessageId = Whatsapp_inbound_sinch?.Notifications[i]?.MessageId ?? string.Empty,
                        NotificationMessageType = Whatsapp_inbound_sinch?.Notifications[i]?.Inbound_sinch_message?.Type ?? string.Empty,
                        NotificationMessageBody = Whatsapp_inbound_sinch?.Notifications[i]?.Inbound_sinch_message?.Body ?? string.Empty,
                        NotificationMessageDetails = Whatsapp_inbound_sinch?.Notifications[i]?.Inbound_sinch_message?.Details ?? string.Empty,
                        NotificationMessageUrl = Whatsapp_inbound_sinch?.Notifications[i]?.Inbound_sinch_message?.Url ?? string.Empty,
                        NotificatinMessageMimeType = Whatsapp_inbound_sinch?.Notifications[i]?.Inbound_sinch_message?.MimeType ?? string.Empty,
                        NotificatinMessageCaption = Whatsapp_inbound_sinch?.Notifications[i]?.Inbound_sinch_message?.Caption ?? string.Empty
                    };

                    _context.Whatsapp_Inbounds.Add(Whatsapp_inbound);

                    if (_context.SaveChanges() < 1)
                        successMessageIdList.Add(Whatsapp_inbound.NotificatinMessageId);
                }

                if (successMessageIdList.Count == 0)
                {
                    log.LogInformation("success save all inbound");
                    //call inbound url
                    return new OkObjectResult("success to save Whatsapp_inbound");
                }
                else
                {
                    log.LogInformation("failed to save Whatsapp_inbound " + string.Join(",", successMessageIdList));
                    return new BadRequestObjectResult("failed to save Whatsapp_inbound " + string.Join(",", successMessageIdList));
                }

                //return new OkObjectResult(responseMessage);
            }
            catch (Exception ex)
            {
                log.LogInformation("exception : " + ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
