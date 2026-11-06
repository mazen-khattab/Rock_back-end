using System.Security.Claims;
using System.Text.Json;
using API.Controllers;
using API.Extensions;
using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Core.Enum;
using Core.ExceptionsTypes;
using Core.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Tests
{
    public class ControllerConventionsTests
    {
        [Theory]
        [InlineData(ErrorType.NotFound, 404)]
        [InlineData(ErrorType.InvalidCredentials, 401)]
        [InlineData(ErrorType.Forbidden, 403)]
        [InlineData(ErrorType.Conflict, 409)]
        [InlineData(ErrorType.ConcurrencyConflict, 409)]
        [InlineData(ErrorType.ValidationError, 422)]
        [InlineData(ErrorType.InsufficientStock, 400)]
        [InlineData(ErrorType.ServerError, 500)]
        [InlineData(ErrorType.None, 400)]
        public void ServiceFailuresReturnConsistentStatusAndRequestMetadata(ErrorType type, int expectedStatus)
        {
            var controller = new TestController { ControllerContext = CreateContext() };
            var result = Assert.IsType<ObjectResult>(controller.ToApiResult(new ApiResponse<string>
            {
                ErrorType = type, Message = "Request failed."
            }));
            var error = Assert.IsType<ApiErrorResponse>(result.Value);
            Assert.Equal(expectedStatus, result.StatusCode);
            Assert.Equal(expectedStatus, error.StatusCode);
            Assert.Equal("/api/test", error.Path);
            Assert.Equal("POST", error.Method);
            Assert.Equal("test-trace", error.TraceId);
            Assert.False(error.IsSuccess);
        }

        [Fact]
        public void MissingServiceResponseReturnsServerErrorAndNeverRunsSuccessCallback()
        {
            var controller = new TestController { ControllerContext = CreateContext() };
            bool callbackRan = false;
            var result = Assert.IsType<ObjectResult>(controller.ToApiResult<string>((ApiResponse<string>?)null, _ =>
            {
                callbackRan = true;
                return controller.Ok();
            }));
            Assert.Equal(500, result.StatusCode);
            Assert.False(callbackRan);
        }

        [Fact]
        public void SuccessKeepsTheServiceEnvelopeAndPaginationKeepsThePlainListShape()
        {
            var controller = new TestController { ControllerContext = CreateContext() };
            var response = new ApiResponse<string> { IsSuccess = true, Data = "value", Message = "Done." };
            Assert.Same(response, Assert.IsType<OkObjectResult>(controller.ToApiResult(response)).Value);
            var pageResult = controller.ToPagedApiResult(new ApiResponse<(IEnumerable<string> Items, int TotalCount)>
            {
                IsSuccess = true, Data = (new[] { "second", "third" }, 5)
            }, pageNumber: 2, pageSize: 2);
            var page = Assert.IsType<PagedResultDto<string>>(Assert.IsType<OkObjectResult>(pageResult).Value);
            Assert.Equal(2, page.PageNumber);
            Assert.Equal(2, page.PageSize);
            Assert.Equal(5, page.TotalCount);
            Assert.Equal(3, page.TotalPages);
            Assert.Equal(new[] { "second", "third" }, page.Items);
        }

        [Fact]
        public void AuthenticationResultsExposeOnlyTheExplicitPublicData()
        {
            var controller = new TestController { ControllerContext = CreateContext() };
            var response = new AuthServiceResponse
            {
                IsSuccess = true, Message = "Signed in.", AccessToken = "private-access", RefreshToken = "private-refresh"
            };
            var result = Assert.IsType<OkObjectResult>(controller.ToApiResult(response, new UserInfoDto { UserId = 42 }));
            var body = Assert.IsType<ApiResponse<UserInfoDto>>(result.Value);
            Assert.Equal(42, body.Data!.UserId);
            Assert.DoesNotContain("private-access", JsonSerializer.Serialize(result.Value));
            Assert.DoesNotContain("private-refresh", JsonSerializer.Serialize(result.Value));
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid")]
        [InlineData("0")]
        [InlineData("-1")]
        public async Task ProtectedCartActionsRejectInvalidClaimsBeforeCallingTheService(string userId)
        {
            var cart = new Mock<ICartService>(MockBehavior.Strict);
            var controller = new CartsController(cart.Object, NullLogger<CartsController>.Instance)
            {
                ControllerContext = CreateContext(userId)
            };
            await Assert.ThrowsAsync<UnauthorizedException>(() => controller.GetUserCart());
            cart.VerifyNoOtherCalls();
        }

        [Fact]
        public void SharedRequestContextIncludesPathBaseAndResolvedLanguage()
        {
            var request = CreateContext().HttpContext.Request;
            Assert.Equal("https://store.test/shop", request.GetBaseUrl());
            Assert.Equal("en", request.GetLanguageCode());
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            Assert.Null(anonymous.GetOptionalUserId());
            Assert.Equal(42, CreateContext("42").HttpContext.User.GetRequiredUserId());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CheckoutKeepsGuestSupportPlainOrderResponseAndSeparateCookieExpirations(bool authenticated)
        {
            var order = new CheckoutResponseDto { OrderId = 7, OrderNumber = "ORD-7", TotalPrice = 50m };
            var tokens = new AuthServiceResponse { IsSuccess = true, AccessToken = "access-value", RefreshToken = "refresh-value" };
            var request = new CheckoutRequestDto { IsAuthenticated = authenticated };
            var service = new Mock<IOrderService>(MockBehavior.Strict);
            service.Setup(s => s.ProcessCheckoutAsync(request, authenticated ? "42" : null, "https://store.test/shop"))
                .ReturnsAsync(new ApiResponse<(AuthServiceResponse? authResponse, CheckoutResponseDto checkoutResponse)>
                {
                    IsSuccess = true, Data = (tokens, order)
                });
            var controller = new OrdersController(service.Object, NullLogger<OrdersController>.Instance,
                Options.Create(new JwtSettings { AccessTokenExpirationMinutes = 15, RefreshTokenExpirationDays = 30 }))
            {
                ControllerContext = CreateContext(authenticated ? "42" : null)
            };

            var result = Assert.IsType<OkObjectResult>(await controller.Checkout(request));
            Assert.Same(order, result.Value);
            var cookies = SetCookieHeaderValue.ParseList(controller.Response.Headers.SetCookie.Select(value => value!).ToList());
            Assert.Equal(2, cookies.Count);
            var access = cookies.Single(c => c.Name == "accessToken");
            var refresh = cookies.Single(c => c.Name == "refreshToken");
            Assert.True(access.HttpOnly && access.Secure);
            Assert.True(refresh.HttpOnly && refresh.Secure);
            Assert.Equal(Microsoft.Net.Http.Headers.SameSiteMode.None, access.SameSite);
            Assert.InRange(access.Expires!.Value, DateTimeOffset.Now.AddMinutes(14), DateTimeOffset.Now.AddMinutes(16));
            Assert.InRange(refresh.Expires!.Value, DateTimeOffset.Now.AddDays(29), DateTimeOffset.Now.AddDays(31));
            service.VerifyAll();
        }

        [Fact]
        public async Task ProductAndContactFailuresUseTheSharedErrorMapping()
        {
            var products = new Mock<IProductService>();
            products.Setup(s => s.GetProductById(8, "en", "https://store.test/shop"))
                .ReturnsAsync(new ApiResponse<ProductDetailsDto> { ErrorType = ErrorType.NotFound, Message = "Product not found." });
            var controller = new ProductsController(products.Object, NullLogger<ProductsController>.Instance)
            {
                ControllerContext = CreateContext()
            };
            Assert.Equal(404, Assert.IsType<ObjectResult>(await controller.GetProductById(8, 0)).StatusCode);

            var contact = new Mock<IContactUsService>();
            var request = new ContactUsDto();
            contact.Setup(s => s.ContactUs(request)).Returns(new ApiResponse<string> { ErrorType = ErrorType.ServerError });
            var contactController = new ContactUsController(NullLogger<ContactUsController>.Instance, contact.Object)
            {
                ControllerContext = CreateContext()
            };
            Assert.Equal(500, Assert.IsType<ObjectResult>(contactController.ContactUs(request)).StatusCode);
        }

        [Fact]
        public async Task CartUsesValidatedUserIdAndPreservesQuantityNormalization()
        {
            var cart = new Mock<ICartService>();
            var response = new ApiResponse<string> { IsSuccess = true, Message = "Added." };
            cart.Setup(s => s.AddToUserCart(42, 9, 1)).ReturnsAsync(response);
            var controller = new CartsController(cart.Object, NullLogger<CartsController>.Instance)
            {
                ControllerContext = CreateContext("42")
            };
            Assert.Same(response, Assert.IsType<OkObjectResult>(await controller.AddToCartUser(new AddToUserCartDto
            {
                VariantId = 9, Quantity = 0
            })).Value);
            cart.Verify(s => s.AddToUserCart(42, 9, 1), Times.Once);
        }

        private static ControllerContext CreateContext(string? userId = null)
        {
            var http = new DefaultHttpContext { TraceIdentifier = "test-trace" };
            http.Request.Scheme = "https";
            http.Request.Host = new HostString("store.test");
            http.Request.PathBase = "/shop";
            http.Request.Path = "/api/test";
            http.Request.Method = "POST";
            http.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(new RequestCulture("en"), null));
            if (userId != null)
                http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test"));
            return new ControllerContext { HttpContext = http };
        }

        private sealed class TestController : ControllerBase { }
    }
}
