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
            // BƯỚC 1: Lấy danh sách phòng từ DB về (chưa sắp xếp)
            var listPhongRaw = db.tblPhongs.Include("tblLoaiPhong").ToList();

            // BƯỚC 2: Sắp xếp danh sách trong bộ nhớ (In-Memory Sorting)
            // Logic: 
            // - Ưu tiên độ dài chuỗi (VD: "2" dài 1 ký tự sẽ đứng trước "10" dài 2 ký tự)
            // - Sau đó sắp xếp theo ký tự (VD: "101" đứng trước "102")
            var listPhong = listPhongRaw.OrderBy(p => p.SoPhong.Length)
                                        .ThenBy(p => p.SoPhong)
                                        .ToList();

            // --- LẤY ID HÓA ĐƠN ĐANG HOẠT ĐỘNG (Code cũ của bạn giữ nguyên) ---
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
       
        public ActionResult Create(tblPhong phong, HttpPostedFileBase HinhAnh)
        {
            // Kiểm tra quyền (như cũ)
            if (Session["User"] == null || Convert.ToInt32(Session["VaiTro"]) != 1)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                // --- BẮT ĐẦU ĐOẠN KIỂM TRA TRÙNG ---
                // 1. Xóa khoảng trắng thừa (ví dụ " 101 " thành "101")
                phong.SoPhong = phong.SoPhong.Trim();

                // 2. Kiểm tra trong Database xem số phòng này đã tồn tại chưa
                bool isDuplicate = db.tblPhongs.Any(x => x.SoPhong == phong.SoPhong);

                if (isDuplicate)
                {
                    // Nếu trùng, thêm lỗi vào ModelState
                    // "SoPhong" là tên trường (name) bên View để hiển thị lỗi ngay dưới ô nhập
                    ModelState.AddModelError("SoPhong", "Lỗi: Số phòng " + phong.SoPhong + " đã tồn tại trong hệ thống!");

                    // Trả lại View để người dùng nhập lại (không lưu vào DB)
                    ViewBag.MaLoai = new SelectList(db.tblLoaiPhongs, "MaLoai", "TenLoai", phong.MaLoai);
                    return View(phong);
                }
                // --- KẾT THÚC ĐOẠN KIỂM TRA TRÙNG ---

                // Nếu không trùng thì xử lý ảnh và lưu bình thường
                if (HinhAnh != null && HinhAnh.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(HinhAnh.FileName);
                    string path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
                    HinhAnh.SaveAs(path);
                    phong.AnhDaiDien = fileName;
                }

                phong.TrangThai = "Trống"; // Mặc định trạng thái
                db.tblPhongs.Add(phong);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.MaLoai = new SelectList(db.tblLoaiPhongs, "MaLoai", "TenLoai", phong.MaLoai);
            return View(phong);
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