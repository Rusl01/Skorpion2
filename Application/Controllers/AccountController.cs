﻿using System.Threading.Tasks;
using Application.Data;
using Application.Models;
using Application.Services;
using Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controllers;

/// <summary>
/// Контроллер для управления аутентификацией, авторизацией и регистрацией
/// </summary>
[Authorize]
public class AccountController : Controller
{
    private readonly ApplicationContext _db;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly IUserService _userService;
    private const int PageSize = 10;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager,
        ApplicationContext context, IUserService userService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = context;
        _userService = userService;
    }

    /// <summary>
    /// Страница профиля
    /// </summary>
    public async Task<IActionResult> Index(string id)
    {
        var userName = id;
        var currentUser = await _userService.getCurrentUserAsync();

        var user = await _userService.getUserByUserNameAsync(userName);
        var games = _userService.getUserGames(user);
        var friends = _userService.getUserFriends(user);
        var addFriend = !_userService.IsFriend(currentUser, user);

        var model = new ProfileViewModel
        {
            User = user,
            Games = games,
            AddFriend = addFriend,
            Friends = friends
        };

        return View(model);
    }

    /// <summary>
    /// Страница списка всех пользователей
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "admin")]
    public IActionResult Admin()
    {
        var users = _userManager.Users;
        return View(users);
    }

    /// <summary>
    /// Страница поиска пользователя
    /// </summary>
    [HttpGet]
    public IActionResult FindUser(FindUserViewModel model, int page = 1)
    {
        List<User> users;
        model.Nickname ??= new string("");
        users = !string.IsNullOrEmpty(model.Nickname)
            ? _db.Users.Where(x => x.Nickname == model.Nickname).ToList()
            : _db.Users.ToList();

        var partUsers = users.Skip((page - 1) * PageSize).Take(PageSize).ToList();
        var pageViewModel = new PageViewModel(users.Count(), page, PageSize);

        var newModel = new FindUserViewModel
        {
            Users = partUsers,
            Nickname = model.Nickname,
            PageViewModel = pageViewModel
        };

        return View(newModel);
    }

    /// <summary>
    /// Страница регистрации
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    /// <summary>
    /// Регистрация пользователя в базе данных
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new User
        {
            UserPhotoUrl = model.UserPhotoUrl,
            Nickname = model.Nickname,
            UserName = model.Email,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, false);

            return RedirectToAction("index", "Home");
        }

        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);

        ModelState.AddModelError(string.Empty, "Недостаточно надежный логин или пароль");

        return View(model);
    }

    /// <summary>
    /// Страница редактирования пользователя
    /// </summary>
    [HttpGet]
    public IActionResult Update()
    {
        var currentUser = _userManager.GetUserAsync(HttpContext.User).Result;
        var model = new UpdateViewModel
        {
            UserPhotoUrl = currentUser.UserPhotoUrl,
            Nickname = currentUser.Nickname,
            Email = currentUser.Email
        };
        return View(model);
    }

    /// <summary>
    /// Страница авторизации
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    /// <summary>
    /// Авторизация пользователя в системе
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel user)
    {
        if (!ModelState.IsValid) return View(user);
        var result = await _signInManager.PasswordSignInAsync(user.Email, user.Password, user.RememberMe, false);
        if (result.Succeeded) return RedirectToAction("Index", "Home");
        ModelState.AddModelError(string.Empty, "Неправильный логин или пароль");
        return View(user);
    }

    /// <summary>
    /// Страница библиотеки игр пользователя
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Library(int page = 1)
    {
        var keyGames = await _userService.getCurrentUserKeysAsync();
        var partKeyGames = keyGames.Skip((page - 1) * PageSize).Take(PageSize).ToDictionary(x => x.Key, x => x.Value);
        var pageViewModel = new PageViewModel(keyGames.Count, page, PageSize);

        var model = new LibraryViewModel
        {
            KeyGames = partKeyGames,
            PageViewModel = pageViewModel
        };

        return View(model);
    }

    /// <summary>
    /// Редактирование пользователя
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Update(UpdateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = _userManager.GetUserAsync(HttpContext.User).Result;
        user.UserPhotoUrl = model.UserPhotoUrl;
        user.Nickname = model.Nickname;
        user.UserName = model.Email;
        user.Email = model.Email;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);

        ModelState.AddModelError(string.Empty, "Неправильный логин или пароль");

        return View(model);
    }

    /// <summary>
    /// Разлогинирование пользователя
    /// </summary>
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    /// <summary>
    /// Добавить пользователя с id к текущему пользователю в друзья
    /// </summary>
    public IActionResult AddFriend(string id)
    {
        var user = _db.Users.First(x => x.Id == id);
        _userService.AddUserToFriendList(user);
        return RedirectToAction("Index", new {id = user.UserName});
    }

    /// <summary>
    /// Удаление пользователя с id из списка друзей текущего пользователя
    /// </summary>
    public IActionResult RemoveFriend(string id)
    {
        var user = _db.Users.First(x => x.Id == id);
        _userService.RemoveUserFromFriendList(user);
        return RedirectToAction("Index", new {id = user.UserName});
    }

    /// <summary>
    /// Удаление пользователя из базы данных
    /// </summary>
    public async Task<IActionResult> Remove(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        _userService.DeleteUser(user);
        return RedirectToAction("Admin");
    }
}