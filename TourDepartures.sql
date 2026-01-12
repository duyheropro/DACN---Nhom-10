create database BookingTour
USE BookingTour;
GO

-- 1. Xóa sạch dữ liệu lịch cũ để làm lại
DELETE FROM TourDepartures;
DBCC CHECKIDENT ('TourDepartures', RESEED, 0);
GO

-- 2. CHẠY LỆNH NẠP DỮ LIỆU (Đã sửa lỗi xuống dòng)
INSERT INTO TourDepartures (
    TourPackageId, StartDate, EndDate, 
    PriceAdult, PriceChild, PriceInfant, 
    AvailableSeats, Airline, 
    FlightNumberOut, FlightNumberIn, 
    FlightTimeOut, FlightTimeIn
)sdsdsds
SELECT 
    tp.Id,
    DATEADD(day, T.OffsetDays, GETDATE()),       
    DATEADD(day, T.OffsetDays + 3, GETDATE()),   
    tp.Price,                                    
    CAST(tp.Price * 0.75 AS DECIMAL(18,2)),      
    CAST(tp.Price * 0.30 AS DECIMAL(18,2)),      
    20,                                          
    CASE WHEN T.OffsetDays % 2 = 0 THEN 'Vietnam Airlines' ELSE 'Vietjet Air' END,
    'VN' + CAST(tp.Id * 10 + 1 AS NVARCHAR(10)), 
    'VN' + CAST(tp.Id * 10 + 2 AS NVARCHAR(10)), 
    '08:00',                                     
    '16:30'                                      
FROM 
    TourPackages tp
CROSS JOIN 
    (VALUES (10), (25), (40), (55)) AS T(OffsetDays);
GO

-- 3. KIỂM TRA KẾT QUẢ (Nếu ra số > 0 là thành công)
SELECT COUNT(*) AS SoLuongLichTrinh FROM TourDepartures;
SELECT TOP 10 * FROM TourDepartures;


-- chạy test
select * from TourPackages
select * from TourDepartures
select * from TourBookings
DELETE FROM TourPackages;

DELETE FROM FlightOrders;
DELETE FROM TourBookings;
UPDATE TourDepartures
SET AvailableSeats = 20;
