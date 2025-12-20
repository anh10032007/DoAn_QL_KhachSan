using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QL_KhachSan.Controllers
{
    public class TapVuController : Controller
    {
        // GET: TapVu
        QL_KhachSanEntities db = new QL_KhachSanEntities();

        // 1. Danh sách các phòng cần dọn dẹp (Trạng thái khác 'Trống' và 'Đang ở')
        public ActionResult Index()
        {
            // Kiểm tra đăng nhập (Ít nhất là Nhân viên trở lên)
            if (Session["User"] == null) return RedirectToAction("Login", "Account");

            // Lấy các phòng có trạng thái "Đang dọn", "Bẩn", hoặc "Bảo trì"
            var phongCanDon = db.tblPhongs
                                .Where(p => p.TrangThai != "Trống" && p.TrangThai != "Đang ở")
                                .ToList();
            return View(phongCanDon);
        }

        // 2. Xác nhận đã dọn xong -> Chuyển về trạng thái "Trống"
        public ActionResult HoanTat(int id)
        {
            if (Session["User"] == null) return RedirectToAction("Login", "Account");

            var phong = db.tblPhongs.Find(id);
            if (phong != null)
            {
                phong.TrangThai = "Trống";
                db.SaveChanges();

                // Ghi nhật ký hoạt động (Nếu có bảng nhật ký)
                var nv = Session["User"] as tblNhanVien;
                // db.sp_GhiNhatKy(nv.MaNV, "Dọn dẹp xong phòng " + phong.SoPhong);
            }
            return RedirectToAction("Index");
        }

        // 3. Đánh dấu phòng cần bảo trì (Ví dụ hỏng bóng đèn, vòi nước)
        public ActionResult BaoTri(int id)
        {
            var phong = db.tblPhongs.Find(id);
            if (phong != null)
            {
                phong.TrangThai = "Bảo trì";
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}