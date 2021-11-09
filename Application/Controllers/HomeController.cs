﻿using System.Diagnostics;
using Application.Models;
using Application.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Application.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationContext db;

    public HomeController(ILogger<HomeController> logger, ApplicationContext context)
    {
        _logger = logger;
        db = context;
    }

    public IActionResult Index()
    {
        var games = db.Games.ToList();
        return View(games);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}