using QL_KhachSan;
using QL_KhachSan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QL_KhachSan.Controllers
{
    [AdminAuthorize]
    public class NhanVienController : Controller
    {
        // Khởi tạo kết nối CSDL
        QL_KhachSanEntities db = new QL_KhachSanEntities();

        // 1. Danh sách nhân viên
        public ActionResult Index()
        {
            // Kiểm tra quyền Admin (Chỉ Admin mới được vào)
            if (Session["User"] == null || Convert.ToInt32(Session["VaiTro"]) != 1)
                return RedirectToAction("Login", "Account");

            // SỬA LỖI 1: Nếu db.tblNhanViens báo lỗi, hãy sửa thành db.tblNhanVien
            // (Thường EF sẽ đặt tên theo số nhiều, nhưng nếu lỗi thì cứ thử bỏ s)
            var listNV = db.tblNhanViens.Include("tblVaiTro").ToList();
            return View(listNV);
        }

        // 2. Giao diện Thêm mới
        public ActionResult Create()
        {
            // Check quyền
            if (Session["User"] == null || Convert.ToInt32(Session["VaiTro"]) != 1)
                return RedirectToAction("Login", "Account");

         
            ViewBag.VaiTro = new SelectList(db.tblVaiTroes, "IDVaiTro", "TenVaiTro");

            return View();
        }

        // 3. Xử lý lưu nhân viên (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(tblNhanVien nv)
        {
            // Check quyền
            if (Session["User"] == null || Convert.ToInt32(Session["VaiTro"]) != 1)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                // Kiểm tra trùng tên đăng nhập
                if (db.tblNhanViens.Any(x => x.TenDangNhap == nv.TenDangNhap))
                {
                    ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã tồn tại!");
                }
                else
                {
                    // Tạm thời comment dòng mã hóa pass nếu chưa có Helper để chạy cho được việc
                    // nv.MatKhau = Helper.SecurityHelper.HashPassword(nv.MatKhau);

                    nv.TrangThai = true; // Mặc định là Hoạt động

                    db.tblNhanViens.Add(nv);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.VaiTro = new SelectList(db.tblVaiTroes, "IDVaiTro", "TenVaiTro", nv.VaiTro);

            return View(nv);
        }
        // 4. Giao diện Chỉnh sửa (GET)
        public ActionResult Edit(int id)
        {
            var nv = db.tblNhanViens.Find(id);
            if (nv == null) return HttpNotFound();

            ViewBag.VaiTro = new SelectList(db.tblVaiTroes, "IDVaiTro", "TenVaiTro", nv.VaiTro);
            return View(nv);
        }

        // 5. Xử lý cập nhật (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(tblNhanVien nv)
        {
            if (ModelState.IsValid)
            {
                var nvInDb = db.tblNhanViens.Find(nv.MaNV);
                if (nvInDb != null)
                {
                    nvInDb.TenNV = nv.TenNV;
                    nvInDb.SDT = nv.SDT;
                    nvInDb.VaiTro = nv.VaiTro;
                    nvInDb.TrangThai = nv.TrangThai;
                    // Chỉ cập nhật mật khẩu nếu người dùng nhập mật khẩu mới
                    if (!string.IsNullOrEmpty(nv.MatKhau))
                    {
                        nvInDb.MatKhau = nv.MatKhau;
                    }

                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            ViewBag.VaiTro = new SelectList(db.tblVaiTroes, "IDVaiTro", "TenVaiTro", nv.VaiTro);
            return View(nv);
        }

        // 6. Xử lý Xóa (Thường dùng cách khóa tài khoản hoặc xóa hẳn)
        public ActionResult Delete(int id)
        {
            var nv = db.tblNhanViens.Find(id);
            if (nv != null)
            {
                // Kiểm tra nếu là chính mình thì không cho xóa
                var adminHienTai = Session["User"] as tblNhanVien;
                if (adminHienTai != null && adminHienTai.MaNV == id)
                {
                    TempData["Error"] = "Bạn không thể tự xóa chính mình!";
                    return RedirectToAction("Index");
                }

                db.tblNhanViens.Remove(nv);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}