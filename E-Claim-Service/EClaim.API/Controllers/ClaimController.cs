using EClaim.Domain.DTOs;
using EClaim.Domain.Entities;
using EClaim.Domain.Enums;
using EClaim.Domain.Interfaces;
using EClaim.Infrastructure;
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
        private readonly ICacheUtility _cacheUtility;

        public ClaimController(IClaimService claimService, ILogger<AuthController> logger, IConnectionMultiplexer redis, ICacheUtility cacheUtility)
        {
            _claimService = claimService;
            _logger = logger;
            _cache = redis.GetDatabase();
            _cacheUtility = cacheUtility;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            _logger.LogInformation($"User claim request search for {id}");

            ClaimRequest claimRequest;

            var jsonData = await _cacheUtility.GetDataFromCachAsync($"Claim:{id}");
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                claimRequest = await _claimService.GetClaimSubmission(id);

                await _cacheUtility.SetDataInCachAsync($"Claim:{id}", claimRequest);
            }
            else
            {
                claimRequest = JsonSerializer.Deserialize<ClaimRequest>(jsonData);
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

            var jsonData = await _cacheUtility.GetDataFromCachAsync($"Claims:{claimKey}");
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                ClaimRequestList = await _claimService.GetClaimDetails(claimSearchDto);

                await _cacheUtility.SetDataInCachAsync($"Claims:{claimKey}", ClaimRequestList);
            }
            else
            {
                ClaimRequestList = JsonSerializer.Deserialize<IEnumerable<ClaimRequest>>(jsonData);
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
