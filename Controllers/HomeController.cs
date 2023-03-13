using LinqToExcel;
using Newtonsoft.Json.Linq;
using QuizApp.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using OfficeOpenXml;



namespace QuizApp.Controllers
{
    public class HomeController : Controller
    {
        QuizAppEntities db = new QuizAppEntities();

        [HttpGet]
        public ActionResult sregister()
        {
            return View();
        }
        [HttpPost]
        public ActionResult sregister(student svm)
        {
            student s = new student();

            try
            {
                s.std_name = svm.std_name;
                s.std_password = svm.std_password;
                //s.std_image = uploadImage(imgfile);
                s.std_hoten = svm.std_hoten;
                s.std_manv = svm.std_manv;
                s.std_bophan = svm.std_bophan;

                db.students.Add(s);
                db.SaveChanges();

                return RedirectToAction("slogin");
            }
            catch
            {
                ViewBag.msg = "Có lỗi xảy ra...";
            }


            return View();
        }

        public string uploadImage(HttpPostedFileBase file)
        {
            Random r = new Random();
            string path = "-1";
            int random = r.Next();
            if (file != null && file.ContentLength > 0)
            {
                string extension = Path.GetExtension(file.FileName);
                if (extension.ToLower().Equals(".jpg") || extension.ToLower().Equals(".jpeg") || extension.ToLower().Equals(".png"))
                {

                    try
                    {
                        path = Path.Combine(Server.MapPath("/Content/img/"), random + Path.GetFileName(file.FileName));
                        file.SaveAs(path);
                        path = "~/Content/upload/" + random + Path.GetFileName(file.FileName);
                    }
                    catch (Exception ex)
                    {
                        path = "-1";
                    }

                }
            }
            else
            {
                Response.Write("<script>alert('Hay chon anh');</script>");
            }
            return path;
        }

        public ActionResult LogOut()
        {
            Session.Abandon();
            Session.RemoveAll();

            return RedirectToAction("Index");
        }

        public ActionResult tlogin()
        {
            return View();
        }

        [HttpPost]
        public ActionResult tlogin(tbl_admin ad)
        {
            tbl_admin a = db.tbl_admin.Where(x => x.ad_name == ad.ad_name && x.ad_pass == ad.ad_pass).SingleOrDefault();
            if(a!= null)
            {
                int idla = a.ad_id;
                Session["ad_id"] = a.ad_id;
                return RedirectToAction("Dashboard");
                
            }
            else
            {
                ViewBag.msg = "Sai mat khau";
            }
            return View();
        }

        public ActionResult slogin()
        {
            return View();
        }

        [HttpPost]
        public ActionResult slogin(student s)
        {
            student sv = db.students.Where(x => x.std_name == s.std_name && x.std_password == s.std_password).SingleOrDefault();
            if (sv == null)
            {
                ViewBag.msg = "Sai ten dang nhap hoac mat khau";
            }
            else
            {
                Session["std_id"] = sv.std_id;
                TempData["std_hoten"] = sv.std_hoten;
                TempData["std_manv"] = sv.std_manv;
                TempData["std_bophan"] = sv.std_bophan;


                return RedirectToAction("ExamDashboard");
            }
            return View();
        }

        public ActionResult ExamDashboard()
        {
            if (Session["std_id"] == null)
            {
                return RedirectToAction("slogin");
            }
            return View();
        }

