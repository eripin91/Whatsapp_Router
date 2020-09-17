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
                Whatsapp_Outbound_Smsdome Whatsapp_outbound_smsdome = JsonConvert.DeserializeObject<Whatsapp_Outbound_Smsdome>(requestBody);

                if (Whatsapp_outbound_smsdome == null)
                    return new BadRequestObjectResult("Whatsapp_outbound_smsdome not found");

                if (Whatsapp_outbound_smsdome.ContestId == 0)
                    return new BadRequestObjectResult("Contest id not found");

                if (Whatsapp_outbound_smsdome.OutboundNumber == null)
                    return new BadRequestObjectResult("Outbound number not found");

                if (Whatsapp_outbound_smsdome.Whatsapp_outbound_sinch == null)
                    return new BadRequestObjectResult("Whatsapp_outbound_sinch not found");

                string outboundNumber = Whatsapp_outbound_smsdome.OutboundNumber ?? string.Empty;
                int contestId = Whatsapp_outbound_smsdome.ContestId;

                //get contest router
                ContestRouter_Contest contestRouter = _context.ContestRouter_Contests.
                    FirstOrDefault(x => x.IsActive &&
                                        x.OutboundNumber == outboundNumber &&
                                        x.ContestId==contestId);

                if (contestRouter == null)
                    return new BadRequestObjectResult("contest not found");

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
                string response = string.Empty;

                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), url))
                    {
                        request.Headers.TryAddWithoutValidation("Accept", "application/json");

                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {bearerTokenValue}");

                        string whatsappOutboundSinch = JsonConvert.SerializeObject(Whatsapp_outbound_smsdome.Whatsapp_outbound_sinch);
                        request.Content = new StringContent(whatsappOutboundSinch, Encoding.UTF8, "application/json");

                        var result = await httpClient.SendAsync(request);
                        response = await result.Content.ReadAsStringAsync();

                        if (result.IsSuccessStatusCode)
                        {
                            //save to db
                            Whatsapp_Outbound Whatsapp_outbound = new Whatsapp_Outbound
                            {
                                ContestId = Whatsapp_outbound_smsdome?.ContestId,
                                OutboundNumber = Whatsapp_outbound_smsdome?.OutboundNumber,
                                CreatedOn = DateTime.UtcNow,
                                OutboundMessageType = Whatsapp_outbound_smsdome?.Whatsapp_outbound_sinch?.Outbound_sinch_text_message?.Type ?? string.Empty,
                                OutboundMessageText = Whatsapp_outbound_smsdome?.Whatsapp_outbound_sinch?.Outbound_sinch_text_message?.Text ?? string.Empty                                
                            };

                            _context.Whatsapp_Outbounds.Add(Whatsapp_outbound);
                            
                            if(_context.SaveChanges()>0)
                                return new OkObjectResult(response);
                            else return new BadRequestObjectResult("failed to save Whatsapp_outbound");
                        }
                        else
                        {
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
                return new BadRequestObjectResult(ex.Message);
            }
        }


    }
}
