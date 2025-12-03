using PayPalCheckoutSdk.Core;
using PayPalHttp;
using System;
using System.IO;
using System.Text.Json;

namespace TL4_SHOP.Services
{
    public class PayPalClient
    {
        // Trả về PayPalEnvironment (Sandbox hoặc Live)
        public static PayPalEnvironment Environment(IConfiguration config)
        {
            string environment = config["PayPal:Environment"] ?? "Sandbox";
            return environment.ToLower() == "live"
                ? (PayPalEnvironment)new LiveEnvironment(config["PayPal:ClientId"], config["PayPal:ClientSecret"])
                : (PayPalEnvironment)new SandboxEnvironment(config["PayPal:ClientId"], config["PayPal:ClientSecret"]);
        }

        // Trả về HttpClient đã được cấu hình
        public static PayPalHttpClient Client(IConfiguration config)
        {
            var environment = Environment(config);
            return new PayPalHttpClient(environment);
        }

        // (Tùy chọn) Helper để serialize/deserialize JSON
        public static string ObjectToJSONString(object serializableObject)
        {
            MemoryStream memoryStream = new MemoryStream();
            var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });

            // SỬA LỖI 4: Chỉ định rõ là System.Text.Json.JsonSerializer
            System.Text.Json.JsonSerializer.Serialize(writer, serializableObject, serializableObject.GetType(), new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            writer.Flush();
            memoryStream.Position = 0;
            return new StreamReader(memoryStream).ReadToEnd();
        }
    }
}