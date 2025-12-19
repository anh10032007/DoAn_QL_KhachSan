using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QL_KhachSan;

namespace QL_KhachSan.Controllers
{
    public class HoaDonController : Controller
    {
        QL_KhachSanEntities db = new QL_KhachSanEntities();

        // 1. Danh sách hóa đơn
        public ActionResult Index()
        {
            var listHD = db.tblHoaDons.OrderByDescending(x => x.MaHD).ToList();
            return View(listHD);
        }

        // 2. Giao diện Check-in (GET)
        public ActionResult Create()
        {
            // Dropdown Khách hàng
            ViewBag.MaKH = new SelectList(db.tblKhachHangs, "MaKH", "TenKH");

            // --- NÂNG CẤP: Dropdown Phòng hiển thị cả Giá tiền ---
            // Chỉ lấy phòng Trống
            var listPhong = db.tblPhongs.Where(x => x.TrangThai == "Trống")
                              .Select(x => new {
                                  x.MaPhong,
                                  // Chuỗi hiển thị: "P.101 (Đơn) - 300,000đ"
                                  ThongTinPhong = "P." + x.SoPhong + " (" + x.tblLoaiPhong.TenLoai + ") - " + x.tblLoaiPhong.GiaMacDinh + " vnđ/ngày"
                              }).ToList();

            ViewBag.MaPhong = new SelectList(listPhong, "MaPhong", "ThongTinPhong");

            return View();
        }

        // 3. Xử lý Check-in (POST)
        [HttpPost]
        public ActionResult Create(tblHoaDon hd, int MaPhong)
        {
            try
            {
                // A. Tạo Hóa Đơn Master
                hd.NgayLap = DateTime.Now;
                hd.NgayCheckIn = DateTime.Now;
                hd.DaThanhToan = false; // Chưa thanh toán
                hd.TinhTrang = 1;       // 1 = Đang ở

                // Lấy nhân viên đang đăng nhập
                var nv = Session["User"] as tblNhanVien;
                hd.MaNV = nv != null ? nv.MaNV : 1; // Nếu null thì gán tạm ID 1 (Admin)

                db.tblHoaDons.Add(hd);
                db.SaveChanges(); // Lưu để lấy MaHD

                // B. Tạo Chi Tiết Hóa Đơn (Lưu giá tiền tại thời điểm thuê)
                var phong = db.tblPhongs.Find(MaPhong);
                tblChiTietHoaDon ct = new tblChiTietHoaDon();
                ct.MaHD = hd.MaHD;
                ct.MaPhong = MaPhong;
                // Lưu đơn giá thực tế (đề phòng sau này giá phòng tăng thì hóa đơn cũ không bị đổi)
                ct.DonGiaThucTe = phong.tblLoaiPhong.GiaMacDinh;
                ct.SoNgayO = 0; // Chưa biết ở bao lâu

                db.tblChiTietHoaDons.Add(ct);

                // C. Cập nhật trạng thái phòng -> "Đang ở"
                phong.TrangThai = "Đang ở";

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                // Nếu lỗi thì load lại dropdown để nhập lại
                return RedirectToAction("Create");
            }
        }

        // 4. Xem chi tiết (để in bill)
        public ActionResult Details(int id)
        {
            var hd = db.tblHoaDons.Find(id);
            if (hd == null) return HttpNotFound();

            // Tính toán số ngày ở tạm tính để hiển thị
            if (hd.DaThanhToan != true && hd.NgayCheckIn.HasValue)
            {
                TimeSpan hieuSo = DateTime.Now - hd.NgayCheckIn.Value;
                // Nếu ở chưa được 1 ngày thì tính là 1 ngày
                ViewBag.SoNgay = hieuSo.TotalDays < 1 ? 1 : (int)Math.Ceiling(hieuSo.TotalDays);
            }
            else
            {
                // Đã thanh toán thì lấy trong DB
                var ct = hd.tblChiTietHoaDons.FirstOrDefault();
                ViewBag.SoNgay = ct != null ? ct.SoNgayO : 1;
            }

            return View(hd);
        }

        // 5. Xử lý Trả Phòng & Tính Tiền (Checkout)
        public ActionResult Checkout(int id)
        {
            var hd = db.tblHoaDons.Find(id);
            if (hd != null && hd.DaThanhToan != true)
            {
                // 1. Cập nhật ngày ra
                hd.NgayCheckOut = DateTime.Now;
                hd.DaThanhToan = true;
                hd.TinhTrang = 2; // 2 = Đã xong

                // 2. Tính toán tiền phòng
                var ct = db.tblChiTietHoaDons.FirstOrDefault(x => x.MaHD == id);
                if (ct != null)
                {
                    TimeSpan hieuSo = (TimeSpan)(hd.NgayCheckOut - hd.NgayCheckIn);

                    // Logic tính tiền: Ở bao nhiêu tính bấy nhiêu, tối thiểu 1 ngày
                    int soNgay = hieuSo.TotalHours <= 24 ? 1 : (int)Math.Ceiling(hieuSo.TotalDays);

                    ct.SoNgayO = soNgay;

                    // Tổng tiền = (Số ngày * Giá phòng) + (Tiền dịch vụ nếu có)
                    // Hiện tại chưa làm dịch vụ nên chỉ tính tiền phòng
                    hd.TongTien = (decimal)soNgay * ct.DonGiaThucTe;

                    // 3. Trả lại phòng Trống
                    var phong = db.tblPhongs.Find(ct.MaPhong);
                    if (phong != null) phong.TrangThai = "Trống";
                }

                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}