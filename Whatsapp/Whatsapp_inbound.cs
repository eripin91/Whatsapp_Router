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
using System.Net.Http;

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
                //if (Whatsapp_inbound_sinch.Contacts == null)
                //{
                //    log.LogInformation("err : Whatsapp_inbound_sinch contact not found");
                //    return new BadRequestObjectResult("Whatsapp_inbound_sinch contact not found");
                //}
                if (Whatsapp_inbound_sinch.Notifications == null)
                {
                    log.LogInformation("err : Whatsapp_inbound_sinch notification not found");
                    return new BadRequestObjectResult("Whatsapp_inbound_sinch notification not found");
                }

                //if (Whatsapp_inbound_sinch.Contacts?.Length != Whatsapp_inbound_sinch.Notifications?.Length)
                //{
                //    log.LogInformation("err : Whatsapp_inbound_sinch contact and notification array length not same");
                //    return new BadRequestObjectResult("Whatsapp_inbound_sinch contact and notification array length not same");
                //}

                List<string> successMessageIdList = new List<string>();

                for (int i = 0; i < Whatsapp_inbound_sinch.Notifications?.Length; i++)
                {
                    Dictionary<string, string> botIdMap = new Dictionary<string, string>();
                    botIdMap.Add("0e2492a8-9a00-4a53-80a3-962e499a69e8", "6597673663");
                    string botId = Whatsapp_inbound_sinch.Notifications[i]?.To ?? string.Empty;
                    var WaAccount = _context.ContestRouter_WA_Accounts.FirstOrDefault(s => s.WhatsappID == botId);

                    string inboundNumber = WaAccount?.WhatsappNumber ?? string.Empty;
                    string entryText = string.Empty;

                    if(Whatsapp_inbound_sinch.Notifications[i]?.Inbound_sinch_message?.Type=="text")
                        entryText = Whatsapp_inbound_sinch.Notifications[i]?.Inbound_sinch_message?.Body ?? string.Empty;
                    else if (Whatsapp_inbound_sinch.Notifications[i]?.Inbound_sinch_message?.Type == "image")
                        entryText = Whatsapp_inbound_sinch.Notifications[i]?.Inbound_sinch_message?.Caption ?? string.Empty;

                    string keyword = entryText.IndexOf(" ") > -1
                                      ? entryText.Substring(0, entryText.IndexOf(" "))
                                      : entryText;

                    //get contest router
                    ContestRouter_Contest Contest_router = _context.ContestRouter_Contests.
                        FirstOrDefault(x => x.IsActive &&
                                            x.ContestNumber == inboundNumber &&
                                            x.Keyword == keyword);

                    if (Contest_router == null)
                    {
                        log.LogInformation("err : Contest not found");
                        return new BadRequestObjectResult("Contest not found");
                    }

                    //save to db
                    ContestRouter_WA_Inbound Whatsapp_inbound = new ContestRouter_WA_Inbound
                    {
                        ContestId = Contest_router?.ContestId,
                        CreatedOn = DateTime.UtcNow,
                        ContactName = Whatsapp_inbound_sinch?.Contacts?[i]?.Profile.Name ?? string.Empty,
                        ContactWaId = Whatsapp_inbound_sinch?.Contacts?[i]?.WaId ?? string.Empty,
                        NotificationFrom = Whatsapp_inbound_sinch?.Notifications?[i]?.From ?? string.Empty,
                        NotificationTo = inboundNumber ?? string.Empty,
                        NotificationMessageId = Whatsapp_inbound_sinch?.Notifications?[i]?.MessageId ?? string.Empty,
                        NotificationMessageType = Whatsapp_inbound_sinch?.Notifications?[i]?.Inbound_sinch_message?.Type ?? string.Empty,
                        NotificationMessageBody = Whatsapp_inbound_sinch?.Notifications?[i]?.Inbound_sinch_message?.Body ?? string.Empty,
                        NotificationMessageDetails = Whatsapp_inbound_sinch?.Notifications?[i]?.Inbound_sinch_message?.Details ?? string.Empty,
                        NotificationMessageUrl = Whatsapp_inbound_sinch?.Notifications?[i]?.Inbound_sinch_message?.Url ?? string.Empty,
                        NotificationMessageMimeType = Whatsapp_inbound_sinch?.Notifications?[i]?.Inbound_sinch_message?.MimeType ?? string.Empty,
                        NotificationMessageCaption = Whatsapp_inbound_sinch?.Notifications?[i]?.Inbound_sinch_message?.Caption ?? string.Empty,
                        NotificationTimestamp = Whatsapp_inbound_sinch?.Notifications?[i]?.Timestamp
                    };

                    _context.ContestRouter_WA_Inbounds.Add(Whatsapp_inbound);

                    //call inbound url
                    using (var httpClient = new HttpClient())
                    {
                        using (var request = new HttpRequestMessage(new HttpMethod("POST"), Contest_router?.ContestInboundUrl))
                        {
                            request.Headers.TryAddWithoutValidation("Accept", "application/json");

                            //request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {bearerTokenValue}");

                            Inbound_Webapp Inbound_webapp = new Inbound_Webapp
                            {
                                CreatedOn = Whatsapp_inbound_sinch?.Notifications?[i]?.Timestamp,
                                MobileNo = Whatsapp_inbound_sinch?.Notifications?[i]?.From ?? string.Empty,
                                Message = entryText,
                                FileLink = Whatsapp_inbound_sinch?.Notifications?[i]?.Inbound_sinch_message?.Url ?? string.Empty,
                                EntrySource = Contest_router?.ContestType
                            };

                            string inboundWebapp = JsonConvert.SerializeObject(Inbound_webapp);
                            request.Content = new StringContent(inboundWebapp, Encoding.UTF8, "application/json");

                            var result = await httpClient.SendAsync(request);
                            string response = await result.Content.ReadAsStringAsync();

                            if (result.IsSuccessStatusCode)
                            {
                                //implement logging here                                

                            }
                            else
                            {
                                log.LogInformation("err: failed to call webapp" + inboundWebapp);
                                successMessageIdList.Add("failed to call web app "+Whatsapp_inbound.NotificationMessageId);
                            }
                        }
                    }

                    if (_context.SaveChanges() < 1)
                        successMessageIdList.Add(Whatsapp_inbound.NotificationMessageId);
                }

                if (successMessageIdList.Count == 0)
                {
                    log.LogInformation("success save all inbound");
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
                log.LogInformation("Exception : " + ex.Message);
                return new BadRequestObjectResult("Exception : " + ex.Message);
            }
        }
    }
}
