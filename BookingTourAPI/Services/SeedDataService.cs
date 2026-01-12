// TRONG FILE: Services/SeedDataService.cs (ĐÃ CẬP NHẬT 20 TOUR)
using Microsoft.AspNetCore.Identity;
using BookingTourAPI.Models;
using BookingTourAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace BookingTourAPI.Services
{
    public class SeedDataService
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>(); 

            await dbContext.Database.MigrateAsync();

            // --- Seed Admin Role ---
            string adminRoleName = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRoleName))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRoleName));
                Console.WriteLine($"Role '{adminRoleName}' created.");
            }

            // --- Seed Admin User ---
            string adminEmail = "admin@gmail.com";
            string adminPassword = "Admin@123";
            var adminUser = (ApplicationUser?)await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                var createUserResult = await userManager.CreateAsync(adminUser!, adminPassword); 
                if (createUserResult.Succeeded)
                {
                    Console.WriteLine($"Default admin user '{adminEmail}' created.");
                    await userManager.AddToRoleAsync(adminUser, adminRoleName);
                    Console.WriteLine($"User '{adminEmail}' added to role '{adminRoleName}'.");
                }
                else
                {
                    Console.WriteLine($"Error creating default admin user '{adminEmail}':");
                    foreach (var error in createUserResult.Errors)
                    {
                        Console.WriteLine($"- {error.Description}");
                    }
                }
            }
            else
            {
                 if (!await userManager.IsInRoleAsync(adminUser, adminRoleName))
                 {
                     await userManager.AddToRoleAsync(adminUser, adminRoleName);
                     Console.WriteLine($"Existing user '{adminEmail}' added to role '{adminRoleName}'.");
                 }
            }

            // --- Seed Tour Packages ---
            await SeedTourPackagesAsync(dbContext);
        }

        private static async Task SeedTourDeparturesAsync(AppDbContext db)
        {
            if (await db.TourDepartures.AnyAsync()) return;
            
            Console.WriteLine("Seeding Tour Departures...");
            
            var tours = await db.TourPackages.ToListAsync();
            var random = new Random();

            foreach (var tour in tours)
            {
                // Tạo 3-5 ngày khởi hành cho mỗi tour
                for (int i = 1; i <= 4; i++)
                {
                    var startDate = DateTime.Now.AddDays(random.Next(15, 21) + (i * 5)); // Ngày khởi hành cách ngày hiện tại từ 15-20 ngày
                    // Tính ngày về dựa trên Duration (vd: "3N2Đ")
                    int days = 3; 
                    if (tour.Duration != null && tour.Duration.Contains("N")) 
                    {
                        int.TryParse(tour.Duration.Split('N')[0], out days);
                    }
                    
                    var departure = new TourDeparture
                    {
                        TourPackageId = tour.Id,
                        StartDate = startDate,
                        EndDate = startDate.AddDays(days - 1),
                        // Giá người lớn bằng giá tour gốc
                        PriceAdult = tour.Price, 
                        // Giá trẻ em = 75%, Em bé = 30%
                        PriceChild = tour.Price * 0.75m,
                        PriceInfant = tour.Price * 0.3m,
                        AvailableSeats = random.Next(5, 25),
                        Airline = "Vietnam Airlines",
                        FlightNumberOut = $"VN{random.Next(100, 999)}",
                        FlightNumberIn = $"VN{random.Next(100, 999)}",
                        FlightTimeOut = "09:00 - 11:30",
                        FlightTimeIn = "14:00 - 16:30"
                    };
                    db.TourDepartures.Add(departure);
                }
            }
            await db.SaveChangesAsync();
        }


        private static async Task SeedTourPackagesAsync(AppDbContext db)
        {
            if (await db.TourPackages.AnyAsync())
            {
                Console.WriteLine("Đã có dữ liệu TourPackage, không seed.");
                return;
            }
            Console.WriteLine("Bắt đầu seed dữ liệu TourPackage (20 tours)...");

            var toursToAdd = new List<TourPackage>
            {
                // --- TOUR 1: HẠ LONG ---
                new TourPackage
                {
                    Title = "Khám phá Vịnh Hạ Long - Du thuyền 5*",
                    ImageUrl = "https://media-cdn-v2.laodong.vn/Storage/NewsPortal/2023/2/13/1147154/007_Rs.jpg",
                    Price = 2990000, Currency = "VND", Duration = "2N1Đ", Country = "Việt Nam", Region = "Quảng Ninh", Area = "MienBac",
                    Highlights = "Tận hưởng kỳ nghỉ sang trọng trên du thuyền 5 sao, khám phá hang Sửng Sốt, chèo thuyền kayak tại Hang Luồn và tắm biển tại đảo Titop.",
                    PolicyIncludes = "1 đêm nghỉ trên du thuyền 5*\nCác bữa ăn theo chương trình (1 bữa sáng, 2 bữa trưa, 1 bữa tối)\nVé tham quan Vịnh, hang Sửng Sốt\nThuyền kayak, tắm biển, lớp học Thái Cực Quyền\nXe đưa đón 2 chiều Hà Nội - Hạ Long",
                    PolicyExcludes = "Đồ uống (bia, rượu, nước ngọt)\nDịch vụ spa và massage trên du thuyền\nChi phí cá nhân và tiền tip",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Hà Nội - Vịnh Hạ Long - Hang Sửng Sốt", Description = "Xe đón tại Hà Nội, di chuyển đến Hạ Long. Lên du thuyền, ăn trưa. Chiều tham quan hang Sửng Sốt, chèo kayak tại Hang Luồn. Ăn tối và nghỉ đêm trên du thuyền." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Đảo Titop - Hà Nội", Description = "Sáng tập Thái Cực Quyền, ăn sáng. Thăm đảo Titop, leo núi hoặc tắm biển. Ăn trưa tại du thuyền. Trả phòng, xe đưa về Hà Nội." }
                    }
                },

                // --- TOUR 2: SA PA ---
                new TourPackage
                {
                    Title = "Chinh phục Sa Pa - Nóc nhà Đông Dương",
                    ImageUrl = "https://image.vietnamnews.vn/uploadvnnews/Article/2024/3/27/339564_4806870009252514_1.jpg",
                    Price = 4500000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Lào Cai", Area = "MienBac",
                    Highlights = "Chinh phục đỉnh Fansipan bằng cáp treo, khám phá bản Cát Cát, Cổng trời Ô Quý Hồ và thưởng thức đặc sản núi rừng Tây Bắc.",
                    PolicyIncludes = "Xe giường nằm cao cấp 2 chiều Hà Nội - Sa Pa\n2 đêm nghỉ khách sạn 3* trung tâm\nVé cáp treo Fansipan khứ hồi\nVé tham quan bản Cát Cát, Cổng trời\n2 bữa sáng buffet tại khách sạn",
                    PolicyExcludes = "Các bữa trưa và bữa tối\nChi phí xe ôm, taxi di chuyển tại Sa Pa\nTàu hỏa leo núi Mường Hoa\nHướng dẫn viên",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Hà Nội - Sa Pa - Bản Cát Cát", Description = "Lên xe giường nằm đi Sa Pa. Nhận phòng khách sạn. Chiều tham quan bản Cát Cát, nhà máy thủy điện cũ. Tối tự do dạo chợ đêm Sa Pa." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Chinh phục Fansipan", Description = "Sáng đi cáp treo chinh phục nóc nhà Đông Dương. Chiêm bái tượng Phật, ngắm toàn cảnh Sa Pa. Chiều tự do." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Sa Pa - Cổng trời - Hà Nội", Description = "Sáng check-out, xe đưa đi Cổng trời Ô Quý Hồ. Check-in Cầu kính Rồng Mây (tùy chọn). Trưa lên xe giường nằm về Hà Nội." }
                    }
                },

                // --- TOUR 3: ĐÀ NẴNG - HỘI AN ---
                new TourPackage
                {
                    Title = "Đà Nẵng - Hội An - Cầu Vàng Bà Nà",
                    ImageUrl = "https://reviewvilla.vn/wp-content/uploads/2022/05/Cau-vang-Da-Nang-1.jpg",
                    Price = 6200000, Currency = "VND", Duration = "4N3Đ", Country = "Việt Nam", Region = "Đà Nẵng, Quảng Nam", Area = "MienTrung",
                    Highlights = "Check-in Cầu Vàng tại Bà Nà Hills, dạo bộ Phố cổ Hội An lung linh, tắm biển Mỹ Khê và khám phá Ngũ Hành Sơn.",
                    PolicyIncludes = "Vé máy bay khứ hồi (SGN/HAN - DAD)\n3 đêm khách sạn 4* gần biển Mỹ Khê\nVé cáp treo và buffet trưa tại Bà Nà Hills\nXe đưa đón tham quan Hội An, Ngũ Hành Sơn\n3 bữa sáng buffet",
                    PolicyExcludes = "Các bữa trưa (trừ ngày đi Bà Nà) và bữa tối\nVé tham quan Ngũ Hành Sơn\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Đến Đà Nẵng - Ngũ Hành Sơn", Description = "Đáp chuyến bay đến Đà Nẵng, nhận phòng. Chiều tham quan Ngũ Hành Sơn, Làng đá Non Nước. Tối xem Cầu Rồng phun lửa (nếu là T7/CN)." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Bà Nà Hills - Cầu Vàng", Description = "Cả ngày vui chơi tại Bà Nà Hills, check-in Cầu Vàng, Làng Pháp, hầm rượu. Đã bao gồm buffet trưa trên Bà Nà." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Đà Nẵng - Phố cổ Hội An", Description = "Sáng tự do tắm biển Mỹ Khê. Chiều xe đưa vào Hội An, dạo phố cổ, đi thuyền thả hoa đăng. Ăn tối tại Hội An. Xe đưa về Đà Nẵng." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Tạm biệt Đà Nẵng", Description = "Ăn sáng, mua sắm đặc sản tại chợ Hàn. Xe đưa ra sân bay." }
                    }
                },

                // --- TOUR 4: PHÚ QUỐC ---
                new TourPackage
                {
                    Title = "Đảo ngọc Phú Quốc - Thiên đường nghỉ dưỡng",
                    ImageUrl = "https://mia.vn/media/uploads/blog-du-lich/dao-ngoc-phu-quoc-thien-duong-nghi-duong-tai-phuong-nam-1681783792.jpg",
                    Price = 5500000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Kiên Giang", Area = "MienNam",
                    Highlights = "Trải nghiệm cáp treo vượt biển dài nhất thế giới, lặn ngắm san hô tại Hòn Thơm, khám phá chợ đêm Phú Quốc và thư giãn tại Bãi Sao.",
                    PolicyIncludes = "Vé máy bay khứ hồi\n2 đêm nghỉ tại resort 5*\nXe đưa đón sân bay tại Phú Quốc\nVé cáp treo Hòn Thơm\nTour lặn ngắm san hô 4 đảo\n2 bữa sáng buffet",
                    PolicyExcludes = "Các bữa trưa và bữa tối\nVé VinWonders / Safari\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Đến Phú Quốc - Chợ đêm", Description = "Đáp chuyến bay đến Phú Quốc (PQC). Xe đón về resort nhận phòng. Tối tự do khám phá Chợ đêm Phú Quốc." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Tour 4 đảo Hòn Thơm", Description = "Trải nghiệm cáp treo Hòn Thơm. Lên cano đi Hòn Móng Tay, Hòn Mây Rút, Hòn Gầm Ghì (lặn ngắm san hô). Ăn trưa trên đảo." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Tạm biệt Phú Quốc", Description = "Ăn sáng, thư giãn tại resort. Check-out, xe đưa ra sân bay." }
                    }
                },

                // --- TOUR 5: THÁI LAN ---
                new TourPackage
                {
                    Title = "Khám phá Bangkok - Pattaya (Thái Lan)",
                    ImageUrl = "https://chodulich.com.vn/images/thai_lan.jpg",
                    Price = 8990000, Currency = "VND", Duration = "5N4Đ", Country = "Thái Lan", Region = "Bangkok", Area = "NuocNgoai",
                    Highlights = "Viếng Chùa Phật Vàng, dạo thuyền trên sông Chao Phraya, tham quan Cung điện Mùa hè, tắm biển Pattaya và xem Alcazar Show.",
                    PolicyIncludes = "Vé máy bay khứ hồi\n4 đêm khách sạn 4*\nCác bữa ăn theo chương trình\nXe du lịch máy lạnh suốt tuyến\nVé tham quan các điểm, Alcazar Show\nHướng dẫn viên tiếng Việt",
                    PolicyExcludes = "Chi phí hộ chiếu\nTiền tip cho HDV và tài xế (khoảng 3 USD/người/ngày)\nChi phí cá nhân, giặt ủi",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: TPHCM/Hà Nội - Bangkok", Description = "Đáp chuyến bay đến Bangkok (BKK). Xe và HDV đón đoàn đi ăn tối, nhận phòng khách sạn." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Bangkok - Dạo thuyền - Pattaya", Description = "Sáng tham quan Chùa Phật Vàng, dạo thuyền sông Chao Phraya. Ăn trưa, khởi hành đi Pattaya. Tối xem Alcazar Show." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Pattaya - Đảo Coral", Description = "Sáng đi cano ra đảo Coral, tự do tắm biển. Ăn trưa. Chiều tham quan trung tâm vàng bạc, Lâu đài Tỷ phú." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Pattaya - Bangkok - Shopping", Description = "Trả phòng, về Bangkok. Tham quan Trại Rắn, Cung điện Mùa hè. Chiều tự do mua sắm tại Big C, CentralWorld." },
                        new DailyItinerary { DayNumber = 5, Title = "Ngày 5: Bangkok - Về Việt Nam", Description = "Ăn sáng. Tự do đến giờ xe đưa ra sân bay, làm thủ tục về Việt Nam." }
                    }
                },

                // --- TOUR 6: MIỀN TÂY ---
                new TourPackage
                {
                    Title = "Du ngoạn Miền Tây: Cần Thơ - Châu Đốc",
                    ImageUrl = "https://www.vietnambooking.com/wp-content/uploads/2017/04/tour-du-lich-chau-doc-ha-tien-can-tho-3.jpg",
                    Price = 3700000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Cần Thơ, An Giang", Area = "MienNam",
                    Highlights = "Khám phá Chợ nổi Cái Răng, tham quan Rừng tràm Trà Sư mùa nước nổi, viếng Miếu Bà Chúa Xứ Núi Sam và thưởng thức ẩm thực miền Tây.",
                    PolicyIncludes = "Xe du lịch đời mới Sài Gòn - Miền Tây\n2 đêm khách sạn 3-4*\nCác bữa ăn chính và 2 bữa sáng\nVé thuyền chợ nổi, thuyền Rừng tràm Trà Sư\nHướng dẫn viên",
                    PolicyExcludes = "Đồ uống trong các bữa ăn\nChi phí cá nhân, vé tham quan ngoài chương trình",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Sài Gòn - Cần Thơ", Description = "Xe đón tại Sài Gòn, đi Cần Thơ. Ăn trưa. Chiều tham quan Thiền viện Trúc Lâm Phương Nam, nhà cổ Bình Thủy. Tối dạo bến Ninh Kiều." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Cần Thơ - Rừng tràm Trà Sư - Châu Đốc", Description = "Sáng sớm đi Chợ nổi Cái Răng. Về khách sạn ăn sáng, trả phòng. Khởi hành đi Châu Đốc. Chiều tham quan Rừng tràm Trà Sư. Tối viếng Miếu Bà Chúa Xứ." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Châu Đốc - Sài Gòn", Description = "Ăn sáng. Tham quan Lăng Thoại Ngọc Hầu. Khởi hành về Sài Gòn, ghé mua đặc sản." }
                    }
                },

                // --- TOUR 7: HÀN QUỐC ---
                new TourPackage
                {
                    Title = "Lãng mạn mùa thu Hàn Quốc: Seoul - Nami",
                    ImageUrl = "https://dulichdemen.vn/wp-content/uploads/2023/09/dao-nami-mua-thu-han-quoc-tour-du-lich-de-men-vn-3-768x640.jpg",
                    Price = 16900000, Currency = "VND", Duration = "5N4Đ", Country = "Hàn Quốc", Region = "Seoul", Area = "NuocNgoai",
                    Highlights = "Dạo bước trên đảo Nami lãng mạn, mặc Hanbok tại Cung điện Gyeongbok, mua sắm tại Myeongdong và khám phá tháp Namsan.",
                    PolicyIncludes = "Vé máy bay khứ hồi\nVisa nhập cảnh Hàn Quốc\n4 đêm khách sạn 4*\nCác bữa ăn theo chương trình\nVé tham quan đảo Nami, Cung điện\nHướng dẫn viên, xe đưa đón",
                    PolicyExcludes = "Vé lên tháp Namsan\nTiền tip cho HDV (khoảng 6 USD/người/ngày)\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Bay đến Seoul", Description = "Bay đêm đến Sân bay Incheon (ICN)." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Đảo Nami - Tháp Namsan", Description = "Ăn sáng. Tham quan đảo Nami. Chiều về Seoul, thăm tháp Namsan (không bao gồm vé lên tháp)." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Cung điện - Hanbok - Mua sắm", Description = "Tham quan Cung điện Gyeongbok, mặc Hanbok, Bảo tàng Dân gian. Chiều mua sắm tại Myeongdong." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Công viên Everland", Description = "Cả ngày vui chơi tại công viên giải trí Everland (hoặc Lotte World tùy mùa)." },
                        new DailyItinerary { DayNumber = 5, Title = "Ngày 5: Seoul - Về Việt Nam", Description = "Sáng mua sắm đặc sản, ra sân bay Incheon về Việt Nam." }
                    }
                },

                // --- TOUR 8: NHẬT BẢN ---
                new TourPackage
                {
                    Title = "Kỳ vĩ Nhật Bản: Tokyo - Núi Phú Sĩ - Kyoto",
                    ImageUrl = "https://daodulich.com/wp-content/uploads/classified-listing/2024/11/9lydotaibannendilichphap-6.jpg",
                    Price = 29990000, Currency = "VND", Duration = "6N5Đ", Country = "Nhật Bản", Region = "Tokyo, Kyoto", Area = "NuocNgoai",
                    Highlights = "Chiêm ngưỡng Núi Phú Sĩ hùng vĩ, tham quan Chùa Vàng Kinkakuji tại Kyoto, trải nghiệm tàu Shinkansen và khám phá thủ đô Tokyo hiện đại.",
                    PolicyIncludes = "Vé máy bay khứ hồi\nVisa nhập cảnh Nhật Bản\n5 đêm khách sạn 3-4*\nCác bữa ăn, trải nghiệm tàu Shinkansen (1 chặng)\nVé tham quan các điểm\nHướng dẫn viên, xe đưa đón",
                    PolicyExcludes = "Chi phí cá nhân\nTiền tip cho HDV\nBữa tối tự túc (nếu có)",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Bay đến Tokyo", Description = "Bay đến Sân bay Narita (NRT), xe đón về khách sạn." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Tokyo City Tour", Description = "Tham quan Chùa Asakusa Kannon, Tháp SkyTree (chụp hình bên ngoài), Cung điện Hoàng gia. Chiều mua sắm ở Akihabara." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Tokyo - Núi Phú Sĩ", Description = "Di chuyển đến khu vực Hakone. Tham quan Núi Phú Sĩ (lên trạm 5 nếu thời tiết cho phép). Ngâm tắm Onsen tại khách sạn." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Trải nghiệm Shinkansen - Kyoto", Description = "Trải nghiệm tàu cao tốc Shinkansen (1 chặng ngắn). Di chuyển đến Kyoto, tham quan Chùa Vàng Kinkakuji." },
                        new DailyItinerary { DayNumber = 5, Title = "Ngày 5: Kyoto - Osaka", Description = "Tham quan Rừng tre Arashiyama. Di chuyển về Osaka, tham quan Lâu đài Osaka, mua sắm tại Shinsaibashi." },
                        new DailyItinerary { DayNumber = 6, Title = "Ngày 6: Osaka - Về Việt Nam", Description = "Ăn sáng, xe đưa ra Sân bay Kansai (KIX) về Việt Nam." }
                    }
                },

                // --- TOUR 9: QUY NHƠN - PHÚ YÊN ---
                new TourPackage
                {
                    Title = "Biển xanh vẫy gọi: Quy Nhơn - Phú Yên",
                    ImageUrl = "https://daklaktour.vn/wp-content/uploads/2019/03/11-1.jpg",
                    Price = 5300000, Currency = "VND", Duration = "4N3Đ", Country = "Việt Nam", Region = "Bình Định, Phú Yên", Area = "MienTrung",
                    Highlights = "Lặn ngắm san hô tại Kỳ Co, check-in Eo Gió, Gành Đá Đĩa kỳ vĩ, Bãi Xép (Hoa vàng trên cỏ xanh) và Tháp Đôi Chăm Pa.",
                    PolicyIncludes = "Vé máy bay khứ hồi đến Quy Nhơn (UIH)\n3 đêm khách sạn 4*\nXe du lịch đưa đón\nTour cano Kỳ Co - Eo Gió (bao gồm bữa trưa hải sản)\nVé tham quan Gành Đá Đĩa, Bãi Xép\n3 bữa sáng buffet",
                    PolicyExcludes = "Các bữa trưa, tối (trừ ngày đi Kỳ Co)\nChi phí cá nhân, giặt ủi",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Đến Quy Nhơn - Tháp Đôi", Description = "Bay đến Sân bay Phù Cát (UIH), xe đón về TP Quy Nhơn. Chiều tham quan Tháp Đôi Chăm Pa. Tối dạo biển." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Kỳ Co - Eo Gió", Description = "Sáng đi cano đến bãi Kỳ Co tắm biển. Lặn ngắm san hô. Ăn trưa hải sản. Chiều tham quan Eo Gió, Tịnh xá Ngọc Hòa." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Gành Đá Đĩa - Bãi Xép (Phú Yên)", Description = "Cả ngày tham quan Phú Yên: Nhà thờ Mằng Lăng, Gành Đá Đĩa. Chiều đến Bãi Xép (phim trường Hoa vàng trên cỏ xanh)." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Tạm biệt Quy Nhơn", Description = "Ăn sáng, tự do. Xe đưa ra sân bay Phù Cát về." }
                    }
                },

                // --- TOUR 10: HUẾ - ĐÀ NẴNG ---
                new TourPackage
                {
                    Title = "Hành trình Di sản Miền Trung: Huế - Đà Nẵng",
                    ImageUrl = "https://homestay.review/wp-content/uploads/2020/04/dia-diem-du-lich-hue.jpg",
                    Price = 3990000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Thừa Thiên Huế, Đà Nẵng", Area = "MienTrung",
                    Highlights = "Tham quan Đại Nội Huế, Lăng vua Khải Định, Chùa Thiên Mụ. Di chuyển qua đèo Hải Vân và khám phá các cây cầu nổi tiếng của Đà Nẵng.",
                    PolicyIncludes = "Xe du lịch đưa đón theo chương trình\n1 đêm khách sạn 4* tại Huế\n1 đêm khách sạn 4* tại Đà Nẵng\n2 bữa sáng buffet\nVé tham quan Đại Nội, Lăng Khải Định\nDu thuyền sông Hương nghe ca Huế",
                    PolicyExcludes = "Phương tiện di chuyển đến Huế/Đà Nẵng (máy bay, tàu)\nCác bữa ăn trưa và tối\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Khám phá Cố đô Huế", Description = "Đến Huế. Chiều tham quan Đại Nội (Hoàng thành), Chùa Thiên Mụ. Tối đi thuyền nghe ca Huế trên sông Hương." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Lăng Vua - Đà Nẵng", Description = "Sáng tham quan Lăng vua Khải Định. Trả phòng, khởi hành đi Đà Nẵng qua Đèo Hải Vân. Chiều dạo biển Mỹ Khê." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Tạm biệt Đà Nẵng", Description = "Ăn sáng, tham quan Chùa Linh Ứng (Bán đảo Sơn Trà), mua sắm. Ra sân bay." }
                    }
                },

                // --- TOUR 11: HÀ GIANG ---
                new TourPackage
                {
                    Title = "Hà Giang hùng vĩ - Cung đường Hạnh Phúc",
                    ImageUrl = "https://gonatour.vn/vnt_upload/news/08_2020/du_lich_tinh_ha_giang_gonatour.jpg",
                    Price = 4200000, Currency = "VND", Duration = "4N3Đ", Country = "Việt Nam", Region = "Hà Giang", Area = "MienBac",
                    Highlights = "Chinh phục Mã Pí Lèng - một trong 'tứ đại đỉnh đèo', du thuyền sông Nho Quế, check-in Cột cờ Lũng Cú và Cao nguyên đá Đồng Văn.",
                    PolicyIncludes = "Xe du lịch 7 chỗ/Limousine suốt tuyến Hà Nội - Hà Giang\n3 đêm nghỉ homestay/khách sạn địa phương\nCác bữa ăn theo chương trình\nVé tham quan các điểm\nThuyền sông Nho Quế",
                    PolicyExcludes = "Đồ uống, chi phí cá nhân\nTiền tip cho lái xe và HDV\nXe ôm đi lên các điểm check-in (nếu cần)",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Hà Nội - Quản Bạ - Yên Minh", Description = "Sáng sớm khởi hành từ Hà Nội. Dừng chân tại Cổng trời Quản Bạ, ngắm Núi Đôi Cô Tiên. Nghỉ đêm tại Yên Minh." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Yên Minh - Lũng Cú - Đồng Văn", Description = "Tham quan Dinh thự Vua Mèo, Cột cờ Lũng Cú - điểm cực Bắc. Chiều dạo Phố cổ Đồng Văn. Nghỉ đêm tại Đồng Văn." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Đồng Văn - Mã Pí Lèng - Sông Nho Quế", Description = "Chinh phục đèo Mã Pí Lèng, hẻm vực Tu Sản. Du thuyền trên sông Nho Quế. Nghỉ đêm tại Mèo Vạc hoặc Đồng Văn." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Mèo Vạc - Hà Nội", Description = "Ăn sáng, trả phòng. Di chuyển về Hà Nội. Kết thúc hành trình." }
                    }
                },

                // --- TOUR 12: ĐÀ LẠT ---
                new TourPackage
                {
                    Title = "Đà Lạt mộng mơ - Săn mây và hoa",
                    ImageUrl = "https://dulichviet.com.vn/images/bandidau/tour-du-lich-da-lat-7.jpg",
                    Price = 3800000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Lâm Đồng", Area = "MienTrung",
                    Highlights = "Săn mây tại Cầu Đất, check-in đồi hoa cẩm tú cầu, khám phá Thác Datanla với xe trượt, tham quan Ga Đà Lạt và Vườn hoa thành phố.",
                    PolicyIncludes = "Vé máy bay khứ hồi (SGN/HAN - DLI)\n2 đêm khách sạn 3* trung tâm\nXe ô tô đưa đón tham quan\nVé săn mây, vé vào cổng các điểm\n2 bữa sáng",
                    PolicyExcludes = "Các bữa trưa và tối\nVé xe trượt Thác Datanla\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Đến Đà Lạt - Vườn hoa", Description = "Bay đến sân bay Liên Khương (DLI), xe đón về trung tâm. Chiều tham quan Vườn hoa thành phố, Quảng trường Lâm Viên." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Săn mây - Cầu Đất - Thác Datanla", Description = "Sáng sớm đi săn mây Cầu Đất, đồi chè. Chiều tham quan Thác Datanla, Ga Đà Lạt, Đồi cẩm tú cầu." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Tạm biệt Đà Lạt", Description = "Ăn sáng, tự do. Xe đưa ra sân bay về." }
                    }
                },

                // --- TOUR 14: ĐÀI LOAN ---
                new TourPackage
                {
                    Title = "Đài Loan: Đài Bắc - Đài Trung - Cao Hùng",
                    ImageUrl = "https://i.ex-cdn.com/vntravellive.com/files/news/2023/02/01/so-tay-kinh-nghiem-du-lich-dai-loan-175319.jpg",
                    Price = 13500000, Currency = "VND", Duration = "5N4Đ", Country = "Đài Loan", Region = "Đài Bắc", Area = "NuocNgoai",
                    Highlights = "Check-in Tháp Taipei 101, du thuyền Hồ Nhật Nguyệt, khám phá Phật Quang Sơn Tự (Cao Hùng) và thả đèn trời tại Phố cổ Thập Phần.",
                    PolicyIncludes = "Vé máy bay khứ hồi\nVisa Đài Loan (theo đoàn)\n4 đêm khách sạn 3-4*\nCác bữa ăn theo chương trình\nVé tham quan, du thuyền Hồ Nhật Nguyệt\nTrải nghiệm tàu cao tốc (1 chặng)",
                    PolicyExcludes = "Vé lên tháp Taipei 101\nTiền tip cho HDV và tài xế\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Bay đến Đài Bắc (TPE)", Description = "Đến sân bay Đào Viên, xe đón về khách sạn. Tối dạo chợ đêm Sĩ Lâm." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Đài Bắc - Thập Phần", Description = "Tham quan Cung điện Cố Cung, Tháp Taipei 101 (chụp hình). Chiều đi Phố cổ Cửu Phần, thả đèn trời Thập Phần." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Đài Trung - Hồ Nhật Nguyệt", Description = "Di chuyển đến Đài Trung. Du thuyền trên Hồ Nhật Nguyệt, tham quan Văn Võ Miếu. Tối dạo chợ đêm Phụng Giáp." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Cao Hùng - Phật Quang Sơn", Description = "Đi tàu cao tốc đến Cao Hùng. Tham quan Phật Quang Sơn Tự, Đầm Liên Trì, Tháp Long Hổ." },
                        new DailyItinerary { DayNumber = 5, Title = "Ngày 5: Cao Hùng - Về Việt Nam", Description = "Ăn sáng, ra sân bay Cao Hùng (KHH) về Việt Nam." }
                    }
                },

                // --- TOUR 15: CÔN ĐẢO ---
                new TourPackage
                {
                    Title = "Côn Đảo linh thiêng - Nghỉ dưỡng biển",
                    ImageUrl = "https://vietrektravel.com/ckeditor/plugins/fileman/Uploads/Images/vuon-quoc-gia-con-dao.jpg",
                    Price = 6500000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Bà Rịa - Vũng Tàu", Area = "MienNam",
                    Highlights = "Viếng mộ nữ anh hùng Võ Thị Sáu, tham quan hệ thống Nhà tù Côn Đảo, Chùa Núi Một và tắm biển Đầm Trầu.",
                    PolicyIncludes = "Vé máy bay khứ hồi (SGN - VCS)\n2 đêm nghỉ tại resort 4*\nXe đưa đón sân bay và tham quan theo lịch trình\nVé tham quan các điểm di tích\n2 bữa sáng",
                    PolicyExcludes = "Các bữa trưa và tối\nHướng dẫn viên\nChi phí lặn biển",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Đến Côn Đảo - Tham quan", Description = "Bay đến Côn Đảo (VCS). Xe đón về resort. Chiều tham quan Chùa Núi Một, Dinh Chúa Đảo. Tắm biển." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Di tích lịch sử - Bãi Đầm Trầu", Description = "Sáng tham quan Nhà tù Côn Đảo, Trại Phú Hải, Chuồng cọp. Chiều thư giãn tại Bãi Đầm Trầu. Tối tự do viếng mộ cô Sáu." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Tạm biệt Côn Đảo", Description = "Ăn sáng, mua sắm đặc sản. Xe đưa ra sân bay." }
                    }
                },
                // --- TOUR 21: BÌNH HƯNG ---
                new TourPackage
                {
                    Title = "Khám phá Đảo Bình Hưng - Vịnh Vĩnh Hy 3N2Đ",
                    ImageUrl = "https://bazantravel.com/cdn/medias/tours/0/267.jpg",
                    Price = 3190000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Ninh Thuận", Area = "MienTrung",
                    Highlights = "Lặn ngắm san hô tại Bình Hưng, khám phá Vườn nho Ninh Thuận, Đồng cừu An Hòa, check-in Hang Rái, thưởng thức tiệc BBQ hải sản.",
                    PolicyIncludes = "Xe du lịch đời mới TP.HCM - Ninh Thuận\n2 đêm khách sạn 3 sao\nCác bữa ăn theo chương trình (3 bữa sáng, 3 bữa trưa, 2 bữa tối)\nTàu tham quan đảo Bình Hưng, vé Hang Rái\nNước suối, nón du lịch",
                    PolicyExcludes = "Đồ uống trong các bữa ăn\nChi phí cá nhân, giặt ủi\nVAT 8%",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: TP.HCM - Phan Rang - Đồng Cừu", Description = "Sáng xe đón tại TP.HCM, khởi hành đi Phan Rang. Trưa ăn tại Cà Ná. Chiều tham quan Đồng cừu An Hòa. Nhận phòng khách sạn, ăn tối." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Vịnh Vĩnh Hy - Đảo Bình Hưng - Hang Rái", Description = "Sáng đi tàu Vịnh Vĩnh Hy, lặn ngắm san hô. Tàu đưa đến Đảo Bình Hưng, tự do tắm biển. Trưa ăn trưa hải sản. Chiều tham quan Hang Rái, Vườn Nho. Tối ăn BBQ." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Phan Rang - Tạm biệt", Description = "Ăn sáng, trả phòng. Tham quan làng gốm Bàu Trúc. Khởi hành về TP.HCM. Ăn trưa trên đường. Chiều về đến TP.HCM." }
                    }
                },

                // --- TOUR 22: ĐÔNG BẮC ---
                new TourPackage
                {
                    Title = "Vòng cung Đông Bắc: Hà Giang - Thác Bản Giốc - Hồ Ba Bể",
                    ImageUrl = "https://dreamtravel.vn/Data/Upload/ResizeImage/userfiles/files/khanh/ha%20giang/bangiocx702x377x2.jpg",
                    Price = 7890000, Currency = "VND", Duration = "6N5Đ", Country = "Việt Nam", Region = "Hà Giang, Cao Bằng, Bắc Kạn", Area = "MienBac",
                    Highlights = "Chinh phục đèo Mã Pí Lèng, Cột cờ Lũng Cú, vẻ đẹp Thác Bản Giốc, thăm hang Pắc Bó, du thuyền Hồ Ba Bể.",
                    PolicyIncludes = "Xe du lịch suốt tuyến\n5 đêm khách sạn/homestay (2 đêm Hà Giang, 2 đêm Cao Bằng, 1 đêm Ba Bể)\nCác bữa ăn theo chương trình\nVé tham quan, thuyền Ba Bể, thuyền sông Nho Quế\nBảo hiểm du lịch",
                    PolicyExcludes = "Vé máy bay (nếu từ nơi khác đến Hà Nội)\nĐồ uống, chi phí cá nhân\nTip cho HDV và tài xế",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Hà Nội - Hà Giang - Cổng trời Quản Bạ", Description = "Xe đón tại Hà Nội. Di chuyển đến Hà Giang. Chụp hình tại Cổng trời Quản Bạ, Núi Đôi Cô Tiên. Nghỉ đêm tại Quản Bạ." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Đồng Văn - Mã Pí Lèng - Lũng Cú", Description = "Tham quan Dinh thự Vua Mèo. Chinh phục đèo Mã Pí Lèng, đi thuyền sông Nho Quế. Thăm Cột cờ Lũng Cú. Nghỉ đêm tại Đồng Văn." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Mèo Vạc - Cao Bằng", Description = "Dạo phố cổ Đồng Văn. Di chuyển từ Mèo Vạc qua các cung đèo hiểm trở để đến thành phố Cao Bằng. Nhận phòng, nghỉ ngơi." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Thác Bản Giốc - Động Ngườm Ngao", Description = "Tham quan Thác Bản Giốc - thác nước đẹp nhất Việt Nam. Khám phá vẻ đẹp kỳ vĩ của Động Ngườm Ngao. Nghỉ đêm tại Cao Bằng." },
                        new DailyItinerary { DayNumber = 5, Title = "Ngày 5: Pắc Bó - Hồ Ba Bể", Description = "Tham quan khu di tích Pắc Bó, suối Lê-nin. Khởi hành đi Bắc Kạn, đến Hồ Ba Bể. Nhận phòng homestay ven hồ." },
                        new DailyItinerary { DayNumber = 6, Title = "Ngày 6: Du thuyền Hồ Ba Bể - Về Hà Nội", Description = "Sáng du thuyền trên Hồ Ba Bể, thăm Đảo Bà Góa, Động Puông. Trả phòng, lên xe về Hà Nội. Kết thúc hành trình." }
                    }
                },

                // --- TOUR 23: TÀ ĐÙNG ---
                new TourPackage
                {
                    Title = "Hồ Tà Đùng - Vịnh Hạ Long của Tây Nguyên 3N2Đ",
                    ImageUrl = "https://statics.vinpearl.com/H%E1%BB%93%20T%C3%A0%20%C4%90%C3%B9ng%201_1684814136.jpg",
                    Price = 3290000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Đắk Nông", Area = "MienTrung", // (Tây Nguyên)
                    Highlights = "Du thuyền trên hồ Tà Đùng, chiêm ngưỡng 36 hòn đảo lớn nhỏ, khám phá văn hóa đồng bào M'Nông, tham quan Thác Diệu Thanh.",
                    PolicyIncludes = "Xe du lịch, 2 đêm khách sạn 3 sao, 2 bữa sáng buffet, 3 bữa trưa, 1 bữa tối\nVé tham quan, thuyền du ngoạn hồ Tà Đùng\nBảo hiểm du lịch",
                    PolicyExcludes = "Chi phí cá nhân, ăn tối ngày 2\nĐồ uống",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: TP.HCM - Gia Nghĩa - Hồ Tà Đùng", Description = "Xe đón tại TP.HCM, khởi hành đi Đắk Nông. Ăn trưa. Chiều đến khu vực Tà Đùng, nhận phòng. Tối tự do khám phá." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Khám phá Tà Đùng - Thác Diệu Thanh", Description = "Sáng lên thuyền khám phá hồ Tà Đùng, ngắm các hòn đảo. Chiều tham quan Thác Diệu Thanh. Tối tự do." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Chợ Gia Nghĩa - Về TP.HCM", Description = "Ăn sáng, trả phòng. Mua sắm đặc sản Tây Nguyên tại chợ Gia Nghĩa. Lên xe về lại TP.HCM." }
                    }
                },

                // --- TOUR 24: CHÂU ÂU ---
                new TourPackage
                {
                    Title = "Tây Âu 3 nước: Pháp - Bỉ - Hà Lan (Làng cổ Giethoorn)",
                    ImageUrl = "https://luhanhvietnam.com.vn/du-lich/vnt_upload/news/12_2019/ngoi-lang-giethoorn-ha-lan-9.jpg",
                    Price = 68990000, Currency = "VND", Duration = "9N8Đ", Country = "Pháp, Bỉ, Hà Lan", Region = "Châu Âu", Area = "NuocNgoai",
                    Highlights = "Tháp Eiffel, du thuyền Sông Seine, Tượng Manneken Pis (Bỉ), Làng cối xay gió Zaanse Schans, Làng cổ tích Giethoorn.",
                    PolicyIncludes = "Vé máy bay khứ hồi (Qatar Airways hoặc tương đương)\n7 đêm khách sạn 4* Châu Âu\nCác bữa ăn theo chương trình\nVé tham quan, tàu cao tốc TGV (1 chặng)\nVisa Schengen, Bảo hiểm du lịch",
                    PolicyExcludes = "Tiền tip cho HDV và tài xế (khoảng 8 EUR/người/ngày)\nChi phí cá nhân, giặt ủi",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Việt Nam - Bay đến Paris", Description = "Tập trung tại sân bay, làm thủ tục bay đi Paris (Pháp). Nghỉ đêm trên máy bay." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Paris - Tháp Eiffel - Sông Seine", Description = "Đến Paris, tham quan Khải Hoàn Môn, Tháp Eiffel (chụp hình), du thuyền Sông Seine." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Paris - Bảo tàng Louvre - Mua sắm", Description = "Sáng tham quan Bảo tàng Louvre (bên ngoài). Chiều tự do mua sắm tại Galeries Lafayette." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Paris - Brussels (Bỉ)", Description = "Khởi hành đi Brussels. Tham quan Quảng trường Grand Place, Tượng Chú Bé Đứng Tè Manneken Pis, Mô hình Atomium." },
                        new DailyItinerary { DayNumber = 5, Title = "Ngày 5: Brussels - Làng Giethoorn (Hà Lan)", Description = "Di chuyển đến Hà Lan. Tham quan làng cổ tích Giethoorn bằng thuyền nhỏ. Nghỉ đêm tại Amsterdam." },
                        new DailyItinerary { DayNumber = 6, Title = "Ngày 6: Amsterdam - Zaanse Schans - Xưởng kim cương", Description = "Tham quan làng cối xay gió Zaanse Schans, xưởng làm guốc gỗ, xưởng phô mai. Chiều tham quan xưởng chế tác kim cương." },
                        new DailyItinerary { DayNumber = 7, Title = "Ngày 7: Amsterdam - Tự do", Description = "Tự do tham quan, mua sắm hoặc đăng ký tour Lễ hội hoa Keukenhof (nếu vào mùa)." },
                        new DailyItinerary { DayNumber = 8, Title = "Ngày 8: Amsterdam - Về Việt Nam", Description = "Ăn sáng, trả phòng. Xe đưa ra sân bay làm thủ tục về Việt Nam." },
                        new DailyItinerary { DayNumber = 9, Title = "Ngày 9: Đến Việt Nam", Description = "Về đến sân bay tại Việt Nam. Kết thúc tour." }
                    }
                },

                // --- TOUR 25: LÝ SƠN ---
                new TourPackage
                {
                    Title = "Đảo Lý Sơn - Vương quốc Tỏi 3N2Đ",
                    ImageUrl = "https://focusasiatravel.vn/wp-content/uploads/2024/07/1.jpg",
                    Price = 3990000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Quảng Ngãi", Area = "MienTrung",
                    Highlights = "Khám phá 'tuyến du lịch biển đảo mới'. Check-in Cổng Tò Vò, Hang Câu, Chùa Hang, du thuyền Đảo Bé, thưởng thức đặc sản gỏi tỏi Lý Sơn.",
                    PolicyIncludes = "Vé máy bay khứ hồi (TP.HCM - Chu Lai)\nXe đưa đón sân bay Chu Lai - Cảng Sa Kỳ\nTàu cao tốc khứ hồi Sa Kỳ - Lý Sơn\n2 đêm khách sạn tại Lý Sơn, 2 bữa sáng\nVé tham quan các điểm",
                    PolicyExcludes = "Các bữa ăn trưa và tối\nChi phí cá nhân\nTàu đi Đảo Bé",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Sân bay Chu Lai - Cảng Sa Kỳ - Đảo Lý Sơn", Description = "Đáp chuyến bay đến Chu Lai. Xe đón đi cảng Sa Kỳ, lên tàu cao tốc ra Đảo Lớn (Lý Sơn). Nhận phòng. Chiều tham quan Chùa Hang, Cổng Tò Vò ngắm hoàng hôn." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Đảo Bé - Hang Câu", Description = "Sáng đi tàu (tự túc) ra Đảo Bé, tự do tắm biển, lặn ngắm san hô. Trưa về lại Đảo Lớn. Chiều tham quan Hang Câu, cột cờ Lý Sơn, miệng núi lửa Thới Lới." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Chợ Lý Sơn - Về đất liền", Description = "Ăn sáng, trả phòng. Mua sắm đặc sản Tỏi Lý Sơn tại chợ. Lên tàu cao tốc về cảng Sa Kỳ. Xe đưa ra sân bay Chu Lai." }
                    }
                },

                // --- TOUR 26: PHÚ QUỐC (BẮC ĐẢO) ---
                new TourPackage
                {
                    Title = "Phú Quốc 4N3Đ: Grand World - VinWonders - Safari",
                    ImageUrl = "https://sacotravel.com/wp-content/uploads/2023/01/phu-quoc.jpg",
                    Price = 7590000, Currency = "VND", Duration = "4N3Đ", Country = "Việt Nam", Region = "Kiên Giang", Area = "MienNam",
                    Highlights = "Khám phá 'Thành phố không ngủ' Grand World, vui chơi tại VinWonders, khám phá Vinpearl Safari, thư giãn tại Bãi Dài.",
                    PolicyIncludes = "Vé máy bay khứ hồi (SGN - PQC)\n3 đêm nghỉ khách sạn 4* tại Bắc Đảo\nVé vui chơi VinWonders & Safari (1 ngày)\nXe đưa đón sân bay, 3 bữa sáng buffet",
                    PolicyExcludes = "Các bữa trưa và tối\nVé show 'Tinh hoa Việt Nam'\nChi phí cá nhân, tour 4 đảo",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Đến Phú Quốc - Grand World", Description = "Bay đến Phú Quốc (PQC), xe đón về khách sạn. Chiều tham quan Grand World, đi thuyền trên kênh Venice. Tối xem nhạc nước." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: VinWonders & Vinpearl Safari", Description = "Cả ngày khám phá công viên chủ đề VinWonders và vườn thú bán hoang dã Vinpearl Safari." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Tự do nghỉ dưỡng", Description = "Tự do tắm biển tại Bãi Dài hoặc thư giãn tại hồ bơi. Tùy chọn: Tham quan chợ đêm Dương Đông." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Tạm biệt Phú Quốc", Description = "Ăn sáng, trả phòng. Xe đưa ra sân bay Phú Quốc." }
                    }
                },

                // --- TOUR 27: QUY NHƠN (NGẮN NGÀY) ---
                new TourPackage
                {
                    Title = "Quy Nhơn 3N2Đ - Kỳ Co, Eo Gió",
                    ImageUrl = "https://flane.vn/wp-content/uploads/2022/11/du-lich-quy-nhon-14.jpg",
                    Price = 4200000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Bình Định", Area = "MienTrung",
                    Highlights = "Tắm biển tại 'Maldives Việt Nam' - bãi Kỳ Co, lặn ngắm san hô, check-in Eo Gió, tham quan Tháp Đôi, Ghềnh Ráng Tiên Sa.",
                    PolicyIncludes = "Vé máy bay khứ hồi (SGN - UIH)\n2 đêm khách sạn 4* FLC hoặc tương đương\nTour cano Kỳ Co - Eo Gió (bao gồm ăn trưa hải sản)\nXe đưa đón sân bay Phù Cát\n2 bữa sáng buffet",
                    PolicyExcludes = "Các bữa trưa (trừ ngày đi tour) và bữa tối\nVé tham quan Ghềnh Ráng, Tháp Đôi\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Sân bay Phù Cát - Ghềnh Ráng", Description = "Đến sân bay Phù Cát (UIH), xe đón về khách sạn. Chiều tham quan Ghềnh Ráng Tiên Sa, Mộ Hàn Mặc Tử. Tối tự do." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Kỳ Co - Eo Gió - Lặn san hô", Description = "Cả ngày đi tour cano khám phá bãi Kỳ Co, lặn ngắm san hô tại Bãi Dứa. Ăn trưa hải sản. Chiều tham quan Eo Gió." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Tháp Đôi - Tạm biệt", Description = "Ăn sáng, trả phòng. Tham quan Tháp Đôi Chăm Pa. Xe đưa ra sân bay Phù Cát." }
                    }
                },

                // --- TOUR 28: SA PA (TIẾT KIỆM) ---
                new TourPackage
                {
                    Title = "Sa Pa 2N1Đ - Bản Cát Cát (Đi xe giường nằm)",
                    ImageUrl = "https://bloganchoi.com/wp-content/uploads/2017/11/sa-pa-bac-thang.jpg",
                    Price = 2100000, Currency = "VND", Duration = "2N1Đ", Country = "Việt Nam", Region = "Lào Cai", Area = "MienBac",
                    Highlights = "Trải nghiệm xe giường nằm Hà Nội - Sa Pa, khám phá bản làng Cát Cát của người H'Mông, check-in Nhà thờ Đá, dạo chợ đêm Sa Pa.",
                    PolicyIncludes = "Xe giường nằm khứ hồi Hà Nội - Sa Pa\n1 đêm khách sạn 3* trung tâm\n1 bữa sáng buffet, 2 bữa trưa, 1 bữa tối\nVé tham quan bản Cát Cát\nHướng dẫn viên",
                    PolicyExcludes = "Đồ uống, chi phí cá nhân\nVé cáp treo Fansipan (tùy chọn)\nTip cho HDV",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Hà Nội - Sa Pa - Bản Cát Cát", Description = "Sáng sớm (hoặc tối hôm trước) đi xe giường nằm từ Hà Nội. Đến Sa Pa, ăn trưa, nhận phòng. Chiều đi bộ tham quan bản Cát Cát. Ăn tối, dạo chợ đêm." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Sa Pa Tự do (Fansipan) - Hà Nội", Description = "Ăn sáng. Tự do (Tùy chọn: Chinh phục Fansipan bằng cáp treo). Ăn trưa, trả phòng. Chiều lên xe giường nằm về Hà Nội. Tối về đến Hà Nội." }
                    }
                },

                // --- TOUR 29: THÁI LAN (MIỀN BẮC) ---
                new TourPackage
                {
                    Title = "Khám phá miền Bắc Thái Lan: Chiang Mai - Chiang Rai 5N4Đ",
                    ImageUrl = "https://www.agoda.com/wp-content/uploads/2024/08/Pai-Mae-Hong-Son-featured-1-1244x700.jpg",
                    Price = 13990000, Currency = "VND", Duration = "5N4Đ", Country = "Thái Lan", Region = "Chiang Mai", Area = "NuocNgoai",
                    Highlights = "Tham quan Chùa Trắng Wat Rong Khun (Chiang Rai), Chùa Xanh, Tam Giác Vàng, Chùa Phrathat Doi Suthep (Chiang Mai), Làng Cổ dài.",
                    PolicyIncludes = "Vé máy bay khứ hồi (SGN/HAN - CNX)\n4 đêm khách sạn 4*\nCác bữa ăn theo chương trình\nXe du lịch, vé tham quan các điểm\nHướng dẫn viên tiếng Việt",
                    PolicyExcludes = "Chi phí cá nhân, giặt ủi\nTip cho HDV và tài xế (khoảng 5 USD/người/ngày)",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Việt Nam - Chiang Mai", Description = "Bay đến sân bay Chiang Mai (CNX). Xe đón về khách sạn. Tối tự do khám phá chợ đêm." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Chiang Mai - Chiang Rai - Tam Giác Vàng", Description = "Di chuyển đến Chiang Rai. Tham quan khu Tam Giác Vàng (biên giới 3 nước). Thăm Chùa Trắng Wat Rong Khun. Nghỉ đêm tại Chiang Rai." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Chiang Rai - Chùa Xanh - Về Chiang Mai", Description = "Tham quan Chùa Xanh Wat Rong Suea Ten. Khởi hành về Chiang Mai. Tối tự do." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Chiang Mai - Doi Suthep - Làng Cổ", Description = "Viếng chùa Phrathat Doi Suthep trên núi. Tham quan Làng Cổ dài, trung tâm thủ công mỹ nghệ. Tối ăn tối Kantoke truyền thống." },
                        new DailyItinerary { DayNumber = 5, Title = "Ngày 5: Chiang Mai - Về Việt Nam", Description = "Ăn sáng, trả phòng. Xe đưa ra sân bay Chiang Mai về Việt Nam." }
                    }
                },

                // --- TOUR 30: MIỀN TÂY (NGẮN NGÀY) ---
                new TourPackage
                {
                    Title = "Miền Tây 2N1Đ: Mỹ Tho - Bến Tre - Cần Thơ",
                    ImageUrl = "https://static.hotdeal.vn/images/1675/1674808/60x60/364618-tour-mien-tay-2n1d-my-tho-ben-tre-can-tho.jpg",
                    Price = 2350000, Currency = "VND", Duration = "2N1Đ", Country = "Việt Nam", Region = "Tiền Giang, Bến Tre, Cần Thơ", Area = "MienNam",
                    Highlights = "Đi thuyền Cồn Thới Sơn (Mỹ Tho), nghe đờn ca tài tử, tham quan lò kẹo dừa Bến Tre, khám phá Chợ nổi Cái Răng (Cần Thơ).",
                    PolicyIncludes = "Xe du lịch đời mới TP.HCM - Miền Tây\n1 đêm khách sạn 4* tại Cần Thơ\nCác bữa ăn (1 sáng, 2 trưa, 1 tối)\nThuyền tham quan Mỹ Tho - Bến Tre, thuyền chợ nổi Cái Răng\nVé tham quan",
                    PolicyExcludes = "Chi phí cá nhân, đồ uống\nĂn uống trên chợ nổi",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: TP.HCM - Mỹ Tho - Bến Tre - Cần Thơ", Description = "Xe đón đi Tiền Giang. Tham quan chùa Vĩnh Tràng. Đi thuyền Cồn Thới Sơn, Cồn Phụng, nghe đờn ca, ăn trái cây, thăm lò kẹo dừa. Ăn trưa. Di chuyển đến Cần Thơ. Nhận phòng, ăn tối." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Chợ nổi Cái Răng - TP.HCM", Description = "Sáng sớm đi thuyền tham quan Chợ nổi Cái Răng. Tham quan lò hủ tiếu. Về khách sạn ăn sáng, trả phòng. Tham quan Thiền viện Trúc Lâm Phương Nam. Ăn trưa. Khởi hành về TP.HCM." }
                    }
                },

                // --- TOUR 31: MỘC CHÂU ---
                new TourPackage
                {
                    Title = "Mộc Châu 2N1Đ - Đồi chè Trái Tim, Thác Dải Yếm",
                    ImageUrl = "https://sinhtour.vn/wp-content/uploads/2024/06/moc-chau-thang-7-4.jpg",
                    Price = 2090000, Currency = "VND", Duration = "2N1Đ", Country = "Việt Nam", Region = "Sơn La", Area = "MienBac",
                    Highlights = "Cảm nhận không khí cao nguyên trong lành (lấy cảm hứng từ các tour miền núi). Check-in Đồi chè Trái Tim, Thác Dải Yếm, Rừng thông Bản Áng.",
                    PolicyIncludes = "Xe du lịch Hà Nội - Mộc Châu\n1 đêm khách sạn 3 sao\nCác bữa ăn (1 sáng, 2 trưa, 1 tối)\nVé tham quan các điểm\nHướng dẫn viên",
                    PolicyExcludes = "Đồ uống, chi phí cá nhân\nCầu kính Bạch Long (tùy chọn)",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Hà Nội - Mộc Châu - Đồi chè", Description = "Sáng xe đón tại Hà Nội. Ăn trưa. Chiều tham quan Đồi chè Trái Tim, Rừng thông Bản Áng. Ăn tối, nhận phòng." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Thác Dải Yếm - Hà Nội", Description = "Ăn sáng. Tham quan Thác Dải Yếm. Tùy chọn: Trải nghiệm Cầu kính Bạch Long. Ăn trưa. Khởi hành về Hà Nội." }
                    }
                },

                // --- TOUR 32: NHA TRANG (VINWONDERS) ---
                new TourPackage
                {
                    Title = "Nha Trang 3N2Đ - Vui chơi VinWonders",
                    ImageUrl = "https://wallpaperaccess.com/full/9444551.jpg",
                    Price = 4990000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Khánh Hòa", Area = "MienTrung",
                    Highlights = "Trải nghiệm tour biển đảo. Vui chơi không giới hạn tại VinWonders Nha Trang, tham quan Tháp Bà Ponagar, Hòn Chồng.",
                    PolicyIncludes = "Vé máy bay khứ hồi (SGN/HAN - CXR)\n2 đêm khách sạn 4* trung tâm\nVé cáp treo và vé vào cổng VinWonders (1 ngày)\nXe đưa đón sân bay Cam Ranh\n2 bữa sáng buffet",
                    PolicyExcludes = "Các bữa trưa và tối\nChi phí tắm bùn\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Đến Nha Trang - Tháp Bà", Description = "Bay đến Cam Ranh (CXR), xe đón về TP Nha Trang. Chiều tham quan Tháp Bà Ponagar, Hòn Chồng. Tối dạo chợ đêm." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Vui chơi VinWonders", Description = "Cả ngày đi cáp treo vượt biển, vui chơi tại công viên giải trí VinWonders trên đảo Hòn Tre." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Chợ Đầm - Tạm biệt", Description = "Ăn sáng, mua sắm đặc sản tại Chợ Đầm. Trả phòng, xe đưa ra sân bay Cam Ranh." }
                    }
                },

                // --- TOUR 33: PHAN THIẾT - MŨI NÉ ---
                new TourPackage
                {
                    Title = "Phan Thiết - Mũi Né 2N1Đ - Đồi Cát Trắng (Bàu Trắng)",
                    ImageUrl = "https://www.tnktravel.com/wp-content/uploads/2017/03/phan-thiet-travel-guide.jpg",
                    Price = 2450000, Currency = "VND", Duration = "2N1Đ", Country = "Việt Nam", Region = "Bình Thuận", Area = "MienTrung",
                    Highlights = "Trải nghiệm xe jeep tại Đồi Cát Trắng (Bàu Trắng), trượt cát tại Đồi Cát Bay (Đỏ), tham quan Suối Tiên, Làng chài Mũi Né.",
                    PolicyIncludes = "Xe du lịch TP.HCM - Phan Thiết\n1 đêm resort 4* tại Mũi Né\nCác bữa ăn (1 sáng, 2 trưa, 1 tối)\nVé xe jeep tham quan Bàu Trắng\nVé vào cổng Suối Tiên",
                    PolicyExcludes = "Đồ uống, chi phí cá nhân\nXe mô tô địa hình tại đồi cát",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: TP.HCM - Mũi Né - Suối Tiên", Description = "Xe đón tại TP.HCM. Đến Mũi Né, ăn trưa, nhận phòng. Chiều tham quan Suối Tiên, Làng chài Mũi Né, Đồi Cát Bay ngắm hoàng hôn." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Bàu Trắng - Về TP.HCM", Description = "Sáng sớm đi xe jeep khám phá Bàu Trắng (Đồi Cát Trắng). Về resort ăn sáng, trả phòng. Khởi hành về TP.HCM, ăn trưa trên đường." }
                    }
                },

                // --- TOUR 34: CAMPUCHIA ---
                new TourPackage
                {
                    Title = "Huyền bí Angkor: Siem Reap 4N3Đ (Angkor Wat & Thom)",
                    ImageUrl = "https://dhtravel.com.vn/upload/product/angkor-wat-angkor-wat-1-3618.jpg",
                    Price = 9500000, Currency = "VND", Duration = "4N3Đ", Country = "Campuchia", Region = "Siem Reap", Area = "NuocNgoai",
                    Highlights = "Ngắm bình minh tại Angkor Wat, khám phá đền Bayon (Angkor Thom) với những gương mặt bí ẩn, Đền Ta Prohm (phim 'Bí mật ngôi mộ cổ'), Biển Hồ Tonle Sap.",
                    PolicyIncludes = "Vé máy bay khứ hồi (SGN - REP)\n3 đêm khách sạn 4*\nCác bữa ăn (bao gồm 1 bữa tối buffet Apsara)\nVé tham quan quần thể Angkor (2 ngày)\nXe du lịch, Hướng dẫn viên",
                    PolicyExcludes = "Tip cho HDV và tài xế (khoảng 4 USD/người/ngày)\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: TP.HCM - Siem Reap", Description = "Bay đến Siem Reap (REP). Xe đón về khách sạn. Tối dạo Phố Tây (Pub Street)." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Angkor Thom - Ta Prohm", Description = "Tham quan cổng Nam Angkor Thom, đền Bayon 4 mặt, Đền Ta Prohm với rễ cây cổ thụ bao trùm." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Angkor Wat - Biển Hồ", Description = "Sáng sớm ngắm bình minh tại Angkor Wat. Chiều du thuyền trên Biển Hồ Tonle Sap, thăm làng nổi. Tối ăn buffet và xem múa Apsara." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Chợ Cũ - Về TP.HCM", Description = "Mua sắm tại Chợ Cũ (Old Market). Xe đưa ra sân bay về TP.HCM." }
                    }
                },

                // --- TOUR 35: Ý - THỤY SĨ ---
                new TourPackage
                {
                    Title = "Hành trình Ý - Thụy Sĩ: Rome - Venice - Đỉnh Titlis 9N8Đ",
                    ImageUrl = "https://duhanhviet.com.vn/wp-content/uploads/2024/11/Titlis-Mt-1-scaled.jpg",
                    Price = 76990000, Currency = "VND", Duration = "9N8Đ", Country = "Ý, Thụy Sĩ", Region = "Châu Âu", Area = "NuocNgoai",
                    Highlights = "Tham quan Đấu trường Colosseum (Rome), Tháp nghiêng Pisa, Du thuyền Gondola tại Venice, Cáp treo xoay 360 độ lên đỉnh núi tuyết Titlis (Thụy Sĩ).",
                    PolicyIncludes = "Vé máy bay khứ hồi quốc tế\nKhách sạn 4* tại Châu Âu\nCác bữa ăn theo chương trình\nVé tham quan (Colosseum, Titlis)\nVisa Schengen, Bảo hiểm du lịch",
                    PolicyExcludes = "Tiền tip (khoảng 8 EUR/người/ngày)\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Việt Nam - Bay đến Rome", Description = "Bay đêm đến Rome (Ý)." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Rome - Vatican", Description = "Tham quan Đấu trường Colosseum (bên trong), Đài phun nước Trevi. Chiều tham quan Tòa thánh Vatican." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Rome - Pisa - Florence", Description = "Di chuyển đến Pisa, tham quan Tháp nghiêng Pisa. Chiều đến Florence, tham quan cầu Ponte Vecchio." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Florence - Venice", Description = "Di chuyển đến Venice. Đi thuyền ra đảo San Marco, tham quan Cung điện Doge, trải nghiệm thuyền Gondola (tùy chọn)." },
                        new DailyItinerary { DayNumber = 5, Title = "Ngày 5: Venice - Lucerne (Thụy Sĩ)", Description = "Khởi hành đi Lucerne (Thụy Sĩ). Tham quan Cầu gỗ Chapel, Tượng đài Sư tử." },
                        new DailyItinerary { DayNumber = 6, Title = "Ngày 6: Đỉnh Titlis", Description = "Trải nghiệm cáp treo xoay 360 độ lên đỉnh Titlis, ngắm sông băng. Chiều về lại Lucerne." },
                        new DailyItinerary { DayNumber = 7, Title = "Ngày 7: Lucerne - Zurich - Sân bay", Description = "Tham quan thành phố Zurich. Chiều xe đưa ra sân bay (ZRH) làm thủ tục." },
                        new DailyItinerary { DayNumber = 8, Title = "Ngày 8: Trên máy bay", Description = "Bay về Việt Nam." },
                        new DailyItinerary { DayNumber = 9, Title = "Ngày 9: Đến Việt Nam", Description = "Về đến sân bay Việt Nam. Kết thúc tour." }
                    }
                },

                // --- TOUR 36: ÚC ---
                new TourPackage
                {
                    Title = "Khám phá Úc: Sydney - Melbourne 7N6Đ",
                    ImageUrl = "https://www.bambooairways.com/documents/d/global/canh-dep-nuoc-uc-1-jpg",
                    Price = 49900000, Currency = "VND", Duration = "7N6Đ", Country = "Úc", Region = "Sydney, Melbourne", Area = "NuocNgoai",
                    Highlights = "Check-in Nhà hát Opera Sydney (Con Sò), Cầu cảng Sydney, Bãi biển Bondi. Khám phá Con đường Great Ocean Road (Melbourne), Ga Flinders Street.",
                    PolicyIncludes = "Vé máy bay khứ hồi (Việt Nam - Úc)\nVé máy bay nội địa (Sydney - Melbourne)\nKhách sạn 4*, Các bữa ăn\nVisa Úc, Bảo hiểm du lịch",
                    PolicyExcludes = "Tiền tip (khoảng 8 AUD/người/ngày)\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Bay đến Sydney", Description = "Bay đêm đến Sydney, Úc." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Sydney - Opera House", Description = "Đến Sydney. Tham quan Nhà hát Opera Sydney, Cầu cảng Harbour, Ghế Bà Macquarie. Tối tự do." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Sydney - Bãi biển Bondi", Description = "Tham quan Bãi biển Bondi. Tự do mua sắm. Tối du thuyền Vịnh Sydney (tùy chọn)." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Sydney - Bay đi Melbourne", Description = "Đáp chuyến bay nội địa đi Melbourne. Chiều tham quan Ga Flinders Street, Sông Yarra." },
                        new DailyItinerary { DayNumber = 5, Title = "Ngày 5: Great Ocean Road", Description = "Cả ngày tham quan Con đường Great Ocean Road, ngắm 12 vị Tông đồ." },
                        new DailyItinerary { DayNumber = 6, Title = "Ngày 6: Melbourne - Sân bay", Description = "Tham quan Vườn bách thảo Fitzroy. Tự do đến giờ ra sân bay." },
                        new DailyItinerary { DayNumber = 7, Title = "Ngày 7: Về đến Việt Nam", Description = "Về đến Việt Nam. Kết thúc tour." }
                    }
                },

                // --- TOUR 37: MỸ (BỜ TÂY) ---
                new TourPackage
                {
                    Title = "Bờ Tây Hoa Kỳ: Los Angeles - Las Vegas - Grand Canyon 8N7Đ",
                    ImageUrl = "https://viettourist.com/resources/images/HOA%20KY/botay-nuocmy/Grand-Canyon14.jpg",
                    Price = 69900000, Currency = "VND", Duration = "8N7Đ", Country = "Hoa Kỳ", Region = "California, Nevada", Area = "NuocNgoai",
                    Highlights = "Đại lộ Danh vọng (Hollywood), Phim trường Universal Studios, Khám phá Las Vegas Strip, Đập Hoover Dam, Vực sâu Grand Canyon (West Rim).",
                    PolicyIncludes = "Vé máy bay khứ hồi\nKhách sạn 3-4*, Các bữa ăn\nVé tham quan Universal Studios, Vé Grand Canyon\nVisa Hoa Kỳ (phí phỏng vấn), Bảo hiểm du lịch",
                    PolicyExcludes = "Tiền tip (khoảng 10 USD/người/ngày)\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Việt Nam - Bay đến Los Angeles (LA)", Description = "Bay đến LA (vượt tuyến đổi ngày)." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Tham quan Los Angeles", Description = "Đến LA. Tham quan Đại lộ Danh vọng, Nhà hát Dolby, Beverly Hills. Nhận phòng." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Universal Studios Hollywood", Description = "Cả ngày vui chơi tại Phim trường Universal Studios." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: LA - Las Vegas", Description = "Khởi hành đi Las Vegas. Trên đường tham quan outlet mua sắm. Tối tham quan Las Vegas Strip." },
                        new DailyItinerary { DayNumber = 5, Title = "Ngày 5: Grand Canyon - Đập Hoover Dam", Description = "Tham quan Vực sâu Grand Canyon (West Rim, tùy chọn Skywalk). Trên đường về ghé Đập Hoover Dam." },
                        new DailyItinerary { DayNumber = 6, Title = "Ngày 6: Las Vegas - Sân bay LA", Description = "Sáng tự do. Trưa khởi hành về lại sân bay LAX." },
                        new DailyItinerary { DayNumber = 7, Title = "Ngày 7: Trên máy bay", Description = "Bay về Việt Nam." },
                        new DailyItinerary { DayNumber = 8, Title = "Ngày 8: Đến Việt Nam", Description = "Về đến Việt Nam. Kết thúc tour." }
                    }
                },

                // --- TOUR 38: HẠ LONG (TÀU 4 SAO) ---
                new TourPackage
                {
                    Title = "Du thuyền Hạ Long 4 sao (2N1Đ) - Tàu Pelican",
                    ImageUrl = "https://motortrip.vn/wp-content/uploads/2022/06/du-thuyen-4-sao-ha-long-1.jpg",
                    Price = 2390000, Currency = "VND", Duration = "2N1Đ", Country = "Việt Nam", Region = "Quảng Ninh", Area = "MienBac",
                    Highlights = "Trải nghiệm ngủ đêm trên Vịnh với du thuyền 4 sao, tham quan Hang Sửng Sốt, chèo kayak tại Hang Luồn, tắm biển đảo Titop.",
                    PolicyIncludes = "1 đêm nghỉ du thuyền 4*\nCác bữa ăn (1 sáng, 2 trưa, 1 tối)\nVé tham quan Vịnh, vé Hang Sửng Sốt\nThuyền kayak, lớp học nấu ăn/Thái Cực Quyền",
                    PolicyExcludes = "Xe đưa đón Hà Nội - Hạ Long (có thể đặt thêm)\nĐồ uống, Dịch vụ spa\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Hạ Long - Hang Sửng Sốt - Kayak", Description = "Trưa lên du thuyền tại cảng Tuần Châu. Ăn trưa. Chiều tham quan Hang Sửng Sốt, chèo kayak tại Hang Luồn. Ăn tối, câu mực đêm." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Đảo Titop - Hà Nội", Description = "Sáng tập Thái Cực Quyền. Thăm đảo Titop, leo núi hoặc tắm biển. Ăn sáng/trưa sớm. Trả phòng, về bến Tuần Châu." }
                    }
                },

                // --- TOUR 39: CÔN ĐẢO (TÂM LINH) ---
                new TourPackage
                {
                    Title = "Côn Đảo 3N2Đ - Viếng mộ Cô Sáu (Tàu cao tốc)",
                    ImageUrl = "https://www.vietnamdragontravel.vn/wp-content/uploads/2025/03/dia-diem-du-lich-tam-linh-con-dao-5.jpg",
                    Price = 4300000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Bà Rịa - Vũng Tàu", Area = "MienNam",
                    Highlights = "Viếng mộ cô Võ Thị Sáu tại nghĩa trang Hàng Dương, tham quan hệ thống Nhà tù Côn Đảo, Chuồng Cọp, Chùa Vân Sơn.",
                    PolicyIncludes = "Xe đưa đón TP.HCM - Cảng Trần Đề (hoặc Vũng Tàu)\nTàu cao tốc khứ hồi ra Côn Đảo\n2 đêm khách sạn tại Côn Đảo\nXe tham quan trên đảo, 2 bữa sáng",
                    PolicyExcludes = "Các bữa trưa và tối\nVé tham quan các di tích\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: TP.HCM - Cảng - Côn Đảo", Description = "Sáng sớm xe đón đi cảng (Trần Đề/Vũng Tàu). Lên tàu cao tốc đi Côn Đảo. Xe đón về khách sạn. Chiều tham quan Chùa Vân Sơn, Dinh Chúa Đảo." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Di tích Nhà tù - Viếng Mộ", Description = "Sáng tham quan Nhà tù Côn Đảo, Trại Phú Hải, Chuồng cọp. Chiều tham quan bãi Đầm Trầu. Tối tự do viếng mộ cô Sáu tại Nghĩa trang Hàng Dương." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Chợ Côn Đảo - Về đất liền", Description = "Ăn sáng, mua sắm tại chợ Côn Đảo. Trả phòng, lên tàu cao tốc về đất liền. Xe đón về lại TP.HCM." }
                    }
                },

                // --- TOUR 40: BALI (FREE & EASY) ---
                new TourPackage
                {
                    Title = "Bali Free & Easy 4N3Đ (Vé máy bay + Khách sạn)",
                    ImageUrl = "https://mutiarabalicollections.com/wp-content/uploads/2022/08/one-bedroom-penestanan1-1568x847.jpg",
                    Price = 7990000, Currency = "VND", Duration = "4N3Đ", Country = "Indonesia", Region = "Bali", Area = "NuocNgoai",
                    Highlights = "Gói linh hoạt bao gồm Vé máy bay và Khách sạn. Tự do khám phá Ubud, Kuta, Đền Uluwatu, hoặc các bãi biển.",
                    PolicyIncludes = "Vé máy bay khứ hồi (SGN/HAN - DPS)\n3 đêm khách sạn 4* (tùy chọn khu vực Kuta/Ubud)\nXe đưa đón sân bay tại Bali\n3 bữa sáng buffet tại khách sạn",
                    PolicyExcludes = "Các bữa trưa và tối\nVé tham quan\nTour và hướng dẫn viên\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Ngày 1: Bay đến Bali", Description = "Bay đến sân bay Denpasar (DPS). Xe đón về khách sạn. Tự do khám phá." },
                        new DailyItinerary { DayNumber = 2, Title = "Ngày 2: Tự do khám phá Bali", Description = "Tự do. Gợi ý: Tham quan khu Ubud (Ruộng bậc thang, Monkey Forest) hoặc đi về phía Nam (Đền Uluwatu)." },
                        new DailyItinerary { DayNumber = 3, Title = "Ngày 3: Tự do khám phá Bali", Description = "Tự do. Gợi ý: Mua sắm tại Kuta, tắm biển Seminyak hoặc tham gia tour đảo Nusa Penida (tự túc)." },
                        new DailyItinerary { DayNumber = 4, Title = "Ngày 4: Tạm biệt Bali", Description = "Ăn sáng. Tự do đến giờ xe đưa ra sân bay." }
                    }
                },
                // --- TOUR 41: MĂNG ĐEN (KON TUM) - "ĐÀ LẠT THỨ 2" ---
                new TourPackage
                {
                    Title = "Về miền sơn cước: Măng Đen - Kon Tum - Gia Lai",
                    ImageUrl = "https://images.vietnamtourism.gov.vn/vn/images/2016/anhInternet/00mang-den-1.jpg",
                    Price = 4990000, Currency = "VND", Duration = "4N3Đ", Country = "Việt Nam", Region = "Kon Tum", Area = "MienTrung",
                    Highlights = "Săn mây tại Măng Đen, check-in Hồ Đak Ke, Thác Pa Sỹ, viếng Đức Mẹ Măng Đen, tham quan Biển Hồ Pleiku (Đôi mắt Pleiku).",
                    PolicyIncludes = "Xe du lịch đời mới đưa đón sân bay Pleiku và tham quan\n3 đêm khách sạn 3-4* (2 đêm Măng Đen, 1 đêm Pleiku)\nCác bữa ăn đặc sản Tây Nguyên (Gà nướng, Cơm lam)\nVé tham quan các điểm",
                    PolicyExcludes = "Vé máy bay khứ hồi\nChi phí cá nhân, rượu bia\nTiền tip cho lái xe và HDV",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Đến Pleiku - Măng Đen", Description = "Xe đón tại sân bay Pleiku. Khởi hành đi Măng Đen (Kon Tum). Tận hưởng không khí se lạnh giữa rừng thông. Nhận phòng khách sạn." },
                        new DailyItinerary { DayNumber = 2, Title = "Khám phá Măng Đen", Description = "Tham quan Hồ Đak Ke, Thác Pa Sỹ. Chiều viếng Tượng Đức Mẹ Măng Đen linh thiêng. Thưởng thức đặc sản Gà nướng cơm lam." },
                        new DailyItinerary { DayNumber = 3, Title = "Măng Đen - Kon Tum - Pleiku", Description = "Về lại TP Kon Tum, thăm Nhà thờ Gỗ hơn 100 tuổi, Cầu treo Kon Klor. Chiều về Pleiku, tham quan Biển Hồ T'Nưng." },
                        new DailyItinerary { DayNumber = 4, Title = "Tạm biệt Tây Nguyên", Description = "Ăn sáng, thưởng thức cafe Pleiku. Mua sắm đặc sản Bò một nắng. Xe đưa ra sân bay Pleiku." }
                    }
                },

                // --- TOUR 42: QUẢNG BÌNH - QUẢNG TRỊ ---
                new TourPackage
                {
                    Title = "Hành trình Di sản: Quảng Bình - Động Phong Nha - Vĩ tuyến 17",
                    ImageUrl = "https://bizweb.dktcdn.net/thumb/1024x1024/100/366/808/products/quang-binh-2.jpg?v=1573879888923",
                    Price = 5200000, Currency = "VND", Duration = "4N3Đ", Country = "Việt Nam", Region = "Quảng Bình", Area = "MienTrung",
                    Highlights = "Khám phá Động Phong Nha/Thiên Đường, viếng Mộ Đại tướng Võ Nguyên Giáp, tham quan Thành cổ Quảng Trị, Địa đạo Vịnh Mốc.",
                    PolicyIncludes = "Xe đón tiễn sân bay Đồng Hới và tham quan\n3 đêm khách sạn 4* sát biển Nhật Lệ\nThuyền tham quan Động Phong Nha\nHương hoa viếng các điểm tâm linh\nCác bữa ăn chính",
                    PolicyExcludes = "Vé máy bay/tàu hỏa đến Đồng Hới\nĐồ uống, chi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Đón khách - Vũng Chùa", Description = "Đón tại sân bay Đồng Hới. Đi Vũng Chùa - Đảo Yến viếng mộ Đại tướng (nếu mở cửa) hoặc ngắm cảnh Đèo Ngang. Tắm biển Nhật Lệ." },
                        new DailyItinerary { DayNumber = 2, Title = "Động Phong Nha - Động Thiên Đường", Description = "Sáng đi thuyền ngược dòng sông Son thăm Động Phong Nha. Chiều khám phá Động Thiên Đường - hoàng cung trong lòng đất." },
                        new DailyItinerary { DayNumber = 3, Title = "Quảng Trị - Ký ức hào hùng", Description = "Khởi hành đi Quảng Trị. Thăm Cầu Hiền Lương, Sông Bến Hải (Vĩ tuyến 17), Thành cổ Quảng Trị, Địa đạo Vịnh Mốc." },
                        new DailyItinerary { DayNumber = 4, Title = "Đồi Cát Quang Phú - Tiễn khách", Description = "Sáng trượt cát tại Đồi cát Quang Phú. Mua sắm đặc sản Khoai gieo. Tiễn ra sân bay/ga Đồng Hới." }
                    }
                },

                // --- TOUR 43: CÁT BÀ - LAN HẠ ---
                new TourPackage
                {
                    Title = "Hải Phòng: Đảo Ngọc Cát Bà - Vịnh Lan Hạ 3N2Đ",
                    ImageUrl = "https://cafefcdn.com/2020/1/30/photo-1-15803789206371731421208.jpg",
                    Price = 3850000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Hải Phòng", Area = "MienBac",
                    Highlights = "Du thuyền thăm Vịnh Lan Hạ (top vịnh đẹp nhất thế giới), chèo Kayak, tắm biển bãi Cát Cò, khám phá Vườn quốc gia Cát Bà.",
                    PolicyIncludes = "Xe Hà Nội - Hải Phòng - Cát Bà khứ hồi\n2 đêm khách sạn 3* tại thị trấn Cát Bà\nTàu thăm Vịnh Lan Hạ\nCác bữa ăn hải sản",
                    PolicyExcludes = "Cáp treo Cát Hải (tùy chọn)\nĐồ uống, chi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Hà Nội - Cát Bà", Description = "Xe đón tại Hà Nội đi Hải Phòng, qua cầu vượt biển Tân Vũ. Đi phà/cáp treo sang đảo Cát Bà. Tắm biển Cát Cò 1, 2, 3." },
                        new DailyItinerary { DayNumber = 2, Title = "Vịnh Lan Hạ - Đảo Khỉ", Description = "Lên tàu thăm Vịnh Lan Hạ, chèo kayak qua Hang Sáng - Hang Tối. Ghé Đảo Khỉ tắm biển. Tối tự do dạo phố biển." },
                        new DailyItinerary { DayNumber = 3, Title = "Vườn Quốc Gia - Hà Nội", Description = "Thăm Vườn quốc gia Cát Bà, Động Trung Trang. Ăn trưa. Lên xe về lại Hà Nội." }
                    }
                },

                // --- TOUR 44: MÙ CANG CHẢI (MÙA LÚA CHÍN) ---
                new TourPackage
                {
                    Title = "Sắc vàng Tây Bắc: Mù Cang Chải - Tú Lệ 3N2Đ",
                    ImageUrl = "https://vj-prod-website-cms.s3.ap-southeast-1.amazonaws.com/shutterstock2246073829-1701309980693.jpg",
                    Price = 2650000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Yên Bái", Area = "MienBac",
                    Highlights = "Chiêm ngưỡng ruộng bậc thang Mù Cang Chải (Di sản quốc gia), Đèo Khau Phạ, thưởng thức cốm Tú Lệ, tắm khoáng nóng Ngọc Chiến.",
                    PolicyIncludes = "Xe ô tô du lịch đời mới Hà Nội - Yên Bái\n2 đêm homestay/nhà nghỉ cộng đồng\nCác bữa ăn mang đậm bản sắc Tây Bắc\nVé tham quan các điểm",
                    PolicyExcludes = "Xe ôm lên đồi Mâm Xôi (nếu cần)\nChi phí tắm khoáng nóng",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Hà Nội - Nghĩa Lộ", Description = "Khởi hành đi Yên Bái. Thăm đồi chè Thanh Sơn. Dừng chân tại cánh đồng Mường Lò. Nghỉ đêm tại Nghĩa Lộ." },
                        new DailyItinerary { DayNumber = 2, Title = "Đèo Khau Phạ - Mù Cang Chải", Description = "Chinh phục đèo Khau Phạ. Check-in Đồi Mâm Xôi, Đồi Móng Ngựa vào mùa lúa chín. Thưởng thức cốm nếp Tú Lệ." },
                        new DailyItinerary { DayNumber = 3, Title = "Bản Lướt - Hà Nội", Description = "Khám phá bản Lướt (Ngọc Chiến), tắm khoáng nóng thư giãn. Ăn trưa. Khởi hành về Hà Nội." }
                    }
                },

                // --- TOUR 45: ĐẢO NAM DU (KIÊN GIANG) ---
                new TourPackage
                {
                    Title = "Khám phá Đảo Nam Du - Hạ Long phương Nam",
                    ImageUrl = "https://cdn3.ivivu.com/2022/11/Du-l%E1%BB%8Bch-%C4%91%E1%BA%A3o-Nam-Du-ivivu.jpg",
                    Price = 3290000, Currency = "VND", Duration = "2N2Đ", Country = "Việt Nam", Region = "Kiên Giang", Area = "MienNam",
                    Highlights = "Lặn ngắm san hô tại Hòn Hai Bờ Đập, tắm biển Bãi Cây Mến đẹp nhất đảo, check-in Hải đăng Nam Du, thưởng thức tiệc BBQ hải sản.",
                    PolicyIncludes = "Xe giường nằm TP.HCM - Rạch Giá\nTàu cao tốc khứ hồi Rạch Giá - Nam Du\nNhà nghỉ máy lạnh tại đảo\nTàu tham quan quanh đảo, lặn san hô\nTiệc BBQ hải sản",
                    PolicyExcludes = "Xe máy đi quanh đảo\nNước ngọt, bia trong bữa ăn",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "TP.HCM - Rạch Giá (Đêm)", Description = "22h00 xe giường nằm đón khách tại TP.HCM khởi hành đi Rạch Giá (ngủ đêm trên xe)." },
                        new DailyItinerary { DayNumber = 2, Title = "Khám phá Quần đảo Nam Du", Description = "Sáng lên tàu cao tốc ra đảo. Nhận phòng. Chiều đi tàu tham quan Hòn Mấu, lặn san hô Hòn Hai Bờ Đập. Tối ăn BBQ hải sản." },
                        new DailyItinerary { DayNumber = 3, Title = "Hải đăng - Bãi Cây Mến - TP.HCM", Description = "Sáng thuê xe máy tham quan Hải đăng Nam Du, tắm biển Bãi Cây Mến. Trưa lên tàu về Rạch Giá. Xe đón về TP.HCM." }
                    }
                },

                // --- TOUR 46: LỤC TỈNH MIỀN TÂY (DÀI NGÀY) ---
                new TourPackage
                {
                    Title = "Lục tỉnh Miền Tây: Cà Mau - Bạc Liêu - Sóc Trăng",
                    ImageUrl = "https://owa.bestprice.vn/images/tours/large/can-tho-soc-trang-ca-mau-bac-lieu-2n1d-6673a5f357631-848x477.jpg",
                    Price = 4550000, Currency = "VND", Duration = "4N3Đ", Country = "Việt Nam", Region = "Cà Mau, Bạc Liêu", Area = "MienNam",
                    Highlights = "Check-in Cột mốc Đất Mũi Cà Mau (cực Nam tổ quốc), Nhà công tử Bạc Liêu, Cánh đồng điện gió, Chùa Dơi Sóc Trăng.",
                    PolicyIncludes = "Xe du lịch đời mới suốt tuyến từ Sài Gòn\n3 đêm khách sạn 3-4* (Cần Thơ, Cà Mau)\nVé tham quan Đất Mũi, xe điện\nCác bữa ăn đặc sản miền Tây",
                    PolicyExcludes = "Chi phí cá nhân\nĐồ uống",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Sài Gòn - Mỹ Tho - Cần Thơ", Description = "Thăm cồn Thới Sơn, nghe đờn ca tài tử. Về Cần Thơ, du thuyền bến Ninh Kiều." },
                        new DailyItinerary { DayNumber = 2, Title = "Sóc Trăng - Cà Mau", Description = "Đi Sóc Trăng thăm Chùa Dơi, Chùa Chén Kiểu. Di chuyển xuống Cà Mau. Ngủ đêm tại TP Cà Mau." },
                        new DailyItinerary { DayNumber = 3, Title = "Đất Mũi - Bạc Liêu", Description = "Chinh phục Đất Mũi Cà Mau, cột mốc GPS 0001. Chiều về Bạc Liêu thăm Nhà công tử Bạc Liêu, khu lưu niệm Cao Văn Lầu." },
                        new DailyItinerary { DayNumber = 4, Title = "Điện gió - Thiền viện - Sài Gòn", Description = "Check-in Cánh đồng điện gió Bạc Liêu. Về lại Cần Thơ thăm Thiền viện Trúc Lâm. Khởi hành về Sài Gòn." }
                    }
                },

                // --- TOUR 47: NINH BÌNH (TRÀNG AN) ---
                new TourPackage
                {
                    Title = "Tuyệt tình cốc: Ninh Bình - Tràng An - Bái Đính",
                    ImageUrl = "https://thungnham.com/wp-content/uploads/2024/03/khu-du-lich-trang-an-2.webp",
                    Price = 1950000, Currency = "VND", Duration = "2N1Đ", Country = "Việt Nam", Region = "Ninh Bình", Area = "MienBac",
                    Highlights = "Ngồi thuyền khám phá Di sản Tràng An, viếng Chùa Bái Đính (chùa lớn nhất ĐNA), check-in Hang Múa (Vạn Lý Trường Thành VN).",
                    PolicyIncludes = "Xe đưa đón Hà Nội - Ninh Bình\n1 đêm khách sạn/bungalow tại Tam Cốc\nVé đò Tràng An, vé Hang Múa, xe điện Bái Đính\nCác bữa ăn (đặc sản Dê núi)",
                    PolicyExcludes = "Đồ uống\nTiền tip",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Hà Nội - Bái Đính - Tràng An", Description = "Xe đón đi Ninh Bình. Viếng Chùa Bái Đính. Chiều ngồi đò thăm KDL Tràng An. Tối nghỉ tại Tam Cốc." },
                        new DailyItinerary { DayNumber = 2, Title = "Hang Múa - Tuyệt Tình Cốc - Hà Nội", Description = "Leo 500 bậc đá lên đỉnh Hang Múa ngắm toàn cảnh. Thăm Động Am Tiên (Tuyệt Tình Cốc). Chiều về Hà Nội." }
                    }
                },

                // --- TOUR 48: MAI CHÂU - PÙ LUÔNG ---
                new TourPackage
                {
                    Title = "Nghỉ dưỡng Pù Luông - Mai Châu mùa lúa",
                    ImageUrl = "https://maichauhideaway.com/Data/Sites/1/News/351/mai-chau-pu-luong-1.jpg",
                    Price = 3150000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Thanh Hóa, Hòa Bình", Area = "MienBac",
                    Highlights = "Check-in Pù Luông Retreat, ngắm ruộng bậc thang, guồng nước bản Hiêu, khám phá thung lũng Mai Châu yên bình.",
                    PolicyIncludes = "Xe Limousine Hà Nội - Pù Luông\n2 đêm resort/bungalow view núi\nCác bữa ăn organic tại bản\nVé tham quan, bè mảng",
                    PolicyExcludes = "Đồ uống\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Hà Nội - Mai Châu", Description = "Đến Bản Lác (Mai Châu). Đạp xe quanh bản, tìm hiểu văn hóa người Thái. Tối xem múa xòe, nhảy sạp." },
                        new DailyItinerary { DayNumber = 2, Title = "Mai Châu - Pù Luông", Description = "Di chuyển sang Khu bảo tồn thiên nhiên Pù Luông. Check-in resort. Chiều trekking bản Đôn, ngắm ruộng bậc thang." },
                        new DailyItinerary { DayNumber = 3, Title = "Guồng nước - Suối Cá Thần - Hà Nội", Description = "Thăm guồng nước, suối Chàm. Trên đường về ghé Suối Cá Thần Cẩm Lương. Về đến Hà Nội." }
                    }
                },

                // --- TOUR 49: BUÔN MA THUỘT (MÙA HOA CÀ PHÊ/DÃ QUỲ) ---
                new TourPackage
                {
                    Title = "Đại ngàn Tây Nguyên: Buôn Ma Thuột - Hồ Lắk",
                    ImageUrl = "https://bizweb.dktcdn.net/thumb/1024x1024/100/366/808/products/quang-binh-2.jpg?v=1573879888923",
                    Price = 3600000, Currency = "VND", Duration = "3N2Đ", Country = "Việt Nam", Region = "Đắk Lắk", Area = "MienTrung",
                    Highlights = "Cưỡi voi (hoặc ngắm voi) tại Bản Đôn, chèo thuyền độc mộc trên Hồ Lắk, check-in Bảo tàng Thế giới Cà phê, thác Dray Nur.",
                    PolicyIncludes = "Xe đón tiễn sân bay Buôn Ma Thuột\n2 đêm khách sạn 3-4* trung tâm\nVé tham quan Thác Dray Nur, Cầu treo\nCác bữa ăn đặc sản",
                    PolicyExcludes = "Vé máy bay đến Buôn Ma Thuột\nPhí cưỡi voi (bảo tồn động vật khuyến khích không cưỡi)",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Đón khách - Thác Dray Nur", Description = "Đón sân bay. Tham quan thác Dray Nur hùng vĩ. Tối thưởng thức bún đỏ, cà phê phố núi." },
                        new DailyItinerary { DayNumber = 2, Title = "Bản Đôn - Mộ Vua Săn Voi", Description = "Đi Buôn Đôn, tham quan nhà sàn cổ, mộ Vua săn voi, cầu treo sông Sêrêpôk." },
                        new DailyItinerary { DayNumber = 3, Title = "Hồ Lắk - Bảo tàng Cà phê", Description = "Thăm Hồ Lắk, Buôn Jun. Check-in Bảo tàng Thế giới Cà phê (kiến trúc độc đáo). Tiễn sân bay." }
                    }
                },

                // --- TOUR 50: VŨNG TÀU - HỒ TRÀM (NGHỈ DƯỠNG) ---
                new TourPackage
                {
                    Title = "Nghỉ dưỡng biển: Vũng Tàu - Hồ Tràm Resort 5*",
                    ImageUrl = "https://cdn3.ivivu.com/2023/07/Angsana-H%E1%BB%93-Tr%C3%A0m-ivivu-2.jpg",
                    Price = 4200000, Currency = "VND", Duration = "2N1Đ", Country = "Việt Nam", Region = "Bà Rịa - Vũng Tàu", Area = "MienNam",
                    Highlights = "Nghỉ dưỡng tại Resort 5 sao Hồ Tràm, tắm khoáng nóng Minera Bình Châu, check-in đồi cừu Suối Nghệ.",
                    PolicyIncludes = "Xe Limousine đưa đón từ Sài Gòn\n1 đêm tại Resort 5* (Melia/Grand Ho Tram...)\nVé tắm khoáng nóng Bình Châu\nBuffet sáng, 2 bữa chính",
                    PolicyExcludes = "Chi phí spa, bùn khoáng\nĐồ uống minibar",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Sài Gòn - Đồi Cừu - Hồ Tràm", Description = "Ghé tham quan Đồi cừu Suối Nghệ chụp ảnh. Đến Hồ Tràm nhận phòng resort 5*. Tự do tắm biển, hồ bơi vô cực." },
                        new DailyItinerary { DayNumber = 2, Title = "Khoáng nóng Bình Châu - Sài Gòn", Description = "Qua KDL Bình Châu luộc trứng, ngâm chân khoáng nóng. Ăn trưa. Khởi hành về lại Sài Gòn." }
                    }
                },
                // --- TOUR 51: SÀI GÒN CITY TOUR ---
                new TourPackage
                {
                    Title = "Hòn ngọc Viễn Đông: Sài Gòn City Tour & Waterbus",
                    ImageUrl = "https://cdn.dealtoday.vn/img/s800x400/hon-ngoc-vien-dong-5_12012024094549.jpg?sign=82kXq4y7sPm3Ai1MVJbrqA--0n5U=&type=webp",
                    Price = 890000, Currency = "VND", Duration = "1N", Country = "Việt Nam", Region = "TP. Hồ Chí Minh", Area = "MienNam",
                    Highlights = "Ngắm toàn cảnh thành phố từ xe buýt 2 tầng, trải nghiệm Saigon Waterbus trên sông Sài Gòn, thăm Dinh Độc Lập, Nhà thờ Đức Bà.",
                    PolicyIncludes = "Xe buýt 2 tầng, vé tàu Waterbus khứ hồi\nVé tham quan Dinh Độc Lập, Bảo tàng Chứng tích Chiến tranh\nĂn trưa buffet\nHướng dẫn viên",
                    PolicyExcludes = "Chi phí mua sắm, ăn uống ngoài chương trình\nTips",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Khám phá Sài Gòn Xưa & Nay", Description = "Sáng: Tham quan Dinh Độc Lập, Nhà thờ Đức Bà, Bưu điện Thành phố. Trưa: Ăn buffet. Chiều: Trải nghiệm xe buýt 2 tầng dạo quanh Quận 1. Ngắm hoàng hôn trên sông Sài Gòn bằng Waterbus." }
                    }
                },

                // --- TOUR 52: CỦ CHI - VÙNG ĐẤT THÉP ---
                new TourPackage
                {
                    Title = "Về nguồn: Địa đạo Củ Chi - Đền Bến Dược",
                    ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/6/66/Ben_Duoc_Temple_Cu_Chi_tunnel_%2839514032832%29.jpg",
                    Price = 650000, Currency = "VND", Duration = "1N", Country = "Việt Nam", Region = "TP. Hồ Chí Minh", Area = "MienNam",
                    Highlights = "Khám phá hệ thống địa đạo Củ Chi kỳ vĩ, dâng hương tại Đền Bến Dược, trải nghiệm bắn súng thể thao quốc phòng, thưởng thức khoai mì chấm muối mè.",
                    PolicyIncludes = "Xe du lịch đời mới đưa đón từ trung tâm Q1\nVé tham quan Địa đạo, Đền Bến Dược\nĂn trưa đặc sản Củ Chi (Bò tơ)\nNước suối, khăn lạnh",
                    PolicyExcludes = "Phí bắn súng (tính theo viên)\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "TP.HCM - Củ Chi - TP.HCM", Description = "Sáng: Khởi hành đi Củ Chi. Xem phim tư liệu, chui hầm địa đạo, tham quan bếp Hoàng Cầm. Viếng Đền Bến Dược. Chiều: Thử tài bắn súng (tự túc), ăn trưa bò tơ Củ Chi. Về lại trung tâm." }
                    }
                },

                // --- TOUR 53: CẦN GIỜ (SINH THÁI) ---
                new TourPackage
                {
                    Title = "Lá phổi xanh: Rừng Sác Cần Giờ - Đảo Khỉ",
                    ImageUrl = "https://cdn.tcdulichtphcm.vn/upload/2-2021/images/2021-06-20/1624188056-thumbnail-width1200height628-watermark.jpg",
                    Price = 990000, Currency = "VND", Duration = "1N", Country = "Việt Nam", Region = "TP. Hồ Chí Minh", Area = "MienNam",
                    Highlights = "Đi cano len lỏi trong rừng ngập mặn, thăm Chiến khu Rừng Sác, vương quốc Khỉ, tham quan Lăng Ông Thủy Tướng và chợ hải sản Hàng Dương.",
                    PolicyIncludes = "Xe đưa đón TP.HCM - Cần Giờ\nVé cano vào chiến khu Rừng Sác\nVé tham quan Đảo Khỉ\nĂn trưa hải sản Cần Giờ",
                    PolicyExcludes = "Chi phí mua hải sản mang về\nGhế dù tại bãi biển (nếu có)",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Sài Gòn - Cần Giờ - Đảo Khỉ", Description = "Sáng: Đến Cần Giờ. Tham quan Đảo Khỉ (Lâm Viên). Đi cano vào Chiến khu Rừng Sác xem xiếc thú. Trưa: Ăn trưa. Chiều: Viếng Lăng Ông Thủy Tướng, mua sắm tại Chợ Hàng Dương. Về lại Sài Gòn." }
                    }
                },

                // --- TOUR 54: TÂY NINH - NÚI BÀ ĐEN ---
                new TourPackage
                {
                    Title = "Chinh phục Nóc nhà Nam Bộ: Tây Ninh - Núi Bà Đen",
                    ImageUrl = "https://sakos.vn/wp-content/uploads/2024/02/304050_17-11-tay-ninh.jpg",
                    Price = 1250000, Currency = "VND", Duration = "1N", Country = "Việt Nam", Region = "Tây Ninh", Area = "MienNam",
                    Highlights = "Đi cáp treo Sun World chinh phục đỉnh Núi Bà Đen, check-in Tượng Phật Bà Tây Bổ Đà Sơn, tham quan Tòa Thánh Cao Đài uy nghi.",
                    PolicyIncludes = "Xe du lịch Sài Gòn - Tây Ninh\nVé cáp treo khứ hồi đỉnh Vân Sơn (Sun World)\nVé vào cổng KDL\nĂn trưa buffet tại KDL hoặc đặc sản Bánh canh Trảng Bàng",
                    PolicyExcludes = "Vé cáp treo tuyến Chùa Hang (nếu muốn đi thêm)\nChi phí cá nhân",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Sài Gòn - Tòa Thánh - Núi Bà", Description = "Sáng: Tham quan Tòa Thánh Tây Ninh (dự lễ cúng thời Tý nếu kịp). Trưa: Ăn đặc sản. Chiều: Lên cáp treo chinh phục đỉnh núi Bà Đen, săn mây, chiêm bái tượng Phật Bà. Về lại TP.HCM." }
                    }
                },

                // --- TOUR 55: SÀI GÒN - MIỀN TÂY (MỸ THO - BẾN TRE) ---
                new TourPackage
                {
                    Title = "Hương sắc Miền Tây: Mỹ Tho - Bến Tre (Tứ linh cồn)",
                    ImageUrl = "https://datviettour.com.vn/uploads/images/mien-tay/Ben-tre/danh-thang/4-con-tien-giang-2.jpg",
                    Price = 790000, Currency = "VND", Duration = "1N", Country = "Việt Nam", Region = "Tiền Giang, Bến Tre", Area = "MienNam",
                    Highlights = "Du ngoạn sông Tiền ngắm 4 cồn Long-Lân-Quy-Phụng, đi xe ngựa đường làng, chèo xuồng ba lá trong rạch dừa nước, thưởng thức trà mật ong và trái cây.",
                    PolicyIncludes = "Xe du lịch Sài Gòn - Mỹ Tho\nTàu lớn và xuồng chèo ba lá\nĂn trưa đặc sản (Cá tai tượng chiên xù)\nTrái cây, trà mật ong, nghe đờn ca tài tử",
                    PolicyExcludes = "Mua sắm đặc sản kẹo dừa, sữa ong chúa\nTiền tip",
                    Itineraries = new List<DailyItinerary>
                    {
                        new DailyItinerary { DayNumber = 1, Title = "Sài Gòn - Mỹ Tho - Bến Tre", Description = "Sáng: Đến bến tàu 30/4 (Mỹ Tho). Du thuyền sông Tiền. Thăm cù lao Thới Sơn: thăm trại ong, lò kẹo dừa, đi xe ngựa. Trải nghiệm xuồng ba lá. Tham quan Chùa Vĩnh Tràng. Chiều về lại Sài Gòn." }
                    }
                }

            };

            await db.TourPackages.AddRangeAsync(toursToAdd);
            await db.SaveChangesAsync();
            Console.WriteLine("Đã seed 20 TourPackage và Itinerary thành công (với ảnh đã sửa).");
        }
    }
}