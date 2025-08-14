using ClosedXML.Excel;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using EClaim.Application.Enum;
using EClaim.Application.Models;
using EClaim.Application.Models.Response;
using EClaim.Application.Models.ViewModel;
using EClaim.Application.Notification;
using EClaim.Application.Notification.EMAILService;
using EClaim.Application.Notification.SMSService;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Newtonsoft.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace EClaim.Application.Controllers
{
    [Authorize]
    public class ClaimController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _httpClient;
        private readonly INotificationFactory _notificationFactory;
        private readonly IConfiguration _config;
        private readonly string _uploadFilePath = "uploads";
        private readonly IViewRenderService _viewRenderService;
        private readonly IEmailBuilder _emailBuilder;
        private readonly ISmsBuilder _smsBuilder;

        public ClaimController(IHttpClientFactory httpClientFactory, IWebHostEnvironment env, INotificationFactory notificationFactory, IConfiguration config, IViewRenderService viewRenderService, IEmailBuilder builder, ISmsBuilder smsBuilder)
        {
            _httpClientFactory = httpClientFactory;
            _env = env;
            _httpClient = _httpClientFactory.CreateClient("api");
            _notificationFactory = notificationFactory;
            _config = config;
            _viewRenderService = viewRenderService;
            _emailBuilder = builder;
            _smsBuilder = smsBuilder;
        }

        [HttpGet]
        [Authorize(Roles = "Claimant")]
        public async Task<IActionResult> Index()
        {
            ClaimSubmissionViewModel claimSubmissionViewModel = new ClaimSubmissionViewModel();
            return View(claimSubmissionViewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Claimant")]
        public IActionResult Submit() => View();

        [HttpPost]
        [Authorize(Roles = "Claimant")]
        public async Task<IActionResult> Submit(ClaimSubmissionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(User.FindFirst("userId").Value);

            var claim = new ClaimSubmissionModel
            {
                ClaimType = model.ClaimType,
                Description = model.Description,
                UserId = userId
            };

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            if (model.Documents != null)
            {
                foreach (var file in model.Documents)
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("Documents", "Only image files (.jpg, .jpeg, .png, .gif) are allowed.");
                        return View(); 
                    }

                    var path = Path.Combine(_env.WebRootPath, _uploadFilePath, file.FileName);
                    using var stream = System.IO.File.Create(path);
                    await file.CopyToAsync(stream);

                    claim.Documents.Add(new ClaimDocumentModel
                    {
                        FileName = file.FileName,
                        FilePath = $"/{_uploadFilePath}/{file.FileName}"
                    });
                }

            }

            var content = new StringContent(JsonConvert.SerializeObject(claim), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/Claim", content);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Claim submition failed.");
                return View(model);
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            ClaimSubmissionModel result = JsonConvert.DeserializeObject<ClaimSubmissionModel>(responseBody);
            await SendNotification(result.Id);
            return RedirectToAction($"Details", new { id = result.Id });
        }



        [HttpGet]
        //[Authorize(Roles = "Claimant")]
        public async Task<IActionResult> Details(int id)
        {
            var response = await _httpClient.GetStringAsync($"api/Claim?id={id}");
            var claimRequestResponse = JsonConvert.DeserializeObject<ClaimRequestResponse>(response);
            

            return View(claimRequestResponse);
        }

        private async Task SendNotification(int claimId)
        {
            var IsNotificationEnable = bool.Parse(_config["IsNotificationEnable"]);
            if (IsNotificationEnable)
            {
                var response = await _httpClient.GetStringAsync($"api/Claim?id={claimId}");
                var claimRequestResponse = JsonConvert.DeserializeObject<ClaimRequestResponse>(response);

                var type = Convert.ToString(_config["NotificationType"]);
                NotificationType notificationType = type.ToUpper() switch
                {
                    "EMAIL" => NotificationType.EMAIL,
                    "SMS" => NotificationType.SMS,
                    _ => throw new ArgumentException("Invalid notification type")
                };

                var service = _notificationFactory.GetNotificationService(notificationType);
                if (notificationType == NotificationType.EMAIL)
                {
                    var IsEnable = bool.Parse(_config["Smtp:IsEnable"]); 
                    var emailSubject = "Claim Request Confirm Mail";
                    string htmlContent = await _viewRenderService.RenderToStringAsync("Views/Claim/Details.cshtml", claimRequestResponse);

                    EmailMessage email = _emailBuilder
                                            .SetRecipient(IsEnable ? claimRequestResponse.User.Email : _config["Smtp:From"])
                                            .SetSubject(emailSubject)
                                            .SetBody(htmlContent, true)
                                            .Build();

                    await service.SendNotificationlAsync(email);
                }
                else
                {

                    var userEmail = User.FindFirst("emailaddress").Value;
                    

                    var IsEnable = bool.Parse(_config["Smtp:IsEnable"]);
                    //await service.SendNotificationlAsync(
                    //   IsEnable ? userEmail : _config["Smtp:From"],
                    // emailSubject, htmlContent);

                    SMSMessage sms = _smsBuilder
                                            .SetRecipient(IsEnable ? claimRequestResponse.User.Phone : _config["SMSConfig:FromPhone"])
                                            .SetSubject("")
                                            .SetBody("Your claim submited/status updated", true)
                                            .Build();
                    await service.SendNotificationlAsync(sms);
                }
            }
        }

        [HttpGet]
        //[Authorize(Roles = "Adjuster,Approver,Admin")]
        public async Task<IActionResult> ClaimDetails(DateTime fromDate, DateTime toDate)
        {
            var userId = int.Parse(User.FindFirst("userId").Value);
            var response = await _httpClient.GetStringAsync($"api/Claim/GetClaimDetails/{fromDate.ToString("dd-MMM-yyyy")}/{toDate.ToString("dd-MMM-yyyy")}/{userId}");

            var claimRequestResponse = JsonConvert.DeserializeObject<List<ClaimRequestResponse>>(response);
            return View(claimRequestResponse);
        }

        [HttpPost]
        public async Task<IActionResult> TakeAction(ClaimStatusUpdateViewModel model)
        {
            model.UserId = int.Parse(User.FindFirst("userId").Value);

            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PatchAsync($"api/Claim/UpdateStatus", content);

            if (response.IsSuccessStatusCode)
            {
                await SendNotification(model.Id);
                return RedirectToAction("Search");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to update status: {response.StatusCode} - {error}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(ClaimSearchViewModel model)
        {
            if (model.FromDate != null && model.ToDate != null && model.FromDate > model.ToDate)
            {
                ModelState.AddModelError("", "FromDate cannot be greater than ToDate");
                return View(model);
            }
            List<ClaimRequestResponse>? claimRequestResponse = await SearchClaims(model);
            model.Results = claimRequestResponse;
            return View(model);
        }

        private async Task<List<ClaimRequestResponse>?> SearchClaims(ClaimSearchViewModel model)
        {
            var userId = int.Parse(User.FindFirst("userId").Value);
            model.UserId = userId;

            var queryParams = new Dictionary<string, string?>
                {
                    { "UserId", model.UserId.ToString() },
                    { "Status", model.Status },
                    { "ClaimType", model.ClaimType },
                    { "FromDate", model.FromDate?.ToString("yyyy-MMM-dd") },
                    { "ToDate", model.ToDate?.ToString("yyyy-MMM-dd") },
                };

            var query = string.Join("&", queryParams
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));


            var response = await _httpClient.GetStringAsync($"api/Claim/GetClaimDetails?{query}");

            return JsonConvert.DeserializeObject<List<ClaimRequestResponse>>(response);
        }

        public async Task<IActionResult> ExportToExcel(ClaimSearchViewModel model)
        {
            List<ClaimRequestResponse>? claimRequestResponse = await SearchClaims(model);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("ClaimDetails");

            // Add headers
            worksheet.Cell(1, 1).Value = "Claim ID";
            worksheet.Cell(1, 2).Value = "Full Name";
            worksheet.Cell(1, 3).Value = "Email";
            worksheet.Cell(1, 4).Value = "Claim Type";
            worksheet.Cell(1, 5).Value = "Description";
            worksheet.Cell(1, 6).Value = "Status";
            worksheet.Cell(1, 7).Value = "Request Raised On";
            worksheet.Cell(1, 8).Value = "Comments";
            worksheet.Cell(1, 9).Value = "Action At";
            worksheet.Cell(1, 10).Value = "Action By";
            // Add rows
            for (int i = 0; i < claimRequestResponse.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = claimRequestResponse[i].Id;
                worksheet.Cell(i + 2, 2).Value = claimRequestResponse[i].User.FullName;
                worksheet.Cell(i + 2, 3).Value = claimRequestResponse[i].User.Email;
                worksheet.Cell(i + 2, 4).Value = claimRequestResponse[i].ClaimType;
                worksheet.Cell(i + 2, 5).Value = claimRequestResponse[i].Description;
                worksheet.Cell(i + 2, 6).Value = claimRequestResponse[i].Status.ToString();
                worksheet.Cell(i + 2, 7).Value = claimRequestResponse[i].WorkflowSteps.FirstOrDefault().CreatedAt;
                worksheet.Cell(i + 2, 8).Value = claimRequestResponse[i].WorkflowSteps.LastOrDefault().Comments;
                worksheet.Cell(i + 2, 9).Value = claimRequestResponse[i].WorkflowSteps.LastOrDefault().CreatedAt;
                worksheet.Cell(i + 2, 10).Value = claimRequestResponse[i].WorkflowSteps.LastOrDefault().User.FullName;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ClaimDetails.xlsx");
        }
    }
}
