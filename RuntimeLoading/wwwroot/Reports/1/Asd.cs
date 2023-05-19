﻿using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
namespace RuntimeLoading.wwwroot.Reports._1;

public class ReportController : Controller
{
    // GET: HomeController
    public ActionResult Index()
    {
        return View();
    }

    // GET: HomeController/Details/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Details(IFormCollection collection, int id)
    {
        _ = collection.TryGetValue("FirstName", out var firstName);

        return Ok($"OI WAAR MY PENOR PIC hoes toes v2{RuntimeLoading.Globals.DefaultIdentifier}");
    }

    public ActionResult Penor() {
        return Ok("OI WAAR MY PENOR PIC");
    }

    // POST: HomeController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(IFormCollection collection)
    {
        try
        {
            dynamic o = new ExpandoObject();
            o.Id = 1;
            o.DefaultIdentifier = RuntimeLoading.Globals.DefaultIdentifier;

            _ = collection.TryGetValue("FirstName", out var firstName);
            o.FirstName = firstName;

            return View(Url.Content("~/wwwroot/Reports/1/Index.cshtml"), o);
        }
        catch
        {
            return View();
        }
    }

    // GET: HomeController/Edit/5
    public ActionResult Edit(int id)
    {
        return View();
    }

    // POST: HomeController/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(int id, IFormCollection collection)
    {
        try
        {
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            return View();
        }
    }

    // GET: HomeController/Delete/5
    public ActionResult Delete(int id)
    {
        return View();
    }

    // POST: HomeController/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Delete(int id, IFormCollection collection)
    {
        try
        {
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            return View();
        }
    }
}