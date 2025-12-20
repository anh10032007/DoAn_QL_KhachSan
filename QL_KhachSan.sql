USE master
GO

-- 1. Xóa database cũ để làm sạch hệ thống
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'QL_KhachSan')
    DROP DATABASE QL_KhachSan
GO

CREATE DATABASE QL_KhachSan
GO

USE QL_KhachSan
GO

-- ========================================
-- PHẦN A: TẠO CÁC BẢNG (TABLES)
-- ========================================

-- 1. Bảng Vai Trò (Phân quyền)
CREATE TABLE tblVaiTro (
    IDVaiTro INT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro NVARCHAR(50), -- Admin, Lễ tân, Tạp vụ
    MoTa NVARCHAR(255)
);

-- 2. Bảng Nhân Viên (Có Tên đăng nhập & Trạng thái hoạt động)
CREATE TABLE tblNhanVien (
    MaNV INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap NVARCHAR(50) UNIQUE NOT NULL, -- Dùng để đăng nhập
    MatKhau NVARCHAR(100) NOT NULL,
    TenNV NVARCHAR(100),
    GioiTinh NVARCHAR(10),
    NgaySinh DATE,
    SDT NVARCHAR(20),
    TrangThai BIT DEFAULT 1, -- 1: Hoạt động, 0: Đã nghỉ/Bị khóa
    VaiTro INT FOREIGN KEY REFERENCES tblVaiTro(IDVaiTro)
);

-- 3. Bảng Nhật Ký Hoạt Động (Để Admin theo dõi hệ thống)
CREATE TABLE tblNhatKyHoatDong (
    IDLog INT IDENTITY(1,1) PRIMARY KEY,
    MaNV INT FOREIGN KEY REFERENCES tblNhanVien(MaNV),
    HanhDong NVARCHAR(255), -- Vd: "Thêm nhân viên mới", "Xóa hóa đơn"
    ThoiGian DATETIME DEFAULT GETDATE(),
    GhiChu NVARCHAR(MAX)
);

-- 4. Bảng Khách Hàng
CREATE TABLE tblKhachHang (
    MaKH INT IDENTITY(1,1) PRIMARY KEY,
    TenKH NVARCHAR(100),
    CCCD NVARCHAR(20),
    GioiTinh NVARCHAR(10),
    NamSinh INT,
    DienThoai NVARCHAR(20),
    Email NVARCHAR(100),
    DiaChi NVARCHAR(255),
    MatKhau NVARCHAR(100) -- Dành cho khách đặt online (nếu có)
);

-- 5. Bảng Loại Phòng
CREATE TABLE tblLoaiPhong (
    MaLoai INT IDENTITY(1,1) PRIMARY KEY,
    TenLoai NVARCHAR(100),    -- Vd: Standard, VIP, Twin
    SoNguoiToiDa INT,
    GiaMacDinh DECIMAL(18,2), -- Giá niêm yết
    MoTa NVARCHAR(255)
);

-- 6. Bảng Phòng
CREATE TABLE tblPhong (
    MaPhong INT IDENTITY(1,1) PRIMARY KEY,
    SoPhong NVARCHAR(20),     -- Vd: 101, 202
    Tang INT,
    TrangThai NVARCHAR(50),   -- Trống, Đang ở, Đang dọn
    MoTaChiTiet NVARCHAR(MAX),
    AnhDaiDien NVARCHAR(255), -- Link ảnh
    MaLoai INT FOREIGN KEY REFERENCES tblLoaiPhong(MaLoai)
);

-- 7. Bảng Dịch Vụ
CREATE TABLE tblDichVu (
    MaDV INT IDENTITY(1,1) PRIMARY KEY,
    TenDV NVARCHAR(100),      -- Vd: Nước ngọt, Giặt ủi
    DonGia DECIMAL(18,2),
    MoTa NVARCHAR(255)
);

-- 8. Bảng Tình Trạng Hóa Đơn
CREATE TABLE tblTinhTrang (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    TenTinhTrang NVARCHAR(50) -- Đã đặt, Check-in, Đã thanh toán, Hủy
);

-- 9. Bảng Hóa Đơn (Phiếu thuê phòng)
CREATE TABLE tblHoaDon (
    MaHD INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT FOREIGN KEY REFERENCES tblKhachHang(MaKH),
    MaNV INT FOREIGN KEY REFERENCES tblNhanVien(MaNV), -- Nhân viên lập phiếu
    NgayLap DATETIME DEFAULT GETDATE(),
    NgayCheckIn DATETIME,
    NgayCheckOut DATETIME,
    TongTien DECIMAL(18,2),
    TinhTrang INT FOREIGN KEY REFERENCES tblTinhTrang(ID),
    GhiChu NVARCHAR(255),
    DaThanhToan BIT DEFAULT 0 -- 0: Chưa, 1: Rồi (Quan trọng để tính Doanh thu)
);

