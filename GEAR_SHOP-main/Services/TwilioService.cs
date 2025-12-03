using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Exceptions;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace TL4_SHOP.Services
{
    public class TwilioService
    {
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _phoneNumber;

        public TwilioService(IConfiguration configuration)
        {
            _accountSid = configuration["Twilio:AccountSid"];
            _authToken = configuration["Twilio:AuthToken"];
            _phoneNumber = configuration["Twilio:PhoneNumber"];
        }

        public async Task<bool> SendOtpAsync(string toPhoneNumber, string otp)
        {
            try
            {
                TwilioClient.Init(_accountSid, _authToken);

                var message = await MessageResource.CreateAsync(
                    body: $"Your OTP code is: {otp}",
                    from: new Twilio.Types.PhoneNumber(_phoneNumber),
                    to: new Twilio.Types.PhoneNumber(toPhoneNumber)
                );
                return true;
            }
            catch (ApiException ex)
            {
                // Handle the error (log it, etc.)
                Console.WriteLine($"Twilio API error: {ex.Message}");
                return false;

            }
        }

        //public async Task<bool> SendOtpAsync(string toPhoneNumber, string otp)
        //{
        //    // ✅ DEV MODE - không gọi Twilio thật
        //    Console.WriteLine("===== MOCK SMS OTP =====");
        //    Console.WriteLine($"To: {toPhoneNumber}");
        //    Console.WriteLine($"OTP: {otp}");
        //    Console.WriteLine("========================");

        //    await Task.CompletedTask;
        //    return true;
        //}
    }
}
