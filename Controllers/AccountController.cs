using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QL_KhachSan.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        // Khởi tạo kết nối CSDL
        QL_KhachSanEntities db = new QL_KhachSanEntities();

        // 1. Hiện form Đăng nhập (GET)
        public ActionResult Login()
        {
            return View();
        }

        // 2. Xử lý nút Đăng nhập (POST)
        [HttpPost]
        public ActionResult Login(string user, string pass)
        {
            // Tìm nhân viên trong DB khớp tài khoản & mật khẩu
            var nv = db.tblNhanViens.FirstOrDefault(x => x.TenDangNhap == user && x.MatKhau == pass);

            if (nv != null)
            {
                // Kiểm tra xem tài khoản có bị khóa không
                if (nv.TrangThai == false)
                {
                    ViewBag.Error = "Tài khoản đã bị khóa!";
                    return View();
                }

                // --- QUAN TRỌNG: Lưu thông tin vào Session ---
                Session["User"] = nv;          // Lưu toàn bộ đối tượng nhân viên
                Session["TenHienThi"] = nv.TenNV;
                Session["VaiTro"] = nv.VaiTro; // Lưu vai trò (1=Admin, 2=NV...)

                // --- PHÂN QUYỀN CHUYỂN HƯỚNG ---
                // Nếu là Admin (VaiTro = 1) -> Vào trang Báo cáo
                if (nv.VaiTro == 1)
                {
                    return RedirectToAction("BaoCao", "Admin");
                }
                else
                {
                    // Nếu là Nhân viên -> Vào trang Đặt phòng hoặc Trang chủ
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
                return View();
            }
        }

        // 3. Đăng xuất
        public ActionResult Logout()
        {
            Session.Clear(); // Xóa hết Session
            return RedirectToAction("Login");
        }
    }
}