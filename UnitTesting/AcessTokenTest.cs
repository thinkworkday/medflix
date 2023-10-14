using CareAPI;
using FileAPI;
using Medlix.Backend.API.BAL.AuthenticationService;
using Medlix.Backend.API.BAL.AzureB2CService;
using Medlix.Backend.API.BAL.FhirPatientService;
using Medlix.Backend.API.BAL.SendGridService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using UnitTesting.Infrastructure;
using UnitTesting.Utils;
using Xunit;

namespace UnitTesting
{
    [Collection(TestsCollection.Name)]
    public class AcessTokenTest
    {
        private readonly CareBackendFunction _function;
        
        public AcessTokenTest(TestHost testHost)
        {
            _function = new CareBackendFunction(
                testHost.ServiceProvider.GetService<IAuthenticationService>(),
                testHost.ServiceProvider.GetService<IAzureB2CService>(),
                testHost.ServiceProvider.GetService<IFhirPatientService>(),
                testHost.ServiceProvider.GetService<ISendGridService>(),
                testHost.ServiceProvider.GetRequiredService<ILogger<CareBackendFunction>>()
            );
        }
        [Fact]
        public async void TestWithoutJwtToken()
        {
            var req = new DefaultHttpContext().Request;

            var loggerMock = new Mock<ILogger>();
            // act
            var result = (UnauthorizedResult) await _function.AccessToken(req, loggerMock.Object);
            Assert.Equal(result.StatusCode, 401);
        }

        [Fact]
        public async void TestWithInvalidToken()
        {
            var req = new DefaultHttpContext().Request;
            
            req.QueryString = new QueryString("?token=Bearer invalid TOken");
            var loggerMock = new Mock<ILogger>();
            // act
            var result = (UnauthorizedResult)await _function.AccessToken(req, loggerMock.Object);
            Assert.Equal(result.StatusCode, 401);
        }

        [Fact]
        public async void TestWithValidToken_InvalidIssuer()
        {
            var req = new DefaultHttpContext().Request;
            var jwtToken = UtilsHelper.GenerateToken(DateTime.UtcNow.AddMinutes(15), "", "testIssuer");

            req.QueryString = new QueryString($"?token={jwtToken}");
            var loggerMock = new Mock<ILogger>();
            // act
            var result = (UnauthorizedResult)await _function.AccessToken(req, loggerMock.Object);
            Assert.Equal(result.StatusCode, 401);
        }

        [Fact]
        public async void TestWithValidToken_InvalidAudience()
        {
            var req = new DefaultHttpContext().Request;
            var jwtToken = UtilsHelper.GenerateToken(DateTime.UtcNow.AddMinutes(15), "", "", "testAudience");

            req.QueryString = new QueryString($"?token={jwtToken}");
            var loggerMock = new Mock<ILogger>();
            // act
            var result = (UnauthorizedResult)await _function.AccessToken(req, loggerMock.Object);
            Assert.Equal(result.StatusCode, 401);
        }

        [Fact]
        public async void TestWithValidToken_InvalidSecretKey()
        {
            var req = new DefaultHttpContext().Request;
            var jwtToken = UtilsHelper.GenerateToken(DateTime.UtcNow.AddMinutes(15), "", "", "", "testsecretkey1231231231111212123");

            req.QueryString = new QueryString($"?token={jwtToken}");
            var loggerMock = new Mock<ILogger>();
            // act
            var result = (UnauthorizedResult)await _function.AccessToken(req, loggerMock.Object);
            Assert.Equal(result.StatusCode, 401);
            
        }

        [Fact]
        public async void TestWithValidToken()
        {
            var req = new DefaultHttpContext().Request;
            var jwtToken = UtilsHelper.GenerateToken(DateTime.UtcNow.AddMinutes(15), "", "","", Environment.GetEnvironmentVariable("JwtRefreshSecretKey"));

            req.QueryString = new QueryString($"?token={jwtToken}");
            var loggerMock = new Mock<ILogger>();
            // act
            var result = (OkObjectResult)await _function.AccessToken(req, loggerMock.Object);
            Assert.Equal(result.StatusCode, 200);
            
        }

    }
}