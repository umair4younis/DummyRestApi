using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Exceptions;
using System;
using System.Net;
using System.Runtime.Serialization;


namespace Puma.MDE.Tests
{
    [TestClass]
    public class CoverageExceptionTests
    {
        [TestMethod]
        public void ApiException_Constructors_Are_Covered()
        {
            var auth1 = new ApiAuthorizationException("auth");
            var auth2 = new ApiAuthorizationException("auth", "body");
            var auth3 = new ApiAuthorizationException("auth", "body", new Exception("x"));
            auth3.RequiredScope = "scope";
            var auth4 = new ApiAuthorizationException("auth", new Exception("x"));

            Assert.AreEqual("auth", auth1.Message);
            Assert.AreEqual("scope", auth3.RequiredScope);
            Assert.IsNotNull(auth2.ResponseBody);
            Assert.IsNotNull(auth4.InnerException);

            var nf = new ApiNotFoundException("missing", "id-1", "body");
            Assert.IsTrue(nf.Message.Contains("id-1"));

            var val = new ApiValidationException("bad", "resp");
            var rate = new ApiRateLimitException("rate", "resp");
            var req1 = new ApiRequestException("req");
            var req2 = new ApiRequestException("req", HttpStatusCode.BadRequest, "resp");
            var req3 = new ApiRequestException("req", HttpStatusCode.BadRequest, "resp", new Exception("x"));
            var req4 = new ApiRequestException("req", HttpStatusCode.BadRequest, new Exception("x"));
            var resp0 = new ApiResponseException("resp-only");
            var resp = new ApiResponseException("resp", "raw");
            var resp2 = new ApiResponseException("resp", "raw", new Exception("x"));
            var resp3 = new ApiResponseException("resp", "body", "raw-full", new Exception("x"));
            var server = new ApiServerException("server");
            var server2 = new ApiServerException("server", HttpStatusCode.InternalServerError);
            var server3 = new ApiServerException("server", "raw");
            var server4 = new ApiServerException("server", HttpStatusCode.BadGateway, "raw");
            var server5 = new ApiServerException("server", new Exception("x"));
            var server6 = new ApiServerException("server", HttpStatusCode.ServiceUnavailable, new Exception("x"));
            var server7 = new ApiServerException("server", "raw", new Exception("x"));
            var server8 = new ApiServerException("server", HttpStatusCode.InternalServerError, "raw", new Exception("x"));
            var serErr = new ApiServerErrorException("serr", HttpStatusCode.InternalServerError, "raw");
            var cb = new CircuitBreakerOpenException("open");

            Assert.AreEqual("bad", val.Message);
            Assert.AreEqual("resp", rate.ResponseBody);
            Assert.AreEqual(HttpStatusCode.BadRequest, req2.StatusCode);
            Assert.AreEqual("resp-only", resp0.Message);
            Assert.AreEqual("raw", resp.ResponseBody);
            Assert.AreEqual("raw", resp2.RawContent);
            Assert.AreEqual("raw-full", resp3.RawContent);
            Assert.AreEqual(HttpStatusCode.InternalServerError, server2.StatusCode);
            Assert.IsNotNull(serErr.ResponseBody);
            Assert.AreEqual("open", cb.Message);

            AssertCompat.Throws<ArgumentOutOfRangeException>(() =>
                new ApiServerException("bad", HttpStatusCode.OK));

            _ = req1; _ = req3; _ = req4;
            _ = server; _ = server3; _ = server4; _ = server5; _ = server6; _ = server7; _ = server8;
        }

        [TestMethod]
        public void HttpStatusException_Constructors_Factories_And_Serialization_Are_Covered()
        {
            var e1 = new HttpStatusException("m1");
            var e2 = new HttpStatusException("m2", HttpStatusCode.ServiceUnavailable);
            var e3 = new HttpStatusException("m3", new Exception("x"));
            var e4 = new HttpStatusException("m4", HttpStatusCode.GatewayTimeout, new Exception("x"));

            Assert.IsNull(e1.StatusCode);
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, e2.StatusCode);
            Assert.IsNotNull(e3.InnerException);
            Assert.AreEqual(HttpStatusCode.GatewayTimeout, e4.StatusCode);

            var s1 = HttpStatusException.ServiceUnavailable();
            var s2 = HttpStatusException.TooManyRequests();
            var s3 = HttpStatusException.GatewayTimeout();
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, s1.StatusCode);
            Assert.AreEqual((HttpStatusCode)429, s2.StatusCode);
            Assert.AreEqual(HttpStatusCode.GatewayTimeout, s3.StatusCode);

            var info = new SerializationInfo(typeof(HttpStatusException), new FormatterConverter());
            var context = new StreamingContext(StreamingContextStates.All);
            e2.GetObjectData(info, context);
            var copy = new TestableHttpStatusException(info, context);
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, copy.StatusCode);

            var infoNoStatus = new SerializationInfo(typeof(HttpStatusException), new FormatterConverter());
            e1.GetObjectData(infoNoStatus, context);
            infoNoStatus.AddValue("StatusCode", string.Empty);
            var copyNoStatus = new TestableHttpStatusException(infoNoStatus, context);
            Assert.IsNull(copyNoStatus.StatusCode);

            var infoInvalidStatus = new SerializationInfo(typeof(HttpStatusException), new FormatterConverter());
            e1.GetObjectData(infoInvalidStatus, context);
            infoInvalidStatus.AddValue("StatusCode", "NotARealStatus");
            var copyInvalidStatus = new TestableHttpStatusException(infoInvalidStatus, context);
            Assert.IsNull(copyInvalidStatus.StatusCode);
        }

        private sealed class TestableHttpStatusException : HttpStatusException
        {
            public TestableHttpStatusException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }
    }
}
