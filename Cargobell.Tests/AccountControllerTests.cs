using DeliveryTrackingSystem.Controllers;
using Cargobell.Shared.Models;
using Cargobell.Shared.Models;
using Cargobell.Shared.ViewModels;
using DeliveryTrackingSystem.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cargobell.Tests
{
    [TestFixture]
    public class AccountControllerTests
    {
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private AccountController _controller;

        [SetUp]
        public void Setup()
        {
            var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockUserClaimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object, mockHttpContextAccessor.Object, mockUserClaimsPrincipalFactory.Object, null, null, null, null);

            _controller = new AccountController(_mockUserManager.Object, _mockSignInManager.Object);
        }

        // FIX: Added explicit teardown method to cleanly wipe out and dispose of the controller instance
        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }

        [Test]
        public async Task Login_Post_InvalidModelState_ReturnsViewWithSameModel()
        {
            // Arrange
            var model = new LoginViewModel { Email = "incomplete@" };
            _controller.ModelState.AddModelError("Email", "Invalid email formatting.");

            // Act
            var result = await _controller.Login(model) as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Model, Is.EqualTo(model));
        }

        [Test]
        public async Task Login_Post_ValidModelButFailedCredentials_ReturnsViewWithError()
        {
            // Arrange
            var model = new LoginViewModel { Email = "operative@cargobell.com", Password = "WrongPassword" };

            _mockSignInManager.Setup(x => x.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _controller.Login(model) as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.ModelState.IsValid, Is.False);
        }
    }
}