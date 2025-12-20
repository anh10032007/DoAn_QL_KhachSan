using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QL_KhachSan;
using System.IO;

namespace QL_KhachSan.Controllers
{
    public class PhongController : Controller
    {
        // Khởi tạo kết nối CSDL
        QL_KhachSanEntities db = new QL_KhachSanEntities();

        // ==========================================
        // 1. DANH SÁCH PHÒNG (Ai cũng xem được)
        // ==========================================
        public ActionResult Index()
        {
            var listPhong = db.tblPhongs.Include("tblLoaiPhong").ToList();

            // --- LẤY ID HÓA ĐƠN ĐANG HOẠT ĐỘNG ---
            // Logic: Tìm các hóa đơn chưa thanh toán để lấy ID trỏ link nút "Thanh toán"
            var activeBills = db.tblChiTietHoaDons
                                .Where(ct => ct.tblHoaDon.DaThanhToan == false)
                                .Select(ct => new { ct.MaPhong, ct.MaHD })
                                .ToDictionary(k => k.MaPhong, v => v.MaHD);

            ViewBag.ActiveBills = activeBills;

            return View(listPhong);
        }

        // ==========================================
        // HELPER: KIỂM TRA QUYỀN ADMIN
        // ==========================================
        private bool IsAdmin()
        {
            // Nếu chưa đăng nhập HOẶC Vai trò khác 1 (1=Admin) -> False
            if (Session["User"] == null || Convert.ToInt32(Session["VaiTro"]) != 1)
                return false;
            return true;
        }

        // ==========================================
        // 2. CHỨC NĂNG THÊM MỚI (CREATE) - CHỈ ADMIN
        // ==========================================

        // GET: Hiện form thêm mới
        public ActionResult Create()
        {
            // Kiểm tra quyền: Nếu không phải Admin -> Đẩy về trang chủ
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            ViewBag.MaLoai = new SelectList(db.tblLoaiPhongs, "MaLoai", "TenLoai");
            return View();
        }

        // POST: Xử lý lưu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(tblPhong p, HttpPostedFileBase HinhAnh)
        {
            // Kiểm tra quyền
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            if (ModelState.IsValid)
            {
                // Xử lý Upload ảnh
                if (HinhAnh != null && HinhAnh.ContentLength > 0)
                {
                    string filename = Path.GetFileName(HinhAnh.FileName);
                    string path = Server.MapPath("~/Content/Images/");

                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    HinhAnh.SaveAs(path + filename);
                    p.AnhDaiDien = filename;
                }

                p.TrangThai = "Trống"; // Mặc định khi tạo mới
                db.tblPhongs.Add(p);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MaLoai = new SelectList(db.tblLoaiPhongs, "MaLoai", "TenLoai", p.MaLoai);
            return View(p);
        }

        // ==========================================
        // 3. CHỨC NĂNG SỬA (EDIT) - CHỈ ADMIN
        // ==========================================

        // GET: Hiện form sửa
        public ActionResult Edit(int id)
        {
            // Kiểm tra quyền
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var phong = db.tblPhongs.Find(id);
            if (phong == null) return HttpNotFound();

            ViewBag.MaLoai = new SelectList(db.tblLoaiPhongs, "MaLoai", "TenLoai", phong.MaLoai);
            return View(phong);
        }

        // POST: Xử lý cập nhật
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(tblPhong p, HttpPostedFileBase HinhAnh)
        {
            // Kiểm tra quyền
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            if (ModelState.IsValid)
            {
                var phongInDb = db.tblPhongs.Find(p.MaPhong);
                if (phongInDb != null)
                {
                    phongInDb.SoPhong = p.SoPhong;
                    phongInDb.Tang = p.Tang;
                    phongInDb.MaLoai = p.MaLoai;
                    phongInDb.MoTaChiTiet = p.MoTaChiTiet;

                    if (HinhAnh != null && HinhAnh.ContentLength > 0)
                    {
                        string filename = Path.GetFileName(HinhAnh.FileName);
                        string path = Server.MapPath("~/Content/Images/");
                        HinhAnh.SaveAs(path + filename);
                        phongInDb.AnhDaiDien = filename;
                    }

                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.MaLoai = new SelectList(db.tblLoaiPhongs, "MaLoai", "TenLoai", p.MaLoai);
            return View(p);
        }

        // ==========================================
        // 4. CHỨC NĂNG XÓA (DELETE) - CHỈ ADMIN
        // ==========================================

        // GET: Hiện xác nhận xóa
        public ActionResult Delete(int id)
        {
            // Kiểm tra quyền
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var phong = db.tblPhongs.Find(id);
            if (phong == null) return HttpNotFound();
            return View(phong);
        }

        // POST: Xử lý xóa thật sự
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Kiểm tra quyền
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var phong = db.tblPhongs.Find(id);
            if (phong != null)
            {
                // Ràng buộc dữ liệu: Không xóa phòng đã từng có hóa đơn
                bool dangSuDung = db.tblChiTietHoaDons.Any(x => x.MaPhong == id);
                if (dangSuDung)
                {
                    ViewBag.Error = "Phòng này đã từng được thuê, không thể xóa! Hãy chuyển trạng thái sang 'Bảo trì'.";
                    return View("Delete", phong);
                }

                db.tblPhongs.Remove(phong);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 5. CHỨC NĂNG KHÁC (Reset phòng)
        // ==========================================

        // Ai cũng có thể gọi hàm này (để Lễ tân fix lỗi trạng thái)
        // Nếu muốn chỉ Admin thì thêm check IsAdmin() vào
        public ActionResult ResetPhong(int id)
        {
            var phong = db.tblPhongs.Find(id);
            if (phong != null)
            {
                phong.TrangThai = "Trống";
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}