        [HttpPost]
        public ActionResult ExamDashboard(string room)
        {
        
            List<string> rep = new List<string>();
            TempData["traloi"] = rep;


            int soid = Convert.ToInt32(room);
            var dem = db.tbl_questions.Where(s => s.q_fk_catid == soid).Count();
            TempData["soluongcauhoi"] = dem;

            var tenchude = db.tbl_category.Where(x=>x.cat_id == soid).ToList();
            foreach(var item in tenchude)
            {
                string chude = item.cat_name;
                TempData["tenchude"] = chude;
            }

            List<tbl_category> list = db.tbl_category.ToList();
            foreach (var item in list)
            {
                if(item.cat_id == Convert.ToInt32(room))
                {
                    List<tbl_questions> li = db.tbl_questions.Where(x => x.q_fk_catid == item.cat_id).ToList();
                    TempData["danhsachcauhoi"] = li;

                    Queue<tbl_questions> queue = new Queue<tbl_questions>();
                    foreach(tbl_questions a in li)
                    {
                        queue.Enqueue(a);
                    }
                    TempData["examid"] = item.cat_fk_ad_id;

                    TempData["questions"] = queue;
                    TempData["score"] = 0;

                    TempData.Keep();
                    return RedirectToAction("StartQuiz");
                }
                else
                {
                    ViewBag.error = "Không tìm thấy phòng thi...";
                }
            }
            return View();
        }

        public ActionResult StartQuiz()
        {
            if (Session["std_id"] == null)
            {
                return RedirectToAction("slogin");
            }

            tbl_questions q = null;
            if(TempData["questions"] != null)
            {
                Queue<tbl_questions> qlist = (Queue<tbl_questions>)TempData["questions"];
                if(qlist.Count > 0)
                {
                    q = qlist.Peek();
                    qlist.Dequeue();
                    TempData["questions"] = qlist;
                    TempData.Keep();
                }
                else
                {
                    return RedirectToAction("EndExam");
                }
            }
            else
            {
                return RedirectToAction("ExamDashboard");
            }
            TempData.Keep();
            return View(q);

        }
        [HttpPost]
        public ActionResult StartQuiz(tbl_questions q,string option)
        {
            List<string> cautraloi = TempData["traloi"] as List<string>;

            if (Session["std_id"] == null)
            {
                return RedirectToAction("slogin");
            }
            string correctans = null;

            if (option == "A")
            {
                correctans = "A";
                cautraloi.Add("A");
            }
            else if (option == "B")
            {
                correctans = "B";
                cautraloi.Add("B");

            }
            else if (option == "C")
            {
                correctans = "C";
                cautraloi.Add("C");
            }
            else if (option == "D")
            {
                correctans = "D";
                cautraloi.Add("D");
            }
            else if (option == null)
            {
                correctans = "Khong";
                cautraloi.Add("Không chọn");
            }
            if (correctans == (q.QCorrectAns))
            {               
                TempData["score"] = Convert.ToInt32(TempData["score"].ToString()) + 1;
               // cautraloi.Add("True");
            }
            if (correctans != (q.QCorrectAns))
            {
                //cautraloi.Add("False");
            }
            TempData["traloi"] = cautraloi;
            TempData.Keep();
            return RedirectToAction("StartQuiz");
        }

