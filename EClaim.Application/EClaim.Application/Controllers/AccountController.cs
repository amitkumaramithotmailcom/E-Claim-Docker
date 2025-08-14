using EClaim.Application.Enum;
using EClaim.Application.Models.ViewModel;
using EClaim.Application.Notification;
using EClaim.Application.Notification.EMAILService;
using EClaim.Application.Notification.SMSService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EClaim.Application.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly INotificationFactory _notificationFactory;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly IEmailBuilder _emailBuilder;
        private readonly ISmsBuilder _smsBuilder;

        public AccountController(IHttpClientFactory httpClientFactory, INotificationFactory notificationFactory, IConfiguration config, IEmailBuilder builder, ISmsBuilder smsBuilder)
        {
            _httpClientFactory = httpClientFactory;
            _notificationFactory = notificationFactory;
            _config = config;
            _httpClient = _httpClientFactory.CreateClient("api");
            _emailBuilder = builder;
            _smsBuilder = smsBuilder;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!model.Password.Equals(model.ConfirmPassword))
            {
                ModelState.AddModelError("", "Password not matched with confirm password");
                return View(model);
            }

            var response = await _httpClient.PostAsync("api/auth/register", new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Registration failed.");
                return View(model);
            }
            else
            {
                await SendNotification(model, response);
            }

            return RedirectToAction("Login");
        }

        private async Task SendNotification(RegisterViewModel model, HttpResponseMessage response)
        {
            var IsNotificationEnable = bool.Parse(_config["IsNotificationEnable"]);
            if (IsNotificationEnable)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<AuthResponseViewModel>(json);
                var emailSubject = "Confirm your email";
                var emailTemplet = $"Click <a href='{_config["AppBaseUrl"]}Account/ConfirmEmail?email={result.Email}&token=test'>here</a> to confirm your email.";

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

                    EmailMessage email = _emailBuilder
                                            .SetRecipient(IsEnable ? model.Email : _config["Smtp:From"])
                                            .SetSubject(emailSubject)
                                            .SetBody(emailTemplet, true)
                                            .Build();

                    await service.SendNotificationlAsync(email);
                }
                else
                {
                    var IsEnable = bool.Parse(_config["SMSConfig:IsEnable"]);

                    SMSMessage sms = _smsBuilder
                                             .SetRecipient(IsEnable ? model.Phone : _config["SMSConfig:FromPhone"])
                                             .SetSubject(emailSubject)
                                             .SetBody(emailTemplet, true)
                                             .Build();
                    

                    await service.SendNotificationlAsync(sms);
                }
            }
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var response = await _httpClient.PostAsync("api/auth/login", new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<AuthResponseViewModel>(json);

            // Parse JWT to Claims
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.Token);

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, result.FullName),
            new Claim(ClaimTypes.Email, result.Email),
            new Claim(ClaimTypes.Role, result.Role),
            new Claim("access_token", result.Token),
            new Claim("userId", result.UserId.ToString())
        };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string email, string token)
        {
            var response = await _httpClient.GetStringAsync($"api/auth/ConfirmEmail/{email}/{token}");
            bool result;
            bool isBoolean = bool.TryParse(response, out result);
            if (!result)
            {
               return BadRequest("Email confirmation failed.");
            }
            else
            {
                return RedirectToAction("ConfirmEmailSuccess");
            }
        }
        [HttpGet]
        public async Task<IActionResult> ConfirmEmailSuccess()
        {
            return View();
        }
    }
}
