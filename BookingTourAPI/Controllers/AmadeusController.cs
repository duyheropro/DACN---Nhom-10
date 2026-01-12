using BookingTourAPI.Data;
using BookingTourAPI.Models;
using BookingTourAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BookingTourAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AmadeusController : ControllerBase
    {
        private readonly AmadeusService _amadeus;
        private readonly AppDbContext _db;

        public AmadeusController(AmadeusService amadeus, AppDbContext db)
        {
            _amadeus = amadeus;
            _db = db;
        }

        //Flights
        //Flight Offers Search API - Tìm kiếm chuyến bay
        [HttpGet("flights")]
        public async Task<IActionResult> GetFlights(
            [FromQuery] string originLocationCode,
            [FromQuery] string destinationLocationCode,
            [FromQuery] string departureDate,
            [FromQuery] string? returnDate,
            [FromQuery] int adults = 1,
            [FromQuery] int? children = null,
            [FromQuery] int? infants = null,
            [FromQuery] string? travelClass = null,
            [FromQuery] bool nonStop = false,
            [FromQuery] string? currencyCode = "VND",
            [FromQuery] int? max = 250)
        {
            if (originLocationCode?.Length != 3 || destinationLocationCode?.Length != 3)
            {
                return BadRequest("Origin and destination location codes must be 3-letter IATA codes.");
            }

            var json = await _amadeus.GetFlightsAsync(
                originLocationCode, destinationLocationCode, departureDate,
                returnDate, adults, children, infants,
                travelClass, nonStop, currencyCode, max);

            try
            {
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    var flightsFromApi = data.EnumerateArray().ToList();

                    var apiFlightIds = flightsFromApi
                        .Select(flightJson => flightJson.TryGetProperty("id", out var id) ? id.GetString() : null)
                        .Where(id => !string.IsNullOrEmpty(id))
                        .Distinct()
                        .ToList();

                    if (!apiFlightIds.Any())
                    {
                        return Content(json, "application/json");
                    }

                    var existingIdsInDb = await _db.Flights
                        .Where(f => f.FlightId != null && apiFlightIds.Contains(f.FlightId))
                        .Select(f => f.FlightId)
                        .ToHashSetAsync();

                    var newFlightsToCache = new List<Flight>();

                    foreach (var item in flightsFromApi)
                    {
                        string? id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                        if (string.IsNullOrEmpty(id)) continue;

                        if (!existingIdsInDb.Contains(id))
                        {
                            string? currency = item.GetProperty("price").GetProperty("currency").GetString();
                            string? totalStr = item.GetProperty("price").GetProperty("total").GetString();
                            decimal price = 0;
                            if (!string.IsNullOrEmpty(totalStr))
                                price = decimal.Parse(totalStr, System.Globalization.CultureInfo.InvariantCulture);

                            string? airline = null, origin = null, destination = null, duration = null;
                            DateTime? depTime = null, arrTime = null;

                            if (item.TryGetProperty("itineraries", out var itins) && itins.GetArrayLength() > 0)
                            {
                                var firstItin = itins[0];
                                if (firstItin.TryGetProperty("duration", out var dur))
                                    duration = dur.GetString();

                                if (firstItin.TryGetProperty("segments", out var segs) && segs.GetArrayLength() > 0)
                                {
                                    var firstSeg = segs.EnumerateArray().First();
                                    var lastSeg = segs.EnumerateArray().Last();

                                    origin = firstSeg.GetProperty("departure").GetProperty("iataCode").GetString();
                                    destination = lastSeg.GetProperty("arrival").GetProperty("iataCode").GetString();
                                    airline = firstSeg.GetProperty("carrierCode").GetString();

                                    var depTimeStr = firstSeg.GetProperty("departure").GetProperty("at").GetString();
                                    var arrTimeStr = lastSeg.GetProperty("arrival").GetProperty("at").GetString();
                                    depTime = depTimeStr != null ? DateTime.Parse(depTimeStr) : null;
                                    arrTime = arrTimeStr != null ? DateTime.Parse(arrTimeStr) : null;
                                }
                            }

                            newFlightsToCache.Add(new Flight
                            {
                                FlightId = id,
                                Airline = airline,
                                Origin = origin,
                                Destination = destination,
                                Duration = duration,
                                DepartureTime = depTime,
                                ArrivalTime = arrTime,
                                Price = price,
                                Currency = currency,
                                TravelClass = travelClass
                            });
                            existingIdsInDb.Add(id); // Avoid duplicates in the same batch
                        }
                    }

                    if (newFlightsToCache.Any())
                    {
                        _db.Flights.AddRange(newFlightsToCache);
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing and saving flight data: {ex.Message}");
            }

            return Content(json, "application/json");
        }
        
    }
}