        public ActionResult ViewAllQuestion(int?id)
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("tlogin");
            }
            if (id == null)
            {
                return RedirectToAction("Dashboard");
            }
            return View(db.tbl_questions.Where(x => x.q_fk_catid == id).ToList());
        }

        public ActionResult EndExam()
        {
            if (Session["std_id"] == null)
            {
                return RedirectToAction("slogin");
            }
            if (TempData["std_hoten"] == null)
            {
                return RedirectToAction("slogin");
            }
            double tong = Convert.ToDouble(TempData["soluongcauhoi"].ToString());
            double dung = Convert.ToDouble(TempData["score"].ToString());
            double sai = tong - dung;
            double tile = ((dung / tong)*100);
            TempData["sai"] = sai;
            TempData["tile"] = tile;
            TempData.Keep();

            return View();
        }
        public ActionResult Dashboard()
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("tlogin");
            }
            int ad_id = Convert.ToInt32(Session["ad_id"].ToString());
            List<tbl_category> catLi = db.tbl_category.Where(x => x.cat_fk_ad_id == ad_id).OrderByDescending(x => x.cat_id).ToList();
            ViewData["list"] = catLi;
            return View();
        }

        [HttpGet]
        public ActionResult Add_Category()
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("tlogin");
            }
            int ad_id = Convert.ToInt32(Session["ad_id"].ToString());
            List<tbl_category> catLi = db.tbl_category.Where(x=>x.cat_fk_ad_id == ad_id).OrderByDescending(x => x.cat_id).ToList();
            ViewData["list"] = catLi;
            return View();
        }

        [HttpPost]
        public ActionResult Add_Category(tbl_category cat)
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("tlogin");
            }
            List<tbl_category> catLi = db.tbl_category.OrderByDescending(x => x.cat_id).ToList();
            ViewData["list"] = catLi;
            tbl_category c = new tbl_category();

            
            c.cat_name = cat.cat_name;
            c.cat_fk_ad_id = Convert.ToInt32(Session["ad_id"].ToString());

            //Random r = new Random();
            //c.cat_encrytped_string = crypt.Encrypt(cat.cat_name.Trim() + r.Next().ToString(), true);

            db.tbl_category.Add(c);
            db.SaveChanges();
            TempData["trangthai"] = "Done";
            return RedirectToAction("Add_Category");

        }

        [HttpGet]
        public ActionResult Add_Questions()
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("tlogin");
            }
            //Session["ad_id"] = 2;
            int s_id = Convert.ToInt32(Session["ad_id"]);
            List<tbl_category> li = db.tbl_category.Where(x => x.cat_fk_ad_id == s_id).ToList();
            ViewBag.list = new SelectList(li,"cat_id","cat_name");

            return View();
        }

        [HttpPost]
        public ActionResult Add_Questions(tbl_questions q)
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("tlogin");
            }
            //Session["ad_id"] = 2;
            int s_id = Convert.ToInt32(Session["ad_id"]);
            List<tbl_category> li = db.tbl_category.Where(x => x.cat_fk_ad_id == s_id).ToList();
            ViewBag.list = new SelectList(li, "cat_id", "cat_name");

            tbl_questions qa = new tbl_questions();
            qa.q_text = q.q_text;
            qa.QA = q.QA;
            qa.QB = q.QB;
            qa.QC = q.QC;
            qa.QD = q.QD;
            qa.QCorrectAns = q.QCorrectAns;

            qa.q_fk_catid = Convert.ToInt32(q.q_fk_catid);
            db.tbl_questions.Add(qa);
            db.SaveChanges();
            TempData.Keep();
            TempData["trangthai"] = "Done";
            return RedirectToAction("Add_Questions");
            
        }

        public ActionResult Index()
        {
            if (Session["ad_id"] != null)
            {             
                return RedirectToAction("Dashboard");
            }
            return RedirectToAction("tlogin");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult Edit(int? id)
        {
            tbl_questions tbl_questions = db.tbl_questions.Find(id);
            return View(tbl_questions);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "q_id,q_text,QA,QB,QC,QD,QCorrectAns,q_fk_catid")] tbl_questions tbl_questions)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tbl_questions).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Add_Category");
            }
            return RedirectToAction("Add_Category");
        }
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tbl_questions tbl_questions = db.tbl_questions.Find(id);
            if (tbl_questions == null)
            {
                return HttpNotFound();
            }
            return View(tbl_questions);
        }

        // POST: tbl_questions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            tbl_questions tbl_questions = db.tbl_questions.Find(id);
            db.tbl_questions.Remove(tbl_questions);
            db.SaveChanges();
            return RedirectToAction("Add_Category");
        }
        public ActionResult ImportExcel()
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("tlogin");
            }
            int s_id = Convert.ToInt32(Session["ad_id"]);
            List<tbl_category> li = db.tbl_category.Where(x => x.cat_fk_ad_id == s_id).ToList();
            ViewBag.list = new SelectList(li, "cat_id", "cat_name");
            return View();
        }
        public ActionResult Upload(FormCollection formCollection, tbl_questions q)
        {
            int s_id = Convert.ToInt32(Session["ad_id"]);
            List<tbl_category> li = db.tbl_category.Where(x => x.cat_fk_ad_id == s_id).ToList();
            ViewBag.list = new SelectList(li, "cat_id", "cat_name");

            var danhsachcauhoi = new List<tbl_questions>();
            if (Request != null)
            {
                HttpPostedFileBase file = Request.Files["UploadedFile"];
                if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    string fileName = file.FileName;
                    string fileContentType = file.ContentType;
                    byte[] fileBytes = new byte[file.ContentLength];
                    var data = file.InputStream.Read(fileBytes, 0, Convert.ToInt32(file.ContentLength));
                    using (var package = new ExcelPackage(file.InputStream))
                    {
                        var currentSheet = package.Workbook.Worksheets;
                        var workSheet = currentSheet.First();
                        var noOfCol = workSheet.Dimension.End.Column;
                        var noOfRow = workSheet.Dimension.End.Row;
                        for (int rowIterator = 2; rowIterator <= noOfRow; rowIterator++)
                        {
                            var cauhoimoi = new tbl_questions();
                            cauhoimoi.q_text = workSheet.Cells[rowIterator, 1].Value.ToString();
                            cauhoimoi.QA = workSheet.Cells[rowIterator, 2].Value.ToString();
                            cauhoimoi.QB = workSheet.Cells[rowIterator, 3].Value.ToString();
                            cauhoimoi.QC = workSheet.Cells[rowIterator, 4].Value.ToString();
                            cauhoimoi.QD = workSheet.Cells[rowIterator, 5].Value.ToString();
                            cauhoimoi.QCorrectAns = workSheet.Cells[rowIterator, 6].Value.ToString();
                            cauhoimoi.q_fk_catid = Convert.ToInt32(q.q_fk_catid);
                            danhsachcauhoi.Add(cauhoimoi);
                        }
                    }
                }
            }
            using (QuizAppEntities excelImportDBEntities = new QuizAppEntities())
            {
                foreach (var item in danhsachcauhoi)
                {
                    excelImportDBEntities.tbl_questions.Add(item);
                }
                excelImportDBEntities.SaveChanges();
            }
            ViewBag.msg = "Đã nhập câu hỏi thành công";
            return View("ImportExcel");
        }

        public ActionResult Luuketqua()
        {
            tbl_setExam s = new tbl_setExam();
            s.exam_date = DateTime.Now.ToString("dd/MM/yyyy");
            s.exam_fk_stu = Convert.ToInt32(Session["std_id"].ToString());
            s.exam_name = TempData["tenchude"].ToString();
            s.std_score = Convert.ToInt32(TempData["tile"].ToString());

            db.tbl_setExam.Add(s);
            db.SaveChanges();
            return RedirectToAction("slogin");
        }


        public ActionResult ThongKe(string bophan, string tenchude, string date,string list)
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("tlogin");
            }
            ViewBag.tenchude = new SelectList(db.tbl_category, "cat_name", "cat_name"); // danh sách Category

            List<SelectListItem> danhsachbophan = new List<SelectListItem>() 
            {
                new SelectListItem {
                    Text = "---Chọn---", Value = ""
                },
                new SelectListItem {
                    Text = "MFE", Value = "MFE"
                },
                new SelectListItem {
                    Text = "PE20", Value = "PE20"
                },
                new SelectListItem {
                    Text = "Văn phòng", Value = "OFFICE"
                },
                new SelectListItem {
                    Text = "Sinh viên", Value = "SINHVIEN"
                }
            };
            ViewBag.bophan = danhsachbophan;
            var lichsu = from a in db.tbl_setExam select a;

            if (!String.IsNullOrEmpty(tenchude)) // kiểm tra chuỗi tìm kiếm có rỗng/null hay không
            {
                lichsu = lichsu.Where(s => s.exam_name.Contains(tenchude)).OrderByDescending(s => s.std_score);
                TempData["tenchude"] = tenchude;

                var demsonguoi = lichsu.Count();
                TempData["songuoi"] = demsonguoi;
                List<tbl_setExam> list_songuoi = lichsu.ToList();
                TempData["list_songuoi"] = list_songuoi;

                var dem_tot = lichsu.Where(s => s.std_score >=75).Count();
                TempData["dem_tot"] = dem_tot;
                List<tbl_setExam> list_tot = lichsu.Where(s => s.std_score >= 75).ToList();
                TempData["list_tot"] = list_tot;

                var dem_trungbinh = lichsu.Where(s => (s.std_score >= 35 && s.std_score < 75)).Count();
                TempData["dem_trungbinh"] = dem_trungbinh;
                List<tbl_setExam> list_tb = lichsu.Where(s => (s.std_score >= 35 && s.std_score < 75)).ToList();
                TempData["list_tb"] = list_tb;

                var dem_NG = lichsu.Where(s => s.std_score < 35).Count();
                TempData["dem_NG"] = dem_NG;
                List<tbl_setExam> list_ng = lichsu.Where(s => s.std_score < 35).ToList();
                TempData["list_ng"] = list_ng;
            }
        

            if (!String.IsNullOrEmpty(bophan)) // kiểm tra chuỗi tìm kiếm có rỗng/null hay không
            {
                lichsu = lichsu.Where(s => s.student.std_bophan.Contains(bophan)).OrderByDescending(s=>s.std_score);
                TempData["bophan"] = bophan;
                var demsonguoi = lichsu.Count();
                TempData["songuoi"] = demsonguoi;
                List<tbl_setExam> list_songuoi = lichsu.ToList();
                TempData["list_songuoi"] = list_songuoi;

                var dem_tot = lichsu.Where(s => s.std_score >= 75).Count();
                TempData["dem_tot"] = dem_tot;
                List<tbl_setExam> list_tot = lichsu.Where(s => s.std_score >= 75).ToList();
                TempData["list_tot"] = list_tot;

                var dem_trungbinh = lichsu.Where(s => (s.std_score >= 45 && s.std_score < 75)).Count();
                TempData["dem_trungbinh"] = dem_trungbinh;
                List<tbl_setExam> list_tb = lichsu.Where(s => (s.std_score >= 45 && s.std_score < 75)).ToList();
                TempData["list_tb"] = list_tb;

                var dem_NG = lichsu.Where(s => s.std_score < 45).Count();
                TempData["dem_NG"] = dem_NG;
                List<tbl_setExam> list_ng = lichsu.Where(s => s.std_score < 45).ToList();
                TempData["list_ng"] = list_ng;
            }
            
            if (!String.IsNullOrEmpty(date)) // kiểm tra chuỗi tìm kiếm có rỗng/null hay không
            {
                lichsu = lichsu.Where(s => s.exam_date.Contains(date)).OrderByDescending(s => s.std_score);
                TempData["date"] = date;
                var demsonguoi = lichsu.Count();
                TempData["songuoi"] = demsonguoi;
                List<tbl_setExam> list_songuoi = lichsu.ToList();
                TempData["list_songuoi"] = list_songuoi;

                var dem_tot = lichsu.Where(s => s.std_score >= 75).Count();
                TempData["dem_tot"] = dem_tot;
                List<tbl_setExam> list_tot = lichsu.Where(s => s.std_score >= 75).ToList();
                TempData["list_tot"] = list_tot;

                var dem_trungbinh = lichsu.Where(s => (s.std_score >= 45 && s.std_score < 75)).Count();
                TempData["dem_trungbinh"] = dem_trungbinh;
                List<tbl_setExam> list_tb = lichsu.Where(s => (s.std_score >= 45 && s.std_score < 75)).ToList();
                TempData["list_tb"] = list_tb;

                var dem_NG = lichsu.Where(s => s.std_score < 45).Count();
                TempData["dem_NG"] = dem_NG;
                List<tbl_setExam> list_ng = lichsu.Where(s => s.std_score < 45).ToList();
                TempData["list_ng"] = list_ng;
            }
        
            return View(lichsu); //trả về kết quả


        }

    }
}

 