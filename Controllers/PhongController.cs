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
        // GET: Phong
        QL_KhachSanEntities db = new QL_KhachSanEntities();

        // 1. Danh sách phòng (Trang chủ quản lý phòng)
        public ActionResult Index()
        {
            // Lấy danh sách phòng kèm thông tin loại phòng
            var listPhong = db.tblPhongs.Include("tblLoaiPhong").ToList();
            return View(listPhong);
        }

        // 2. Giao diện Thêm phòng
        public ActionResult Create()
        {
            // Tạo Dropdown chọn Loại phòng
            ViewBag.MaLoai = new SelectList(db.tblLoaiPhongs, "MaLoai", "TenLoai");
            return View();
        }

        // 3. Xử lý Thêm phòng (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(tblPhong p, HttpPostedFileBase HinhAnh)
        {
            if (ModelState.IsValid)
            {
                // Xử lý Upload ảnh
                if (HinhAnh != null && HinhAnh.ContentLength > 0)
                {
                    string filename = Path.GetFileName(HinhAnh.FileName);
                    string path = Server.MapPath("~/Content/Images/");

                    // Tạo thư mục nếu chưa có
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    // Lưu file
                    HinhAnh.SaveAs(path + filename);
                    p.AnhDaiDien = filename; // Lưu tên file vào DB
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
        // CHỨC NĂNG SỬA (EDIT)
        // ==========================================

        // 4. Hiện giao diện sửa phòng (GET)
        public ActionResult Edit(int id)
        {
            // Tìm phòng theo ID
            var phong = db.tblPhongs.Find(id);
            if (phong == null) return HttpNotFound();

            // Load dropdown loại phòng, chọn sẵn loại hiện tại của phòng đó
            ViewBag.MaLoai = new SelectList(db.tblLoaiPhongs, "MaLoai", "TenLoai", phong.MaLoai);

            return View(phong);
        }

        // 5. Xử lý cập nhật phòng (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(tblPhong p, HttpPostedFileBase HinhAnh)
        {
            if (ModelState.IsValid)
            {
                // Lấy thông tin phòng cũ trong DB ra để sửa
                var phongInDb = db.tblPhongs.Find(p.MaPhong);

                if (phongInDb != null)
                {
                    // Cập nhật các thông tin văn bản
                    phongInDb.SoPhong = p.SoPhong;
                    phongInDb.Tang = p.Tang;
                    phongInDb.MaLoai = p.MaLoai;
                    phongInDb.MoTaChiTiet = p.MoTaChiTiet;
                    // phongInDb.TrangThai = p.TrangThai; // Có thể cho sửa hoặc không tùy nghiệp vụ

                    // Xử lý ảnh: Chỉ cập nhật nếu người dùng chọn ảnh mới
                    if (HinhAnh != null && HinhAnh.ContentLength > 0)
                    {
                        string filename = Path.GetFileName(HinhAnh.FileName);
                        string path = Server.MapPath("~/Content/Images/");

                        // Xóa ảnh cũ nếu cần (tùy chọn)
                        // if (System.IO.File.Exists(path + phongInDb.AnhDaiDien)) System.IO.File.Delete(path + phongInDb.AnhDaiDien);

                        HinhAnh.SaveAs(path + filename);
                        phongInDb.AnhDaiDien = filename; // Cập nhật tên ảnh mới
                    }

                    db.SaveChanges(); // Lưu thay đổi vào DB
                    return RedirectToAction("Index");
                }
            }

            // Nếu lỗi, load lại dropdown
            ViewBag.MaLoai = new SelectList(db.tblLoaiPhongs, "MaLoai", "TenLoai", p.MaLoai);
            return View(p);
        }

        // ==========================================
        // CHỨC NĂNG XÓA (DELETE)
        // ==========================================

        // 6. Hiện xác nhận xóa (GET)
        public ActionResult Delete(int id)
        {
            var phong = db.tblPhongs.Find(id);
            if (phong == null) return HttpNotFound();
            return View(phong);
        }

        // 7. Xử lý xóa (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var phong = db.tblPhongs.Find(id);
            if (phong != null)
            {
                // Kiểm tra xem phòng có đang nằm trong Hóa đơn nào không?
                // Nếu có thì không được xóa (Ràng buộc khóa ngoại)
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
    }
}