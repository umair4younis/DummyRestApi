using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Attributes;
using Puma.MDE.OPUS.Models;
using System;
using System.ComponentModel.DataAnnotations;


namespace Puma.MDE.Tests
{
    [TestClass]
    public class CoverageAttributeTests
    {
        private static ValidationContext Ctx(string name)
        {
            return new ValidationContext(new object()) { MemberName = name, DisplayName = name };
        }

        [TestMethod]
        public void BasicEmailAttribute_Covers_Valid_And_Invalid()
        {
            var a = new BasicEmailAttribute();
            Assert.IsNull(a.GetValidationResult("a@b.com", Ctx("email")));
            Assert.IsNotNull(a.GetValidationResult("bad", Ctx("email")));
            Assert.IsNotNull(a.GetValidationResult("", Ctx("email")));
        }

        [TestMethod]
        public void EmailLengthAndFormatAttribute_Covers_Valid_And_Invalid()
        {
            var a = new EmailLengthAndFormatAttribute(5, 50);
            Assert.IsNull(a.GetValidationResult("abc@xyz.com", Ctx("email")));
            Assert.IsNotNull(a.GetValidationResult("x", Ctx("email")));
            Assert.IsNotNull(a.GetValidationResult("bad-email", Ctx("email")));
        }

        [TestMethod]
        public void EmailDomainWhitelistAttribute_Covers_Valid_And_Invalid()
        {
            var a = new EmailDomainWhitelistAttribute("example.com");
            Assert.IsNull(a.GetValidationResult("u@example.com", Ctx("email")));
            Assert.IsNotNull(a.GetValidationResult("u@other.com", Ctx("email")));
            Assert.IsNotNull(a.GetValidationResult("bad", Ctx("email")));
        }

        [TestMethod]
        public void EmailDomainResolvableAttribute_Covers_Input_Paths()
        {
            var a = new EmailDomainResolvableAttribute();
            Assert.IsNull(a.GetValidationResult(null, Ctx("email")));
            Assert.IsNotNull(a.GetValidationResult("bad", Ctx("email")));
            Assert.IsNotNull(a.GetValidationResult("u@nonexistent.invalid", Ctx("email")));
        }

        [TestMethod]
        public void EnumValueAttribute_Covers_Valid_And_Invalid()
        {
            var a = new EnumValueAttribute(typeof(DayOfWeek));
            Assert.IsNull(a.GetValidationResult(DayOfWeek.Monday, Ctx("dow")));
            Assert.IsNotNull(a.GetValidationResult((DayOfWeek)100, Ctx("dow")));
            Assert.IsNull(a.GetValidationResult(null, Ctx("dow")));
            Assert.IsNotNull(a.GetValidationResult((DayOfWeek)101, new ValidationContext(new object())));
        }

        [TestMethod]
        public void Future_And_Past_Date_Attributes_Covered()
        {
            var future = new FutureDateAttribute();
            var past = new PastDateAttribute();
            var notPast = new NotInPastAttribute();

            Assert.IsNull(future.GetValidationResult(DateTime.Today.AddDays(1), Ctx("d")));
            Assert.IsNotNull(future.GetValidationResult(DateTime.Today.AddDays(-1), Ctx("d")));

            Assert.IsNull(past.GetValidationResult(DateTime.Today.AddDays(-1), Ctx("d")));
            Assert.IsNotNull(past.GetValidationResult(DateTime.Today, Ctx("d")));

            Assert.IsNull(notPast.GetValidationResult(DateTime.Today, Ctx("d")));
            Assert.IsNotNull(notPast.GetValidationResult(DateTime.Today.AddDays(-1), Ctx("d")));
        }

        [TestMethod]
        public void PositiveNumber_And_Uuid_And_StrictEmail_Covered()
        {
            var num = new PositiveNumberAttribute();
            var uuid = new ValidUUIDAttribute();
            var strict = new StrictEmailAttribute();

            Assert.IsNull(num.GetValidationResult(5, Ctx("n")));
            Assert.IsNull(num.GetValidationResult(5m, Ctx("n")));
            Assert.IsNull(num.GetValidationResult(5L, Ctx("n")));
            Assert.IsNull(num.GetValidationResult(5d, Ctx("n")));
            Assert.IsNull(num.GetValidationResult(null, Ctx("n")));
            Assert.IsNotNull(num.GetValidationResult(-1, Ctx("n")));
            Assert.IsNotNull(num.GetValidationResult(0, Ctx("n")));
            Assert.IsNotNull(num.GetValidationResult("bad", Ctx("n")));
            Assert.IsNotNull(num.GetValidationResult(-1m, new ValidationContext(new object())));

            Assert.IsNull(uuid.GetValidationResult(Guid.NewGuid().ToString(), Ctx("id")));
            Assert.IsNotNull(uuid.GetValidationResult("bad-guid", Ctx("id")));

            Assert.IsNull(strict.GetValidationResult("a@b.com", Ctx("email")));
            Assert.IsNotNull(strict.GetValidationResult("bad", Ctx("email")));
        }
    }
}
