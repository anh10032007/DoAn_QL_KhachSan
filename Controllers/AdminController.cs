using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QL_KhachSan;

namespace QL_KhachSan.Controllers
{
    public class AdminController : Controller
    {
        QL_KhachSanEntities db = new QL_KhachSanEntities();

        // 1. Trang Báo cáo doanh thu
        public ActionResult BaoCao(int? thang, int? nam)
        {
            // Kiểm tra quyền Admin
            var nv = Session["User"] as tblNhanVien;
            if (nv == null || nv.VaiTro != 1) return RedirectToAction("Login", "Account");

            if (thang == null) thang = DateTime.Now.Month;
            if (nam == null) nam = DateTime.Now.Year;

            ViewBag.Thang = thang;
            ViewBag.Nam = nam;

            // Gọi Stored Procedure: sp_BaoCaoDoanhThu
            // Lưu ý: Bạn cần Update Model EDMX để nó nhận diện SP này thì mới gọi được như dưới
            var data = db.sp_BaoCaoDoanhThu(thang, nam).ToList();

            return View(data);
        }
        public ActionResult Log()
        {
            if (Session["User"] == null || Convert.ToInt32(Session["VaiTro"]) != 1)
                return RedirectToAction("Login", "Account");

            // Load nhật ký kèm theo thông tin nhân viên (Lazy loading hoặc Eager loading)
            var logs = db.tblNhatKyHoatDongs
                         .Include("tblNhanVien") // Đảm bảo lấy được TenNV
                         .OrderByDescending(x => x.ThoiGian)
                         .ToList();

            return View(logs);
        }
    }
}