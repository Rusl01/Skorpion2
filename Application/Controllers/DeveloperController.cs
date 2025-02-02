﻿using System.Linq;
using System.Threading.Tasks;
using Application.Data;
using Application.Models;
using Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers;

/// <summary>
/// Контроллер для управления добавлением и редактирование игр разработчиком
/// </summary>
[Authorize]
public class DeveloperController : Controller
{
    private readonly ApplicationContext _db;
    private readonly UserManager<User> _userManager;
    private const int PageSize = 6;

    public DeveloperController(UserManager<User> userManager, ApplicationContext context)
    {
        _userManager = userManager;
        _db = context;
    }
    
    /// <summary>
    /// Страница с таблицей игр разработчика
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page=1)
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        var developerGames = _db.Games.Where(game => game.Developer == currentUser).ToList();
        
        var count = developerGames.Count();
        var items = developerGames.Skip((page - 1) * PageSize).Take(PageSize).ToList();
        var pageViewModel = new PageViewModel(count, page, PageSize);
        
        var model = new DeveloperViewModel
        {
            Games = items,
            PageViewModel = pageViewModel
        };
        return View(model);
    }
    
    /// <summary>
    /// Страница добавления игры в базу данных
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var genres = _db.Genres.ToList();
        var platforms = _db.Platforms.ToList();
        var players = _db.Players.ToList();
        var model = new GameViewModel
        {
            Genres = genres, 
            Platforms = platforms, 
            Players = players
        };
        return View(model);
    }
    
    /// <summary>
    /// Страница обновления информации об игре в базе данных
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        var game = await _db.Games
            .Include(g => g.Genres)
            .Include(g => g.Platforms)
            .Include(g => g.Players)
            .FirstAsync(game => game.Id == id);
        var genres = _db.Genres.ToList();
        var platforms = _db.Platforms.ToList();
        var players = _db.Players.ToList();

        for (var i = 0; i < genres.Count; i++)
        {
            genres[i].Selected = game.Genres.Any(gg => gg.GenreId == genres[i].Id);
        }
        
        for (var i = 0; i < platforms.Count; i++)
        {
            platforms[i].Selected = game.Platforms.Any(gp => gp.PlatformId == platforms[i].Id);
        }
        
        for (var i = 0; i < players.Count; i++)
        {
            players[i].Selected = game.Players.Any(gp => gp.PlayerId == players[i].Id);
        }
        
        var model = new GameViewModel
        {
            Id = game.Id,
            Cover = game.Cover,
            Title = game.Title,
            Description = game.Description,
            ReleaseDate = game.ReleaseDate.ToUniversalTime(),
            Price = game.Price,
            Genres = genres,
            Platforms = platforms,
            Players = players,
            DeveloperSite = game.DeveloperSite[..8].Equals("https://") ? game.DeveloperSite : "https://" + game.DeveloperSite
        };
        return View(model);
    }

    /// <summary>
    /// Добавление игры в базу данных
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(GameViewModel model)
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        var selectedGenres = model.Genres.Where(v => v.Selected).ToList();
        var selectedPlatforms = model.Platforms.Where(v => v.Selected).ToList();
        var selectedPlayers = model.Players.Where(v => v.Selected).ToList();
        
        var game = new Game()
        {
            Cover = model.Cover,
            Title = model.Title,
            Description = model.Description,
            ReleaseDate = model.ReleaseDate.ToUniversalTime(),
            Price = model.Price,
            Developer = currentUser,
            Genres = selectedGenres.Select(genre => new GameGenre {GenreId = genre.Id}).ToList(),
            Platforms = selectedPlatforms.Select(platform => new GamePlatform {PlatformId = platform.Id}).ToList(),
            Players = selectedPlayers.Select(player => new GamePlayer {PlayerId = player.Id}).ToList(),
            DeveloperSite = model.DeveloperSite[..8].Equals("https://") ? model.DeveloperSite : "https://" + model.DeveloperSite
        };

        await _db.Games.AddAsync(game);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
    
    /// <summary>
    /// Обновление информации об игре в базе данных
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Update(GameViewModel model)
    {
        var selectedGenres = model.Genres.Where(v => v.Selected).ToList();
        var selectedPlatforms = model.Platforms.Where(v => v.Selected).ToList();
        var selectedPlayers = model.Players.Where(v => v.Selected).ToList();
        
        var game = await _db.Games
            .Include(g => g.Genres)
            .Include(g => g.Platforms)
            .Include(g => g.Players)
            .FirstAsync(game => game.Id == model.Id);

        _db.GameGenres.RemoveRange(game.Genres);
        _db.GamePlatforms.RemoveRange(game.Platforms);
        _db.GamePlayers.RemoveRange(game.Players);
        
        game.Cover = model.Cover;
        game.Title = model.Title;
        game.Description = model.Description;
        game.ReleaseDate = model.ReleaseDate.ToUniversalTime();
        game.Price = model.Price;
        game.Genres = selectedGenres.Select(genre => new GameGenre {GenreId = genre.Id}).ToList();
        game.Platforms = selectedPlatforms.Select(platform => new GamePlatform {PlatformId = platform.Id}).ToList();
        game.Players = selectedPlayers.Select(player => new GamePlayer {PlayerId = player.Id}).ToList();
        game.DeveloperSite = model.DeveloperSite[..8].Equals("https://") ? model.DeveloperSite : "https://" + model.DeveloperSite;
        
        _db.Games.Update(game);
        await _db.SaveChangesAsync();
        
        return RedirectToAction("Index");
    }
    
    /// <summary>
    /// Удаление игры из базы данных
    /// </summary>
    public async Task<IActionResult> Delete(int id)
    {
        var game = await _db.Games.FirstAsync(game => game.Id == id);
        _db.Games.Remove(game);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}