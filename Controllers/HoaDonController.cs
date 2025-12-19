using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QL_KhachSan;
using System.IO;

namespace QL_KhachSan.Controllers
{
    public class HoaDonController : Controller
    {
        // GET: HoaDon
        QL_KhachSanEntities db = new QL_KhachSanEntities();

        // 1. Danh sách các đơn đặt phòng
        public ActionResult Index()
        {
            var listHD = db.tblHoaDons.OrderByDescending(x => x.NgayLap).ToList();
            return View(listHD);
        }

        // 2. Giao diện Đặt phòng (Check-in)
        public ActionResult Create()
        {
            // Dropdown chọn Khách hàng
            ViewBag.MaKH = new SelectList(db.tblKhachHangs, "MaKH", "TenKH");
            // Dropdown chọn Phòng (Chỉ lấy phòng Trống)
            var phongTrong = db.tblPhongs.Where(x => x.TrangThai == "Trống").ToList();
            ViewBag.MaPhong = new SelectList(phongTrong, "MaPhong", "SoPhong");

            return View();
        }

        // 3. Xử lý Lưu Đặt phòng
        [HttpPost]
        public ActionResult Create(tblHoaDon hd, int MaPhong)
        {
            // A. Lưu Hóa Đơn (Master)
            hd.NgayLap = DateTime.Now;
            hd.NgayCheckIn = DateTime.Now;
            hd.DaThanhToan = false;
            hd.TinhTrang = 2; // Giả sử ID 2 là "Đang ở"

            // Lấy ID nhân viên từ Session đăng nhập
            var nv = Session["User"] as tblNhanVien;
            hd.MaNV = nv != null ? nv.MaNV : 1;

            db.tblHoaDons.Add(hd);
            db.SaveChanges(); // Lưu để lấy MaHD vừa sinh ra

            // B. Lưu Chi Tiết Hóa Đơn (Detail)
            var phong = db.tblPhongs.Find(MaPhong);
            tblChiTietHoaDon ct = new tblChiTietHoaDon();
            ct.MaHD = hd.MaHD;
            ct.MaPhong = MaPhong;
            ct.DonGiaThucTe = phong.tblLoaiPhong.GiaMacDinh; // Lấy giá hiện tại
            ct.SoNgayO = 1; // Mặc định 1 ngày, sẽ tính lại khi Checkout

            db.tblChiTietHoaDons.Add(ct);

            // C. Cập nhật trạng thái phòng -> "Đang ở"
            phong.TrangThai = "Đang ở";

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // 4. Thanh toán & Trả phòng (Checkout)
        public ActionResult Checkout(int id)
        {
            var hd = db.tblHoaDons.Find(id);
            if (hd != null)
            {
                hd.NgayCheckOut = DateTime.Now;
                hd.DaThanhToan = true;
                hd.TinhTrang = 3; // Giả sử ID 3 là "Đã thanh toán"

                // Tính tổng tiền (Số ngày * Giá + Dịch vụ nếu có)
                // Đây là logic đơn giản, bạn có thể tính kỹ hơn
                var ct = db.tblChiTietHoaDons.FirstOrDefault(x => x.MaHD == id);
                if (ct != null)
                {
                    TimeSpan songay = (TimeSpan)(hd.NgayCheckOut - hd.NgayCheckIn);
                    int days = songay.Days > 0 ? songay.Days : 1; // Tối thiểu 1 ngày
                    ct.SoNgayO = days;
                    hd.TongTien = days * ct.DonGiaThucTe;

                    // Trả lại trạng thái phòng Trống
                    var phong = db.tblPhongs.Find(ct.MaPhong);
                    phong.TrangThai = "Trống";
                }

                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        public ActionResult Details(int id)
        {
            // Tìm hóa đơn theo ID
            var hd = db.tblHoaDons.Find(id);

            if (hd == null) return HttpNotFound();

            // Tính toán số ngày ở thực tế để hiển thị (chưa lưu vào DB, chỉ hiển thị)
            var ctPhong = hd.tblChiTietHoaDons.FirstOrDefault();
            if (ctPhong != null && hd.NgayCheckOut == null)
            {
                // Nếu chưa checkout, tạm tính đến thời điểm hiện tại
                TimeSpan timeSpan = DateTime.Now - hd.NgayCheckIn.Value;
                ViewBag.SoNgay = timeSpan.Days > 0 ? timeSpan.Days : 1;
            }
            else if (ctPhong != null)
            {
                ViewBag.SoNgay = ctPhong.SoNgayO;
            }

            //Lấy danh sách dịch vụ đã dùng(nếu có bảng ChiTietDichVu)
             ViewBag.ListDV = db.tblChiTietDichVus.Where(x => x.MaHD == id).ToList();

            return View(hd);
        }
    }
}