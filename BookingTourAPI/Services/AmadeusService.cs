using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace BookingTourAPI.Services
{
    public class AmadeusService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _client;
        private readonly IMemoryCache _cache;

        public AmadeusService(IConfiguration config, HttpClient client, IMemoryCache cache)
        {
            _config = config;
            _client = client;
            _cache = cache;
        }

        // Lấy access token
        public async Task<string> GetTokenAsync()
        {
            var url = $"{_config["Amadeus:ApiBase"]}/v1/security/oauth2/token";
            var data = new Dictionary<string, string>
            {
                {"grant_type", "client_credentials"},
                {"client_id", _config["Amadeus:ClientId"]!},
                {"client_secret", _config["Amadeus:ClientSecret"]!}
            };

            var resp = await _client.PostAsync(url, new FormUrlEncodedContent(data));
            var body = await resp.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            return json.RootElement.GetProperty("access_token").GetString()!;
        }

        private async Task<HttpRequestMessage> CreateAuthorizedRequest(HttpMethod method, string url, HttpContent? content = null)
        {
            var token = await GetTokenAsync();
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (content != null)
            {
                request.Content = content;
            }
            return request;
        }

        // Gọi API Flights
        public async Task<string> GetFlightsAsync(
            string origin, string destination, string departureDate,
            string? returnDate = null,
            int adults = 1,
            int? children = null,
            int? infants = null,
            string? travelClass = null,
            bool nonStop = false,
            string? currencyCode = null,
            int? max = null,
            string? includedAirlineCodes = null,
            string? excludedAirlineCodes = null)
        {
            var query = new StringBuilder();
            query.Append($"?originLocationCode={origin}");
            query.Append($"&destinationLocationCode={destination}");
            query.Append($"&departureDate={departureDate}");
            query.Append($"&adults={adults}");

            if (!string.IsNullOrEmpty(returnDate))
                query.Append($"&returnDate={returnDate}");
            if (children.HasValue)
                query.Append($"&children={children.Value}");
            if (infants.HasValue)
                query.Append($"&infants={infants.Value}");
            if (!string.IsNullOrEmpty(travelClass))
                query.Append($"&travelClass={travelClass}");
            if (nonStop)
                query.Append("&nonStop=true");
            if (!string.IsNullOrEmpty(currencyCode))
                query.Append($"&currencyCode={currencyCode}");
            if (max.HasValue)
                query.Append($"&max={max.Value}");
            if (!string.IsNullOrEmpty(includedAirlineCodes))
                query.Append($"&includedAirlineCodes={includedAirlineCodes}");
            if (!string.IsNullOrEmpty(excludedAirlineCodes))
                query.Append($"&excludedAirlineCodes={excludedAirlineCodes}");

            var url = $"{_config["Amadeus:ApiBase"]}/v2/shopping/flight-offers{query}";
            var request = await CreateAuthorizedRequest(HttpMethod.Get, url);
            var response = await _client.SendAsync(request);

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Amadeus error: {body}");

            return body;
        }
    }
}