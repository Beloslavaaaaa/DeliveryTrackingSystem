using NUnit.Framework;
using Cargobell.Shared.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

namespace Cargobell.Tests
{
    [TestFixture]
    public class AccountViewModelTests
    {
        [Test]
        public void LoginViewModel_MissingEmail_FailsValidation()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "", // Intentionally left blank to trigger failure
                Password = "SecurePassword123!"
            };

            // Act
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(model, context, results, true);

            // Assert
            Assert.That(isValid, Is.False, "Validation should fail when Email is missing.");
            Assert.That(results.Any(r => r.MemberNames.Contains("Email")), Is.True);
        }
    }
}