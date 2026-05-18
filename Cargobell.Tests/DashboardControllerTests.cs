using DeliveryTrackingSystem.Controllers;
using Cargobell.Data;
using Cargobell.Shared.Models; 
using Cargobell.Shared.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Cargobell.Data.Data;

namespace Cargobell.Tests
{
    [TestFixture]
    public class DashboardControllerTests
    {
        [Test]
        public async Task Index_ReturnsViewWithPopulatedDashboardMetrics()
        {
            // 1. Arrange - Mock the three required constructor dependencies
            var mockDbContext = new Mock<ApplicationDbContext>();

            // Identity managers require specialized internal store configurations, so we mock them like this:
            var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockUserClaimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                mockUserManager.Object, mockHttpContextAccessor.Object, mockUserClaimsPrincipalFactory.Object, null, null, null, null);

            // 2. Instantiate controller with our mocked dependencies
            var controller = new DashboardController(mockDbContext.Object, mockUserManager.Object, mockSignInManager.Object);

            // 3. Setup user context to simulate an authenticated identity
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Name, "Operative-04")
            }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // 4. Act - Notice the 'await' keyword here! This extracts the IActionResult from the Task container.
            var result = await controller.Index() as ViewResult;

            // 5. Assert
            Assert.That(result, Is.Not.Null, "The action did not return a ViewResult.");

            // Note: If your Index method fetches data using the DbContext or UserManager, 
            // you may need to add .Setup() methods above to prevent an empty/null Model error here.
            var model = result.Model as DashboardViewModel;
            Assert.That(model, Is.Not.Null, "The ViewResult does not contain a DashboardViewModel.");
            Assert.That(model.UserName, Is.EqualTo("Operative-04"));
        }
    }
}