-- 10. Chi Tiết Thuê Phòng (Lưu giá tại thời điểm thuê)
CREATE TABLE tblChiTietHoaDon (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    MaHD INT FOREIGN KEY REFERENCES tblHoaDon(MaHD),
    MaPhong INT FOREIGN KEY REFERENCES tblPhong(MaPhong),
    DonGiaThucTe DECIMAL(18,2), 
    SoNgayO INT
);

-- 11. Chi Tiết Dịch Vụ (Khách dùng thêm)
CREATE TABLE tblChiTietDichVu (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    MaHD INT FOREIGN KEY REFERENCES tblHoaDon(MaHD),
    MaDV INT FOREIGN KEY REFERENCES tblDichVu(MaDV),
    SoLuong INT,
    DonGia DECIMAL(18,2)
);
GO

-- ========================================
-- PHẦN B: CÁC STORED PROCEDURES (QUAN TRỌNG CHO ADMIN)
-- ========================================

-- 1. SP: Thêm Nhân Viên Mới (Có kiểm tra trùng & Ghi Log)
CREATE PROCEDURE sp_ThemNhanVien
    @TenDangNhap NVARCHAR(50),
    @MatKhau NVARCHAR(100),
    @TenNV NVARCHAR(100),
    @GioiTinh NVARCHAR(10),
    @NgaySinh DATE,
    @SDT NVARCHAR(20),
    @VaiTro INT,
    @NguoiThucHien INT -- ID Admin thực hiện
AS
BEGIN
    -- Check trùng user
    IF EXISTS (SELECT 1 FROM tblNhanVien WHERE TenDangNhap = @TenDangNhap)
    BEGIN
        RETURN -1; -- Lỗi: Đã tồn tại
    END

    -- Thêm mới
    INSERT INTO tblNhanVien (TenDangNhap, MatKhau, TenNV, GioiTinh, NgaySinh, SDT, VaiTro, TrangThai)
    VALUES (@TenDangNhap, @MatKhau, @TenNV, @GioiTinh, @NgaySinh, @SDT, @VaiTro, 1);

    -- Ghi Log
    DECLARE @NewID INT = SCOPE_IDENTITY();
    INSERT INTO tblNhatKyHoatDong (MaNV, HanhDong, GhiChu)
    VALUES (@NguoiThucHien, N'Thêm nhân viên', N'Tạo user: ' + @TenDangNhap);

    RETURN 1; -- Thành công
END;
GO

-- 2. SP: Báo Cáo Doanh Thu Theo Tháng
CREATE PROCEDURE sp_BaoCaoDoanhThu
    @Thang INT,
    @Nam INT
AS
BEGIN
    SELECT 
        DAY(NgayLap) AS Ngay,
        COUNT(MaHD) AS SoLuongDon,
        SUM(TongTien) AS DoanhThu
    FROM tblHoaDon
    WHERE MONTH(NgayLap) = @Thang 
      AND YEAR(NgayLap) = @Nam 
      AND DaThanhToan = 1 -- Chỉ tính đơn đã thu tiền
    GROUP BY DAY(NgayLap)
    ORDER BY DAY(NgayLap);
END;
GO

-- 3. SP: Thống Kê Tỷ Lệ Sử Dụng (Loại phòng nào hot nhất)
CREATE PROCEDURE sp_ThongKeTanSuatPhong
    @TuNgay DATE,
    @DenNgay DATE
AS
BEGIN
    SELECT 
        lp.TenLoai,
        COUNT(ct.ID) AS SoLuotThue,
        SUM(ct.DonGiaThucTe * ct.SoNgayO) AS DoanhThuMangLai
    FROM tblLoaiPhong lp
    JOIN tblPhong p ON lp.MaLoai = p.MaLoai
    JOIN tblChiTietHoaDon ct ON p.MaPhong = ct.MaPhong
    JOIN tblHoaDon hd ON ct.MaHD = hd.MaHD
    WHERE hd.NgayLap BETWEEN @TuNgay AND @DenNgay AND hd.DaThanhToan = 1
    GROUP BY lp.TenLoai
    ORDER BY SoLuotThue DESC;
END;
GO

-- 4. SP: Reset Mật Khẩu (Admin dùng)
CREATE PROCEDURE sp_Admin_ResetMatKhau
    @MaNV INT,
    @MatKhauMoi NVARCHAR(100),
    @AdminID INT
