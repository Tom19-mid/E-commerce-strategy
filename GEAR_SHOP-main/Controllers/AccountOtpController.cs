using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TL4_SHOP.Helpers;
using TL4_SHOP.Services;

namespace TL4_SHOP.Controllers
{
    public class AccountOtpController : Controller
    {
        private readonly TwilioService _twilioService;
        public AccountOtpController(TwilioService twilioService)
        {
            _twilioService = twilioService;
        }

        [HttpGet]
        public IActionResult SendOtp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendOtp(string phoneNumber)
        {

            // Generate OTP
            var otp = OtpHelper.GenerateOtp();

            // Send OTP via Twilio
            bool success = await _twilioService.SendOtpAsync(phoneNumber, otp);

            if (success)
            {
                // Store OTP temporarily (e.g., in Session or a database)
                HttpContext.Session.SetString("Otp", otp);

                // Redirect to OTP verification page
                return RedirectToAction("VerifyOtp");
            }

            ModelState.AddModelError("", "Failed to send OTP.");

            return View();
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            return View();
        }

        [HttpPost]
        public IActionResult VerifyOtp(string otp)
        {

            // Retrieve the OTP stored in Session or database
            var storedOtp = HttpContext.Session.GetString("Otp");

            if (storedOtp == otp)
            {
                // OTP is valid, proceed with authentication (e.g., login)
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid OTP.");

            return View();
        }
    }
}
