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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web.Http;

namespace Whatsapp
{
    public class Whatsapp_outbound
    {
        private readonly BaseEntities _context;
        private Helper _helper;
        public Whatsapp_outbound(BaseEntities context)
        {
            _context = context;
            _helper = new Helper();
        }

        [FunctionName("whatsapp_outbound")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "whatsapp/outbound")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                Outbound_Webapp Outbound_webapp = JsonConvert.DeserializeObject<Outbound_Webapp>(requestBody);

                if (Outbound_webapp == null)
                {
                    log.LogInformation("err : Whatsapp_outbound_smsdome not found");
                    return new BadRequestObjectResult("Whatsapp_outbound_smsdome not found");
                }

                if (Outbound_webapp.ContestId == 0)
                {
                    log.LogInformation("err : Contest id not found");
                    return new BadRequestObjectResult("Contest id not found");
                }
                
                int contestId = Outbound_webapp.ContestId;

                //get contest router
                ContestRouter_Contest contestRouter = _context.ContestRouter_Contests.
                    FirstOrDefault(x => x.IsActive &&
                                        x.ContestId==contestId);

                if (contestRouter == null)
                {
                    log.LogInformation("err : Contest not found");
                    return new BadRequestObjectResult("Contest not found");
                }

                string outboundNumber = contestRouter.ContestNumber ?? string.Empty;

                //validate campaign date
                //if(DateTime.UtcNow<contestRouter.StartDate || DateTime.UtcNow>contestRouter.EndDate)
                //    return new BadRequestObjectResult("contest not in campaign period");

                string KvUriTemplate = Environment.GetEnvironmentVariable("KvSecretURI") +
                Environment.GetEnvironmentVariable("KvPrefix") +
                outboundNumber;

                string endpointValue = await _helper.GetKeyVaultValue(
                    KvUriTemplate + Environment.GetEnvironmentVariable("KvEndpointSuffix"));
                string botIdValue = await _helper.GetKeyVaultValue(
                    KvUriTemplate + Environment.GetEnvironmentVariable("KvBotIdSuffix"));
                string bearerTokenValue = await _helper.GetKeyVaultValue(
                    KvUriTemplate + Environment.GetEnvironmentVariable("KvBearerTokenSuffix"));

                string ApiVersionURI = Environment.GetEnvironmentVariable("ApiVersionURI");

                var url = endpointValue + ApiVersionURI + botIdValue + "/messages";

                log.LogInformation("url : "+ url);

                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), url))
                    {
                        request.Headers.TryAddWithoutValidation("Accept", "application/json");

                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {bearerTokenValue}");

                        Whatsapp_Outbound_Sinch Whatsapp_outbound_sinch = new Whatsapp_Outbound_Sinch
                        {
                            To = new string[]{ Outbound_webapp?.MobileNo },
                            Outbound_sinch_text_message = new Outbound_Sinch_Text_Message
                            {
                                Type = Outbound_webapp?.MessageType,
                                Text = Outbound_webapp?.MessageText
                            }
                        };
                        string whatsappOutboundSinch = JsonConvert.SerializeObject(Whatsapp_outbound_sinch);
                        request.Content = new StringContent(whatsappOutboundSinch, Encoding.UTF8, "application/json");

                        var result = await httpClient.SendAsync(request);
                        string response = await result.Content.ReadAsStringAsync();

                        if (result.IsSuccessStatusCode)
                        {
                            //save to db
                            ContestRouter_WA_Outbound Whatsapp_outbound = new ContestRouter_WA_Outbound
                            {
                                ContestId = Outbound_webapp?.ContestId,
                                OutboundNumber = outboundNumber,
                                CreatedOn = DateTime.UtcNow,
                                Response = response,
                                OutboundMessageType = Outbound_webapp?.MessageType ?? string.Empty,
                                OutboundMessageText = Outbound_webapp?.MessageText ?? string.Empty,
                            };

                            _context.ContestRouter_WA_Outbounds.Add(Whatsapp_outbound);

                            if (_context.SaveChanges() > 0)
                                return new OkObjectResult(response);
                            else
                            {
                                log.LogInformation("err : failed to save Whatsapp_outbound");
                                return new BadRequestObjectResult("failed to save Whatsapp_outbound");
                            }
                        }
                        else
                        {
                            log.LogInformation("err : " + response);
                            return new BadRequestObjectResult(response);
                        }
                    }
                }

                //string responseMessage = string.IsNullOrEmpty(name)
                //    ? endpointValue +", " + botIdValue +", " + bearerTokenValue + ", "
                //    + JsonConvert.SerializeObject(whatsapp_outboundSmsdome)
                //    + " This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                //    : $"Hello, {name}. This HTTP triggered function executed successfully.";

                //var postsArray = _context.ContestRouter_Contests.Where(x => x.InboundNumber == inboundNumber);
                //return new OkObjectResult(postsArray);

                //return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult("Exception : " + ex.Message);
            }
        }


    }
}