AS
BEGIN
    UPDATE tblNhanVien SET MatKhau = @MatKhauMoi WHERE MaNV = @MaNV;
    INSERT INTO tblNhatKyHoatDong (MaNV, HanhDong, GhiChu)
    VALUES (@AdminID, N'Reset mật khẩu', N'Reset cho ID: ' + CAST(@MaNV AS NVARCHAR));
END;
GO

-- ========================================
-- PHẦN C: DỮ LIỆU MẪU (DATA)
-- ========================================

-- 1. Vai Trò
INSERT INTO tblVaiTro (TenVaiTro, MoTa) VALUES (N'Admin', N'Quản trị hệ thống'); -- ID 1
INSERT INTO tblVaiTro (TenVaiTro, MoTa) VALUES (N'LeTan', N'Lễ tân'); -- ID 2
INSERT INTO tblVaiTro (TenVaiTro, MoTa) VALUES (N'TapVu', N'Dọn phòng'); -- ID 3

-- 2. Nhân Viên (Tài khoản Admin mặc định)
-- User: admin / Pass: admin123
INSERT INTO tblNhanVien (TenDangNhap, MatKhau, TenNV, GioiTinh, NgaySinh, SDT, VaiTro, TrangThai)
VALUES ('admin', 'admin123', N'Administrator', N'Nam', '1995-01-01', '0909999888', 1, 1);

INSERT INTO tblNhanVien (TenDangNhap, MatKhau, TenNV, GioiTinh, NgaySinh, SDT, VaiTro, TrangThai)
VALUES ('letan01', '123456', N'Trần Thị Mai', N'Nữ', '2000-05-05', '0911222333', 2, 1);

-- 3. Loại Phòng
INSERT INTO tblLoaiPhong (TenLoai, SoNguoiToiDa, GiaMacDinh) VALUES (N'Standard (Đơn)', 2, 400000); -- ID 1
INSERT INTO tblLoaiPhong (TenLoai, SoNguoiToiDa, GiaMacDinh) VALUES (N'Double (Đôi)', 4, 700000);    -- ID 2
INSERT INTO tblLoaiPhong (TenLoai, SoNguoiToiDa, GiaMacDinh) VALUES (N'VIP Suite', 2, 1500000);     -- ID 3

-- 4. Phòng
INSERT INTO tblPhong (SoPhong, Tang, TrangThai, MaLoai) VALUES (N'101', 1, N'Trống', 1);
INSERT INTO tblPhong (SoPhong, Tang, TrangThai, MaLoai) VALUES (N'102', 1, N'Đang ở', 1);
INSERT INTO tblPhong (SoPhong, Tang, TrangThai, MaLoai) VALUES (N'201', 2, N'Trống', 2);
INSERT INTO tblPhong (SoPhong, Tang, TrangThai, MaLoai) VALUES (N'305', 3, N'Trống', 3);

-- 5. Dịch Vụ
INSERT INTO tblDichVu (TenDV, DonGia) VALUES (N'Nước suối', 10000);
INSERT INTO tblDichVu (TenDV, DonGia) VALUES (N'Mì ly', 15000);
INSERT INTO tblDichVu (TenDV, DonGia) VALUES (N'Giặt ủi', 50000);

-- 6. Tình Trạng HĐ
INSERT INTO tblTinhTrang (TenTinhTrang) VALUES (N'Đang đặt');
INSERT INTO tblTinhTrang (TenTinhTrang) VALUES (N'Đang ở');
INSERT INTO tblTinhTrang (TenTinhTrang) VALUES (N'Đã thanh toán');

-- 7. Khách Hàng
INSERT INTO tblKhachHang (TenKH, DienThoai, DiaChi) VALUES (N'Nguyễn Văn Khách', '0912345678', N'Hà Nội');

-- 8. Tạo Hóa Đơn Mẫu (Để test Báo cáo doanh thu)
-- Giả sử hôm nay có khách trả phòng
INSERT INTO tblHoaDon (MaKH, MaNV, NgayLap, TongTien, TinhTrang, DaThanhToan)
VALUES (1, 2, GETDATE(), 820000, 3, 1);

-- Chi tiết hóa đơn trên
INSERT INTO tblChiTietHoaDon (MaHD, MaPhong, DonGiaThucTe, SoNgayO) VALUES (1, 1, 400000, 2); -- Ở 2 ngày phòng 101
INSERT INTO tblChiTietDichVu (MaHD, MaDV, SoLuong, DonGia) VALUES (1, 1, 2, 10000); -- Uống 2 chai nước