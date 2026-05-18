using Cargobell.Data;
using Cargobell.Data.Data;
using Cargobell.Shared.Models;
using Cargobell.Shared.Models;
using DeliveryTrackingSystem.Controllers;
using DeliveryTrackingSystem.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Cargobell.Tests
{
    [TestFixture]
    public class ShipmentsControllerTests
    {
        private ApplicationDbContext _context;
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private ShipmentsController _controller;
        private ITempDataDictionary _tempData;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "CargobellTestDb_" + System.Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            // 1. Create and seed the Status lookup record first
            var inTransitStatus = new Status
            {
                StatusId = 1,
                Name = "InTransit",
                Description = "Cargo asset is currently moving between terminal nodes."
            };
            _context.Set<Status>().Add(inTransitStatus);

            // 2. Seed the Shipment record and link it to the status using the foreign key ID
            _context.Shipments.Add(new Shipment
            {
                TrackingCode = "BELL-GOLD-777",
                StatusId = 1 // FIXED: Links directly to the StatusId we defined above
            });
            _context.SaveChanges();

            var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);

            _controller = new ShipmentsController(_context, _mockUserManager.Object);

            var tempDataProvider = new Mock<ITempDataProvider>();
            _tempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object);
            _controller.TempData = _tempData;
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
            _controller?.Dispose();
        }

        [Test]
        public async Task Details_InvalidOrMissingTrackingCode_RedirectsToHomeWithErrorMessage()
        {
            // Arrange
            string invalidTrackingCode = "BAD-MANIFEST-999";

            // Act
            var result = await _controller.Details(invalidTrackingCode) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("Index"));
            Assert.That(result.ControllerName, Is.EqualTo("Home"));
            Assert.That(_controller.TempData["Error"], Is.Not.Null);
            Assert.That(_controller.TempData["Error"].ToString(), Contains.Substring("not found"));
        }

        [Test]
        public async Task Details_ValidTrackingCodeFoundInSystem_ReturnsViewWithShipmentPayload()
        {
            // Arrange
            string validCode = "BELL-GOLD-777";

            // Act
            var result = await _controller.Details(validCode) as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as Shipment;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.TrackingCode, Is.EqualTo("BELL-GOLD-777"));

            // FIXED: Verify the foreign key matches our setup environment
            Assert.That(model.StatusId, Is.EqualTo(1));
        }
    }
}