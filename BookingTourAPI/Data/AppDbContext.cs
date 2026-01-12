using Microsoft.EntityFrameworkCore;
using BookingTourAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace BookingTourAPI.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public required DbSet<Flight> Flights { get; set; }
        public required DbSet<FlightOrder> FlightOrders { get; set; }
        public required DbSet<TourBooking> TourBookings { get; set; }        public required DbSet<TopDestination> TopDestinations { get; set; }
        public required DbSet<TourPackage> TourPackages { get; set; }
        public required DbSet<TourDeparture> TourDepartures { get; set; }
        public required DbSet<DailyItinerary> DailyItineraries { get; set; }
        public required DbSet<Review> Reviews { get; set; }

    }
}