using EClaim.Domain.DTOs;
using EClaim.Domain.Entities;
using EClaim.Domain.Enums;
using EClaim.Domain.Interfaces;
using EClaim.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EClaim.Tests
{
    public class ClaimServiceTests
    {
        private readonly AppDbContext _context;
        private readonly ClaimService _service;

        public ClaimServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)) 
                .Options;

            _context = new AppDbContext(options);
            var configMock = new Mock<IConfiguration>();
            _service = new ClaimService(_context, configMock.Object);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var user = new User
            {
                Id = 1,
                FullName = "Test User",
                Email = "Test@gmail.com",
                Phone = "234567895",
                Address = "Test Address",
                IsEmailVerified = true,
                PasswordHash = "",
                Role = Role.Claimant
            };
            var claim = new ClaimRequest
            {
                Id = 1,
                UserId = 1,
                Description = "Test Claim",
                ClaimType = "Health",
                Status = Status.Submitted,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.Claims.Add(claim);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetClaimSubmission_ShouldReturnClaim_WhenClaimExists()
        {
            var result = await _service.GetClaimSubmission(1);

            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Description.Should().Be("Test Claim");
        }

        [Fact]
        public async Task GetClaimDetails_ShouldReturnClaims_WithMatchingCriteria()
        {
            var result = await _service.GetClaimDetails(new ClaimSearchDto
            {
                Status = Status.Submitted,
                ClaimType = "Health",
                FromDate = DateTime.UtcNow.AddDays(-1),
                ToDate = DateTime.UtcNow.AddDays(1),
                UserId = 1
            });

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task ClaimSubmission_ShouldCreateNewClaim_WithDocumentsAndWorkflow()
        {
            var dto = new ClaimSubmissionDto
            {
                UserId = 1,
                ClaimType = "Travel",
                Description = "Travel Claim",
                Documents = new List<ClaimDocumentDto> {
                new ClaimDocumentDto { 
                        FileName = "bill.jpg", FilePath = "/files/bill.jpg" 
                    }
                }
            };

            var result = await _service.ClaimSubmission(dto);

            result.Should().NotBeNull();
            result.ClaimType.Should().Be("Travel");

            var savedClaim = await _context.Claims.Include(c => c.Documents).FirstOrDefaultAsync(c => c.Id == result.Id);
            savedClaim.Documents.Should().HaveCount(1);

            var workflow = await _context.ClaimWorkflowSteps.FirstOrDefaultAsync(w => w.ClaimRequestId == result.Id);
            workflow.Should().NotBeNull();
            workflow.Action.Should().Be(Status.Submitted);
        }

        [Fact]
        public async Task UpdateStatus_ShouldUpdateClaimStatus_AndAddWorkflowStep()
        {
            var updateDto = new ClaimStatusUpdateDto
            {
                Id = 1,
                Action = Status.Approved,
                Comments = "Approved by Adjuster",
                UserId = 1
            };

            var updatedClaim = await _service.UpdateStatus(updateDto);

            updatedClaim.Status.Should().Be(Status.Approved);

            var workflow = await _context.ClaimWorkflowSteps.FirstOrDefaultAsync(w => w.ClaimRequestId == updatedClaim.Id && w.Action == Status.Approved);
            workflow.Should().NotBeNull();
            workflow.Comments.Should().Be("Approved by Adjuster");
        }
    }

}
