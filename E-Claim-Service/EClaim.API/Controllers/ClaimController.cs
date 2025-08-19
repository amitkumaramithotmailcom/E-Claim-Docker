using EClaim.Domain.DTOs;
using EClaim.Domain.Entities;
using EClaim.Domain.Enums;
using EClaim.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace E_Claim_Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimController : ControllerBase
    {
        private readonly IClaimService _claimService;
        private readonly ILogger<AuthController> _logger;
        private readonly IDatabase _cache;

        public ClaimController(IClaimService claimService, ILogger<AuthController> logger, IConnectionMultiplexer redis)
        {
            _claimService = claimService;
            _logger = logger;
            _cache = redis.GetDatabase(); 
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            _logger.LogInformation($"User claim request search for {id}");

            ClaimRequest claimRequest;
            var jsonClaimData = await _cache.StringGetAsync($"Claim:{id}");
            if (jsonClaimData.IsNullOrEmpty)
            {
                claimRequest = await _claimService.GetClaimSubmission(id);
                if (claimRequest != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                        WriteIndented = true
                    };

                    var jsonData = JsonSerializer.Serialize(claimRequest, options);
                    await _cache.StringSetAsync($"Claim:{id}", jsonData, TimeSpan.FromMinutes(2));
                }
            }
            else
            {
                claimRequest = JsonSerializer.Deserialize<ClaimRequest>(jsonClaimData);
            }
            
            _logger.LogInformation("User claim request search response", claimRequest);
            return Ok(claimRequest);
        }

        [HttpGet("GetClaimDetails")]
        public async Task<IActionResult> Get([FromQuery] ClaimSearchDto claimSearchDto)
        {
            _logger.LogInformation($"User claim request search", claimSearchDto);

            IEnumerable<ClaimRequest> ClaimRequestList;
            StringBuilder claimKey = new StringBuilder();
            if (claimSearchDto.UserId > 0)
                claimKey.Append(claimSearchDto.UserId.ToString());
            
            if (Enum.IsDefined(typeof(Status), claimSearchDto.Status))
                claimKey.Append(claimSearchDto.Status.ToString());
            
            if (!string.IsNullOrEmpty(claimSearchDto.ClaimType))
                claimKey.Append(claimSearchDto.ClaimType);

            if (claimSearchDto.FromDate.HasValue && claimSearchDto.ToDate.HasValue)
            {
                claimKey.Append(claimSearchDto.FromDate);
                claimKey.Append(claimSearchDto.ToDate);
            }



            var jsonClaimData = await _cache.StringGetAsync($"Claims:{claimKey}");
            if (jsonClaimData.IsNullOrEmpty)
            {
                ClaimRequestList = await _claimService.GetClaimDetails(claimSearchDto);
                if (ClaimRequestList != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                        WriteIndented = true
                    };

                    var jsonData = JsonSerializer.Serialize(ClaimRequestList, options);
                    await _cache.StringSetAsync($"Claims:{claimKey}", jsonData, TimeSpan.FromMinutes(2));
                }
            }
            else
            {
                ClaimRequestList = JsonSerializer.Deserialize<IEnumerable<ClaimRequest>>(jsonClaimData);
            }

            _logger.LogInformation("User claim request search response", ClaimRequestList);
            return Ok(ClaimRequestList);
        }

        [HttpPost]
        public async Task<IActionResult> Post(ClaimSubmissionDto claimSubmissionDto)
        {
            _logger.LogInformation("User claim request save request", claimSubmissionDto);
            var result = await _claimService.ClaimSubmission(claimSubmissionDto);
            _logger.LogInformation("User claim request save response", result);
            return Ok(result);
        }

        [HttpPatch("UpdateStatus")]
        public async Task<IActionResult> UpdateStatus(ClaimStatusUpdateDto claimStatusUpdateDto)
        {
            _logger.LogInformation("User claim request update request", claimStatusUpdateDto);
            var result = await _claimService.UpdateStatus(claimStatusUpdateDto);
            _logger.LogInformation("User claim request update request", result);
            return Ok(result);
        }
    }
}
