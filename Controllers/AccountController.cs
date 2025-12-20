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
            var nv = db.tblNhanViens.FirstOrDefault(x => x.TenDangNhap == user && x.MatKhau == pass);

            if (nv != null)
            {
                if (nv.TrangThai == false)
                {
                    ViewBag.Error = "Tài khoản đã bị khóa!";
                    return View();
                }

                // 1. Lưu thông tin vào Session
                Session["User"] = nv;
                Session["TenHienThi"] = nv.TenNV;
                Session["VaiTro"] = nv.VaiTro;

                // 2. Ghi nhật ký đăng nhập thành công
                LuuNhatKy("Đăng nhập", "Nhân viên " + nv.TenNV + " đã đăng nhập vào hệ thống.");

                // 3. Trả về trang chủ (Home/Index)
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
                return View();
            }
        }

        public void LuuNhatKy(string hanhDong, string ghiChu)
        {
            // Lấy thông tin nhân viên từ Session
            var nv = Session["User"] as tblNhanVien;

            tblNhatKyHoatDong log = new tblNhatKyHoatDong();
            log.HanhDong = hanhDong;
            log.GhiChu = ghiChu;
            log.ThoiGian = DateTime.Now;

            // Nếu đã đăng nhập thì lưu MaNV, nếu chưa (lúc đăng nhập lỗi) thì để null
            if (nv != null)
            {
                log.MaNV = nv.MaNV;
            }

            db.tblNhatKyHoatDongs.Add(log);
            db.SaveChanges();
        }
        // 3. Đăng xuất
        public ActionResult Logout()
        {
            Session.Clear(); // Xóa hết Session
            return RedirectToAction("Login");
        }
    }
}