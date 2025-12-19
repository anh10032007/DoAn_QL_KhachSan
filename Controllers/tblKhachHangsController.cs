using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QL_KhachSan;

namespace QL_KhachSan.Controllers
{
    public class tblKhachHangsController : Controller
    {
        private QL_KhachSanEntities db = new QL_KhachSanEntities();

        // GET: tblKhachHangs
        public ActionResult Index()
        {
            return View(db.tblKhachHangs.ToList());
        }

        // GET: tblKhachHangs/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tblKhachHang tblKhachHang = db.tblKhachHangs.Find(id);
            if (tblKhachHang == null)
            {
                return HttpNotFound();
            }
            return View(tblKhachHang);
        }

        // GET: tblKhachHangs/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: tblKhachHangs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaKH,TenKH,CCCD,GioiTinh,NamSinh,DienThoai,Email,DiaChi,MatKhau")] tblKhachHang tblKhachHang)
        {
            if (ModelState.IsValid)
            {
                db.tblKhachHangs.Add(tblKhachHang);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(tblKhachHang);
        }

        // GET: tblKhachHangs/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tblKhachHang tblKhachHang = db.tblKhachHangs.Find(id);
            if (tblKhachHang == null)
            {
                return HttpNotFound();
            }
            return View(tblKhachHang);
        }

        // POST: tblKhachHangs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaKH,TenKH,CCCD,GioiTinh,NamSinh,DienThoai,Email,DiaChi,MatKhau")] tblKhachHang tblKhachHang)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tblKhachHang).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tblKhachHang);
        }

        // GET: tblKhachHangs/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tblKhachHang tblKhachHang = db.tblKhachHangs.Find(id);
            if (tblKhachHang == null)
            {
                return HttpNotFound();
            }
            return View(tblKhachHang);
        }

        // POST: tblKhachHangs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            tblKhachHang tblKhachHang = db.tblKhachHangs.Find(id);
            db.tblKhachHangs.Remove(tblKhachHang);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
