﻿using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TP_PWEB.Models;

namespace TP_PWEB.Controllers
{
    [Authorize(Roles = "Client,Employee")]
    public class BookingsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Bookings
        
        public ActionResult Index()
        {
            var userName = User.Identity.Name;

            if (User.IsInRole("Client"))
            {
                var userBookings = db.Bookings.Where(b => b.User.UserName.Equals(userName));
                return View(userBookings.ToList());
            }else if (User.IsInRole("Employee"))
            {
                var company = db.Employees.Where(c => c.idUser.UserName == userName).Select(c => c.idCompany).First();
                var companieBookings = db.Bookings.Where(b => 
                b.vehicle.Company.IDCompany == company.IDCompany && 
                b.state == States.PENDING
                );
                return View(companieBookings.ToList());
            }
            
            return View(new List<Booking>());
        }

        [Authorize(Roles = "Employee")]
        public ActionResult IndexCheckedIn()
        {
            var userName = User.Identity.Name;
            var company = db.Employees.Where(c => c.idUser.UserName == userName).Select(c => c.idCompany).First();
            var companieBookings = db.Bookings.Where(b =>
            b.vehicle.Company.IDCompany == company.IDCompany &&
            b.state == States.APPROVED
            );
            return View(companieBookings.ToList());
        }


        public ActionResult Remove(int id)
        {
            if (ModelState.IsValid)
            {
                var booking = db.Bookings.Find(id);
                db.Bookings.Remove(booking);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult Approve(int id)
        {
            if (ModelState.IsValid)
            {
                var booking = db.Bookings.Find(id);
                booking.state = States.APPROVED;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult CheckedIn(int id)
        {
            if (ModelState.IsValid)
            {
                var booking = db.Bookings.Find(id);
                booking.state = States.CHECKED_IN;
                CheckedIn ck_in = new CheckedIn();
                ck_in.Booking = booking;
                ck_in.Damages = booking.vehicle.Damages;
                ck_in.InitKm = booking.vehicle.NumberKm;
                ck_in.FuelTankStatus = booking.vehicle.VehicleTank;
                db.CheckedIns.Add(ck_in);
                db.Entry(booking).State = EntityState.Modified;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }


        // GET: Bookings/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Booking booking = db.Bookings.Find(id);
            if (booking == null)
            {
                return HttpNotFound();
            }
            return View(booking);
        }

        private HashSet<Booking> getFilteredBookings(DateTime initDate, DateTime endDate)
        {
            var filteredBookingsInit = db.Bookings.Where(b => ((DateTime.Compare(initDate, b.bookingInit) >= 0) && (DateTime.Compare(initDate, b.bookingEnd) <= 0)));
            var filteredBookingsEnd = db.Bookings.Where(b => ((DateTime.Compare(endDate, b.bookingInit) >= 0) && (DateTime.Compare(endDate, b.bookingEnd) <= 0)));
            var filteredBookings = filteredBookingsInit.Concat(filteredBookingsEnd).ToList();
            var uniqueBookings = new HashSet<Booking>(filteredBookings);
            return uniqueBookings;
        }
        private bool verifyBooking(Booking booking)
        {
            var all = getFilteredBookings(booking.bookingInit, booking.bookingEnd);
            return all.Count() == 0;
        }

        // GET: Bookings/Create
        public ActionResult Create(int? id)
        {
            BookingsAndList bal = new BookingsAndList();
            bal.listBookings = db.Bookings.Where(b => b.vehicle.IDVehicle == id).ToList();
            return View(bal);
        }

        // POST: Bookings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Booking booking, int? id)
        {
            ViewBag.validBooking = false;
            if (ModelState.IsValid)
            {
                if (verifyBooking(booking))
                {
                    ViewBag.validBooking = true;
                    var userID = User.Identity.GetUserId();
                    ApplicationUser currentUser = db.Users.Where(u => u.Id == userID).First();
                    booking.User = currentUser;
                    booking.vehicle = db.Vehicles.Find(id);
                    booking.state = States.PENDING;
                    db.Bookings.Add(booking);
                    db.SaveChanges();
                    return RedirectToAction("Index", "Vehicles");
                }
                ModelState.Clear();
                BookingsAndList bal = new BookingsAndList();
                bal.listBookings = db.Bookings.Where(b => b.vehicle.IDVehicle == id).ToList();
                return View(bal);
            }
            return View(booking);
        }

        // GET: Bookings/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Booking booking = db.Bookings.Find(id);
            if (booking == null)
            {
                return HttpNotFound();
            }
            return View(booking);
        }

        // POST: Bookings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "idBooking,bookingInit,bookingEnd")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                db.Entry(booking).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", "Bookings", new { userName = User.Identity.Name });
            }
            return View(booking);
        }

        // GET: Bookings/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Booking booking = db.Bookings.Find(id);
            if (booking == null)
            {
                return HttpNotFound();
            }
            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Booking booking = db.Bookings.Find(id);
            db.Bookings.Remove(booking);
            db.SaveChanges();
            return RedirectToAction("Index", "Bookings", new { userName = User.Identity.Name });
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