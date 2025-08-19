using EClaim.Domain.DTOs;
using EClaim.Domain.Entities;
using EClaim.Domain.Enums;
using EClaim.Domain.Interfaces;
using EClaim.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

namespace E_Claim_Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;
        private readonly IDatabase _cache;

        public UsersController(IUserService userService, ILogger<AuthController> logger, IConnectionMultiplexer redis)
        {
            _userService = userService;
            _logger = logger;
            _cache = redis.GetDatabase();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            _logger.LogInformation($"User search for {id}");

            User user;
            var jsonClaimData = await _cache.StringGetAsync($"user:{id}");
            if (jsonClaimData.IsNullOrEmpty)
            {
                user = await _userService.GetUser(id);
                if (user != null)
                {
                    var jsonData = JsonSerializer.Serialize(user);
                    await _cache.StringSetAsync($"user:{id}", jsonData, TimeSpan.FromMinutes(2));
                }
            }
            else
            {
                user = JsonSerializer.Deserialize<User>(jsonClaimData);
            }

            _logger.LogInformation("User search response", user);
            return Ok(user);
        }

        [HttpGet("GetUsers")]
        public async Task<IActionResult> Get([FromQuery] UserSearchDto userSearchDto)
        {
            _logger.LogInformation($"User search request", userSearchDto);

            IEnumerable<User> users;
            StringBuilder claimKey = new StringBuilder();

            if (userSearchDto.Id > 0)
                claimKey.Append(userSearchDto.Id.ToString());

            if (userSearchDto.UserId > 0)
                claimKey.Append(userSearchDto.UserId.ToString());

            if (!string.IsNullOrEmpty(userSearchDto.FullName))
                claimKey.Append(userSearchDto.FullName.ToString());

            if (!string.IsNullOrEmpty(userSearchDto.Email))
                claimKey.Append(userSearchDto.Email.ToString());

            if (!string.IsNullOrEmpty(userSearchDto.Phone))
                claimKey.Append(userSearchDto.Phone.ToString());

            if (userSearchDto.Role != null && Enum.IsDefined(typeof(EClaim.Domain.Enums.Role), userSearchDto.Role))
                 claimKey.Append(userSearchDto.Role.ToString());

            if (userSearchDto.IsEmailVerified != null)
                claimKey.Append(userSearchDto.IsEmailVerified.ToString());


            var jsonClaimData = await _cache.StringGetAsync($"users:{claimKey}");
            if (jsonClaimData.IsNullOrEmpty)
            {
                users = await _userService.GetAllUser(userSearchDto);
                if (users != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                        WriteIndented = true
                    };

                    var jsonData = JsonSerializer.Serialize(users, options);
                    await _cache.StringSetAsync($"users:{claimKey}", jsonData, TimeSpan.FromMinutes(2));
                }
            }
            else
            {
                users = JsonSerializer.Deserialize<IEnumerable<User>>(jsonClaimData);
            }



            _logger.LogInformation($"User search response", users);
            return Ok(users);
        }

        [HttpPut]
        public async Task<IActionResult> Put(UserSearchDto userSearchDto)
        {
            _logger.LogInformation($"User update request", userSearchDto);
            var result = await _userService.EditAsync(userSearchDto);
            _logger.LogInformation($"User update response", result);
            return Ok(result);
        }
    }
}
