using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QL_KhachSan.Controllers
{
    public class HomeController : Controller
    {
        QL_KhachSanEntities db = new QL_KhachSanEntities();

        public ActionResult Index()
        {
            // 1. Kiểm tra đăng nhập
            if (Session["User"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. Lấy ngày hôm nay (để so sánh)
            var today = DateTime.Today;

            // --- THỐNG KÊ SỐ LIỆU ---

            // A. Tình trạng phòng hiện tại
            ViewBag.SoPhongTrong = db.tblPhongs.Count(x => x.TrangThai == "Trống");
            ViewBag.SoPhongDangO = db.tblPhongs.Count(x => x.TrangThai == "Đang ở");
            ViewBag.SoPhongDon = db.tblPhongs.Count(x => x.TrangThai != "Trống" && x.TrangThai != "Đang ở"); // Đang dọn/Bảo trì

            // B. Hoạt động trong ngày (Dùng DbFunctions hoặc EntityFunctions để so sánh ngày bỏ qua giờ)
            // Đếm số khách check-in hôm nay
            ViewBag.CheckInHomNay = db.tblHoaDons
                .Count(x => DbFunctions.TruncateTime(x.NgayCheckIn) == today);

            // C. Doanh thu hôm nay (Chỉ tính các hóa đơn ĐÃ thanh toán có ngày checkout là hôm nay)
            // Lưu ý: Dùng (decimal?) để tránh lỗi null nếu không có đơn nào
            decimal doanhThu = db.tblHoaDons
                .Where(x => x.DaThanhToan == true && DbFunctions.TruncateTime(x.NgayCheckOut) == today)
                .Sum(x => (decimal?)x.TongTien) ?? 0;

            ViewBag.DoanhThuHomNay = doanhThu;

            return View();
        }
    }
}